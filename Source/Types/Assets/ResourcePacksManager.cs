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
using Object = UnityEngine.Object;

namespace ResourcefulHands;


// basically, textures are never fixed rh just hopes that the texture will eventually get reset by something
// while this works for hands, it doesn't really work for anything else without a scene reload
internal static class OriginalAssetTracker
{
    // i used to use a simple prefix on the name, but that was kinda jank (atleast i think)
    internal static List<Object> ModifiedAssets = new List<Object>();
    
    // as long as no assets of the same type share a name this should be good
    public static Dictionary<string, Sprite> sprites = new();
    public static Dictionary<string, Texture2D> textures = new();
    public static Dictionary<string, AudioClip> sounds = new();

    public static void ClearAll()
    {
        sprites.Clear();
        textures.Clear();
        sounds.Clear();
    }
    
    public static AudioClip? GetSound(string clipName)
    {
        return sounds.GetValueOrDefault(clipName);
    }
    public static Texture2D? GetTexture(string texName)
    {
        return textures.GetValueOrDefault(texName);
    }
    public static Sprite? GetSprite(string spriteName)
    {
        return sprites.GetValueOrDefault(spriteName);
    }
    public static Sprite? GetFirstSpriteFromTextureName(string textureName)
    {
        return sprites.Values.FirstOrDefault(sprite => sprite.texture.name == textureName);
    }
}

public static class ResourcePacksManager
{
    public static bool IsUsingRHPacksFolder => LoadedPacks.Exists(p => p.IsConfigFolderPack);
    
    public static List<ResourcePack> LoadedPacks { get; internal set; } = [];
    public static ResourcePack[] ActivePacks => (LoadedPacks ?? []).Where(pack => pack is { IsActive: true }).ToArray();
    public static bool HasPacksChanged = true;

    public static ResourcePack? GetPackWithID(string id)
    {
        foreach (var pack in LoadedPacks)
        {
            if (pack.guid == id)
                return pack;
        }

        return null;
    }
    public static bool IsPackEnabledWithID(string id)
    {
        foreach (var pack in ActivePacks)
        {
            if (pack.guid == id)
                return true;
        }

        return false;
    }

    public static bool DoesPackATakePriorityOverPackB(ResourcePack a, ResourcePack b)
    {
        bool hasFoundA = false;
        foreach (var pack in LoadedPacks)
        {
            if (pack.guid == a.guid)
                hasFoundA = true;
            if (pack.guid == b.guid && !hasFoundA)
                return true;
        }

        return false;
    }
    
    // mapping of [texture name] -> [texture, pack to pull]
    private static Dictionary<string, Tuple<string, string>> _textureOverrides = new Dictionary<string, Tuple<string, string>>();
    
    /// Adds or replaces a texture override
    public static void AddTextureOverride(string originalTextureName, string newTextureName, string packId)
    {
        _textureOverrides[originalTextureName] = Tuple.Create(newTextureName, packId);
    }
    /// Removes a texture override
    public static void RemoveTextureOverride(string textureName)
    {
        _textureOverrides.Remove(textureName);
    }

    public static Tuple<string, string>? GetTextureOverride(string textureName)
    {
        return _textureOverrides.GetValueOrDefault(textureName);
    }

    public static ResourcePack? FindPackWithTexture(string textureName)
    {
        ResourcePack? pack = null;
        foreach (var activePack in ActivePacks)
        {
            var myTexture = activePack.GetTexture(textureName);
            if(myTexture != null)
                pack = activePack;
        }

        return pack;
    }
    
    public static Texture2D? GetTextureFromPacks(string textureName, bool nullOnFail = false)
    {
        if (isReloading)
        {
            if (nullOnFail) return null;
            var originalTexture = OriginalAssetTracker.GetTexture(textureName);
            return originalTexture ? originalTexture : null;
        }
        
        Texture2D? texture = null;

        if (_textureOverrides.TryGetValue(textureName, out var replacementInfo) && IsPackEnabledWithID(replacementInfo.Item2))
        {
            ResourcePack? pack = GetPackWithID(replacementInfo.Item2);
            if (pack != null)
            {
                texture = pack.GetTexture(replacementInfo.Item1);
                if(texture == null)
                    texture = OriginalAssetTracker.GetTexture(textureName);
            }
        }

        if (texture == null)
        {
            foreach (var pack in ActivePacks)
            {
                var myTexture = pack.GetTexture(textureName);
                if(myTexture != null)
                    texture = myTexture;
            }
        }

        var og = OriginalAssetTracker.GetTexture(textureName);
        if (texture) return texture;
        if (nullOnFail) return null;
        if (og) return og; // fallback
        
        if (textureName is "DeathFloor_02" or "_CORRUPTTEXTURE")
            return Plugin.CorruptionTexture;
        
        return null;
    }
    
    public static AudioClip? GetSoundFromPacks(string soundName)
    {
        if (isReloading)
        {
            var originalSound = OriginalAssetTracker.GetSound(soundName);
            return originalSound ? originalSound : null;
        }
        
        AudioClip? clip = null;
        foreach (var pack in ActivePacks)
        {
            var myClip = pack.GetSound(soundName);
            if(myClip != null)
                clip = myClip;
        }
        
        // fallback to any cached original sounds
        var og = OriginalAssetTracker.GetSound(soundName);
        if(clip ==null && og) return og;

        return clip;
    }
    
    internal static void MovePack(ResourcePack pack, bool isUp)
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

        ResourcePack previousPack = ResourcePacksManager.LoadedPacks[nextPackIndex];
        ResourcePacksManager.LoadedPacks[nextPackIndex] = pack;
        ResourcePacksManager.LoadedPacks[packIndex] = previousPack;
        
        ResourcePacksManager.Save();
    }
    
    internal static void SaveDisabledPacks()
    {
        if (ResourcePacksManager.isReloading) return;

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
        if (ResourcePacksManager.isReloading) return;

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
        if (ResourcePacksManager.isReloading) return;
        
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
        if (ResourcePacksManager.isReloading) return;

        RHConfig.PackPrefs.Load();
        string[] previousPacksState = RHConfig.PackPrefs.PackOrder;
            
        ResourcePack[] previousPacks = LoadedPacks.ToArray();
        ResourcePack?[] newPacks = new ResourcePack[previousPacksState.Length];
        for (int i = 0; i < newPacks.Length; i++)
            newPacks[i] = null;
            
        for (int i = 0; i < previousPacksState.Length; i++)
        {
            string guid = previousPacksState[i];
            ResourcePack? pack = previousPacks.FirstOrDefault(p => p.guid == guid);
            if (pack != null)
                newPacks[i] = pack;
        }

        List<ResourcePack> finalPacks = [];
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
    // wait till ready is needed for commands to not act weird (i.e. allows new command's changes to be applied after old command finishes reloading)
    public static bool ReloadPacks(bool waitTillReady = false, Action? callback = null)
    {
        if (isReloading && !waitTillReady)
        {
            RHLog.Warning("Tried to reload while already reloading?");
            return false;
        }

        RHLog.Debug("Dispatching pack reloader task...");
        Task.Run(async () =>
        {
            if (isReloading && waitTillReady)
            {
                RHLog.Debug("Waiting for previous reload...");
                while (isReloading)
                    await Task.Delay(125);
                RHLog.Debug("Reloading...");
            }
            
            await ReloadPacks_Internal();
            if (callback != null)
                CoroutineDispatcher.RunOnMainThread(callback);
        });
        return true;
    }
    
    internal static async Task ReloadPacks_Internal()
    {
        if (isReloading) return;
        
        HasPacksChanged = false;
        if (LoadedPacks.Count != 0)
        {
            RHLog.Debug("Saving packs before reloading!");
            Save();
        }
        
        isReloading = true;
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
                ResourcePack? pack = await ResourcePack.Load(path);
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
            isReloading = false; // no longer reloading texture packs, we can load and save orders,ect
            
            RHLog.Info("Re-ordering to user order...");
            LoadPackOrder();
            RHLog.Info("Disabling packs that should be disabled...");
            LoadDisabledPacks();
            
            // attempt to refresh previously loaded stuff
            Plugin.RefreshAllAssets(false);

            // just incase
            if (UI_RHPacksList.Instance != null)
                UI_RHPacksList.Instance?.BuildList();
        });
    }
}