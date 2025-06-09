using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResourcefulHands;

public static class RHCommands
{
    public const string DumpCommand = "dumptopack";
    public const string ReloadCommand = "reloadpacks";
    public const string MoveCommand = "reorderpack";
    public const string ListCommand = "listpacks";
    public const string EnableCommand = "enablepack";
    public const string DisableCommand = "disablepack";

    public static void RefreshCommands()
    {
        var ccInst = CommandConsole.instance;
        if (ccInst)
        {
            CommandConsole.RemoveCommand(DumpCommand);
            CommandConsole.RemoveCommand(ReloadCommand);
            CommandConsole.RemoveCommand(MoveCommand);
            CommandConsole.RemoveCommand(ListCommand);
            CommandConsole.RemoveCommand(EnableCommand);
            CommandConsole.RemoveCommand(DisableCommand);
                
            ccInst.RegisterCommand(DumpCommand, DumpAllToPack, false);
            ccInst.RegisterCommand(ReloadCommand, ReloadPacks, false);
            ccInst.RegisterCommand(MoveCommand, MovePacks, false);
            ccInst.RegisterCommand(ListCommand, ListPacks, false);
            ccInst.RegisterCommand(EnableCommand, EnablePack, false);
            ccInst.RegisterCommand(DisableCommand, DisablePack, false);
        }
    }
    

    internal static void MovePack(TexturePack pack, bool isUp)
    {
        int packIndex = ResourcePacksManager.LoadedPacks.FindIndex(p => p == pack);
        
        int nextPackIndex = 0;
        nextPackIndex = isUp ? Math.Clamp(packIndex - 1, 0, ResourcePacksManager.LoadedPacks.Count - 1) : Math.Clamp(packIndex + 1, 0, ResourcePacksManager.LoadedPacks.Count - 1);

        // failsafe (make sure the numbers didn't get mixed up)
        // TODO: better fix
        try
        {
            var a = ResourcePacksManager.LoadedPacks[nextPackIndex];
            var b = ResourcePacksManager.LoadedPacks[packIndex];
            var aa = a.GetHashCode(); var bb = b.GetHashCode();
        }
        catch (ArgumentOutOfRangeException e)
        { Debug.LogWarning("Failed to move packs!"); return; }
        
        TexturePack previousPack = ResourcePacksManager.LoadedPacks[nextPackIndex];
        ResourcePacksManager.LoadedPacks[nextPackIndex] = pack;
        ResourcePacksManager.LoadedPacks[packIndex] = previousPack;
    }
    
    private static void MovePacks(string[] args)
    {
        const string helpText = $"Usage: {MoveCommand} [pack guid/pack index] [up/down]\nResource packs at the bottom of the loaded list will override textures at the top, use this command to move a texture pack up or down the list.";
        if (args.Length != 2)
        {
            CommandConsole.LogError("Invalid number of arguments!");
            CommandConsole.Log(helpText);
            return;
        }

        string packName;
        TexturePack? pack = null;
        if (int.TryParse(args[0], out int index))
        {
            try // too tried rn to do this properly :sob:
            {  pack = ResourcePacksManager.LoadedPacks[index]; }catch{/**/}
            
            if (pack == null)
            {
                CommandConsole.LogError($"Invalid first argument!\nThe resource pack at index {index} doesn't exist!");
                CommandConsole.Log(helpText);
                return;
            }
        }
        else
        {
            packName = args[0].ToLower();
            pack = ResourcePacksManager.LoadedPacks.FirstOrDefault(p => p.guid == packName);
            if (pack == null)
            {
                CommandConsole.LogError($"Invalid first argument!\nThe resource pack with guid '{packName}' doesn't exist!");
                CommandConsole.Log(helpText);
                return;
            }
        }
        
        string dir = args[1].ToLower();
        if (dir is not ("up" or "down" or "u" or "d"))
        {
            CommandConsole.LogError("Invalid second argument!\nExpected: up or down");
            CommandConsole.Log(helpText);
            return;
        }
        
        bool isUp = dir is "up" or "u";
        MovePack(pack, isUp);
        
        CommandConsole.Log("Reloading packs...");
        ResourcePacksManager.ReloadPacks_Internal(Debug.Log);
        CommandConsole.Log($"Moved {pack.name} {'{'}{pack.guid}{'}'} {(isUp ? "up" : "down")} successfully!");
    }
    
    private static void ListPacks(string[] args)
    {
        for (int i = 0; i < ResourcePacksManager.LoadedPacks.Count; i++)
        {
            var pack = ResourcePacksManager.LoadedPacks[i];
            CommandConsole.Log($"{(!pack.IsActive ? "[DISABLED] " : $"[{i}] ")}{pack.name} by {pack.author}\n-- description:\n{pack.desc}\n-- guid: '{pack.guid}'\n____", true);
        }
    }
    
    private static void ReloadPacks(string[] args)
    {
        ResourcePacksManager.ReloadPacks_Internal(str => CommandConsole.Log(str, true));
    }

    private static TexturePack? GetPackFromArgs(string[] args, Action<string> logErr)
    {
        string packName;
        TexturePack? pack = null;
        if (int.TryParse(args[0], out int index))
        {
            try // too tried rn to do this properly :sob:
            {  pack = ResourcePacksManager.LoadedPacks[index]; }catch{/**/}
            
            if (pack == null)
            {
                logErr($"Invalid first argument!\nThe resource pack at index {index} doesn't exist!");
                return null;
            }
        }
        else
        {
            packName = args[0].ToLower();
            pack = ResourcePacksManager.LoadedPacks.FirstOrDefault(p => p.guid == packName);
            if (pack == null)
            {
                logErr($"Invalid first argument!\nThe resource pack with guid '{packName}' doesn't exist!");
                return null;
            }
        }

        return pack;
    }
    private static void DisablePack(string[] args)
    {
        const string helpText = $"Usage: {DisableCommand} [pack guid/pack index]\nDisables a resource pack.";
        TexturePack? pack = GetPackFromArgs(args, CommandConsole.LogError);
        if (pack == null)
        {
            CommandConsole.Log(helpText, true);
            return;
        }
        
        pack.IsActive = false;
        CommandConsole.Log("Reloading packs...");
        ResourcePacksManager.ReloadPacks_Internal(Debug.Log);
        CommandConsole.Log($"Disabled {pack.name} {'{'}{pack.guid}{'}'} successfully!");
    }
    private static void EnablePack(string[] args)
    {
        const string helpText = $"Usage: {EnableCommand} [pack guid/pack index]\nEnables a resource pack.";
        TexturePack? pack = GetPackFromArgs(args, CommandConsole.LogError);
        if (pack == null)
        {
            CommandConsole.Log(helpText, true);
            return;
        }

        pack.IsActive = true;
        CommandConsole.Log("Reloading packs...");
        ResourcePacksManager.ReloadPacks_Internal(Debug.Log);
        CommandConsole.Log($"Enabled {pack.name} {'{'}{pack.guid}{'}'} successfully!");
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
        List<Texture2D> spriteTextures = [];
        List<AudioClip> sounds = [];
        
        // get assets from current scene
        Texture2D[] texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        AudioClip[] audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        spriteTextures.AddRange(sprites.Where(sprite => sprite.texture && !spriteTextures.Contains(sprite.texture)).Select(sprite => sprite.texture));
        
        CommandConsole.Log("Loading Playground [to extract assets]", true);
        SceneManager.LoadScene("Playground");
        texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        spriteTextures.AddRange(sprites.Where(sprite => sprite.texture && !spriteTextures.Contains(sprite.texture)).Select(sprite => sprite.texture));
        
        CommandConsole.Log("Loading Training-Level [to extract assets]", true);
        SceneManager.LoadScene("Training-Level");
        texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        spriteTextures.AddRange(sprites.Where(sprite => sprite.texture && !spriteTextures.Contains(sprite.texture)).Select(sprite => sprite.texture));
        
        CommandConsole.Log("Loading Main-Menu [to extract assets and finish]", true);
        SceneManager.LoadScene("Main-Menu");
        texture2Ds = Resources.FindObjectsOfTypeAll<Texture2D>();
        textures.AddRange(texture2Ds.Where(tex => !textures.Contains(tex)));
        audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        sounds.AddRange(audioClips.Where(sound => !sounds.Contains(sound)));
        sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        spriteTextures.AddRange(sprites.Where(sprite => sprite.texture && !spriteTextures.Contains(sprite.texture)).Select(sprite => sprite.texture));
        
        CommandConsole.Log("Packing assets...", true);
        
        int texturesAmnt = textures.Count;
        int spriteTexturesAmnt = spriteTextures.Count;
        int soundsAmnt = sounds.Count;
        
        string path = Path.Combine(Plugin.ConfigFolder, $"extracted-assets-{texturesAmnt+soundsAmnt+spriteTexturesAmnt}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        string texturesPath = Path.Combine(path, "Textures");
        string spriteTexturesPath = Path.Combine(texturesPath, "Sprites");
        string soundsPath = Path.Combine(path, "Sounds");
        if (!Directory.Exists(texturesPath))
            Directory.CreateDirectory(texturesPath);
        if (!Directory.Exists(spriteTexturesPath))
            Directory.CreateDirectory(spriteTexturesPath);
        if (!Directory.Exists(soundsPath))
            Directory.CreateDirectory(soundsPath);

        StringBuilder textureInfo = new StringBuilder();
        StringBuilder spriteTextureInfo = new StringBuilder();
        StringBuilder audioInfo = new StringBuilder();
        textureInfo.AppendLine("-- ingame textures list --");
        spriteTextureInfo.AppendLine("-- ingame sprite textures list --");
        audioInfo.AppendLine("-- ingame sounds list --");
        int savedTextures = 0;
        int savedSounds = 0;
        int savedSprites = 0;

        bool ExportTexture(Texture2D texture)
        {
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
                        saved = true;
                    }catch{/**/}
                    
                    RenderTexture.active = oldActive;
                    renderTexture.Release();
                }
                else
                {
                    byte[] b = texture.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(texturesPath, texture.name + ".png"), b);
                    saved = true;
                }
            }
            catch (Exception e)
            {
                CommandConsole.LogError($"{texture.name} failed because {e.Message}");
            }

            return saved;
        }
        
        for (int i = 0; i < texturesAmnt; i++)
        {
            var texture = textures[i];
            CommandConsole.Log($"Saving textures ({i}/{texturesAmnt})", true);
            textureInfo.Append(texture.name);
            bool saved = false;
            try
            {
                saved = ExportTexture(texture);
            }
            catch (Exception e)
            {
                CommandConsole.LogError($"{texture.name} failed because {e.Message}");
            }

            if (saved) savedTextures++;
            textureInfo.AppendLine(saved ? "" : " [failed to extract]");
        }
        for (int i = 0; i < spriteTexturesAmnt; i++)
        {
            var spriteTexture = spriteTextures[i];
            CommandConsole.Log($"Saving textures ({i}/{spriteTexturesAmnt})", true);
            spriteTextureInfo.Append(spriteTexture.name);
            bool saved = false;
            try
            {
                saved = ExportTexture(spriteTexture);
            }
            catch (Exception e)
            {
                CommandConsole.LogError($"{spriteTexture.name} failed because {e.Message}");
            }

            if (saved) savedSprites++;
            spriteTextureInfo.AppendLine(saved ? "" : " [failed to extract]");
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
        File.WriteAllText(Path.Combine(path, "info.json"), TexturePack.DefaultJson);
        // export info
        File.WriteAllText(Path.Combine(path, "textures_list.txt"), textureInfo.ToString());
        File.WriteAllText(Path.Combine(path, "sprite_textures_list.txt"), spriteTextureInfo.ToString());
        File.WriteAllText(Path.Combine(path, "audio_list.txt"), audioInfo.ToString());
        
        CommandConsole.Log($"Successfully saved {savedTextures} of {texturesAmnt} textures!", true);
        CommandConsole.Log($"Successfully saved {savedSprites} of {spriteTexturesAmnt} sprite textures!", true);
        CommandConsole.Log($"Successfully saved {savedSounds} of {soundsAmnt} sounds!", true);
        CommandConsole.Log($"Packed all assets to '{path}'", true);
    }
}