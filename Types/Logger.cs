using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResourcefulHands;

// TODO: check if this is messes with anything
public static class AnsiSupport
{
    const int STD_OUTPUT_HANDLE = -11;
    const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static void EnableConsoleColors()
    {
        RHLog.Info("Enabling console colors...");
        IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (handle == IntPtr.Zero)
        {
            RHLog.Warning("Could not get console.");
            return;
        }
        
        if (!GetConsoleMode(handle, out uint mode))
        {
            RHLog.Warning("Could not get console mode.");
            return;
        }

        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        if (!SetConsoleMode(handle, mode))
        {
            RHLog.Warning("Could not enable virtual terminal processing.");
            return;
        }

        RHLog.EnableColors = true;
        RHLog.Info("Console colors enabled!");
    }
}

/// <summary>
/// - Use RHLog in place of Debug.Log (for the BepInEx console).
/// - Use RHLog.Player for player logs when logging to the CommandConsole (for the in-game console).
/// </summary>
public static class RHLog
{
    // console colors for the cli window thingy, probably won't work ingame at all
    internal static bool EnableColors = false;
    public static string Reset  => EnableColors ? "\u001b[0m" : "";
    public static string Blue   => EnableColors ? "\u001b[34m" : "";
    public static string Magenta => EnableColors ? "\u001b[35m" : "";
    public static string Cyan   => EnableColors ? "\u001b[36m" : "";
    
    
    private const string Prefix = "[Resourceful Hands] ";

    // TODO: unsure if this needs to be ran on the main thread to work, test this

    [Conditional("DEBUG")]
    public static void Debug(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Plugin.Log.LogDebug($"{Magenta}[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }
    public static void Info(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Plugin.Log.LogInfo($"{Cyan}[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }
    public static void Message(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") 
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Plugin.Log.LogMessage($"{Blue}[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }
    
    public static void Warning(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Plugin.Log.LogWarning($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }
    public static void Error(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Plugin.Log.LogError($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }
    
    public static class Player
    {
        private static void Message(string message, bool printToConsole = true) => CommandConsole.Log(Prefix + message, printToConsole);
        
        public static void Info(string message, bool printToConsole = true) => Message(message, printToConsole);
        public static void Warning(string message, bool printToConsole = true) => Message(Prefix + "[WARNING] " + message, printToConsole);
        public static void Error(string message) => CommandConsole.LogError(Prefix + message);
    }
}