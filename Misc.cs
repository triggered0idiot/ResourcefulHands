using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

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
    
    [System.NonSerialized]
    public bool IsActive = true;
    [System.NonSerialized]
    public Dictionary<string, Texture2D> Textures = [];
    [System.NonSerialized]
    public Dictionary<string, AudioClip> Sounds = [];
    

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

    public void ReplaceTexture(Texture2D texture)
    {
        Texture2D? newTexture = GetTexture(texture.name);
        if (newTexture != null)
        {
            texture.SetPixels(newTexture.GetPixels(0, 0, texture.width, texture.height));
            texture.Apply(true);
        }
    }
    public void ReplaceSound(AudioClip clip)
    {
        AudioClip? newClip = GetSound(clip.name);
        if (newClip != null)
        {
            var data = new float[newClip.samples * newClip.channels];
            if (!newClip.GetData(data, 0))
            {
                Debug.LogError($"Failed to get custom sound data for {newClip.name}!");
                return;
            }
            clip.SetData(data, 0);
        }
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
            
        Debug.Log($"Texture pack at {path} is valid, loading assets...");
        string texturesFolder = Path.Combine(path, "Textures");
        string soundsFolder = Path.Combine(path, "Sounds");
        
        string[] textureFiles = [];
        string[] soundFiles = [];

        if (Directory.Exists(texturesFolder))
            textureFiles = Directory.GetFiles(texturesFolder, "*.*", SearchOption.TopDirectoryOnly);
        if (Directory.Exists(soundsFolder))
            soundFiles = Directory.GetFiles(soundsFolder, "*.*", SearchOption.TopDirectoryOnly);

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

public static class MiscUtils
{
    public static string CleanString(string str)
    {
        // remove all directly invalid characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
            str = str.Replace(invalidChar.ToString(), "");

        // limit chars to the ascii range so no weird characters like ∞
        StringBuilder strBuild = new StringBuilder();
        foreach (char c in str)
        {
            if(c > 127) continue;
            strBuild.Append(c);
        }
        
        return strBuild.ToString();
    }
    
    public static void WriteString(this Stream stream, string value)
    {
        foreach (var character in value)
            stream.WriteByte((byte)character);
    }

    public static void WriteInteger(this Stream stream, uint value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    public static void WriteShort(this Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }
    
    // https://discussions.unity.com/t/how-to-encodetopng-compressed-textures-in-unity/707911/2
    /// <summary>
    /// Converts this RenderTexture to a RenderTexture in ARGB32 format.
    /// Can also return the original RenderTexture if it's already in ARGB32 format.
    /// Note that the resulting temporary RenderTexture should be released if no longer used to prevent a memory leak.
    /// </summary>
    public static RenderTexture ConvertToARGB32(this RenderTexture self)
    {
        if (self.format == RenderTextureFormat.ARGB32) return self;
        RenderTexture result = RenderTexture.GetTemporary(self.width, self.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(self, result);
        return result;
    }
    
    public static float[] LoadAudioFile(string filePath, out int sampleRate, out int channels)
    {
        using (var reader = new AudioFileReader(filePath))
        {
            sampleRate = reader.WaveFormat.SampleRate;
            channels = reader.WaveFormat.Channels;
    
            // Allocate buffer for the whole file
            var sampleCount = (int)(reader.Length / sizeof(float));
            var samples = new float[sampleCount];
    
            int read = reader.Read(samples, 0, sampleCount);
            return samples[..read]; // in case not all were read
        }
    }
    
    public static AudioClip CreateAudioClip(float[] samples, int sampleRate, int channels, string name = "GeneratedClip")
    {
        int lengthSamples = samples.Length / channels;

        AudioClip clip = AudioClip.Create(name, lengthSamples, channels, sampleRate, false);
        clip.SetData(samples, 0);

        return clip;
    }
    
    public static AudioClip LoadAudioClipFromFile(string path)
    {
        int sampleRate, channels;
        var samples = LoadAudioFile(path, out sampleRate, out channels);
        return CreateAudioClip(samples, sampleRate, channels);
    }
    
    // https://discussions.unity.com/t/save-audio-to-a-file-solved/56671/7
    public static byte[] ToWav(this AudioClip clip)
    {
        float[] floats = new float[clip.samples * clip.channels];
        clip.GetData(floats, 0);
  
        byte[] bytes = new byte[floats.Length * 2];
  
        for (int ii = 0; ii < floats.Length; ii++)
        {
            short uint16 = (short)(floats[ii] * short.MaxValue);
            byte[] vs = BitConverter.GetBytes(uint16);
            bytes[ii * 2] = vs[0];
            bytes[ii * 2 + 1] = vs[1];
        }
  
        byte[] wav = new byte[44 + bytes.Length];
  
        byte[] header = {0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,
            0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
            0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61 };
  
        Buffer.BlockCopy(header, 0, wav, 0, header.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(36 + bytes.Length), 0, wav, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(clip.channels), 0, wav, 22, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(clip.frequency), 0, wav, 24, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(clip.frequency * clip.channels * 2), 0, wav, 28, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(clip.channels * 2), 0, wav, 32, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(bytes.Length), 0, wav, 40, 4);
        Buffer.BlockCopy(bytes, 0, wav, 44, bytes.Length);
  
        return wav;
    }
    
    // TODO: add mesh support
    // https://gist.github.com/MattRix/0522c27ee44c0fbbdf76d65de123eeff
    private static int StartIndex = 0;
	
    public static void Start()
    {
        StartIndex = 0;
    }
    public static void End()
    {
        StartIndex = 0;
    }

    public static string MeshToString(this MeshFilter mf, Transform t) 
    {	
        Vector3 s 		= t.localScale;
        Vector3 p 		= t.localPosition;
        Quaternion r 	= t.localRotation;
		
		
        int numVertices = 0;
        Mesh m = mf.sharedMesh;
        if (!m)
        {
            return "####Error####";
        }
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
		
        StringBuilder sb = new StringBuilder();
		
        foreach(Vector3 vv in m.vertices)
        {
            Vector3 v = t.TransformPoint(vv);
            numVertices++;
            sb.Append($"v {v.x} {v.y} {-v.z}\n");
        }
        sb.Append("\n");
        foreach(Vector3 nn in m.normals) 
        {
            Vector3 v = r * nn;
            sb.Append($"vn {-v.x} {-v.y} {v.z}\n");
        }
        sb.Append("\n");
        foreach(Vector3 v in m.uv) 
        {
            sb.Append($"vt {v.x} {v.y}\n");
        }
        for (int material=0; material < m.subMeshCount; material ++) 
        {
            sb.Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");
			
            int[] triangles = m.GetTriangles(material);
            for (int i=0;i<triangles.Length;i+=3) {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
                    triangles[i]+1+StartIndex, triangles[i+1]+1+StartIndex, triangles[i+2]+1+StartIndex));
            }
        }
		
        StartIndex += numVertices;
        return sb.ToString();
    }
}