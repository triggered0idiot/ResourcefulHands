using System.Collections.Generic;
using System.IO;
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
            
            public static PrefsObject? FromJson(string json) => JsonConvert.DeserializeObject<PrefsObject>(json);
            public string ToJson() => JsonConvert.SerializeObject(this);
        }
        
        public static string[] DisabledPacks = [];
        public static string[] PackOrder = [];

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
        }

        public static void Save()
        {
            string path = GetFile();
            
            PrefsObject prefs = new PrefsObject
            {
                disabledPacks = DisabledPacks,
                packOrder = PackOrder
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
        
        // Debugging
        colorConsole = Plugin.Instance.Config.Bind(
            "Debugging",
            "Colored Console",
            true,
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