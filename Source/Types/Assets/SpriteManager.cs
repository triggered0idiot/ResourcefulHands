using System;
using System.ArrayExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourcefulHands;

public class RHSpriteManager
{
    // -- Hand Sprites --
    
    public static string[] HandSpriteNames
    {
        get
        {
            if (Plugin.IsDemo)
            {
                return
                [
                    "Hands_Sprite_library",
                    "Hands_Sprite_Library_02",
                    "Fingers_Sprite_library",
                    "Fingers_Sprite_Library_02"
                ];
            }
            return
            [
                "Hands_Background_Sprite_Library_01",
                "Hands_Background_Sprite_Library_02",
                "Hands_Background_Sprite_Library_03",
                
                "Hands_Foreground_Sprite_Library_01",
                "Hands_Foreground_Sprite_Library_02",
                "Hands_Foreground_Sprite_Library_03",
                
                "Hands_Foreground_Sprite_Library_03",
                "Perk_handPoses",
                "milk_fingers"
            ];
        }
    }

    /// Applies <see cref="ResourcePacksManager.AddTextureOverride"/> to each hand sprite for a given pack
    public static void OverrideHands(string packId, string lrPrefix = "")
    {
        foreach (var spriteName in HandSpriteNames)
            ResourcePacksManager.AddTextureOverride(lrPrefix + spriteName, spriteName, packId);
        
        ResourcePacksManager.AddTextureOverride(lrPrefix + "hand-sheet", "hand-sheet", packId);
    }
    
    public static void ClearHandsOverride(string lrPrefix = "")
    {
        foreach (var spriteName in HandSpriteNames)
            ResourcePacksManager.RemoveTextureOverride(lrPrefix + spriteName);
                
        ResourcePacksManager.RemoveTextureOverride(lrPrefix + "hand-sheet");
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
    public static Sprite? GetReplacementSpriteForRenderer(SpriteRenderer spriteRenderer)
    {
        Sprite s = spriteRenderer.sprite;
        if(s == null) return null;
        string spriteTexName = s.texture.name;

        if (IsHandRenderer(spriteRenderer))
        {
            string prefix = GetHandPrefix(spriteRenderer);
            string newSpriteTexName = spriteTexName;
            
            if(!newSpriteTexName.StartsWith(prefix))
                newSpriteTexName = prefix + newSpriteTexName;
            
            // if there isnt a pack associated to a l/r hand then dont replace the l/r hand
            if ((RHConfig.PackPrefs.GetLeftHandPack() == null && prefix == GetHandPrefix(0))||
                (RHConfig.PackPrefs.GetRightHandPack() == null && prefix == GetHandPrefix(1)))
            {
                return null;
            }

            // check if texture exists
            Texture2D? replacementTexture = ResourcePacksManager.GetTextureFromPacks(newSpriteTexName);
            if (replacementTexture != null)
            {
                string originalSpriteName = s.name;
                string spriteName = s.name;
                if (!spriteName.StartsWith(prefix))
                    spriteName = prefix + spriteName;

                if (_customHandCache.TryGetValue(spriteName, out var spr))
                {
                    if (replacementTexture == spr.texture)
                        return spr;
                }

                s.name = spriteName;
                Sprite? newSprite = RHSpriteManager.GetReplacementSprite(s, newSpriteTexName);
                s.name = originalSpriteName;
                
                if (newSprite != null)
                {
                    _customHandCache[spriteName] = newSprite;
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

        bool isModifiedSprite = OriginalAssetTracker.ModifiedAssets.Contains(sprite) || _customSpriteCache.ContainsValue(sprite);
        string spriteCacheName = sprite.name;
        
        string textureCacheName = textureNameOverride ?? sprite.texture.name;
        
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
        localSprite.name = spriteCacheName;
        OriginalAssetTracker.ModifiedAssets.Add(localSprite);
        
        if(!isModifiedSprite)
        {
            RHLog.Debug($"{sprite} is being replaced, assuming its an original sprite we are caching it");
            var tex = sprite.texture;
            if (tex == null)
                RHLog.Warning($"{sprite} has no texture");
            else
            {
                OriginalAssetTracker.textures.TryAdd(tex.name, tex);
                OriginalAssetTracker.sprites.TryAdd(sprite.name, sprite);
            }
        }
        RHLog.Debug($"cached new replacement {spriteCacheName} as {localSprite}");
        
        _customSpriteCache.Remove(spriteCacheName);
        _customSpriteCache.Add(spriteCacheName, localSprite);
        return localSprite; 
    }
}