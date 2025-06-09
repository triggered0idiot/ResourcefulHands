using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace ResourcefulHands;

public static class ResourcePacksManager
{
    public static List<TexturePack> LoadedPacks { get; internal set; } = [];
    public static TexturePack[] ActivePacks => LoadedPacks.Where(pack => pack.IsActive).ToArray();
    public static bool HasPacksChanged = true;
    
    public const string PackPrefsName = "__ResourcefulHands_Mod_PackOrder";
    public const string DisabledPacksPrefsName = "__ResourcefulHands_Mod_DisabledPacks";

    public static Texture2D? GetTextureFromPacks(string textureName)
    {
        Texture2D? texture = null;
        foreach (var pack in ActivePacks)
        {
            var myTexture = pack.GetTexture(textureName);
            if(myTexture != null)
                texture = myTexture;
        }
        
        return texture;
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

    internal static void SaveDisabledPacks()
    {
        List<string> disabledPacks = [];
        foreach (var pack in LoadedPacks)
        {
            if (!pack.IsActive)
                disabledPacks.Add(pack.guid);
        }
        string listJson = JsonConvert.SerializeObject(disabledPacks.ToArray());
        PlayerPrefs.SetString(DisabledPacksPrefsName, listJson);
    }
    internal static void LoadDisabledPacks()
    {
        string listJson = PlayerPrefs.GetString(DisabledPacksPrefsName, "");
        if(string.IsNullOrWhiteSpace(listJson)) return;
        string[]? disabledPacks = JsonConvert.DeserializeObject<string[]>(listJson);
        if(disabledPacks == null || disabledPacks.Length == 0) return;
        
        foreach (var disabledPackGuid in disabledPacks)
        {
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
        string listJson = JsonConvert.SerializeObject(currentPacksState);
        PlayerPrefs.SetString(PackPrefsName, listJson);
    }
    internal static void LoadPackOrder()
    {
        string listJson = PlayerPrefs.GetString(PackPrefsName, "");
        if (!string.IsNullOrWhiteSpace(listJson))
        {
            string[]? previousPacksState = JsonConvert.DeserializeObject<string[]>(listJson);
            if(previousPacksState == null) return;
            
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
            // save the new order
            SavePackOrder();
        }
    }

    // exposed api ig
    public static void ReloadPacks()
    {
        ResourcePacksManager.ReloadPacks_Internal(Debug.Log);
    }
    
    internal static void ReloadPacks_Internal(Action<string> log)
    {
        ResourcePacksManager.HasPacksChanged = false;
        if (LoadedPacks.Count != 0)
        {
            SavePackOrder();
            SaveDisabledPacks();
        }
        LoadedPacks.Clear();
        log("Expanding zips...");
        string[] zipPaths = Directory.GetFiles(Plugin.ConfigFolder, "*.zip", SearchOption.TopDirectoryOnly);
        foreach (string zipPath in zipPaths)
        {
            try
            {
                log($"Expanding texture pack zip: {zipPath}");
                bool isTopLevelZip = true;
                using (ZipArchive zip = ZipFile.OpenRead(zipPath))
                    isTopLevelZip = zip.GetEntry("info.json") != null;

                if (isTopLevelZip)
                {
                    var zipTargDir = Directory.CreateDirectory(Path.Combine(Plugin.ConfigFolder, Path.GetFileNameWithoutExtension(zipPath)));
                    ZipFile.ExtractToDirectory(zipPath, zipTargDir.FullName);
                }
                else
                    ZipFile.ExtractToDirectory(zipPath, Plugin.ConfigFolder);
                
                File.Delete(zipPath);
                log($"Expanded!");
            }
            catch (Exception e)
            {
                log($"Failed to expand!");
                Debug.LogError(e);
            }
        }
        log("Loading texture packs...");
        List<string> paths = new();
        paths.AddRange(Directory.GetDirectories(Plugin.ConfigFolder, "*", SearchOption.TopDirectoryOnly));
        // incase some people wanna upload packs to thunderstore... for some reason
        paths.AddRange(Directory.GetDirectories(Paths.PluginPath, "*", SearchOption.TopDirectoryOnly));
        foreach (string path in paths)
        {
            // not a pack without info.json
            if(!File.Exists(Path.Combine(path, "info.json")))
                continue;
            
            try
            {
                log($"Loading texture pack: {path}");
                TexturePack? pack = TexturePack.Load(path);
                if (pack == null)
                    Debug.LogWarning($"Failed to load pack at {path}!");

                LoadedPacks.Add(pack);
                log($"Loaded!");
            }
            catch (Exception e)
            {
                log($"Failed to load!");
                Debug.LogError(e);
            }
        }

        log("Re-ordering to user order...");
        LoadPackOrder();
        log("Disabling packs that should be disabled...");
        LoadDisabledPacks();

        log($"Loaded {LoadedPacks.Count}/{paths.Count} texture packs");

        // attempt to refresh previously loaded stuff
        Plugin.RefreshTextures();
        Plugin.RefreshSounds();
        
        // just incase
        if(UI_RHPacksList.Instance != null)
            UI_RHPacksList.Instance?.BuildList();
    }
}