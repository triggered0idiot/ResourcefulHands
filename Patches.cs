using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ResourcefulHands.Patches;

// thanks McArdellje
[HarmonyPatch(typeof(Image))]
public static class ImagePatches
{
    // unbelievably simple!
    [HarmonyPatch("activeSprite", MethodType.Getter)]
    [HarmonyPostfix]
    public static void Getter_sprite_Postfix(Image __instance, ref Sprite __result) {
        if (__result == null)
            return;
        __result = SpriteRendererPatches.GetSprite(__result);
    }
}

[HarmonyPatch(typeof(SpriteRenderer))]
public static class SpriteRendererPatches
{
    private static bool dontPatch = false; // prevents loopbacks
    internal static Dictionary<string, Sprite> _customSpriteCache { get; private set; } = new();

    public static Sprite GetSprite(Sprite sprite)
    {
        if (sprite == null)
            return sprite;

        bool isModifiedSprite = sprite.name.EndsWith(Plugin.ModifiedStr);
        string spriteCacheName = sprite.name;
        if(isModifiedSprite)
            spriteCacheName = spriteCacheName.Substring(0, spriteCacheName.Length - Plugin.ModifiedStr.Length);
        
        if (_customSpriteCache.TryGetValue(spriteCacheName, out var spriteCache))
        {
            if(spriteCache != null)
                return spriteCache;
            else
            {
                #if DEBUG
                RHLog.Info($"{spriteCacheName} is null now for some reason [cached]");
                #endif
                _customSpriteCache.Remove(spriteCacheName);
            }
        }
        
        var texture = ResourcePacksManager.GetTextureFromPacks(sprite.texture.name);
        if(texture == null) return sprite;

        // clamp rect incase someone fucks the texture size
        float clampedX = Mathf.Clamp(sprite.rect.x, 0, texture.width);
        float clampedY = Mathf.Clamp(sprite.rect.y, 0, texture.height);
        float clampedWidth = Mathf.Clamp(sprite.rect.width, 0, texture.width - clampedX);
        float clampedHeight = Mathf.Clamp(sprite.rect.height, 0, texture.height - clampedY);
        
        var localSprite = Sprite.Create(texture, new Rect(clampedX, clampedY, clampedWidth, clampedHeight), new Vector2(sprite.pivot.x/sprite.rect.width, sprite.pivot.y/sprite.rect.height), sprite.pixelsPerUnit);
        localSprite.name = spriteCacheName + Plugin.ModifiedStr;
#if DEBUG
        RHLog.Info($"cached {spriteCacheName} as {localSprite}");
        if(isModifiedSprite)
            RHLog.Warning("cached a modified sprite? idk some fucky stuff is going on");
#endif
        
        _customSpriteCache.Add(spriteCacheName, localSprite);
        return localSprite; 
    }
    
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(SpriteRenderer __instance) // i dont think this gets called
    {
        __instance.sprite = __instance.sprite;
        
#if DEBUG
        RHLog.Info($"{__instance?.sprite?.texture.name} was accessed [ctor]");
#endif
    }

    [HarmonyPatch("sprite", MethodType.Getter)]
    [HarmonyPostfix]
    private static void Getter_Postfix(SpriteRenderer __instance, ref Sprite __result)
    {
        if (dontPatch)
        { dontPatch = false; return; }
        
        __result = GetSprite(__result);
        dontPatch = true;
        __instance.sprite = __result;
    }

    [HarmonyPatch("sprite", MethodType.Setter)]
    [HarmonyPostfix]
    private static void Setter_Prefix(SpriteRenderer __instance, ref Sprite value)
    {
        if (dontPatch)
        { dontPatch = false; return; }
        
        value = GetSprite(value);
        dontPatch = true;
        __instance.sprite = value;
    }
}

[HarmonyPatch(typeof(AudioSource))]
public static class AudioSourcePatches
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(AudioSource __instance) // i dont think this gets called
    {
#if DEBUG
        RHLog.Info($"{__instance?.clip.name} was accessed [ctor]");
#endif
        
        if(__instance == null) return;
        if(__instance.clip == null) return;
        
        var newClip = ResourcePacksManager.GetSoundFromPacks(__instance.clip.name);
        if (newClip != null)
        {
            __instance.clip = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                dontPatch = true;
                __instance.Play();
            }
        }
    }

    private static bool dontPatch = false; // prevents loopbacks
    
    [HarmonyPatch(methodName:"set_clip")]
    [HarmonyPostfix]
    private static void Setter_Postfix(AudioSource __instance, ref AudioClip value)
    {
        if(dontPatch)
        { dontPatch = false; return; }
        
        if (value == null) return;
        
        var newClip = ResourcePacksManager.GetSoundFromPacks(value.name);
        if (newClip != null)
        {
            dontPatch = true;
            __instance.clip = newClip;
            value = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                dontPatch = true;
                __instance.Play();
            }
        }
    }
    
    [HarmonyPatch(methodName:"get_clip")]
    [HarmonyPostfix]
    private static void Getter_Postfix(AudioSource __instance, ref AudioClip __result)
    {
        if(dontPatch)
        { dontPatch = false; return; }

        if (__result == null) return;
        
        var newClip = ResourcePacksManager.GetSoundFromPacks(__result.name);
        if (newClip != null)
        {
            dontPatch = true;
            __instance.clip = newClip;
            __result = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                dontPatch = true;
                __instance.Play();
            }
        }
    }

    [HarmonyPatch(methodName:"Play", argumentTypes: [typeof(double)])]
    [HarmonyPostfix]
    private static void Play_Prefix(AudioSource __instance, double delay)
    {
        if(dontPatch)
        { dontPatch = false; return; }

        if(__instance.clip == null) return;
        
        __instance.clip = __instance.clip;
    }
    
    [HarmonyPatch(methodName:"PlayHelper", argumentTypes: [typeof(AudioSource), typeof(ulong)])]
    [HarmonyPostfix]
    private static void PlayHelper_Prefix(AudioSource source, ulong delay)
    {
        if(dontPatch)
        { dontPatch = false; return; }

        if(source.clip == null) return;
        
        source.clip = source.clip;
    }
}

[HarmonyPatch(typeof(Material))]
public static class MaterialPatches
{
    private static bool dontPatch = false; // prevents loopbacks
    internal static Dictionary<string, Texture> previousTextures = new();
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    // TODO: add .GetTexture and .SetTexture patches

    public static void SetMainTexture(Material m, Texture texture)
    {
        dontPatch = true;
        m.mainTexture = texture;
    }
    public static Texture GetMainTexture(Material m)
    {
        dontPatch = true;
        return m.mainTexture;
    }
    
    [HarmonyPatch(methodName:"get_mainTexture")]
    [HarmonyPostfix]
    private static void Getter_Postfix(Material __instance, ref Texture __result)
    {
        if (dontPatch)
        { dontPatch = false; return; }
        
        if(__instance == null) return;
        if(!__instance.HasTexture(MainTex)) return;
        
        dontPatch = true;
        var mainTex = __instance.mainTexture;
        if(mainTex == null) return;
        
        var texture = ResourcePacksManager.GetTextureFromPacks(mainTex.name);
        if(texture == null) return;
        texture.name = mainTex.name + " [replaced]"; // change name to help with restoration?
        
        previousTextures.TryAdd(mainTex.name, mainTex);
        __instance.mainTexture = texture;
    }

    [HarmonyPatch(methodName:"set_mainTexture")]
    [HarmonyPostfix]
    private static void Setter_Prefix(Material __instance, ref Texture value)
    {
        if (dontPatch)
        { dontPatch = false; return; }
        
        if(__instance == null) return;
        if(!__instance.HasTexture(MainTex)) return;
        
        dontPatch = true;
        var mainTex = __instance.mainTexture;
        if(mainTex == null) return;
        
        var texture = ResourcePacksManager.GetTextureFromPacks(mainTex.name);
        if(texture == null) return;
        texture.name = mainTex.name + " [replaced]"; // change name to help with restoration?
        
        previousTextures.TryAdd(mainTex.name, mainTex);
        __instance.mainTexture = texture;
    }
}

[HarmonyPatch(typeof(Renderer))]
public static class RendererPatches
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(Renderer __instance)
    {
#if DEBUG
        RHLog.Info($"{__instance?.name} (Renderer) was accessed [ctor]");
#endif

        foreach (var material in __instance.sharedMaterials)
        {
            if(!material.HasProperty(MainTex)) continue;
#if DEBUG
            RHLog.Info("invoking material.mainTexture [passing to patch]");
#endif
            var texture = material.mainTexture;
        }
    }

    public static void Patch(ref Material material)
    {
        if(material == null) return;
        if(!material.HasProperty(MainTex)) return;
        var texture = material.mainTexture;
    }

    [HarmonyPatch(methodName:"get_material")]
    [HarmonyPostfix]
    private static void Getter_material_Postfix(Renderer __instance, ref Material __result)
    {
        Patch(ref __result);
    }
    [HarmonyPatch(methodName:"set_material")]
    [HarmonyPostfix]
    private static void Setter_material_Prefix(Renderer __instance, ref Material value)
    {
        Patch(ref value);
    }
    [HarmonyPatch(methodName:"get_materials")]
    [HarmonyPostfix]
    private static void Getter_materials_Postfix(Renderer __instance, ref Material[] __result)
    {
        for (int i = 0; i < __result.Length; i++)
        { Patch(ref __result[i]); }
    }
    [HarmonyPatch(methodName:"set_materials")]
    [HarmonyPostfix]
    private static void Setter_materials_Prefix(Renderer __instance, ref Material[] value)
    {
        for (int i = 0; i < value.Length; i++)
        { Patch(ref value[i]); }
    }
    
    [HarmonyPatch(methodName:"get_sharedMaterial")]
    [HarmonyPostfix]
    private static void Getter_sharedMaterial_Postfix(Renderer __instance, ref Material __result)
    {
        Patch(ref __result);
    }
    [HarmonyPatch(methodName:"set_sharedMaterial")]
    [HarmonyPostfix]
    private static void Setter_sharedMaterial_Prefix(Renderer __instance, ref Material value)
    {
        Patch(ref value);
    }
    [HarmonyPatch(methodName:"get_sharedMaterials")]
    [HarmonyPostfix]
    private static void Getter_sharedMaterials_Postfix(Renderer __instance, ref Material[] __result)
    {
        for (int i = 0; i < __result.Length; i++)
        { Patch(ref __result[i]); }
    }
    [HarmonyPatch(methodName:"set_sharedMaterials")]
    [HarmonyPostfix]
    private static void Setter_sharedMaterials_Prefix(Renderer __instance, ref Material[] value)
    {
        for (int i = 0; i < value.Length; i++)
        { Patch(ref value[i]); }
    }
}