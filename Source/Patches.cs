using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ResourcefulHands.Patches;

// NOTE: some debug related patches are in DebugTools.cs

// thanks McArdellje
[HarmonyPatch(typeof(Image))]
public static class ImagePatches
{
    // unbelievably simple!
    [HarmonyPatch("activeSprite", MethodType.Getter)]
    [HarmonyPostfix]
    public static void Getter_sprite_Postfix(Image __instance, ref Sprite __result) { // TODO: fix left/right ui sprites not working
        if (__result == null)
            return;
        
        if (__result.texture.name == "hand-sheet")
        {
            // cache the original texture
            OriginalAssetTracker.textures.TryAdd(__result.texture.name, __result.texture);

            string spriteTexName = __result.texture.name;
            int handId = string.Equals(__instance.gameObject.name, "Interact_L", StringComparison.CurrentCultureIgnoreCase) ? 0 : 1;
            
            string prefix = RHSpriteManager.GetHandPrefix(handId);
            string newSpriteTexName = spriteTexName;
            
            if(!newSpriteTexName.StartsWith(prefix))
                newSpriteTexName = prefix + newSpriteTexName;

            string oldName = __result.name;
            if (!__result.name.StartsWith(prefix))
                __result.name = prefix + __result.name;
            
            Sprite? newSpr = RHSpriteManager.GetReplacementSprite(__result, newSpriteTexName);
            __result.name = oldName;
            if (newSpr != null && newSpr != __result)
            {
                __result = newSpr;
                return;
            }
        }
        
        __result = RHSpriteManager.GetReplacementSprite(__result) ?? __result;
    }
}

[HarmonyPatch(typeof(SpriteRenderer))]
public static class SpriteRendererPatches
{
    private static bool dontPatch = false; // prevents loopbacks

    public static void Patch(SpriteRenderer sr)
    {
        if (sr == null) return;
        dontPatch = true;
        Sprite s = sr.sprite;
        if(s == null) return;
        dontPatch = true;
        sr.sprite = RHSpriteManager.GetReplacementSpriteForRenderer(sr) ?? s;
    }
    
    [HarmonyPatch("sprite", MethodType.Setter)]
    [HarmonyPostfix]
    private static void Setter_Postfix(SpriteRenderer __instance, ref Sprite value)
    {
        if (dontPatch)
        { dontPatch = false; return; }
        
        Patch(__instance);
        value = __instance.sprite;
    }
}

[HarmonyPatch(typeof(AudioSource))]
public static class AudioSourcePatches
{
    private static bool dontPatch = false; // prevents loopbacks

    private static void Cache(AudioClip? clip)
    {
        if(clip == null) return;
        //bool isModified = clip.name.EndsWith(Plugin.ModifiedStr);
        OriginalAssetTracker.sounds.TryAdd(clip.name, clip);
    }
    
    // Setters and Getters for clip are not needed,
    // Patching the play functions is the better and 100% working way
    
    // Patch parameterless Play()
    [HarmonyPatch(nameof(AudioSource.Play), [])]
    [HarmonyPrefix]
    private static void Play_NoArgs_Postfix(AudioSource __instance)
        => SwapClip(__instance);

    // Patch Play(double delay)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
    [HarmonyPrefix]
    private static void Play_DelayDouble_Postfix(AudioSource __instance)
        => SwapClip(__instance);

    // Patch Play(ulong delaySamples)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
    [HarmonyPrefix]
    private static void Play_DelayUlong_Postfix(AudioSource __instance)
        => SwapClip(__instance);
    
    // Patch PlayOneShot(AudioClip)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipOnly_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        // if the original is already cached this will just silently fail
        Cache(__instance.clip);
        
        var clip = ResourcePacksManager.GetSoundFromPacks(__instance.clip.name);
        if (clip is not null)
            __0 = clip;
    }

    // Patch PlayOneShot(AudioClip, float volumeScale)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip), typeof(float))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipAndVolume_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        // if the original is already cached this will just silently fail
        Cache(__instance.clip);
        
        var clip = ResourcePacksManager.GetSoundFromPacks(__instance.clip.name);
        if (clip is not null)
            __0 = clip;
    }
    
    // Shared logic
    internal static void SwapClip(AudioSource src)
    {
        if(dontPatch)
        { dontPatch = false; return; }
        
        if (src?.clip is null) 
            return;

        // if the original is already cached this will just silently fail
        Cache(src.clip);
        var clip = ResourcePacksManager.GetSoundFromPacks(src.clip.name);
        if (clip is null) 
            return;

        src.clip = clip;
    }
}

[HarmonyPatch(typeof(Material))]
public static class MaterialPatches
{
    private static bool dontPatch = false; // prevents loopbacks
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    
    internal static void Cache(Texture2D? tex)
    {
        //bool isModified = tex.name.EndsWith(Plugin.ModifiedStr);
        if(tex == null) return;
        OriginalAssetTracker.textures.TryAdd(tex.name, tex);
    }
    
    [HarmonyPatch(nameof(Material.SetTexture), new[] { typeof(string), typeof(Texture) })]
    [HarmonyPrefix]
    public static void SetTexture_Prefix(Material __instance, string name, ref Texture value)
    {
        if (value == null) return;
        if (value is not Texture2D texture2D) return;
        
        // if the original is already cached this will just silently fail
        Cache(texture2D);
        var texture = ResourcePacksManager.GetTextureFromPacks(texture2D.name, true);
        if(texture == null) return;
        
        value = texture;
    }
    [HarmonyPatch(nameof(Material.SetTexture), new[] { typeof(int), typeof(Texture) })]
    [HarmonyPrefix]
    public static void SetTexture_Prefix(Material __instance, int nameID, ref Texture value)
    {   
        if (value == null) return;
        if (value is not Texture2D texture2D) return;
        
        // if the original is already cached this will just silently fail
        Cache(texture2D);
        var texture = ResourcePacksManager.GetTextureFromPacks(texture2D.name, true);
        if(texture == null) return;
        
        value = texture;
    }

    public static void PatchMainTexture(Material __instance, ref Texture __result)
    {
        if(__instance == null) return;
        if(!__instance.HasTexture(MainTex)) return;
        
        dontPatch = true;
        var mainTex = __instance.mainTexture;
        if(mainTex == null) return;
        
        // if the original is already cached this will just silently fail
        Cache(mainTex as Texture2D);
        var texture = ResourcePacksManager.GetTextureFromPacks(mainTex.name, true);
        if(texture == null) return;
        
        __instance.mainTexture = texture;
    }
    [HarmonyPatch(methodName:"set_mainTexture")]
    [HarmonyPostfix]
    private static void Setter_Postfix(Material __instance, ref Texture value)
    {
        if (dontPatch)
        { dontPatch = false; return; }
        
        PatchMainTexture(__instance, ref value);
    }
}

[HarmonyPatch(typeof(Renderer))]
public static class RendererPatches
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    public static void Patch(ref Material material)
    {
        if(material == null) return;
        if(!material.HasProperty(MainTex)) return;
        
        // if the original is already cached this will just silently fail
        MaterialPatches.Cache(material.mainTexture as Texture2D);
        material.mainTexture = material.mainTexture;
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