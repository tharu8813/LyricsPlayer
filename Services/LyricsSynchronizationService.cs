using LyricsPlayer.Core;

namespace LyricsPlayer.Services;

public static class LyricsSynchronizationService
{
    public static async Task<LyricTimeline> LoadAsync(string mediaPath, CancellationToken token)
    {
        string lrc = Path.ChangeExtension(mediaPath, ".lrc");
        string srt = Path.ChangeExtension(mediaPath, ".srt");
        string? path = File.Exists(lrc) ? lrc : File.Exists(srt) ? srt : null;
        if (path is null) return new LyricTimeline([]);
        string text = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
        return await Task.Run(() => LyricTimeline.Parse(text, path == srt), token).ConfigureAwait(false);
    }
}
