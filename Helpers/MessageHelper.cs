namespace LyricsPlayer.Helpers;

// Parsers remain independent of the presentation framework.
public static class MessageHelper
{
    public static void LogToConsole(string level, string text) =>
        System.Diagnostics.Trace.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {text}");
}
