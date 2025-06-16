using BepInEx.Configuration;

namespace ResourcefulHands;

public static class RHConfig
{
    internal static ConfigEntry<bool>? LoadFullAudio = null;
    internal static ConfigEntry<bool>? AlwaysDebug = null;

    internal static void BindConfigs()
    {
        LoadFullAudio = Plugin.Instance.Config.Bind(
            "General",
            "Load entire audio file",
            false,
            "When enabled every audio file is always fully loaded, this can reduce stutters but will slow down loading."
        );
        
        AlwaysDebug = Plugin.Instance.Config.Bind(
            "General",
            "Always debug mode",
            false,
            $"When enabled pack debug mode is always enabled unless toggled via the command ({RHCommands.ToggleDebug})."
        );
    }
}