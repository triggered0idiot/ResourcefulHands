using static CommandConsole;

namespace ResourcefulHands;

/// <summary>
/// - Use RHLog in place of Debug.Log (for the BepInEx console).
/// - Use RHLog.Player for player logs when logging to the CommandConsole (for the in-game console).
/// </summary>
public static class RHLog
{
    private const string Prefix = "[Resourceful Hands] ";
    

    public static void Info(object data) => Plugin.Log.LogInfo(Prefix + data);
    public static void Message(object data) => Plugin.Log.LogMessage(Prefix + data);
    public static void Warning(object data) => Plugin.Log.LogWarning(Prefix + data);
    public static void Error(object data) => Plugin.Log.LogError(Prefix + data);
    
    public static class Player
    {
        public static void Message(string message, bool printToConsole = true) => Log(Prefix + message, printToConsole);
        public static void Info(string message, bool printToConsole = true) => Message(message, printToConsole);
        public static void Warning(string message, bool printToConsole = true) => Log(Prefix + "[WARNING] " + message, printToConsole);
        public static void Error(string message) => LogError(Prefix + message);
    }
}