using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace ResourcefulHands;

/*
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
 */
[System.Serializable]
public class TexturePack
{
    public string name = string.Empty;
    public string desc = string.Empty;
    public string author = string.Empty;
    [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
    public ulong steamid = 0; //TODO: implement this in a ui?
    [JsonProperty(NullValueHandling=NullValueHandling.Include)]
    public string guid = string.Empty;
    
    [JsonProperty(propertyName:"hidden-from-list", NullValueHandling=NullValueHandling.Ignore)]
    public bool hiddenFromList = false;
    [JsonProperty(propertyName:"only-in-full-game", NullValueHandling=NullValueHandling.Ignore)]
    public bool onlyInFullGame = false;
    [JsonProperty(propertyName:"format-version", NullValueHandling=NullValueHandling.Ignore)]
    public int texturePackVersion = CurrentFormatVersion;
    
    [JsonIgnore]
    [System.NonSerialized]
    public const int CurrentFormatVersion = 1;
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
    public Texture2D Icon { get; private set; }
    
    // considering cloning these values, TODO: test if the textures and sounds are safe to be shared
    public Texture2D? GetTexture(string textureName)
    {
        Textures.TryGetValue(textureName, out var texture);
        return texture;
    }

    public AudioClip? GetSound(string soundName)
    {
        Sounds.TryGetValue(soundName, out var clip);
        return clip;
    }
    
    public static TexturePack? Load(string path, bool force = false)
    {
        string jsonPath = Path.Combine(path, "info.json");
        if(!File.Exists(jsonPath))
        {
            Debug.LogWarning($"{path} doesn't have an info.json!");
            return null;
        }
        TexturePack? pack = JsonConvert.DeserializeObject<TexturePack>(File.ReadAllText(jsonPath));
        if (pack == null)
        {
            Debug.LogWarning($"{jsonPath} isn't a valid TexturePack json!");
            Debug.Log("Example: " + Plugin.DefaultJson);
            return null;
        }
        if (!force)
        {
            if (pack.hiddenFromList)
            {
                Debug.Log($"Not loading texture pack at {path} because it is hidden.");
                return null;
            }
            if (pack.onlyInFullGame && Plugin.IsDemo)
            {
                Debug.Log($"Skipping incompatible texture pack (it says it only works for the fullgame): {path}");
                return null;
            }
        }
        
        if(pack.texturePackVersion != CurrentFormatVersion)
            Debug.LogWarning($"Texture pack at {path} is format version {pack.texturePackVersion} which isn't {CurrentFormatVersion} (the current version), it may not function correctly.");
        
        string iconPath = Path.Combine(path, "pack.png");
        if(!File.Exists(iconPath))
        {
            Debug.LogWarning($"{path} doesn't have an pack.png!");
            pack.Icon = new Texture2D(2, 2);
        }
        else
        {
            byte[] fileBytes = File.ReadAllBytes(iconPath);
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(fileBytes, false))
            {
                Debug.LogWarning($"{iconPath} isn't a valid texture!");
                try{Object.Destroy(texture);}catch{/**/}
            }
            else
            {
                texture.name = Path.GetFileNameWithoutExtension(iconPath);
                texture.filterMode = FilterMode.Point; // something, something retro
                texture.Apply();
                pack.Icon = texture;
            }
        }
        
        string prevGuid = pack.guid;
        if(string.IsNullOrWhiteSpace(pack.guid))
            pack.guid = pack.author.ToLower() + "." + pack.name.ToLower();
        pack.guid = MiscUtils.CleanString(pack.guid.Replace(' ', '_'));

        if (pack.guid != prevGuid)
        {
            string newJson = JsonConvert.SerializeObject(pack);
            File.WriteAllText(jsonPath, newJson);
            Debug.LogWarning($"Corrected {pack.name}'s guid: {prevGuid} -> {pack.guid}");
        }
            
        Debug.Log($"Texture pack at {path} is valid, loading assets...");
        string texturesFolder = Path.Combine(path, "Textures");
        string soundsFolder = Path.Combine(path, "Sounds");
        
        string[] textureFiles = [];
        string[] soundFiles = [];

        if (Directory.Exists(texturesFolder))
            textureFiles = Directory.GetFiles(texturesFolder, "*.*", SearchOption.AllDirectories);
        if (Directory.Exists(soundsFolder))
            soundFiles = Directory.GetFiles(soundsFolder, "*.*", SearchOption.AllDirectories);

        int textureCount = textureFiles.Length;
        int soundCount = soundFiles.Length;
        int i = 0;
        foreach (string textureFile in textureFiles)
        {
            Debug.Log($"Loading textures ({i++}/{textureCount})");
            string extension = Path.GetExtension(textureFile).ToLower();
            if (!(extension.Contains("png") || extension.Contains("jpg")))
            {
                Debug.LogWarning($"{extension} isn't supported! Only png and jpg files are supported! [at: {textureFile}]");
                continue;
            }
            byte[] fileBytes = File.ReadAllBytes(textureFile);
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(fileBytes, false))
            {
                Debug.LogWarning($"{textureFile} isn't a valid texture!");
                try{Object.Destroy(texture);}catch{/**/}
                continue;
            }
            texture.name = Path.GetFileNameWithoutExtension(textureFile);
            texture.filterMode = FilterMode.Point; // something, something retro
            texture.Apply();
            if (!pack.Textures.TryAdd(texture.name, texture))
                Debug.LogError($"Failed to add {textureFile} because texture of that name already exists in the same pack!");
        }

        i = 0;
        foreach (var soundFile in soundFiles)
        {
            Debug.Log($"Loading sounds ({i++}/{soundCount})");
            string extension = Path.GetExtension(soundFile).ToLower();
            if (extension.Contains("ogg"))
            {
                Debug.LogWarning($"{extension} isn't supported! Only mp3, wav, aiff, wma, acc files are supported! [at: {soundFile}]");
                continue;
            }
            AudioClip clip = MiscUtils.LoadAudioClipFromFile(soundFile);
            clip.name = Path.GetFileNameWithoutExtension(soundFile);
            if (!pack.Sounds.TryAdd(clip.name, clip))
                Debug.LogError($"Failed to add {soundFile} because texture of that name already exists in the same pack!");
        }

        return pack;
    }
}
