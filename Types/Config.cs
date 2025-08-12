using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace ResourcefulHands;

public static class RHConfig
{
    // Always debug mode
    private static ConfigEntry<bool>? alwaysDebug = null;
    public static bool AlwaysDebug => alwaysDebug?.Value ?? false;
    
    // Lazy loading
    private static ConfigEntry<bool>? lazyManip = null;
    public static bool LazyManip => lazyManip?.Value ?? false;
    
    // Colored console
    private static ConfigEntry<bool>? colorConsole = null;
    public static bool ColoredConsole => colorConsole?.Value ?? false;

    internal static void InitConfigs()
    {
        RHLog.Info("Initialising configs...");
        
        RHLog.Debug("Checking packs folder...");
        if (!Directory.Exists(PacksFolder))
            Directory.CreateDirectory(PacksFolder);
        
        // bind configs
        RHLog.Debug("Binding configs with bepinex...");
        
        // Debugging
        alwaysDebug = Plugin.Instance.Config.Bind(
            "Debugging",
            "Always debug mode",
            false,
            $"When enabled pack debug mode is always enabled unless toggled via the command ({RHCommands.ToggleDebug})."
        );
        RHLog.Debug("Bound alwaysDebug");
        colorConsole = Plugin.Instance.Config.Bind(
            "Debugging",
            "Colored Console",
            true,
            $"When enabled certain logs are given colors, disable if this is causing issues. Additionally, only works on windows."
        );
        RHLog.Debug("Bound colorConsole");
        
        // General
        lazyManip = Plugin.Instance.Config.Bind(
            "General",
            "Lazy Loading",
            true,
            $"When enabled every pack doesn't get reloaded when reordering or enabling/disabling packs in the settings menu."
        );
        RHLog.Debug("Bound lazyManip");
    }

    public static string PacksFolder => Path.Combine(Paths.ConfigPath, "RHPacks");
}