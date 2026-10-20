namespace LyricsPlayer.Core;

public static class MediaFiles
{
    public static bool IsSupported(string path) => Path.GetExtension(path).ToLowerInvariant()
        is ".mp4" or ".mp3" or ".wmv" or ".mkv";

    // Navigation uses the full library and never wraps at either boundary.
    public static int AdjacentIndex(IReadOnlyList<string> paths, string? current, int direction)
    {
        int index = -1;
        for (int i = 0; i < paths.Count; i++)
            if (string.Equals(paths[i], current, StringComparison.OrdinalIgnoreCase)) { index = i; break; }
        if (index < 0) return direction > 0 && paths.Count > 0 ? 0 : -1;
        int next = index + direction;
        return next >= 0 && next < paths.Count ? next : -1;
    }

    public static string FormatTime(long ms)
    {
        var time = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return time.TotalHours >= 1 ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}" : $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}
