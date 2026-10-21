namespace LyricsPlayer.Core;

/// <summary>Session-local LRU; only completed, unchanged files are retained.</summary>
public sealed class MusicReactionCache
{
    public readonly record struct Key(string Path, long Size, DateTime ModifiedUtc, int AlgorithmVersion);
    public const int AlgorithmVersion = 2;
    private const long MaximumBytes = 8 * 1024 * 1024;
    private readonly LinkedList<(Key Key, MusicReactionTimeline Timeline)> _entries = new();
    private long _bytes;

    public static Key Identify(string path)
    {
        // shortcut: same-size edits preserving mtime collide; add a content fingerprint if that workflow is needed.
        var file = new FileInfo(path);
        return new(file.FullName.ToUpperInvariant(), file.Length, file.LastWriteTimeUtc, AlgorithmVersion);
    }

    public MusicReactionTimeline? Find(Key key)
    {
        for (var node = _entries.First; node is not null; node = node.Next)
            if (node.Value.Key == key)
            {
                _entries.Remove(node);
                _entries.AddFirst(node);
                return node.Value.Timeline;
            }
        return null;
    }

    public void Store(Key key, MusicReactionTimeline timeline)
    {
        long bytes = timeline.FrameCount * sizeof(double);
        if (bytes > MaximumBytes) return;
        for (var node = _entries.First; node is not null;)
        {
            var next = node.Next;
            if (node.Value.Key.Path == key.Path) Remove(node);
            node = next;
        }
        _entries.AddFirst((key, timeline));
        _bytes += bytes;
        while (_entries.Count > 16 || _bytes > MaximumBytes) Remove(_entries.Last!);
    }

    private void Remove(LinkedListNode<(Key Key, MusicReactionTimeline Timeline)> node)
    {
        _bytes -= node.Value.Timeline.FrameCount * sizeof(double);
        _entries.Remove(node);
    }
}
