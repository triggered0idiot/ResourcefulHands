using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using static CommandConsole;

namespace ResourcefulHands;

/// <summary>
/// - Use RHLog in place of Debug.Log (for the BepInEx console).
/// - Use RHLog.Player for player logs when logging to the CommandConsole (for the in-game console).
/// </summary>
public static class RHLog
{
    private const string Prefix = "[Resourceful Hands] ";
    

    [Conditional("DEBUG")]
    public static void Debug(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") => Plugin.Log.LogInfo($"[{Path.GetFileName(file)}:{lineNumber}] {data}");
    
    public static void Info(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") => Plugin.Log.LogInfo($"[{Path.GetFileName(file)}:{lineNumber}] {data}");
    public static void Message(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") => Plugin.Log.LogMessage($"[{Path.GetFileName(file)}:{lineNumber}] {data}");
    
    public static void Warning(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") => Plugin.Log.LogWarning($"[{Path.GetFileName(file)}:{lineNumber}] {data}");
    public static void Error(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") => Plugin.Log.LogError($"[{Path.GetFileName(file)}:{lineNumber}] {data}");
    
    public static class Player
    {
        public static void Message(string message, bool printToConsole = true) => Log(Prefix + message, printToConsole);
        public static void Info(string message, bool printToConsole = true) => Message(message, printToConsole);
        public static void Warning(string message, bool printToConsole = true) => Log(Prefix + "[WARNING] " + message, printToConsole);
        public static void Error(string message) => LogError(Prefix + message);
    }
}