using LyricsPlayer.Models;
using LyricsPlayer.Parsers;

namespace LyricsPlayer.Core;

/// <summary>The original MediaLyricsSync timeline, without timers, media or views.</summary>
public sealed class LyricTimeline
{
    private readonly double[] _starts;
    public IReadOnlyList<LyricLine> Lines { get; }
    public double SyncOffsetMs { get; set; } = 200;

    public LyricTimeline(IReadOnlyList<LyricLine> lines)
    {
        Lines = lines;
        _starts = lines.Select(x => x.StartTime.TotalMilliseconds).ToArray();
    }

    public static LyricTimeline Parse(string text, bool srt)
    {
        var lines = srt ? SrtParser.Parse(text) : LrcParser.Parse(text);
        if (!srt)
            for (int i = 0; i < lines.Count - 1; i++) lines[i].EndTime = lines[i + 1].StartTime;
        return new LyricTimeline(InsertPauses(lines));
    }

    public int FindCurrentIndex(double mediaMs)
    {
        if (_starts.Length == 0) return -1;
        double time = Math.Max(0, mediaMs + SyncOffsetMs);
        // Preserve the original pre-roll behavior: the first line is active before its timestamp.
        int lo = 0, hi = _starts.Length - 1, answer = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (_starts[mid] <= time) { answer = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return string.IsNullOrWhiteSpace(Lines[answer].Text) ? -1 : answer;
    }

    private static List<LyricLine> InsertPauses(List<LyricLine> lines)
    {
        if (lines.Count == 0 || !lines[0].EndTime.HasValue) return lines;
        var result = new List<LyricLine>();
        if (lines[0].StartTime.TotalSeconds > 3) result.Add(new LyricLine(TimeSpan.FromSeconds(1), "..."));
        for (int i = 0; i < lines.Count - 1; i++)
        {
            result.Add(lines[i]);
            if (lines[i].EndTime is { } end && (lines[i + 1].StartTime - end).TotalSeconds > 3)
                result.Add(new LyricLine(end + TimeSpan.FromSeconds(1), "..."));
        }
        result.Add(lines[^1]);
        return result;
    }
}
