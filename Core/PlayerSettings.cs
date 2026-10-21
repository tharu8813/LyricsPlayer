using System.Text.Json;

namespace LyricsPlayer.Core;

public sealed class PlayerSettings
{
    public int Volume { get; set; } = 80;
    public double SyncOffsetMs { get; set; } = 200;
    public bool MusicReactionEnabled { get; set; } = true;

    public static PlayerSettings Load(string? path)
    {
        if (path is null) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<PlayerSettings>(File.ReadAllText(path)) ?? new();
            settings.Volume = Math.Clamp(settings.Volume, 0, 100);
            settings.SyncOffsetMs = double.IsFinite(settings.SyncOffsetMs) ? Math.Clamp(settings.SyncOffsetMs, -10000, 10000) : 200;
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { System.Diagnostics.Trace.WriteLine($"Settings reset to defaults: {ex.Message}"); return new(); }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
