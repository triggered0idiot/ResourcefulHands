using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace ResourcefulHands;

/*
    // incase whoever is reading this doesn't understand the point of the whole format-version thing
    // its just used to show which json files are compatible with what mod versions
    // i.e features from version 2 will only work on >0.9.60 and not 0.9.51
    // ideally version 1 will work on any version of rh unless the core formatting is changed for some reason

    // version 1 spec
    {
        "name":"generated-game-assets",
        "desc":"Every game asset",
        "author":"Dark Machine Games",
        "guid":"generated-game-assets",
        "steamid":0,
        "hidden-from-list":true,
        "only-in-full-game":false,
        "format-version":1
    }
    
    // version 2 spec
    {
        "name":"generated-game-assets",
        "desc":"Every game asset",
        "author":"Dark Machine Games",
        "guid":"generated-game-assets",
        "steamid":0,
        "hidden-from-list":true,
        "only-in-full-game":false,
        
        // new feature (optional extra)
        "textures-folder":"Textures",
        "sounds-folder":"Sounds",
        "icon-file":"pack.png",
        
        "format-version":2
    }
    
    // version 3 spec
    {
        "name":"generated-game-assets",
        "desc":"Every game asset",
        "author":"Dark Machine Games",
        // new feature, version strings for packs
        // now you can show people your pack's progress
        "pack-version":"b0.50p",
        
        "guid":"generated-game-assets",
        "steamid":0,
        "hidden-from-list":true,
        "only-in-full-game":false,
        
        // uses the version string at the top left, i.e b0.50p
        // if the game version has a different number like 0.55 then
        // a warning will appear, otherwise if a letter changes like b0.50d
        // no warning will appear
        "game-string":"b0.50p",
        
        "textures-folder":"Textures",
        "sounds-folder":"Sounds",
        "icon-file":"pack.png",
        
        "format-version":3
    }
*/

[System.Serializable]
public class ResourcePack
{
    public string name = string.Empty;
    public string desc = string.Empty;
    public string author = string.Empty;
    [JsonProperty(propertyName:"pack-version", NullValueHandling=NullValueHandling.Ignore)]
    public string packVersion = string.Empty;
    
    [JsonProperty(propertyName:"steamid", NullValueHandling=NullValueHandling.Ignore)]
    public ulong steamId = 0;
    [JsonProperty(NullValueHandling=NullValueHandling.Include)]
    public string guid = string.Empty;
    [JsonProperty(propertyName:"hidden-from-list", NullValueHandling=NullValueHandling.Ignore)]
    public bool hiddenFromList = false;
    [JsonProperty(propertyName:"only-in-full-game", NullValueHandling=NullValueHandling.Ignore)]
    public bool onlyInFullGame = false;

    [JsonProperty(propertyName:"textures-folder", NullValueHandling=NullValueHandling.Ignore)]
    public string relativeTexturesPath = "Textures";
    [JsonProperty(propertyName:"sounds-folder", NullValueHandling=NullValueHandling.Ignore)]
    public string relativeSoundsPath = "Sounds";
    [JsonProperty(propertyName:"icon-file", NullValueHandling=NullValueHandling.Ignore)]
    public string relativeIconPath = "pack.png";
    
    [JsonProperty(propertyName:"format-version", NullValueHandling=NullValueHandling.Ignore)]
    public int resourcePackVersion = CurrentFormatVersion;
    
    [JsonIgnore]
    [System.NonSerialized]
    public const int CurrentFormatVersion = 3;
    [JsonIgnore]
    [System.NonSerialized]
    public bool IsActive = true;
    [JsonIgnore]
    [System.NonSerialized]
    public Dictionary<string, Texture2D> Textures = [];
    [JsonIgnore]
    [System.NonSerialized]
    public Dictionary<string, AudioClip> Sounds = [];
    [JsonIgnore]
    [System.NonSerialized]
    protected List<AudioClip> RawSounds = [];
    [JsonIgnore]
    [System.NonSerialized]
    public bool IsConfigFolderPack = true;

    [JsonIgnore]
    public string PackPath { private set; get; } = string.Empty;
    [JsonIgnore]
    public Texture2D Icon { get; private set; } = null!;
    
    [JsonIgnore]
    [System.NonSerialized]
    public const string DefaultJson =
    """
    {
        "name":"generated-game-assets",
        "desc":"Every game asset",
        "author":"Dark Machine Games",
        "pack-version":"0.50",
        
        "guid":"generated-game-assets",
        "steamid":0,
        "hidden-from-list":true,
        "only-in-full-game":false,
        
        "game-string":"b0.50p",
        
        "textures-folder":"Textures",
        "sounds-folder":"Sounds",
        "icon-file":"pack.png",
        
        "format-version":3
    }                                                           
    """;
    
    // considering cloning these values, TODO: test if the textures and sounds are safe to be shared
    public Texture2D? GetTexture(string textureName)
    {
        Textures.TryGetValue(textureName, out var texture);
        return texture;
    }

    public bool HasHandTextures()
    {
        foreach (var handSpriteName in RHSpriteManager.HandSpriteNames)
        {
            if (Textures.ContainsKey(handSpriteName))
                return true;
            if (Textures.ContainsKey(RHSpriteManager.GetHandPrefix(0) + handSpriteName))
                return true;
            if (Textures.ContainsKey(RHSpriteManager.GetHandPrefix(1) + handSpriteName))
                return true;
        }
        
        return false;
    }

    public AudioClip? GetSound(string soundName)
    {
        Sounds.TryGetValue(soundName, out var clip);
        return clip;
    }
    
    public static async Task<ResourcePack?> Load(string path, bool force = false)
    {
        bool isConfigPack = path.Contains("config") && path.Contains("RHPacks");

        string jsonPath = Path.Combine(path, "info.json");
        if(!File.Exists(jsonPath))
        {
            RHLog.Warning($"{path} doesn't have an info.json!");
            return null;
        }
        ResourcePack? pack = JsonConvert.DeserializeObject<ResourcePack>(await File.ReadAllTextAsync(jsonPath));
        if (pack == null)
        {
            RHLog.Warning($"{jsonPath} isn't a valid ResourcePack json!");
            RHLog.Info("Example: " + DefaultJson);
            return null;
        }
        pack.PackPath = path;
        pack.IsConfigFolderPack = isConfigPack;
        
        if (!force)
        {
            if (pack.hiddenFromList)
            {
                RHLog.Info($"Not loading texture pack at {path} because it is hidden.");
                return null;
            }
            if (pack.onlyInFullGame && Plugin.IsDemo)
            {
                RHLog.Info($"Skipping incompatible texture pack (it says it only works for the fullgame): {path}");
                return null;
            }
        }
        
        if(pack.resourcePackVersion != CurrentFormatVersion)
            RHLog.Warning($"Texture pack at {path} is format version {pack.resourcePackVersion} which isn't {CurrentFormatVersion} (the current version), it may not function correctly.");
        
        string iconPath = Path.Combine(path, pack.relativeIconPath);
        if(!File.Exists(iconPath))
        {
            RHLog.Warning($"{path} doesn't have an pack.png! (icon path: '{pack.relativeIconPath}')");
            await CoroutineDispatcher.RunOnMainThreadAndWait(() =>
            {
                pack.Icon = Plugin.IconGray ?? new Texture2D(2,2);
            });
        }
        else
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(iconPath);
            await CoroutineDispatcher.RunOnMainThreadAndWait(() =>
            {
                Texture2D texture = new Texture2D(2, 2);
                if (!texture.LoadImage(fileBytes, false))
                {
                    RHLog.Warning($"{iconPath} isn't a valid texture!");
                    try{Object.Destroy(texture);}catch{/**/}
                }
                else
                {
                    texture.name = Path.GetFileNameWithoutExtension(iconPath);
                    texture.filterMode = FilterMode.Point; // something, something retro
                    texture.Apply();
                    pack.Icon = texture;
                }
            });
        }
        
        string prevGuid = pack.guid;
        if(string.IsNullOrWhiteSpace(pack.guid))
            pack.guid = pack.author.ToLower() + "." + pack.name.ToLower();
        pack.guid = MiscUtils.CleanString(pack.guid.Replace(' ', '_'));

        if (pack.guid != prevGuid)
        {
            string newJson = JsonConvert.SerializeObject(pack);
            await File.WriteAllTextAsync(jsonPath, newJson);
            RHLog.Warning($"Corrected {pack.name}'s guid: {prevGuid} -> {pack.guid}");
        }

        var conflictingPack = ResourcePacksManager.LoadedPacks.FirstOrDefault(p => p.guid == pack.guid);
        if (conflictingPack != null)
        {
            RHLog.Error($"Resource pack's guid ({pack.guid}) at '{path}' is the same as the resource pack's guid at '{conflictingPack.PackPath}" );
            return null;
        }
            
        RHLog.Info($"Texture pack at {path} is valid, loading assets...");
        string texturesFolder = Path.Combine(path, pack.relativeTexturesPath);
        string soundsFolder = Path.Combine(path, pack.relativeSoundsPath);
        RHLog.Debug($"Texture pack at {path} uses '{texturesFolder}' and '{soundsFolder}'");
        
        string[] textureFiles = [];
        string[] soundFiles = [];

        if (Directory.Exists(texturesFolder))
            textureFiles = Directory.GetFiles(texturesFolder, "*.*", SearchOption.AllDirectories);
        if (Directory.Exists(soundsFolder))
            soundFiles = Directory.GetFiles(soundsFolder, "*.*", SearchOption.AllDirectories);

        int textureCount = textureFiles.Length;
        int i = 0;
        foreach (string textureFile in textureFiles)
        {
            RHLog.Info($"Loading textures ({i++}/{textureCount})");
            string extension = Path.GetExtension(textureFile).ToLower();
            if (!(extension.Contains("png") || extension.Contains("jpg")))
            {
                RHLog.Warning($"{extension} isn't supported! Only png and jpg files are supported! [at: {textureFile}]");
                continue;
            }
            byte[] fileBytes = await File.ReadAllBytesAsync(textureFile);
            RHLog.Debug("Texture valid, queuing...");
            await CoroutineDispatcher.RunOnMainThreadAndWait(() =>
            {
                Texture2D texture = new Texture2D(2, 2);
                if (!texture.LoadImage(fileBytes, false))
                {
                    RHLog.Warning($"{textureFile} isn't a valid texture!");
                    try{Object.Destroy(texture);}catch{/**/}
                    return;
                }
                texture.name = Path.GetFileNameWithoutExtension(textureFile);
                texture.filterMode = FilterMode.Point; // something, something retro
                texture.Apply();
                if (!pack.Textures.TryAdd(texture.name, texture))
                    RHLog.Error($"Failed to add {textureFile} because texture of that name already exists in the same pack!");
            });
        }

        // Magic happens here :D
        await LoadAllSounds(soundFiles, pack);
        
        return pack;
    }

    private static async Task LoadAllSounds(IEnumerable<string> soundFiles, ResourcePack pack)
    {
        var soundTasks = new List<Task>();
        var files = soundFiles.ToList();
        
        var i = 0;
        var soundCount = files.ToList().Count;

        foreach (var soundFile in files)
        {
            RHLog.Info($"Queuing sounds ({++i}/{soundCount})");
            
            // Add all sounds to a task list
            soundTasks.Add(LoadSound(soundFile, pack));
        }
        
        await Task.WhenAll(soundTasks);
        
        RHLog.Info($"Loaded {soundTasks.Count} sounds for {pack.guid}!");
    }
    
    // ive tweaked this because for some reason unity decided to randomly remember
    // that it isn't thread safe and all of this code stopped working
    private static async Task LoadSound(string filepath, ResourcePack pack)
    {
        var type = Path.GetExtension(filepath)[1..]; // This also returns a dot for some reason
        
        var audioType = type.ToLower() switch
        {
            "wav" => AudioType.WAV,
            "ogg" => AudioType.OGGVORBIS,
            "mp3" => AudioType.MPEG,
            "aiff" => AudioType.AIFF,
            "wma" => AudioType.MPEG,
            "acc" => AudioType.ACC,
            _ => AudioType.UNKNOWN
        };

        if (audioType == AudioType.UNKNOWN)
        {
            RHLog.Error($"Failed to load sound file, '{type}' is not a supported format [at: {filepath}]");
            return;
        }

        var rand = new Random();

        // wait a random amount so all sounds don't get loaded at once
        await Task.Delay(rand.Next(100, 300));
        
        CoroutineDispatcher.Dispatch(LoadSoundRoutine(filepath, pack, audioType));
    }

    private static IEnumerator LoadSoundRoutine(string filepath, ResourcePack pack, AudioType audioType)
    {
        var clipName = Path.GetFileNameWithoutExtension(filepath);
        AudioClip? audioClip = null;
        
        UnityWebRequest? uwr = null;
        DownloadHandlerAudioClip? dh = null;
        
        uwr = new UnityWebRequest(filepath, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerAudioClip(filepath, audioType)
        };
        dh = (DownloadHandlerAudioClip)uwr.downloadHandler;
        dh.streamAudio = false;
        dh.compressed = true;
        
        yield return uwr.SendWebRequest();
        
        try
        {
            if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError || dh.audioClip == null)
                RHLog.Error($"Error while loading {clipName} [at: {filepath}]");
            else
                audioClip = dh.audioClip;
        }
        catch(Exception e)
        {
            RHLog.Error($"Error while loading {clipName} [at: {filepath}]\n" + e.Message);
        }

        if (audioClip == null) yield break;
        
        pack.RawSounds.Add(audioClip);
        
        audioClip.name = clipName;
        lock (pack.Sounds)
        {
            if (!pack.Sounds.TryAdd(clipName, audioClip))
                RHLog.Error($"Failed to add {clipName} because sound of that name already exists in the same pack! [at: {filepath}]");
        }
    } 
}
