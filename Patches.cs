using System.Collections.Generic;
using HarmonyLib;
using Sirenix.Utilities;
using UnityEngine;

namespace ResourcefulHands.Patches;

[HarmonyPatch(typeof(SpriteRenderer))]
public static class SpriteRendererPatches
{
    private static bool isSelf = false;
    private static Dictionary<string, Sprite> _customSpriteCache = new();

    private static Sprite GetSprite(Sprite sprite)
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

        var localSprite = Sprite.Create(texture, sprite.rect, new Vector2(sprite.pivot.x/sprite.rect.width, sprite.pivot.y/sprite.rect.height), sprite.pixelsPerUnit);
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
        if(__instance.clip == null) return;
        
        isSelf = true;
        var newClip = Plugin.GetSoundFromPacks(__instance.clip.name);
        if (newClip != null)
        {
            isSelf = true;
            __instance.clip = newClip;
        }
    }

    [HarmonyPatch(methodName:"set_clip")]
    [HarmonyPostfix]
    private static void Setter_Prefix(AudioSource __instance, ref AudioClip value)
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
        }
    }
}

[HarmonyPatch(typeof(Material))]
public static class MaterialPatches
{
    private static bool isSelf = false;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

/*    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    private static void Constructor_Postfix(Material __instance)
    {
        if (isSelf)
        { isSelf = false; return; }
        
#if DEBUG
        Debug.Log($"{__instance?.name} was accessed [ctor]");
#endif
        
        if(__instance == null) return;
        if(!__instance.HasProperty(MainTex)) return;
        isSelf = true;
        if(__instance.mainTexture == null) return;
        isSelf = true;
        var texture = Plugin.GetTextureFromPacks(__instance.mainTexture.name);
        isSelf = true;
        __instance.mainTexture = texture;
    }*/

    [HarmonyPatch(methodName:"get_mainTexture")]
    [HarmonyPostfix]
    private static void Getter_Postfix(Material __instance, ref Texture __result)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        if(!__instance.HasProperty(MainTex)) return;
        isSelf = true;
        if(__instance.mainTexture == null) return;
        isSelf = true;
        var texture = Plugin.GetTextureFromPacks(__instance.mainTexture.name);
        if(texture == null) return;
        isSelf = true;
        __instance.mainTexture = texture;
    }

    [HarmonyPatch(methodName:"set_mainTexture")]
    [HarmonyPostfix]
    private static void Setter_Prefix(Material __instance, ref Texture value)
    {
        if (isSelf)
        { isSelf = false; return; }
        
        if(__instance == null) return;
        if(!__instance.HasProperty(MainTex)) return;
        isSelf = true;
        if(__instance.mainTexture == null) return;
        isSelf = true;
        var texture = Plugin.GetTextureFromPacks(__instance.mainTexture.name);
        if(texture == null) return;
        isSelf = true;
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
            Debug.Log("invoking material.mainTexture [passing to patch]");
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