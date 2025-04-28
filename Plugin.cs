using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ResourcefulHands;

[BepInPlugin(GUID, "Resourceful Hands", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "triggeredidiot.wkd.resourcefulhands";
    public const string DumpCommand = "dumptopack";
    public const string DefaultJson =
    """
    {
        "name":"generated-game-assets",
        "desc":"Every game asset",
        "author":"Dark Machine Games",
        "steamid":0,
        "hidden-from-list":true
    }                                                  
    """;

    public const string ModifiedStr = " [modified asset]";
    
    public static Plugin Instance { get; private set; } = null!;
    public static string ConfigFolder => Path.Combine(Paths.ConfigPath, "RHPacks");
    public static List<TexturePack> LoadedPacks { get; internal set; } = [];
    public static TexturePack[] ActivePacks => LoadedPacks.Where(pack => pack.IsActive).ToArray();
    
    
    public Harmony? Harmony { get; private set; }

    public static Texture2D? GetTextureFromPacks(string textureName)
    {
        Texture2D? texture = null;
        foreach (var pack in ActivePacks)
            texture = pack.GetTexture(textureName);

        return texture;
    }
    public static AudioClip? GetSoundFromPacks(string soundName)
    {
        AudioClip? clip = null;
        foreach (var pack in ActivePacks)
            clip = pack.GetSound(soundName);

        return clip;
    }
    public static void ReplaceTextureFromPacks(Texture2D texture, bool modifyName = true)
    {
        foreach (var pack in ActivePacks)
            pack.ReplaceTexture(texture);
        if (modifyName)
            texture.name += " [modified]";
#if DEBUG
        Debug.Log("Swapped: " + texture.name);
#endif
    }
    public static void ReplaceSoundFromPacks(AudioClip sound, bool modifyName = true)
    {
        foreach (var pack in ActivePacks)
            pack.ReplaceSound(sound);
        if (modifyName)
            sound.name += " [modified]";
#if DEBUG
        Debug.Log("Swapped: " + sound.name);
#endif
    }
    
    public void Awake()
    {
        Instance = this;
        Logger.LogInfo("Setting up config");
        if (!Directory.Exists(ConfigFolder))
            Directory.CreateDirectory(ConfigFolder);
        
        Harmony = new Harmony(GUID);
        Harmony.PatchAll();
        
        LoadedPacks.Clear();
        Logger.LogInfo("Loading texture packs...");
        string[] paths = Directory.GetDirectories(ConfigFolder, "*", SearchOption.TopDirectoryOnly);
        foreach (string path in paths)
        {
            try
            {
                Logger.LogInfo($"Loading texture pack: {path}");
                TexturePack? pack = TexturePack.Load(path);
                if(pack == null)
                    throw new NullReferenceException($"Failed to load texture pack: {path}");
                else
                {
                    if (pack.hiddenFromList)
                    {
                        Logger.LogInfo($"Skipping hidden texture pack: {path}");
                        if(LoadedPacks.Contains(pack))
                            LoadedPacks.Remove(pack);
                        continue;
                    }
                    
                    LoadedPacks.Add(pack);
                    Logger.LogInfo($"Loaded texture pack: {path}");
                }
            }
            catch (Exception e)
            { Logger.LogError(e); }
        }
        Logger.LogInfo($"Loaded {LoadedPacks.Count}/{paths.Length} texture packs");

        bool hasLoadedCommands = false;
        SceneManager.sceneLoaded += (arg0, mode) =>
        {
            var ccInst = CommandConsole.instance;
            if (ccInst != null && !hasLoadedCommands)
            {
                hasLoadedCommands = true;
                ccInst.RegisterCommand(DumpCommand, DumpAllToPack, false);
            }

            foreach (var renderer in FindObjectsOfType<Renderer>(includeInactive: true))
            { var mats = renderer.sharedMaterials; }
        };
    }

    private static void DumpAllToPack(string[] args)
    {
        if (args.Any(arg => arg.ToLower() == "help"))
        {
            CommandConsole.Log("Use this command to generate a resource pack that contains every in-game asset. Good to find assets to replace but beware that there will probably be unused assets!", true);
            return;
        }
        
        bool isConfirmed = args.Any(arg => arg.ToLower() == "confirm");
        if (!isConfirmed)
        {
            CommandConsole.LogError($"Warning: This takes up alot of storage space due to uncompressed audio!\nTHIS WILL ALSO END YOUR RUN AND LOAD YOU BACK TO THE MAIN MENU!!\nARE YOU SURE? (type '{DumpCommand} confirm')");
            return;
        }
        
        CommandConsole.Log("Dumping all resources to a template texture pack [this will take some time]...", true);
        List<Texture2D> textures = [];
        List<AudioClip> sounds = [];
        
        // get assets from current scene
        Texture2D[] texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        AudioClip[] audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));

        CommandConsole.Log("Loading Playground [to extract assets]", true);
        SceneManager.LoadScene("Playground");
        texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        
        CommandConsole.Log("Loading Training-Level [to extract assets]", true);
        SceneManager.LoadScene("Training-Level");
        texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        
        CommandConsole.Log("Loading Main-Menu [to extract assets and finish]", true);
        SceneManager.LoadScene("Main-Menu");
        texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        
        CommandConsole.Log("Packing assets...", true);
        
        int texturesAmnt = textures.Count;
        int soundsAmnt = sounds.Count;
        
        string path = Path.Combine(ConfigFolder, $"extracted-assets-{texturesAmnt+soundsAmnt}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        string texturesPath = Path.Combine(path, "Textures");
        string soundsPath = Path.Combine(path, "Sounds");
        if (!Directory.Exists(texturesPath))
            Directory.CreateDirectory(texturesPath);
        if (!Directory.Exists(soundsPath))
            Directory.CreateDirectory(soundsPath);

        StringBuilder textureInfo = new StringBuilder();
        StringBuilder audioInfo = new StringBuilder();
        textureInfo.AppendLine("-- ingame textures list --");
        audioInfo.AppendLine("-- ingame sounds list --");
        int savedTextures = 0;
        int savedSounds = 0;
        
        for (int i = 0; i < texturesAmnt; i++)
        {
            var texture = textures[i];
            CommandConsole.Log($"Saving textures ({i}/{texturesAmnt})", true);
            textureInfo.Append(texture.name);
            bool saved = false;
            try
            {
                if (!texture.isReadable)
                {
                    CommandConsole.Log($"{texture.name} isn't readable, saving the slow way...", true);
                    RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 24);

                    var oldActive = RenderTexture.active;

                    try // make sure we restore the previous active render texture... with a nested try catch ikik its bad
                    {
                        RenderTexture.active = renderTexture;
                        Graphics.Blit(texture, renderTexture);
                        RenderTexture converted = renderTexture.ConvertToARGB32();
                        RenderTexture.active = converted;
                        
                        Texture2D readableTexture = new Texture2D(texture.width, texture.height, texture.format, false);
                        readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                        readableTexture.Apply();
                        converted.Release();

                        byte[] rb = readableTexture.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(texturesPath, texture.name + ".png"), rb);
                        savedTextures++;
                        saved = true;
                    }catch{/**/}
                    
                    RenderTexture.active = oldActive;
                    renderTexture.Release();
                }
                else
                {
                    byte[] b = texture.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(texturesPath, texture.name + ".png"), b);
                    savedTextures++;
                    saved = true;
                }
            }
            catch (Exception e)
            {
                CommandConsole.LogError($"{texture.name} failed because {e.Message}");
            }
            textureInfo.AppendLine(saved ? "" : " [failed to extract]");
        }

        for (int i = 0; i < soundsAmnt; i++)
        {
            var clip = sounds[i];
            CommandConsole.Log($"Saving sounds ({i}/{soundsAmnt})", true);
            audioInfo.Append(clip.name);
            bool saved = false;
            try
            {
                string outPath = Path.Combine(soundsPath, clip.name + ".wav");
                var data = new float[clip.samples * clip.channels];
                if (!clip.GetData(data, 0))
                {
                    CommandConsole.LogError($"Failed to access {clip.name}'s audio data!");
                    audioInfo.AppendLine(" [failed to extract]");
                    continue;
                }
                using (var stream = new FileStream(outPath, FileMode.CreateNew, FileAccess.Write))
                {
                    // The following values are based on http://soundfile.sapp.org/doc/WaveFormat/
                    var bitsPerSample = (ushort)16;
                    var chunkID = "RIFF";
                    var format = "WAVE";
                    var subChunk1ID = "fmt ";
                    var subChunk1Size = (uint)16;
                    var audioFormat = (ushort)1;
                    var numChannels = (ushort)clip.channels;
                    var sampleRate = (uint)clip.frequency;
                    var byteRate = (uint)(sampleRate * clip.channels * bitsPerSample / 8);  // SampleRate * NumChannels * BitsPerSample/8
                    var blockAlign = (ushort)(numChannels * bitsPerSample / 8); // NumChannels * BitsPerSample/8
                    var subChunk2ID = "data";
                    var subChunk2Size = (uint)(data.Length * clip.channels * bitsPerSample / 8); // NumSamples * NumChannels * BitsPerSample/8
                    var chunkSize = (uint)(36 + subChunk2Size); // 36 + SubChunk2Size
                    // Start writing the file.
                    stream.WriteString(chunkID);
                    stream.WriteInteger(chunkSize);
                    stream.WriteString(format);
                    stream.WriteString(subChunk1ID);
                    stream.WriteInteger(subChunk1Size);
                    stream.WriteShort(audioFormat);
                    stream.WriteShort(numChannels);
                    stream.WriteInteger(sampleRate);
                    stream.WriteInteger(byteRate);
                    stream.WriteShort(blockAlign);
                    stream.WriteShort(bitsPerSample);
                    stream.WriteString(subChunk2ID);
                    stream.WriteInteger(subChunk2Size);
                    foreach (var sample in data)
                    {
                        // De-normalize the samples to 16 bits.
                        var deNormalizedSample = (short)0;
                        if (sample > 0)
                        {
                            var temp = sample * short.MaxValue;
                            if (temp > short.MaxValue)
                                temp = short.MaxValue;
                            deNormalizedSample = (short)temp;
                        }
                        if (sample < 0)
                        {
                            var temp = sample * (-short.MinValue);
                            if (temp < short.MinValue)
                                temp = short.MinValue;
                            deNormalizedSample = (short)temp;
                        }
                        stream.WriteShort((ushort)deNormalizedSample);
                    }

                    savedSounds++;
                    saved = true;
                }
            }
            catch (Exception e)
            {
                CommandConsole.Log($"{clip.name} failed because {e.Message}", true);
            }
            audioInfo.AppendLine(saved ? "" : " [failed to extract]");
        }

        CommandConsole.Log($"Writing data files", true);
        // template json
        File.WriteAllText(Path.Combine(path, "info.json"), DefaultJson);
        // export info
        File.WriteAllText(Path.Combine(path, "textures_list.txt"), textureInfo.ToString());
        File.WriteAllText(Path.Combine(path, "audio_list.txt"), audioInfo.ToString());
        
        CommandConsole.Log($"Successfully saved {savedTextures} of {texturesAmnt} textures!", true);
        CommandConsole.Log($"Successfully saved {savedSounds} of {soundsAmnt} sounds!", true);
        CommandConsole.Log($"Packed all assets to '{path}'", true);
    }
}