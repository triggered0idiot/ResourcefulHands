using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace ResourcefulHands;

public static class ResourcePacksManager
{
    public static bool IsUsingRHPacksFolder => LoadedPacks.Exists(p => p.IsConfigFolderPack);
    
    public static List<TexturePack> LoadedPacks { get; internal set; } = [];
    public static TexturePack[] ActivePacks => (LoadedPacks ?? []).Where(pack => pack is { IsActive: true }).ToArray();
    public static bool HasPacksChanged = true;

    public static Texture2D? GetTextureFromPacks(string textureName)
    {
        Texture2D? texture = null;
        foreach (var pack in ActivePacks)
        {
            var myTexture = pack.GetTexture(textureName);
            if(myTexture != null)
                texture = myTexture;
        }

        if (texture) return texture;

        if (textureName is "DeathFloor_02" or "_CORRUPTTEXTURE")
            return Plugin.CorruptionTexture;
        
        return null;
    }
    public static AudioClip? GetSoundFromPacks(string soundName)
    {
        AudioClip? clip = null;
        foreach (var pack in ActivePacks)
        {
            var myClip = pack.GetSound(soundName);
            if(myClip != null)
                clip = myClip;
        }

        return clip;
    }
    
    internal static void MovePack(TexturePack pack, bool isUp)
    {
        int packIndex = ResourcePacksManager.LoadedPacks.FindIndex(p => p == pack);

        int nextPackIndex = 0;
        nextPackIndex = isUp
            ? Math.Clamp(packIndex - 1, 0, ResourcePacksManager.LoadedPacks.Count - 1)
            : Math.Clamp(packIndex + 1, 0, ResourcePacksManager.LoadedPacks.Count - 1);

        if (nextPackIndex == packIndex)
        {
            RHLog.Warning("Can't move pack out of range");
            return;
        }

        TexturePack previousPack = ResourcePacksManager.LoadedPacks[nextPackIndex];
        ResourcePacksManager.LoadedPacks[nextPackIndex] = pack;
        ResourcePacksManager.LoadedPacks[packIndex] = previousPack;
        
        ResourcePacksManager.Save();
    }
    
    internal static void SaveDisabledPacks()
    {
        List<string> disabledPacks = [];
        foreach (var pack in LoadedPacks)
        {
            if (!pack.IsActive)
                disabledPacks.Add(pack.guid);
        }
        RHConfig.PackPrefs.DisabledPacks = disabledPacks.ToArray();
        RHConfig.PackPrefs.Save();
    }
    internal static void LoadDisabledPacks()
    {
        RHConfig.PackPrefs.Load();
        string[] disabledPacks = RHConfig.PackPrefs.DisabledPacks;
        if(disabledPacks.Length == 0) return;
        
        foreach (var disabledPackGuid in disabledPacks)
        {
            RHLog.Debug($"{disabledPackGuid} is a disabled pack.");
            foreach (var pack in LoadedPacks.Where(pack => pack.guid == disabledPackGuid))
                pack.IsActive = false;
        }
    }
    
    internal static void SavePackOrder()
    {
        string[] currentPacksState = new string[LoadedPacks.Count];
        for (int i = 0; i < LoadedPacks.Count; i++)
        {
            var pack = LoadedPacks[i];
            currentPacksState[i] = pack.guid;
        }
        
        RHConfig.PackPrefs.PackOrder = currentPacksState;
        RHConfig.PackPrefs.Save();
    }
    internal static void LoadPackOrder()
    {
        RHConfig.PackPrefs.Load();
        string[] previousPacksState = RHConfig.PackPrefs.PackOrder;
            
        TexturePack[] previousPacks = LoadedPacks.ToArray();
        TexturePack?[] newPacks = new TexturePack[previousPacksState.Length];
        for (int i = 0; i < newPacks.Length; i++)
            newPacks[i] = null;
            
        for (int i = 0; i < previousPacksState.Length; i++)
        {
            string guid = previousPacksState[i];
            TexturePack? pack = previousPacks.FirstOrDefault(p => p.guid == guid);
            if (pack != null)
                newPacks[i] = pack;
        }

        List<TexturePack> finalPacks = [];
        foreach (var p in newPacks)
            if(p != null) finalPacks.Add(p);
        // incase we missed any or replaced them just add them to the end
        foreach(var p in previousPacks)
            if(!finalPacks.Contains(p)) finalPacks.Add(p);
            
        LoadedPacks = finalPacks;
    }

    public static void Save()
    {
        SavePackOrder();
        SaveDisabledPacks();
    }

    private static bool isReloading = false;
    
    // Reloads every pack
    public static void ReloadPacks()
    {
        if (isReloading)
        {
            RHLog.Warning("Tried to reload while already reloading?");
            return;
        }

        RHLog.Debug("Dispatching pack reloader task...");
        Task.Run(async () =>
        {
            await ReloadPacks_Internal();
        });
    }
    
    internal static async Task ReloadPacks_Internal()
    {
        if (isReloading) return;
        isReloading = true;
        
        HasPacksChanged = false;
        if (LoadedPacks.Count != 0)
        {
            RHLog.Debug("Saving packs before reloading!");
            Save();
        }
        LoadedPacks.Clear();
        RHLog.Info($"Expanding zips in {RHConfig.PacksFolder}...");
        string[] zipPaths = Directory.GetFiles(RHConfig.PacksFolder, "*.zip", SearchOption.TopDirectoryOnly);
        foreach (string zipPath in zipPaths)
        {
            try
            {
                RHLog.Info($"Expanding texture pack zip: {zipPath}");
                bool isTopLevelZip = true;
                using (ZipArchive zip = ZipFile.OpenRead(zipPath))
                    isTopLevelZip = zip.GetEntry("info.json") != null;

                if (isTopLevelZip)
                {
                    var zipTargDir = Directory.CreateDirectory(Path.Combine(RHConfig.PacksFolder, Path.GetFileNameWithoutExtension(zipPath)));
                    ZipFile.ExtractToDirectory(zipPath, zipTargDir.FullName);
                }
                else
                    ZipFile.ExtractToDirectory(zipPath, RHConfig.PacksFolder);
                
                File.Delete(zipPath);
                RHLog.Info($"Expanded!");
            }
            catch (Exception e)
            {
                RHLog.Info($"Failed to expand!");
                RHLog.Error(e);
            }
        }
        RHLog.Info("Loading texture packs...");
        List<string> paths = new();
        paths.AddRange(Directory.GetDirectories(RHConfig.PacksFolder, "*", SearchOption.TopDirectoryOnly));
        paths.AddRange(Directory.GetDirectories(Paths.PluginPath, "*", SearchOption.AllDirectories)); // check sub dirs for plugins

        int failedPacks = 0;
        foreach (string path in paths)
        {
            // not a pack without info.json
            if(!File.Exists(Path.Combine(path, "info.json")))
                continue;
            
            try
            {
                RHLog.Info($"Loading texture pack: {path}");
                TexturePack? pack = await TexturePack.Load(path);
                if (pack == null)
                {
                    RHLog.Warning($"Failed to load pack at {path}!");
                    failedPacks++;
                    continue;
                }

                LoadedPacks.Add(pack);
                RHLog.Info($"Loaded!");
            }
            catch (Exception e)
            {
                failedPacks++;
                RHLog.Info($"Failed to load!");
                RHLog.Error(e);
            }
        }

        RHLog.Info($"Loaded {LoadedPacks.Count}/{LoadedPacks.Count + failedPacks} texture packs");
        if (failedPacks > 0)
            RHLog.Warning($"{failedPacks} packs failed to load!");

        await CoroutineDispatcher.RunOnMainThreadAndWait(() =>
        {
            RHLog.Info("Re-ordering to user order...");
            LoadPackOrder();
            RHLog.Info("Disabling packs that should be disabled...");
            LoadDisabledPacks();
            
            // attempt to refresh previously loaded stuff
            Plugin.RefreshTextures();
            Plugin.RefreshSounds();

            // just incase
            if (UI_RHPacksList.Instance != null)
                UI_RHPacksList.Instance?.BuildList();
        });

        isReloading = false;
    }
}