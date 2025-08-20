using System;
using System.ArrayExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourcefulHands;

public class SpriteManager
{
    // -- Hand Sprites --

    public static string[] HandSpriteNames { get; } =
    [
        "Fingers_Sprite_library",
        "Fingers_Sprite_Library_02",
        "Hands_Sprite_library",
        "Hands_Sprite_Library_02"
    ];

    /// Applies <see cref="ResourcePacksManager.AddTextureOverride"/> to each hand sprite for a given pack
    public static void OverrideHands(string packId, string lrPrefix = "")
    {
        foreach (var spriteName in HandSpriteNames)
            ResourcePacksManager.AddTextureOverride(lrPrefix + spriteName, spriteName, packId);
    }
    
    public static void ClearHandsOverride(string lrPrefix = "")
    {
        foreach (var spriteName in HandSpriteNames)
            ResourcePacksManager.RemoveTextureOverride(lrPrefix + spriteName);
    }
    
    public static string GetHandsOverride(string lrPrefix = "")
    {
        foreach (var spriteName in HandSpriteNames)
            return ResourcePacksManager.GetTextureOverride(lrPrefix + spriteName)?.Item2 ?? string.Empty;
        return string.Empty;
    }
    
    /// Returns if the given sprite is a sprite for the player's hands
    public static bool IsHandSprite(Sprite sprite)
    {
        if(sprite == null) return false;
        
        string name = sprite.name.ToLower();
        return Array.Exists(HandSpriteNames, s => string.Equals(s, name, StringComparison.CurrentCultureIgnoreCase));
    }

    /// Returns if the given sprite renderer is a part of the player's hands 
    public static bool IsHandRenderer(SpriteRenderer spriteRenderer)
    {
        if(spriteRenderer == null) return false;

        // avoid using ENT_Player due to it not working with the demo (yes, I want to keep demo support)
        Transform? parent = MiscUtils.FindParentWithName(spriteRenderer.transform, "Inventory-Root");
        return parent != null && parent;
    }

    /// Returns if the given sprite renderer is on the left or the right
    public static bool IsRightHand(SpriteRenderer spriteRenderer)
    {
        if(spriteRenderer == null) return false;
        Transform? target = MiscUtils.FindChildWithParentNamed(spriteRenderer.transform, "Inventory-Root");
        if (target == null) return false;
        
        return target.localPosition.x > 0;
    }
    /// Returns Left_ or Right_ depending if the given sprite renderer is on the left or the right
    public static string GetHandPrefix(SpriteRenderer spriteRenderer)
    {
        if(spriteRenderer == null) return "Left_";

        return IsRightHand(spriteRenderer) ? "Right_" : "Left_";
    }
    /// Returns Left_ or Right_ depending on the hand id
    public static string GetHandPrefix(int id)
    {
        return id == 0 ? "Left_" : "Right_";
    }
    
    // -- General Sprites --
    private static Dictionary<string, Sprite> _customSpriteCache = new();
    private static Dictionary<string, Sprite> _customHandCache = new();
    public static void ClearSpriteCache()
    {
        _customSpriteCache.Clear();
        _customHandCache.Clear();
    }
    public static void ClearSpecificSprite(string key)
    {
        _customSpriteCache.Remove(key);
        _customHandCache.Remove(key);
    }
    public static void ClearHandSprites()
    {
        _customHandCache.Clear();
        List<string> keysToRemove = [];
        keysToRemove.AddRange(from kv in _customSpriteCache where IsHandSprite(kv.Value) select kv.Key);

        foreach (var key in keysToRemove)
            _customSpriteCache.Remove(key);
    }
    
    /// Takes a given sprite renderer and returns the replaced version
    /// This function is used for additional context, such as left and right hands
    /// TODO: fix spam cache bug with custom Right_ textures NOT command ones, they don't seem to be affected
    public static Sprite? GetReplacementSpriteForRenderer(SpriteRenderer spriteRenderer)
    {
        Sprite s = spriteRenderer.sprite;
        if(s == null) return null;
        string spriteTexName = s.texture.name;

        if (IsHandRenderer(spriteRenderer))
        {
            string prefix = GetHandPrefix(spriteRenderer);
            string newSpriteTexName = prefix + spriteTexName;
            
            // check if texture exists
            Texture2D? replacementTexture = ResourcePacksManager.GetTextureFromPacks(newSpriteTexName);
            if (replacementTexture != null)
            {
                if (!s.name.StartsWith(prefix))
                    s.name = prefix + s.name;

                if (_customHandCache.TryGetValue(s.name, out var spr))
                {
                    if (replacementTexture == spr.texture)
                        return spr;
                }
                
                Sprite? newSprite = SpriteManager.GetReplacementSprite(s, newSpriteTexName);

                if (newSprite != null)
                {
                    _customHandCache[s.name] = newSprite;
                    return newSprite;
                }
            }
        }

        return GetReplacementSprite(s);
    }
    
    /// Takes a given sprite and returns the replaced version
    public static Sprite? GetReplacementSprite(Sprite sprite, string? textureNameOverride = null)
    {
        if (sprite == null || sprite.texture == null)
            return sprite;

        bool isModifiedSprite = sprite.name.EndsWith(Plugin.ModifiedStr);
        string spriteCacheName = sprite.name;
        if(isModifiedSprite)
            spriteCacheName = spriteCacheName.Substring(0, spriteCacheName.Length - Plugin.ModifiedStr.Length);
        
        string textureCacheName = textureNameOverride ?? sprite.texture.name;
        bool isModifiedTexture = textureCacheName.EndsWith(Plugin.ModifiedStr);
        if(isModifiedTexture)
            textureCacheName = textureCacheName.Substring(0, textureCacheName.Length - Plugin.ModifiedStr.Length);
        
        Sprite? cachedSprite = null;
        if (_customSpriteCache.TryGetValue(spriteCacheName, out var spriteCache))
        {
            if(spriteCache != null)
                cachedSprite = spriteCache;
            else
            {
                RHLog.Debug($"{spriteCacheName} is null now for some reason [cached]");
                _customSpriteCache.Remove(spriteCacheName);
            }
        }
        
        var texture = ResourcePacksManager.GetTextureFromPacks(textureCacheName);
        if (texture == null)
            return cachedSprite ?? sprite;
        
        if (cachedSprite != null && cachedSprite.texture == texture) return cachedSprite;
        else RHLog.Debug($"Regenerating new sprite for {sprite} because the texture changed.");

        // clamp rect incase someone fucks the texture size
        float clampedX = Mathf.Clamp(sprite.rect.x, 0, texture.width);
        float clampedY = Mathf.Clamp(sprite.rect.y, 0, texture.height);
        float clampedWidth = Mathf.Clamp(sprite.rect.width, 0, texture.width - clampedX);
        float clampedHeight = Mathf.Clamp(sprite.rect.height, 0, texture.height - clampedY);
        
        var localSprite = Sprite.Create(texture, new Rect(clampedX, clampedY, clampedWidth, clampedHeight), new Vector2(sprite.pivot.x/sprite.rect.width, sprite.pivot.y/sprite.rect.height), sprite.pixelsPerUnit);
        localSprite.name = spriteCacheName + Plugin.ModifiedStr;
        
        if(!isModifiedSprite)
        {
            RHLog.Debug($"{sprite} is being replaced, assuming its an original sprite we are caching it");
            var tex = sprite.texture;
            if (tex == null)
                RHLog.Warning($"{sprite} has no texture");
            else
                OriginalAssetTracker.textures.TryAdd(tex.name, tex);
        }
        RHLog.Debug($"cached new replacement {spriteCacheName} as {localSprite}");
        
        _customSpriteCache.Remove(spriteCacheName);
        _customSpriteCache.Add(spriteCacheName, localSprite);
        return localSprite; 
    }
}