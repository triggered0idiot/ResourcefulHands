using BepInEx.Configuration;

namespace ResourcefulHands;

public static class RHConfig
{
    internal static ConfigEntry<bool>? AlwaysDebug = null;
    internal static ConfigEntry<bool>? LazyManip = null;

    internal static void BindConfigs()
    {
        AlwaysDebug = Plugin.Instance.Config.Bind(
            "General",
            "Always debug mode",
            false,
            $"When enabled pack debug mode is always enabled unless toggled via the command ({RHCommands.ToggleDebug})."
        );
        
        LazyManip = Plugin.Instance.Config.Bind(
            "General",
            "Lazy Loading",
            true,
            $"When enabled every pack doesn't get reloaded when reordering or enabling/disabling packs in the settings menu."
        );
    }
}