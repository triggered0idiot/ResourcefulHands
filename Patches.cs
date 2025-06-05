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
    private static bool isSelf = false;
    internal static Dictionary<string, Sprite> _customSpriteCache { get; private set; } = new();

    public static Sprite GetSprite(Sprite sprite)
    {
        if (_customSpriteCache.TryGetValue(sprite.name, out var spriteCache))
        {
            if(spriteCache != null)
                return spriteCache;
            else
            {
                #if DEBUG
                Debug.Log($"{sprite.name} is null now for some reason [cached]");
                #endif
                _customSpriteCache.Remove(sprite.name);
            }
        }
        
        var texture = Plugin.GetTextureFromPacks(sprite.texture.name);
        if(texture == null) return sprite;

        // clamp rect incase someone fucks the texture size
        float clampedX = Mathf.Clamp(sprite.rect.x, 0, texture.width);
        float clampedY = Mathf.Clamp(sprite.rect.y, 0, texture.height);
        float clampedWidth = Mathf.Clamp(sprite.rect.width, 0, texture.width - clampedX);
        float clampedHeight = Mathf.Clamp(sprite.rect.height, 0, texture.height - clampedY);
        
        var localSprite = Sprite.Create(texture, new Rect(clampedX, clampedY, clampedWidth, clampedHeight), new Vector2(sprite.pivot.x/sprite.rect.width, sprite.pivot.y/sprite.rect.height), sprite.pixelsPerUnit);
        localSprite.name = sprite.name + Plugin.ModifiedStr;
        Debug.Log($"cached {sprite.name} as {localSprite}");
        _customSpriteCache.Add(sprite.name, localSprite);
        return localSprite; 
    }
    
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(SpriteRenderer __instance)
    {
        if (isSelf)
        { isSelf = false; return; }
        
#if DEBUG
        Debug.Log($"{__instance?.sprite?.texture.name} was accessed [ctor]");
#endif
        
        if(__instance == null) return;
        isSelf = true;
        var sprite = __instance.sprite;
        
        if(sprite == null || sprite.texture == null) return;
        sprite = GetSprite(sprite);
        
        isSelf = true;
        __instance.sprite = sprite;
    }

    [HarmonyPatch(methodName:"get_sprite")]
    [HarmonyPostfix]
    private static void Getter_Postfix(SpriteRenderer __instance, ref Sprite __result)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        isSelf = true;
        var sprite = __instance.sprite;
        
        if(sprite == null || sprite.texture == null) return;
        sprite = GetSprite(sprite);
        
        isSelf = true;
        __instance.sprite = sprite;
    }

    [HarmonyPatch(methodName:"set_sprite")]
    [HarmonyPostfix]
    private static void Setter_Prefix(SpriteRenderer __instance, ref Sprite value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        isSelf = true;
        var sprite = __instance.sprite;
        
        if(sprite == null || sprite.texture == null) return;
        sprite = GetSprite(sprite);
        
        isSelf = true;
        __instance.sprite = sprite;
    }
}

[HarmonyPatch(typeof(AudioSource))]
public static class AudioSourcePatches
{
    private static bool isSelf = false;
    
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(AudioSource __instance)
    {
        if (isSelf)
        { isSelf = false; return; }
        
#if DEBUG
        Debug.Log($"{__instance?.clip.name} was accessed [ctor]");
#endif
        
        if(__instance == null) return;
        isSelf = true;
        if(__instance.clip == null) return;
        
        isSelf = true;
        var newClip = Plugin.GetSoundFromPacks(__instance.clip.name);
        if (newClip != null)
        {
            isSelf = true;
            __instance.clip = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                isSelf = true;
                __instance.Play();
            }
        }
    }

    [HarmonyPatch(methodName:"set_clip")]
    [HarmonyPostfix]
    private static void Setter_Postfix(AudioSource __instance)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        isSelf = true;
        if (__instance.clip == null) return;
        
        isSelf = true;
        var newClip = Plugin.GetSoundFromPacks(__instance.clip.name);
        if (newClip != null)
        {
            isSelf = true;
            __instance.clip = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                isSelf = true;
                __instance.Play();
            }
        }
    }
    
    [HarmonyPatch(methodName:"get_clip")]
    [HarmonyPostfix]
    private static void Getter_Postfix(AudioSource __instance, ref AudioClip __result)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        isSelf = true;
        if (__instance.clip == null) return;
        
        isSelf = true;
        var newClip = Plugin.GetSoundFromPacks(__instance.clip.name);
        if (newClip != null)
        {
            isSelf = true;
            __instance.clip = newClip;
            __result = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                isSelf = true;
                __instance.Play();
            }
        }
    }

    [HarmonyPatch(methodName:"Play", argumentTypes: [typeof(double)])]
    [HarmonyPostfix]
    private static void Play_Prefix(AudioSource __instance, double delay)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        isSelf = true;
        if(__instance.clip == null) return;
        
        isSelf = true;
        var newClip = Plugin.GetSoundFromPacks(__instance.clip.name);
        if (newClip != null)
        {
            isSelf = true;
            __instance.clip = newClip;
            if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
            {
                isSelf = true;
                __instance.Play();
            }
        }
    }
    
    [HarmonyPatch(methodName:"PlayHelper", argumentTypes: [typeof(AudioSource), typeof(ulong)])]
    [HarmonyPostfix]
    private static void PlayHelper_Prefix(AudioSource source, ulong delay)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(source == null) return;
        isSelf = true;
        if(source.clip == null) return;
        
        isSelf = true;
        var newClip = Plugin.GetSoundFromPacks(source.clip.name);
        if (newClip != null)
        {
            isSelf = true;
            source.clip = newClip;
        }
    }
}

[HarmonyPatch(typeof(Material))]
public static class MaterialPatches
{
    private static bool isSelf = false;
    internal static Dictionary<string, Texture> previousTextures = new();
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    // TODO: add .GetTexture and .SetTexture patches

    public static void SetMainTexture(Material m, Texture texture)
    {
        isSelf = true;
        m.mainTexture = texture;
    }
    public static Texture GetMainTexture(Material m)
    {
        isSelf = true;
        return m.mainTexture;
    }
    
    [HarmonyPatch(methodName:"get_mainTexture")]
    [HarmonyPostfix]
    private static void Getter_Postfix(Material __instance, ref Texture __result)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        if(!__instance.HasTexture(MainTex)) return;
        
        isSelf = true;
        var mainTex = __instance.mainTexture;
        if(mainTex == null) return;
        
        var texture = Plugin.GetTextureFromPacks(mainTex.name);
        if(texture == null) return;
        texture = Object.Instantiate(texture);
        texture.name = mainTex.name + " [replaced]"; // change name to help with restoration?
        
        previousTextures.TryAdd(mainTex.name, mainTex);
        __instance.mainTexture = texture;
    }

    [HarmonyPatch(methodName:"set_mainTexture")]
    [HarmonyPostfix]
    private static void Setter_Prefix(Material __instance, ref Texture value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        if(!__instance.HasTexture(MainTex)) return;
        
        isSelf = true;
        var mainTex = __instance.mainTexture;
        if(mainTex == null) return;
        
        var texture = Plugin.GetTextureFromPacks(mainTex.name);
        if(texture == null) return;
        texture = Object.Instantiate(texture);
        texture.name = mainTex.name + " [replaced]"; // change name to help with restoration?
        
        previousTextures.TryAdd(mainTex.name, mainTex);
        __instance.mainTexture = texture;
    }
}

[HarmonyPatch(typeof(Renderer))]
public static class RendererPatches
{
    private static bool isSelf = false;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(Renderer __instance)
    {
        if (isSelf)
        { isSelf = false; return; }
        
#if DEBUG
        Debug.Log($"{__instance?.name} (Renderer) was accessed [ctor]");
#endif

        foreach (var material in __instance.sharedMaterials)
        {
            if(!material.HasProperty(MainTex)) continue;
#if DEBUG
            Debug.Log("invoking material.mainTexture [passing to patch]");
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
        if (isSelf)
        { isSelf = false; return; }

        Patch(ref __result);
    }
    [HarmonyPatch(methodName:"set_material")]
    [HarmonyPostfix]
    private static void Setter_material_Prefix(Renderer __instance, ref Material value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        Patch(ref value);
    }
    [HarmonyPatch(methodName:"get_materials")]
    [HarmonyPostfix]
    private static void Getter_materials_Postfix(Renderer __instance, ref Material[] __result)
    {
        if (isSelf)
        { isSelf = false; return; }

        for (int i = 0; i < __result.Length; i++)
        { Patch(ref __result[i]); }
    }
    [HarmonyPatch(methodName:"set_materials")]
    [HarmonyPostfix]
    private static void Setter_materials_Prefix(Renderer __instance, ref Material[] value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        for (int i = 0; i < value.Length; i++)
        { Patch(ref value[i]); }
    }
    
    [HarmonyPatch(methodName:"get_sharedMaterial")]
    [HarmonyPostfix]
    private static void Getter_sharedMaterial_Postfix(Renderer __instance, ref Material __result)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        Patch(ref __result);
    }
    [HarmonyPatch(methodName:"set_sharedMaterial")]
    [HarmonyPostfix]
    private static void Setter_sharedMaterial_Prefix(Renderer __instance, ref Material value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        Patch(ref value);
    }
    [HarmonyPatch(methodName:"get_sharedMaterials")]
    [HarmonyPostfix]
    private static void Getter_sharedMaterials_Postfix(Renderer __instance, ref Material[] __result)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        for (int i = 0; i < __result.Length; i++)
        { Patch(ref __result[i]); }
    }
    [HarmonyPatch(methodName:"set_sharedMaterials")]
    [HarmonyPostfix]
    private static void Setter_sharedMaterials_Prefix(Renderer __instance, ref Material[] value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        for (int i = 0; i < value.Length; i++)
        { Patch(ref value[i]); }
    }
}