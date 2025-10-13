using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

namespace ResourcefulHands;

public static class RHConfig
{
    // --- GENERAL ---
    // Lazy loading
    private static ConfigEntry<bool>? lazyManip = null;
    public static bool LazyManip => lazyManip?.Value ?? false;
    // Use old sprite replacer
    private static ConfigEntry<bool>? useOldSpr = null;
    public static bool UseOldSprReplace => useOldSpr?.Value ?? false;
    // Don't load outdated packs
    private static ConfigEntry<bool>? useOutdatedPacks = null;
    public static bool UseOutdatedPacks => useOutdatedPacks?.Value ?? false;
    
    // --- DEBUG STUFF ---
    // Colored console
    private static ConfigEntry<bool>? colorConsole = null;
    public static bool ColoredConsole => colorConsole?.Value ?? false;
    // Always debug mode
    private static ConfigEntry<bool>? alwaysDebug = null;
    public static bool AlwaysDebug => alwaysDebug?.Value ?? false;

    
    // Config folder stuff
    public static string PacksFolder => Path.Combine(Paths.ConfigPath, "RHPacks");
    public static string GenericFolder => Path.Combine(Paths.ConfigPath, "RHConfig");

    public static class PackPrefs
    {
        [System.Serializable]
        internal class PrefsObject
        {
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string[] disabledPacks = [];
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string[] packOrder = [];
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string leftHandPack = string.Empty;
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string rightHandPack = string.Empty;
            
            public static PrefsObject? FromJson(string json) => JsonConvert.DeserializeObject<PrefsObject>(json);
            public string ToJson() => JsonConvert.SerializeObject(this);
        }
        
        public static string[] DisabledPacks = [];
        public static string[] PackOrder = [];
        
        public static string LeftHandPack = string.Empty;
        public static ResourcePack? GetLeftHandPack()
        {
            return ResourcePacksManager.LoadedPacks.FirstOrDefault(pack => pack.guid == LeftHandPack && pack.IsActive);
        }
        
        public static string RightHandPack = string.Empty;
        public static ResourcePack? GetRightHandPack()
        {
            return ResourcePacksManager.LoadedPacks.FirstOrDefault(pack => pack.guid == RightHandPack && pack.IsActive);
        }

        internal static string GetFile()
        {
            string path = Path.Combine(GenericFolder, "prefs.json");
            if(!File.Exists(path))
                File.WriteAllText(path, "");

            return path;
        }
        
        public static void Load()
        {
            string path = GetFile();
            
            PrefsObject prefs = PrefsObject.FromJson(File.ReadAllText(path)) ?? new PrefsObject();
            DisabledPacks = prefs.disabledPacks;
            PackOrder = prefs.packOrder;
            LeftHandPack = prefs.leftHandPack;
            RightHandPack = prefs.rightHandPack;
            
            // TODO: quick fix, ill replace it later
            if (!string.IsNullOrEmpty(LeftHandPack))
                RHSpriteManager.OverrideHands(LeftHandPack, RHSpriteManager.GetHandPrefix(0));
            if (!string.IsNullOrEmpty(RightHandPack))
                RHSpriteManager.OverrideHands(RightHandPack, RHSpriteManager.GetHandPrefix(1));
        }

        public static void Save()
        {
            string path = GetFile();
            
            PrefsObject prefs = new PrefsObject
            {
                disabledPacks = DisabledPacks,
                packOrder = PackOrder,
                leftHandPack = LeftHandPack,
                rightHandPack = RightHandPack
            };
            
            File.WriteAllText(path, prefs.ToJson());
        }
    }
    
    internal static void InitConfigs()
    {
        RHLog.Info("Initialising configs...");
        
        RHLog.Debug("Checking packs folder...");
        if (!Directory.Exists(PacksFolder))
            Directory.CreateDirectory(PacksFolder);
        
        RHLog.Debug("Checking generic folder...");
        if (!Directory.Exists(GenericFolder))
            Directory.CreateDirectory(GenericFolder);
        
        RHLog.Debug("Loading packs prefs...");
        PackPrefs.Load();
        Application.quitting += () =>
        {
            RHLog.Info("Saving pack prefs...");
            ResourcePacksManager.SavePackOrder();
            ResourcePacksManager.SaveDisabledPacks();
            PackPrefs.Save();
        };
        
        // bind configs
        RHLog.Debug("Binding configs with bepinex...");
        
        // General
        lazyManip = Plugin.Instance.Config.Bind(
            "General",
            "Lazy Loading",
            true,
            $"When enabled every pack doesn't get reloaded when reordering or enabling/disabling packs in the settings menu."
        );
        RHLog.Debug("Bound lazyManip");
        useOldSpr = Plugin.Instance.Config.Bind(
            "General",
            "Use Old Sprite Replacer",
            false,
            $"A new sprite replacer (the thing that lets you have custom hands) has been added, hopefully this should improve performance. However, if you do have issues with this new replacer, turn this on to disable it."
        );
        RHLog.Debug("Bound useOldSpr");
        useOutdatedPacks = Plugin.Instance.Config.Bind(
            "General",
            "Load outdated packs?",
            true,
            $"When enabled packs that are made with an older pack-version/game-version won't be loaded."
        );
        RHLog.Debug("Bound useOutdatedPacks");
        
        // Debugging
        colorConsole = Plugin.Instance.Config.Bind(
            "Debugging",
            "Colored Console",
            // decided to disable by default because it's a bit prestigious to have rh do it automatically
            // instead people could turn it on to help see errors in the console i guess, also i like the looks
            false, 
            $"When enabled certain logs are given colors, disable if this is causing issues. Additionally, only works on windows."
        );
        RHLog.Debug("Bound colorConsole");
        alwaysDebug = Plugin.Instance.Config.Bind(
            "Debugging",
            "Always debug mode",
            false,
            $"When enabled pack debug mode is always enabled unless toggled via the command ({RHCommands.ToggleDebug})."
        );
        RHLog.Debug("Bound alwaysDebug");
    }
}