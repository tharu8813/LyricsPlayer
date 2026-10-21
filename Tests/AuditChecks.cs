using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.Services;
using LyricsPlayer.ViewModels;
using LyricsPlayer.Views;

internal static partial class Program
{
    private static async Task FileAction(Action action)
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            try { action(); return; }
            catch (IOException) when (elapsed.ElapsedMilliseconds < 5000) { await Task.Delay(50); }
        }
    }
    private static void AuditPureChecks()
    {
        Check(LyricsPlayer.Parsers.SrtParser.Parse("1\n2147483647:00:00,000 --> 2147483647:00:01,000\ninvalid").Count == 0,
            "overflowing SRT timestamp is rejected without failing playback");
        Check(LyricsPlayer.Parsers.SrtParser.Parse("1\n00:00:02,000 --> 00:00:01,000\nbackward").Count == 0,
            "backward SRT interval is rejected");
        var clock = new PlaybackClock();
        clock.Reset(1000, 0, true);
        clock.Seek(5000, 1000, 50);
        Check(clock.Read(1000, 500, true, 1) == 1050, "unacknowledged seek does not advance lyrics to the requested target");
        Check(clock.Read(5000, 600, true, 1) == 5000, "native seek acknowledgement reanchors clock");
        clock.Seek(0, 5000, 700);
        Check(clock.Read(0, 710, true, 1) == 0, "backward seek acknowledges native zero");
        clock.Reset(2000, 0, true);
        clock.Seek(9000, 2000, 10);
        Check(clock.Read(2300, 1600, true, 1) == 2300, "failed seek falls back to observed native position");
        Check(double.IsFinite(clock.Read(2300, 1700, true, double.NaN)), "nonfinite playback rate cannot poison the clock");
        clock.Reset(0, 0, true);
        Check(clock.Read(0, 500, true, 1) == 0, "zero native startup does not predict nonexistent playback");
        clock.Reset(1000, 0, true, 0.5);
        Check(clock.Estimate(200) == 1100 && clock.Estimate(1000) == 1150, "half-speed clock still caps stalled extrapolation");
        Check(clock.Read(900, 210, true, 0.5) >= 1100, "out-of-order native report does not rewind playback");

        var invalid = new MusicReactionTimeline([double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1, double.MaxValue]);
        Check(Enumerable.Range(0, 5).All(i => double.IsFinite(invalid.LevelAt(i * 1000d / 30))), "invalid energies yield finite envelope values");
        Check(new MusicReactionTimeline([]).LevelAt(0) == 0, "empty reaction is safe");
        Check(new MusicReactionTimeline([0, 0.0001, 0.0005, 0]).LevelAt(40) == 0, "sub -60dB noise is not amplified by normalization");
        var shortBuilder = new MusicReactionTimeline.Builder();
        shortBuilder.Append(new short[] { 1 });
        Check(shortBuilder.Finish().FrameCount == 1, "one-sample PCM remains bounded");

        using (var buffers = new MusicReactionService.PcmBuffers(new MusicReactionTimeline.Builder()))
        {
            foreach (nuint size in new nuint[] { 0, 16 * 1024 * 1024 + 1, nuint.MaxValue })
            {
                bool rejected = false;
                try { buffers.Request(size); } catch (IOException) { rejected = true; }
                Check(rejected, $"invalid native allocation request rejected ({size})");
            }
            var a = buffers.Request(4);
            var b = buffers.Request(8);
            Check(a != b, "overlapping PCM callbacks own separate buffers");
            Marshal.Copy(new short[] { 1, 2 }, 0, a, 2);
            Marshal.Copy(new short[] { 1, 2, 3, 4 }, 0, b, 4);
            buffers.Process(b, 4, 8);
            buffers.Process(a, 2, 4);
            bool mismatch = false;
            try { buffers.Process(IntPtr.Zero, 2, 4); } catch (IOException) { mismatch = true; }
            Check(mismatch, "unowned PCM pointer rejected before Marshal.Copy");
            a = buffers.Request(4);
            mismatch = false;
            try { buffers.Process(a, 3, 6); } catch (IOException) { mismatch = true; }
            Check(mismatch, "PCM size mismatch rejected; outstanding allocation freed on teardown");
        }

        string folder = Path.GetFullPath("artifacts/audit-unit");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "cache-test.bin");
        File.WriteAllText(path, "a");
        var key = MusicReactionCache.Identify(path);
        var cache = new MusicReactionCache();
        var timeline = new MusicReactionTimeline([0, 1]);
        cache.Store(key, timeline);
        Check(ReferenceEquals(cache.Find(key), timeline), "completed reaction cache hit preserves timeline identity");
        File.WriteAllText(path, "longer");
        Check(cache.Find(MusicReactionCache.Identify(path)) is null, "modified file cannot use stale cached reaction");
        var sameSizeKey = MusicReactionCache.Identify(path);
        cache.Store(sameSizeKey, timeline);
        File.SetLastWriteTimeUtc(path, sameSizeKey.ModifiedUtc.AddMinutes(1));
        Check(cache.Find(MusicReactionCache.Identify(path)) is null, "same-size file with changed timestamp invalidates cache");
        Check(cache.Find(key with { AlgorithmVersion = key.AlgorithmVersion + 1 }) is null, "algorithm version invalidates cache");
        for (int i = 0; i < 17; i++) cache.Store(key with { Path = $"file{i}" }, timeline);
        Check(cache.Find(key) is null, "LRU evicts old cache entries");
        var oversized = new MusicReactionTimeline(new double[1024 * 1024 + 1]);
        cache.Store(key, oversized);
        Check(cache.Find(key) is null, "oversized timeline cannot exceed cache byte budget");
        File.Delete(path);

        path = Path.Combine(folder, "settings.json");
        File.WriteAllText(path, "{broken");
        Check(PlayerSettings.Load(path).Volume == 80, "corrupt settings recover to defaults");
        File.WriteAllText(path, "{\"Volume\":999,\"SyncOffsetMs\":-99999,\"FutureField\":true}");
        var settings = PlayerSettings.Load(path);
        Check(settings.Volume == 100 && settings.SyncOffsetMs == -10000, "settings clamp bounds and tolerate future fields");
        settings.MusicReactionEnabled = false;
        settings.Save(path);
        Check(!PlayerSettings.Load(path).MusicReactionEnabled && !File.Exists(path + ".tmp"), "settings atomically replace and roundtrip");
        File.Delete(path);
    }

    private static async Task AuditRuntimeChecks(MainWindow window, MainViewModel main)
    {
        string folder = Path.GetFullPath("artifacts/test-media");
        string path = Path.Combine(folder, "audio-reaction/Reaction.mp3");
        var waiting = new List<TaskCompletionSource<MusicReactionTimeline>>();
        var cancellations = new List<CancellationToken>();
        int calls = 0;
        Task<MusicReactionTimeline> Analyze(LibVLC _, string __, CancellationToken token)
        {
            calls++;
            var result = new TaskCompletionSource<MusicReactionTimeline>(TaskCreationOptions.RunContinuationsAsynchronously);
            waiting.Add(result);
            cancellations.Add(token);
            return result.Task; // Deliberately ignores cancellation to verify generation rejection.
        }
        await main.StopAsync();
        var session = new MainViewModel(window.Dispatcher, folder, Analyze);
        try
        {
            await session.InitializeAsync(null);
            await session.PlayFileAsync(path);
            await Until(() => session.IsPlaying && session.Length > 0, "slow-analysis playback");
            Check(session.ReactionState == ReactionStatus.Analyzing && !session.IsLoading && session.PlayPauseCommand.CanExecute(null) && session.StopCommand.CanExecute(null),
                "pending analysis leaves pause and stop enabled");
            session.TogglePlayback();
            await Until(() => !session.IsPlaying, "pause while analysis is blocked");
            Check(session.Player!.State == VLCState.Paused, "pause works during blocked reaction analysis");
            await session.StopAsync();
            Check(cancellations[0].IsCancellationRequested && session.Lines.Count == 0 && session.ActiveIndex == -1, "stop cancels analysis and hides previous lyrics");
            session.SyncOffsetMs = 400;
            Check(session.ActiveIndex == -1, "offset edit after stop cannot resurrect old lyric highlight");

            await session.PlayFileAsync(Path.Combine(folder, "01 - Evening light.mp4"));
            await Until(() => session.IsPlaying, "navigation analysis fixture");
            Check(session.NextCommand.CanExecute(null), "next enabled during analysis");
            await session.NavigateAsync(1);
            Check(session.CurrentPath == Path.Combine(folder, "02 - Quiet room.mp3") && session.PreviousCommand.CanExecute(null), "next changes track during analysis");
            await session.NavigateAsync(-1);
            Check(session.CurrentPath == Path.Combine(folder, "01 - Evening light.mp4"), "previous changes track during analysis");
            for (int i = 0; i < 20; i++) await session.PlayFileAsync(i % 2 == 0 ? path : Path.Combine(folder, "02 - Quiet room.mp3"));
            int latest = waiting.Count - 1;
            for (int i = 0; i < latest; i++) waiting[i].TrySetResult(new MusicReactionTimeline([0, 1]));
            await Task.Yield();
            Check(session.ReactionState == ReactionStatus.Analyzing && !session.MusicReactionReady, "20 switches reject results from cancelled generations");
            waiting[latest].SetException(new InvalidOperationException("injected analysis failure"));
            await Until(() => session.ReactionState == ReactionStatus.Unavailable, "injected analysis failure");
            await Until(() => session.IsPlaying, "playback survives failed analysis");
            Check(session.HasMedia && session.Lines.Count > 0 && session.PlaybackState != "오류", "analysis failure preserves playback and lyrics");

            session.MusicReactionEnabled = false;
            int before = calls;
            await session.PlayFileAsync(path);
            Check(calls == before && session.ReactionState == ReactionStatus.Unavailable, "disabled reaction skips decoding entirely");
            session.MusicReactionEnabled = true;
            waiting[^1].SetResult(new MusicReactionTimeline([0, 1]));
            await Until(() => session.MusicReactionReady, "cache fill");
            before = calls;
            var replayTime = Stopwatch.StartNew();
            await session.PlayFileAsync(path);
            Console.WriteLine($"SESSION CACHE REPLAY: {replayTime.Elapsed.TotalMilliseconds:F2}ms including stop/play and lyric reload");
            Check(session.MusicReactionReady && calls == before, "replay reuses completed unchanged reaction without decoding");
        }
        finally
        {
            foreach (var task in waiting) task.TrySetCanceled();
            await session.DisposeAsync();
        }

        var pip = window.ShowPip();
        window.WindowState = WindowState.Minimized;
        await Task.Delay(150);
        Check(pip.Owner is null && pip.IsVisible && pip.WindowState != WindowState.Minimized, "PiP stays visible when main window is minimized");
        window.WindowState = WindowState.Normal;
        pip.CloseImmediately();

        var playlist = main.Playlist!;
        var watcherField = typeof(PlaylistService).GetField("_watcher", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var oldWatcher = (FileSystemWatcher)watcherField.GetValue(playlist)!;
        typeof(PlaylistService).GetMethod("WatcherError", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(playlist,
            new object[] { oldWatcher, new ErrorEventArgs(new InternalBufferOverflowException("injected buffer overflow")) });
        await Until(() => !ReferenceEquals(watcherField.GetValue(playlist), oldWatcher), "watcher recovery instance");
        string watched = Path.Combine(folder, "97 - Recovery.mp3");
        try
        {
            File.Copy(Path.Combine(folder, "02 - Quiet room.mp3"), watched, true);
            await Until(() => main.Items.Any(item => item.Path == watched), "watcher receives events after replacement");
            Check(main.Items.Any(item => item.Path == watched), "injected watcher overflow replaces instance and new events continue");
        }
        finally
        {
            await FileAction(() => File.Delete(watched));
            await Until(() => main.Items.Count == 4, "watcher recovery cleanup");
        }

        string longPath = Path.Combine(folder, "audio-reaction", "native-stress.wav");
        await Task.Run(() => WriteWave(longPath, 1800));
        try
        {
            await using var native = await Task.Run(() => new MediaPlaybackService());
            using var process = Process.GetCurrentProcess();
            long beforePrivate = process.PrivateMemorySize64;
            for (int i = 0; i < 20; i++)
            {
                using var cancellation = new CancellationTokenSource();
                var analysis = MusicReactionService.AnalyzeAsync(native.Engine, longPath, cancellation.Token);
                await Task.Delay(50);
                Check(!analysis.IsCompleted, $"native stress {i + 1} cancels a running long decode");
                cancellation.Cancel();
                bool cancelled = false;
                try { await analysis.WaitAsync(TimeSpan.FromSeconds(10)); }
                catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled, $"native stress {i + 1} joins callbacks and tears down decoder");
                if (i % 4 == 0) GC.Collect();
            }
            var recovered = await MusicReactionService.AnalyzeAsync(native.Engine, path, CancellationToken.None);
            Check(recovered.FrameCount > 0, "native analysis still works after 20 in-flight cancellations");
            process.Refresh();
            Console.WriteLine($"NATIVE CANCELLATION SAMPLE: private {beforePrivate} -> {process.PrivateMemorySize64} bytes, managed={GC.GetTotalMemory(false)} bytes, handles={process.HandleCount}; mixed process memory, not a native leak proof");
        }
        finally { File.Delete(longPath); }

        var pending = new TaskCompletionSource<MusicReactionTimeline>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = new MainViewModel(window.Dispatcher, folder, async (_, _, token) => await pending.Task.WaitAsync(token));
        await closing.InitializeAsync(null);
        await closing.PlayFileAsync(path);
        await closing.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Check(closing.Player!.NativeReference == IntPtr.Zero, "shutdown awaits directly invoked playback and cancels analysis before releasing engine");
        var initializing = new MainViewModel(window.Dispatcher, folder);
        Task initialize = initializing.InitializeAsync(path);
        await initializing.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await initialize;
        Check(initializing.Player is null || initializing.Player.NativeReference == IntPtr.Zero, "close during engine initialization releases locally created native objects");

        string broken = Path.Combine(folder, "audio-reaction", "broken-audit.mp3");
        try
        {
            await File.WriteAllBytesAsync(broken, new byte[] { 1, 2, 3, 4 });
            await main.PlayFileAsync(broken);
            try { await Until(() => main.PlaybackState == "재생 오류", "actual native playback failure"); }
            catch { Console.WriteLine($"BROKEN MEDIA DIAGNOSTIC: native={main.Player?.State}, state={main.PlaybackState}, current={main.CurrentPath}, status={main.Status}, loading={main.IsLoading}"); throw; }
            Check(!main.HasMedia && !main.IsPlaying && main.Lines.Count == 0, "native playback failure clears stale media and lyrics");
            await main.PlayFileAsync(Path.Combine(folder, "01 - Evening light.mp4"));
            await Until(() => main.IsPlaying, "recovery after failed media");
            Check(main.HasMedia && main.Lines.Count > 0, "valid playback and clock recover after malformed media");
        }
        finally { await FileAction(() => File.Delete(broken)); }

        await main.PlayFileAsync(Path.Combine(folder, "01 - Evening light.mp4"));
        await Until(() => main.IsPlaying && main.Length > 0, "scroll fixture");
        main.TogglePlayback();
        await Until(() => !main.IsPlaying, "scroll pause");
        var view = (LyricsView)window.FindName("MainLyrics");
        var rows = Find<ListBox>(view).Single();
        Check(VirtualizingPanel.GetScrollUnit(rows) == ScrollUnit.Pixel, "lyrics virtualization explicitly uses physical pixel offsets");
        main.Seek(8000);
        await Until(() => main.ActiveIndex == 4, "center acknowledgement");
        await Task.Delay(500);
        var row = (ListBoxItem)rows.ItemContainerGenerator.ContainerFromIndex(4);
        var scroll = Find<ScrollViewer>(rows).First();
        double centerError = Math.Abs(row.TranslatePoint(new Point(0, row.ActualHeight / 2), scroll).Y - scroll.ViewportHeight / 2);
        Check(centerError < 4, $"lyric center uses pixel coordinates (error={centerError:F2}px)");
        typeof(LyricsView).GetMethod("ApplyMotionPreference", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { false });
        Check(Find<ListBoxItem>(rows).All(item =>
            Math.Abs(((ScaleTransform)((Border)item.Template.FindName("Role", item)).RenderTransform).ScaleX - (item.IsSelected ? 1.08 : 1)) < 0.0001),
            "reduced-motion branch sets final emphasis without changing Windows preferences");
        window.Width = 1000;
        await Task.Delay(100);
        Check(((FrameworkElement)window.FindName("LibraryPanel")).Visibility == Visibility.Collapsed, "narrow window gives space to lyrics by collapsing library");
        window.Width = 1360;
        await Task.Delay(100);
        SaveVisual(window, "artifacts/audit-window.png");
        await main.StopAsync();
    }

    private static void SaveVisual(FrameworkElement element, string path)
    {
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
        png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }
}
