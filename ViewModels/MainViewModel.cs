using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.Models;
using LyricsPlayer.Services;

namespace LyricsPlayer.ViewModels;

public enum ReactionStatus { Unavailable, Analyzing, Ready }

/// <summary>One playback session shared by the main view and the lyrics PiP.</summary>
public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly string? _libraryFolder;
    private readonly DispatcherTimer _clock;
    private MusicReactionTimeline? _musicReaction;
    private readonly PlaybackClock _positionClock = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Task> _pending = [];
    private CancellationTokenSource? _load;
    private CancellationTokenSource? _analysis;
    private readonly MusicReactionCache _reactionCache = new();
    private readonly Func<LibVLC, string, CancellationToken, Task<MusicReactionTimeline>> _analyze;
    private ReactionStatus _reactionState;
    private long _reactionReadyAt;
    private bool _reactionEnabled = true;
    private long _generation;
    private bool _disposed;
    private MediaPlaybackService? _playback;
    private PlaylistService? _playlist;
    private LyricTimeline _timeline = new([]);
    private string _search = "", _title = "음악과 가사, 그 사이에.", _status = "라이브러리를 준비하고 있습니다.", _state = "대기 중";
    private string? _currentPath;
    private bool _loading = true, _playing, _hasMedia, _hasVideo;
    public bool IsUserSeeking { get; set; }
    private long _position, _length;
    private int _activeIndex = -1, _volume = 80;
    private double _rate = 1, _offset = 200;
    public MediaPlayer? Player => _playback?.Player;
    public PlaylistService? Playlist => _playlist;
    public ObservableCollection<PlaylistItem> Items { get; } = [];
    public ICollectionView FilteredItems { get; }
    public IReadOnlyList<LyricLine> Lines => _timeline.Lines;
    public double[] Rates { get; } = [0.5, 0.75, 1, 1.25, 1.5, 2];
    public string? CurrentPath => _currentPath;
    public string Title { get => _title; private set => Set(ref _title, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public string PlaybackState { get => _state; private set => Set(ref _state, value); }
    public bool IsLoading { get => _loading; private set { if (Set(ref _loading, value)) CommandManager.InvalidateRequerySuggested(); } }
    public bool IsPlaying { get => _playing; private set { if (Set(ref _playing, value)) Changed(nameof(PlayPauseLabel)); } }
    public string PlayPauseLabel => IsPlaying ? "일시정지 (Space)" : "재생 (Space)";
    public bool HasMedia { get => _hasMedia; private set => Set(ref _hasMedia, value); }
    public bool HasVideo { get => _hasVideo; private set => Set(ref _hasVideo, value); }
    public long Position { get => _position; private set { if (Set(ref _position, value)) Changed(nameof(CurrentTime)); } }
    public long Length { get => _length; private set { if (Set(ref _length, value)) Changed(nameof(TotalTime)); } }
    public string CurrentTime => MediaFiles.FormatTime(Position);
    public string TotalTime => MediaFiles.FormatTime(Length);
    public int ActiveIndex { get => _activeIndex; private set => Set(ref _activeIndex, value); }
    public bool MusicReactionReady => _musicReaction is { FrameCount: > 0 };
    public ReactionStatus ReactionState { get => _reactionState; private set => Set(ref _reactionState, value); }
    public bool MusicReactionEnabled
    {
        get => _reactionEnabled;
        set
        {
            if (!Set(ref _reactionEnabled, value) || _disposed) return;
            _analysis?.Cancel();
            _musicReaction = null;
            ReactionState = ReactionStatus.Unavailable;
            Changed(nameof(MusicReactionReady));
            ResetAudio();
            if (value && HasMedia && Lines.Count > 0 && _currentPath is { } path)
                _ = TrackAsync(AnalyzeReactionAsync(path, _generation));
        }
    }
    public double MusicReactionLevel => IsPlaying ? _musicReaction?.LevelAt(
        Math.Clamp(_positionClock.Estimate(Environment.TickCount64), 0, Math.Max(0, Length))) *
        Math.Clamp((Environment.TickCount64 - _reactionReadyAt) / 400d, 0, 1) ?? 0 : 0;
    public bool MusicReactionMoving => IsPlaying && _musicReaction is { } reaction &&
        Math.Max(reaction.LevelAt(_positionClock.Estimate(Environment.TickCount64)),
            reaction.LevelAt(_positionClock.Estimate(Environment.TickCount64) + 50)) > 0.0001;
    public string LyricsMessage => IsLoading ? "가사를 불러오고 있습니다." : HasMedia ? "함께 듣는 시간.\n같은 이름의 LRC 또는 SRT 파일을 놓으면 가사가 표시됩니다." : "음악이 시작되면\n이곳에 가사가 흐릅니다.";
    public string LibraryCount => $"{FilteredItems.Cast<object>().Count()} / {Items.Count} 곡";
    public string Search
    {
        get => _search;
        set { if (Set(ref _search, value)) { FilteredItems.Refresh(); Changed(nameof(LibraryCount)); } }
    }
    public int Volume
    {
        get => _volume;
        set { if (Set(ref _volume, Math.Clamp(value, 0, 100)) && Player is { } player) player.Volume = _volume; }
    }
    public double Rate
    {
        get => _rate;
        set
        {
            if (!Rates.Contains(value) || _rate == value) return;
            if (Player is { } player && player.SetRate((float)value) != 0)
            { Status = "이 미디어는 재생속도 변경을 지원하지 않습니다."; Changed(nameof(Rate)); return; }
            Set(ref _rate, value);
        }
    }
    public double SyncOffsetMs
    {
        get => _offset;
        set
        {
            if (!double.IsFinite(value)) return;
            if (Set(ref _offset, Math.Clamp(value, -10000, 10000))) { _timeline.SyncOffsetMs = _offset; Tick(null, EventArgs.Empty); }
        }
    }
    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand RevealCommand { get; }
    public ICommand SeekLyricCommand { get; }
    public event Action? SeekCompleted;
    public event Action? AudioReset;

    private void ResetAudio()
    {
        AudioReset?.Invoke();
    }

    public MainViewModel(Dispatcher dispatcher, string? libraryFolder = null,
        Func<LibVLC, string, CancellationToken, Task<MusicReactionTimeline>>? analyze = null, PlayerSettings? settings = null)
    {
        _dispatcher = dispatcher;
        _libraryFolder = libraryFolder;
        _analyze = analyze ?? MusicReactionService.AnalyzeAsync;
        if (settings is not null)
        {
            _volume = settings.Volume;
            _offset = settings.SyncOffsetMs;
            _reactionEnabled = settings.MusicReactionEnabled;
        }
        FilteredItems = CollectionViewSource.GetDefaultView(Items);
        FilteredItems.Filter = item => item is PlaylistItem entry && entry.Title.Contains(Search, StringComparison.OrdinalIgnoreCase);
        Items.CollectionChanged += (_, _) => Changed(nameof(LibraryCount));
        _clock = new DispatcherTimer(DispatcherPriority.Render, dispatcher) { Interval = TimeSpan.FromMilliseconds(20) };
        _clock.Tick += Tick;
        PlayPauseCommand = new RelayCommand(_ => TogglePlayback(), _ => Player is not null && !IsLoading && (HasMedia || CurrentPath is not null || Items.Count > 0));
        StopCommand = new RelayCommand(async _ => await RunAsync(StopAsync), _ => HasMedia || IsLoading);
        NextCommand = new RelayCommand(async _ => await RunAsync(() => NavigateAsync(1)), _ => CanNavigate(1));
        PreviousCommand = new RelayCommand(async _ => await RunAsync(() => NavigateAsync(-1)), _ => CanNavigate(-1));
        RefreshCommand = new RelayCommand(async _ => { if (_playlist is { } playlist) await RunAsync(playlist.RefreshAsync); }, _ => _playlist is not null);
        OpenFolderCommand = new RelayCommand(_ => OpenFolder(), _ => _playlist is not null);
        RevealCommand = new RelayCommand(item => Reveal((item as PlaylistItem)?.Path ?? CurrentPath));
        SeekLyricCommand = new RelayCommand(item => { if (item is LyricLine line) Seek((long)line.StartTime.TotalMilliseconds); });
    }

    public async Task RunAsync(Func<Task> action)
    {
        if (_disposed) return;
        Task task;
        try { task = action(); }
        catch (Exception ex) { Status = ex.Message; return; }
        _pending.Add(task);
        try { await task; }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_disposed) { Status = ex.Message; PlaybackState = "오류"; IsLoading = false; Changed(nameof(LyricsMessage)); }
        }
        finally { _pending.Remove(task); }
    }

    private async Task TrackAsync(Task task)
    {
        _pending.Add(task);
        try { await task; }
        finally { _pending.Remove(task); }
    }

    public Task InitializeAsync(string? initialFile) => TrackAsync(InitializeCoreAsync(initialFile));

    private async Task InitializeCoreAsync(string? initialFile)
    {
        long generation = _generation;
        var playback = await Task.Run(() => new MediaPlaybackService());
        if (_disposed) { await playback.DisposeAsync(); return; }
        _playback = playback;
        Changed(nameof(Player));
        Player!.EndReached += EndReached;
        Player.EncounteredError += PlaybackError;
        Player.Playing += StartedPlaying;
        Player.Volume = Volume;
        _playlist = new PlaylistService(_playback.Engine, _dispatcher, _libraryFolder);
        _playlist.StatusChanged += PlaylistStatus;
        _playlist.Refreshed += PlaylistRefreshed;
        _playlist.StartWatching();
        IsLoading = false;
        Changed(nameof(LyricsMessage));
        Status = "파일을 끌어 놓거나 라이브러리의 곡을 더블클릭하세요.";
        var refresh = _playlist.RefreshAsync();
        if (!_disposed && generation == _generation && !string.IsNullOrEmpty(initialFile)) await PlayFileAsync(initialFile);
        await refresh;
    }

    private void PlaylistStatus(string message) => Status = message;
    private void PlaylistRefreshed()
    {
        if (_playlist is null || _disposed) return;
        var desired = _playlist.Items;
        var paths = desired.Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int i = Items.Count - 1; i >= 0; i--) if (!paths.Contains(Items[i].Path)) Items.RemoveAt(i);
        for (int i = 0; i < desired.Count; i++)
        {
            var item = desired[i];
            if (i >= Items.Count || !ReferenceEquals(Items[i], item))
            {
                int previous = Items.IndexOf(item);
                if (previous < 0) Items.Insert(i, item);
                else Items.Move(previous, i);
            }
            item.IsPlaying = HasMedia && string.Equals(item.Path, _currentPath, StringComparison.OrdinalIgnoreCase);
        }
        Changed(nameof(LibraryCount));
        CommandManager.InvalidateRequerySuggested();
    }

    public Task PlayFileAsync(string path) => TrackAsync(PlayFileCoreAsync(path));

    private async Task PlayFileCoreAsync(string path)
    {
        if (_disposed || _playback is null) return;
        if (!MediaFiles.IsSupported(path)) { Status = "지원 형식: MP4, MP3, WMV, MKV"; return; }
        if (!File.Exists(path)) { Status = "파일을 찾을 수 없습니다. 이동 또는 삭제되었는지 확인해 주세요."; return; }
        _load?.Cancel();
        _analysis?.Cancel();
        using var load = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _load = load;
        long generation = ++_generation;
        IsLoading = true;
        HasMedia = false;
        IsPlaying = false;
        HasVideo = false;
        PlaybackState = "미디어 준비 중";
        ReactionState = ReactionStatus.Unavailable;
        _clock.Stop();
        _musicReaction = null;
        Changed(nameof(MusicReactionReady));
        _positionClock.Reset(0, Environment.TickCount64);
        ResetAudio();
        ActiveIndex = -1;
        _timeline = new LyricTimeline([]) { SyncOffsetMs = SyncOffsetMs };
        Changed(nameof(Lines));
        Changed(nameof(LyricsMessage));
        try
        {
            await _playback.PlayAsync(Path.GetFullPath(path), load.Token);
            load.Token.ThrowIfCancellationRequested();
            if (generation != _generation || _disposed) return;
            _currentPath = Path.GetFullPath(path);
            Title = Path.GetFileNameWithoutExtension(path);
            HasMedia = true;
            IsLoading = false;
            _clock.Start();
            Position = 0;
            Length = 0;
            Player!.Volume = Volume;
            Player.SetRate((float)Rate);
            PlaylistRefreshed();
            LyricTimeline timeline;
            try { timeline = await LyricsSynchronizationService.LoadAsync(path, load.Token); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { Status = $"미디어는 재생 중입니다. 가사를 읽을 수 없습니다: {ex.Message}"; return; }
            load.Token.ThrowIfCancellationRequested();
            if (generation != _generation || _disposed) return;
            _timeline = timeline;
            _timeline.SyncOffsetMs = SyncOffsetMs;
            Changed(nameof(Lines));
            Status = Lines.Count == 0 ? "가사 파일이 없습니다. 미디어는 계속 재생됩니다." : "가사를 클릭해 이동 · 수동 스크롤 후 3초 뒤 자동 복귀";
            if (MusicReactionEnabled && Lines.Count > 0) _ = TrackAsync(AnalyzeReactionAsync(path, generation));
        }
        catch (OperationCanceledException) when (load.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (generation != _generation || _disposed) return;
            HasMedia = IsPlaying = HasVideo = false;
            PlaybackState = "재생 오류";
            Status = $"미디어를 열 수 없습니다: {ex.Message}";
            _clock.Stop();
        }
        finally
        {
            if (generation == _generation && !_disposed)
            {
                _load = null;
                IsLoading = false;
                Changed(nameof(LyricsMessage));
                Tick(null, EventArgs.Empty);
                SeekCompleted?.Invoke();
            }
        }
    }

    private async Task AnalyzeReactionAsync(string path, long generation)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _analysis = cancellation;
        ReactionState = ReactionStatus.Analyzing;
        bool Current() => !_disposed && !cancellation.IsCancellationRequested && generation == _generation &&
            string.Equals(_currentPath, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
        try
        {
            var key = MusicReactionCache.Identify(path);
            var reaction = _reactionCache.Find(key) ?? await _analyze(_playback!.Engine, path, cancellation.Token);
            if (!Current()) return;
            if (MusicReactionCache.Identify(path) != key)
                throw new IOException("분석 중 파일이 변경되었습니다. 다시 재생하면 재분석합니다.");
            _reactionCache.Store(key, reaction);
            _musicReaction = reaction;
            _reactionReadyAt = Environment.TickCount64;
            ReactionState = MusicReactionReady ? ReactionStatus.Ready : ReactionStatus.Unavailable;
            Changed(nameof(MusicReactionReady));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (Current())
            {
                ReactionState = ReactionStatus.Unavailable;
                Status = $"재생과 가사는 계속됩니다. 음악 반응을 분석하지 못했습니다: {ex.Message}";
            }
        }
        finally { if (ReferenceEquals(_analysis, cancellation)) _analysis = null; }
    }

    public void TogglePlayback()
    {
        if (_disposed || Player is null || IsLoading) return;
        if (HasMedia)
        {
            if (Player.IsPlaying) Player.Pause();
            else if (Player.State == VLCState.Ended) { _ = RunAsync(() => PlayFileAsync(CurrentPath!)); return; }
            else Player.Play();
            Tick(null, EventArgs.Empty);
        }
        else _ = RunAsync(() => CurrentPath is { } path && File.Exists(path) ? PlayFileAsync(path) : NavigateAsync(1));
    }

    public Task StopAsync() => TrackAsync(StopCoreAsync());

    private async Task StopCoreAsync()
    {
        long generation = ++_generation;
        _load?.Cancel();
        _analysis?.Cancel();
        _load = null;
        if (_playback is not null) await _playback.StopAsync();
        if (_disposed || generation != _generation) return;
        _clock.Stop();
        _musicReaction = null;
        Changed(nameof(MusicReactionReady));
        _positionClock.Reset(0, Environment.TickCount64);
        ResetAudio();
        HasMedia = false;
        HasVideo = false;
        IsLoading = false;
        IsPlaying = false;
        Position = 0;
        Length = 0;
        ActiveIndex = -1;
        _timeline = new LyricTimeline([]) { SyncOffsetMs = SyncOffsetMs };
        ReactionState = ReactionStatus.Unavailable;
        Changed(nameof(Lines));
        Changed(nameof(LyricsMessage));
        PlaybackState = "정지됨";
        PlaylistRefreshed();
    }

    private bool CanNavigate(int direction) => Player is not null && !_disposed &&
        MediaFiles.AdjacentIndex(Items.Select(x => x.Path).ToArray(), CurrentPath, direction) >= 0;
    public async Task NavigateAsync(int direction)
    {
        int index = MediaFiles.AdjacentIndex(Items.Select(x => x.Path).ToArray(), CurrentPath, direction);
        if (index >= 0) await PlayFileAsync(Items[index].Path);
    }

    public void Seek(long milliseconds)
    {
        if (_disposed || !HasMedia || IsLoading || Player is not { IsSeekable: true } player || Length <= 0) return;
        long target = Math.Clamp(milliseconds, 0, Length);
        _positionClock.Seek(target, player.Time, Environment.TickCount64);
        player.Time = target;
        ResetAudio();
        SeekCompleted?.Invoke();
    }

    private void Tick(object? sender, EventArgs args)
    {
        if (_disposed || !HasMedia || Player is not { } player) return;
        var state = player.State;
        IsPlaying = state == VLCState.Playing;
        var interval = TimeSpan.FromMilliseconds(IsPlaying ? 20 : 50);
        if (_clock.Interval != interval) _clock.Interval = interval;
        HasVideo = HasMedia && player.VoutCount > 0;
        if (state is VLCState.Playing or VLCState.Paused or VLCState.Buffering)
        {
            Length = Math.Max(0, player.Length);
            long now = Environment.TickCount64;
            double position = _positionClock.Read(player.Time, now, IsPlaying, player.Rate);
            if (!IsUserSeeking) Position = (long)Math.Clamp(position, 0, Math.Max(0, Length));
            ActiveIndex = _timeline.FindCurrentIndex(Position);
            PlaybackState = state == VLCState.Paused ? "일시정지" : state == VLCState.Buffering ? "버퍼링" : "재생 중";
        }
        else if (state == VLCState.Ended) PlaybackState = "재생 완료";
    }

    private void EndReached(object? sender, EventArgs args)
    {
        long generation = _generation;
        if (_lifetime.IsCancellationRequested || _dispatcher.HasShutdownStarted) return;
        _dispatcher.BeginInvoke(async () =>
        {
            if (!_disposed && generation == _generation && Player?.State == VLCState.Ended)
                await RunAsync(() => NavigateAsync(1));
        });
    }
    private void PlaybackError(object? sender, EventArgs args)
    {
        long generation = _generation;
        if (_lifetime.IsCancellationRequested || _dispatcher.HasShutdownStarted) return;
        _dispatcher.BeginInvoke(() =>
        {
            // VLC may already report Ended when its queued error notification reaches the UI.
            if (!_disposed && generation == _generation && Player?.State is VLCState.Error or VLCState.Ended)
            {
                ++_generation;
                _load?.Cancel();
                _load = null;
                _analysis?.Cancel();
                _clock.Stop();
                IsLoading = false;
                Status = "재생에 실패했습니다. 파일의 코덱, 접근 권한 또는 손상 여부를 확인해 주세요.";
                PlaybackState = "재생 오류";
                IsPlaying = false;
                HasMedia = HasVideo = false;
                _musicReaction = null;
                ReactionState = ReactionStatus.Unavailable;
                ActiveIndex = -1;
                _timeline = new LyricTimeline([]) { SyncOffsetMs = SyncOffsetMs };
                Changed(nameof(Lines));
                Changed(nameof(MusicReactionReady));
                Changed(nameof(LyricsMessage));
            }
        });
    }

    private void StartedPlaying(object? sender, EventArgs args)
    {
        if (_lifetime.IsCancellationRequested || _dispatcher.HasShutdownStarted) return;
        _dispatcher.BeginInvoke(() =>
        {
            if (_disposed || Player is not { } player) return;
            player.SetSpu(-1);
            player.Volume = Volume;
            player.SetRate((float)Rate);
        });
    }

    private void OpenFolder()
    {
        try { if (_playlist is not null) Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { _playlist.Folder } }); }
        catch (Exception ex) { Status = $"폴더를 열 수 없습니다: {ex.Message}"; }
    }
    private void Reveal(string? path)
    {
        if (path is null || !File.Exists(path)) { Status = "파일을 찾을 수 없습니다."; return; }
        try { Process.Start(new ProcessStartInfo("explorer.exe") { Arguments = $"/select,\"{path}\"" }); }
        catch (Exception ex) { Status = $"파일 위치를 열 수 없습니다: {ex.Message}"; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _load?.Cancel();
        _analysis?.Cancel();
        _clock.Stop();
        _clock.Tick -= Tick;
        _musicReaction = null;
        IsPlaying = false;
        ResetAudio();
        if (Player is { } player) { player.EndReached -= EndReached; player.EncounteredError -= PlaybackError; player.Playing -= StartedPlaying; }
        if (_playlist is not null)
        {
            _playlist.StatusChanged -= PlaylistStatus;
            _playlist.Refreshed -= PlaylistRefreshed;
            await _playlist.DisposeAsync();
        }
        try { await Task.WhenAll(_pending.ToArray()); }
        catch (Exception ex) { Trace.WriteLine($"Completed session operation during shutdown: {ex.Message}"); }
        if (_playback is not null) await _playback.DisposeAsync();
        _lifetime.Dispose();
    }
}
