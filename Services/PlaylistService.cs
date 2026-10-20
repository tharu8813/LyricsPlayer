using System.Collections.ObjectModel;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.Models;

namespace LyricsPlayer.Services;

public sealed class PlaylistService : IAsyncDisposable
{
    private readonly LibVLC _engine;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _debounce;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Task> _pending = [];
    private CancellationTokenSource? _refresh;
    private FileSystemWatcher? _watcher;
    private bool _disposed;
    public string Folder { get; }
    public ObservableCollection<PlaylistItem> Items { get; } = [];
    public event Action<string>? StatusChanged;
    public event Action? Refreshed;

    public PlaylistService(LibVLC engine, Dispatcher dispatcher, string? folder = null)
    {
        _engine = engine;
        _dispatcher = dispatcher;
        Folder = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "LyricsPlayer");
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounce.Tick += DebounceTick;
    }

    public void StartWatching()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            _watcher = new FileSystemWatcher(Folder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = false
            };
            _watcher.Created += FileChanged;
            _watcher.Deleted += FileChanged;
            _watcher.Changed += FileChanged;
            _watcher.Renamed += FileChanged;
            _watcher.Error += WatcherError;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { StatusChanged?.Invoke($"폴더 자동 감시를 시작할 수 없습니다: {ex.Message}"); }
    }

    private void FileChanged(object sender, FileSystemEventArgs args) => ScheduleRefresh();
    private void WatcherError(object sender, ErrorEventArgs args) => ScheduleRefresh();
    private void ScheduleRefresh()
    {
        if (_lifetime.IsCancellationRequested || _dispatcher.HasShutdownStarted) return;
        _dispatcher.BeginInvoke(() =>
        {
            if (_disposed) return;
            _debounce.Stop();
            _debounce.Start();
        });
    }

    private async void DebounceTick(object? sender, EventArgs args)
    { _debounce.Stop(); await RefreshAsync(); }

    public async Task RefreshAsync()
    {
        if (_disposed) return;
        _refresh?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _refresh = cancellation;
        Task work = RefreshCoreAsync(cancellation.Token);
        _pending.Add(work);
        try { await work; }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!_disposed) StatusChanged?.Invoke($"목록을 갱신할 수 없습니다: {ex.Message}"); }
        finally
        {
            _pending.Remove(work);
            if (ReferenceEquals(_refresh, cancellation)) _refresh = null;
            cancellation.Dispose();
        }
    }

    private async Task RefreshCoreAsync(CancellationToken token)
    {
        var files = await Task.Run(() => Directory.EnumerateFiles(Folder).Where(MediaFiles.IsSupported)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(path => new FileInfo(path)).Select(info => (info.FullName, info.Length, info.LastWriteTimeUtc)).ToList(), token);
        token.ThrowIfCancellationRequested();
        var paths = files.Select(f => f.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int i = Items.Count - 1; i >= 0; i--)
            if (!paths.Contains(Items[i].Path)) Items.RemoveAt(i);
        var existing = Items.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (!existing.TryGetValue(file.FullName, out var item))
            {
                item = new PlaylistItem(file.FullName);
                Items.Insert(i, item);
            }
            else if (!ReferenceEquals(Items[i], item)) Items.Move(Items.IndexOf(item), i);
            if (item.FileSize != file.Length || item.LastWriteTimeUtc != file.LastWriteTimeUtc)
            {
                item.Duration = "--:--";
                item.FileSize = file.Length;
                item.LastWriteTimeUtc = file.LastWriteTimeUtc;
            }
        }
        Refreshed?.Invoke();
        foreach (var item in Items.Where(x => x.Duration == "--:--").ToArray())
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var media = new Media(_engine, new Uri(item.Path));
                // Resume on the dispatcher: releasing Media inside VLC's parse callback can corrupt its native event stack.
                await media.Parse(MediaParseOptions.ParseLocal, 3000, token);
                token.ThrowIfCancellationRequested();
                long duration = media.Duration;
                if (duration > 0) item.Duration = MediaFiles.FormatTime(duration);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { StatusChanged?.Invoke($"재생시간을 읽을 수 없습니다: {item.Title}"); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _debounce.Stop();
        _debounce.Tick -= DebounceTick;
        if (_watcher is { } watcher)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= FileChanged;
            watcher.Deleted -= FileChanged;
            watcher.Changed -= FileChanged;
            watcher.Renamed -= FileChanged;
            watcher.Error -= WatcherError;
            watcher.Dispose();
        }
        _lifetime.Cancel();
        try { await Task.WhenAll(_pending.ToArray()); }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Completed playlist operation during shutdown: {ex.Message}"); }
        _lifetime.Dispose();
    }
}
