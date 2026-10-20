using LyricsPlayer.ViewModels;

namespace LyricsPlayer.Models;

public sealed class PlaylistItem(string path) : ObservableObject
{
    public string Path { get; } = path;
    public string Title { get; } = System.IO.Path.GetFileNameWithoutExtension(path);
    public string Format { get; } = System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
    private string _duration = "--:--";
    private bool _isPlaying;
    public string Duration { get => _duration; set => Set(ref _duration, value); }
    public bool IsPlaying { get => _isPlaying; set => Set(ref _isPlaying, value); }
    internal long FileSize { get; set; }
    internal DateTime LastWriteTimeUtc { get; set; }
}
