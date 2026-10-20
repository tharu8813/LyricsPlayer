using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.Parsers;
using LyricsPlayer.Services;
using LyricsPlayer.ViewModels;
using LyricsPlayer.Views;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine($"PASS {++_checks:00} {name}");
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            PureChecks();
            if (args.Contains("--runtime")) return RuntimeChecks(args.Contains("--hold"));
            Console.WriteLine($"{_checks} checks passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void PureChecks()
    {
        var lrc = LrcParser.Parse("[00:01.00][00:03.000]first\n[00:01.00]translation\n[01:02:03.125]hour\n[ar:metadata]\ninvalid");
        Check(lrc.Count == 3, "LRC multiple timestamps and invalid metadata");
        Check(lrc[0].Text == "first / translation", "LRC duplicate timestamp collapse");
        Check(lrc[2].StartTime == TimeSpan.FromMilliseconds(3723125), "LRC hours and fractional seconds");
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new("fr-FR");
            Check(LrcParser.Parse("[00:01.25]decimal")[0].StartTime.TotalMilliseconds == 1250, "LRC invariant decimal culture");
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
        var srt = SrtParser.Parse("1\r\n00:00:01,000 --> 00:00:02,500\r\nfirst\r\nsecond\r\n\r\n00:00:08,000 --> 00:00:09,000\r\nlast");
        Check(srt.Count == 2 && srt[0].Text == "first\nsecond", "SRT multiline and unnumbered cues");
        Check(srt[0].EndTime?.TotalMilliseconds == 2500, "SRT explicit end retained");
        var timeline = LyricTimeline.Parse("[00:01]one\n[00:02]two\n[00:03]\n[00:04]four", false);
        Check(timeline.SyncOffsetMs == 200 && timeline.FindCurrentIndex(1799) == 0 && timeline.FindCurrentIndex(1800) == 1, "200ms offset and exact boundary");
        Check(timeline.FindCurrentIndex(2800) == -1, "blank LRC line clears highlight");
        Check(timeline.FindCurrentIndex(0) == 0, "original pre-roll behavior preserved");
        Check(timeline.Lines[0].EndTime == timeline.Lines[1].StartTime, "LRC end inferred from next start");
        timeline.SyncOffsetMs = -500;
        Check(timeline.FindCurrentIndex(2400) == 0, "negative user offset");
        var gaps = LyricTimeline.Parse("1\n00:00:05,000 --> 00:00:06,000\nfirst\n\n2\n00:00:10,001 --> 00:00:11,000\nnext", true);
        Check(gaps.Lines.Count == 4 && gaps.Lines[0].Text == "..." && gaps.Lines[0].StartTime.TotalMilliseconds == 1000, "initial pause at 1 second");
        Check(gaps.Lines[2].Text == "..." && gaps.Lines[2].StartTime.TotalMilliseconds == 7000, "gap over 3 seconds inserts at end plus 1 second");
        var threshold = LyricTimeline.Parse("1\n00:00:00,000 --> 00:00:01,000\na\n\n2\n00:00:04,000 --> 00:00:05,000\nb", true);
        Check(threshold.Lines.Count == 2, "exactly 3 second gap has no pause");
        Check(new LyricTimeline([]).FindCurrentIndex(1000) == -1, "no lyrics is valid");
        var large = new LyricTimeline(Enumerable.Range(0, 100000).Select(i => new LyricsPlayer.Models.LyricLine(TimeSpan.FromMilliseconds(i * 500), $"line {i}")).ToArray());
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
            if (large.FindCurrentIndex(i * 500 - 200) != i) throw new InvalidOperationException("binary search regression");
        Check(watch.ElapsedMilliseconds < 2000, $"100000 timeline lookups ({watch.ElapsedMilliseconds}ms)");
        Check(MediaFiles.IsSupported("SONG.MP4") && MediaFiles.IsSupported("a.mp3") && MediaFiles.IsSupported("b.wmv") && MediaFiles.IsSupported("c.mkv") && !MediaFiles.IsSupported("a.exe"), "format validation case insensitive");
        string[] paths = ["a.mp4", "b.mp3", "c.mkv"];
        Check(MediaFiles.AdjacentIndex(paths, "b.mp3", 1) == 2 && MediaFiles.AdjacentIndex(paths, "B.MP3", -1) == 0, "navigation uses current path, case insensitive");
        Check(MediaFiles.AdjacentIndex(paths, "c.mkv", 1) == -1 && MediaFiles.AdjacentIndex(paths, "a.mp4", -1) == -1, "playlist boundaries never wrap");
        Check(MediaFiles.FormatTime(3723000) == "1:02:03", "duration over one hour");
    }

    private static int RuntimeChecks(bool hold)
    {
        int result = 1;
        var app = new LyricsPlayer.App();
        app.InitializeComponent();
        var window = new MainWindow(libraryFolder: Path.GetFullPath("artifacts/test-media"));
        app.MainWindow = window;
        window.Loaded += async (_, _) =>
        {
            try
            {
                var session = (MainViewModel)window.DataContext;
                await Until(() => !session.IsLoading && session.Player is not null, "engine initialization");
                Check(window.VideoOutputHandle != IntPtr.Zero, "WPF VideoView owns native output HWND");
                var handle = window.VideoOutputHandle;
                for (int i = 0; i < 3; i++) { window.SetFullscreen(true); await Task.Delay(120); window.SetFullscreen(false); await Task.Delay(120); }
                Check(window.VideoOutputHandle == handle, "fullscreen prototype retains one native output handle");
                var pip = window.ShowPip();
                Check(ReferenceEquals(pip.DataContext, session) && pip.Topmost, "PiP shares session and stays on top");
                pip.CloseImmediately();
                await PlaybackChecks(window, session);
                if (hold)
                {
                    await session.PlayFileAsync(Path.GetFullPath("artifacts/test-media/01 - Evening light.mp4"));
                    Console.WriteLine("HOLD: ready for visual inspection for 60 seconds");
                    await Task.Delay(60000);
                }
                Console.WriteLine($"{_checks} checks passed.");
                result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { window.Close(); }
        };
        app.Run(window);
        Check(((MainViewModel)window.DataContext).Player?.NativeReference == IntPtr.Zero, "native player released after window shutdown");
        return result;
    }

    private static async Task PlaybackChecks(MainWindow window, MainViewModel session)
    {
        string folder = Path.GetFullPath("artifacts/test-media");
        string mp4 = Path.Combine(folder, "01 - Evening light.mp4");
        string mp3 = Path.Combine(folder, "02 - Quiet room.mp3");
        string mkv = Path.Combine(folder, "03 - Window seat.mkv");
        string wmv = Path.Combine(folder, "04 - After hours.wmv");
        await Until(() => session.Items.Count == 4 && session.Items.All(x => x.Duration != "--:--"), "asynchronous durations");
        Check(session.Items.Count == 4, "all four media formats discovered with durations");
        var retained = session.Items[0];
        await session.Playlist!.RefreshAsync();
        Check(ReferenceEquals(retained, session.Items[0]), "refresh retains playlist item identity");
        await session.PlayFileAsync(mp4);
        await Until(() => session.IsPlaying && session.HasVideo && session.Length > 0, "MP4 video playback");
        Check(session.Lines.Count == 9 && session.Length > 17000, "MP4 output, duration and same-name LRC loaded");
        await Until(() => DisplayedFrames(session) > 2, "decoded video frames");
        Check(DisplayedFrames(session) > 2, "LibVLC reports displayed video frames");
        session.TogglePlayback();
        await Until(() => session.Player!.State == VLCState.Paused, "pause");
        session.Volume = 37;
        session.Rate = 1.25;
        await Until(() => session.Player!.Volume == 37 && Math.Abs(session.Player.Rate - 1.25) < 0.01, "native volume and rate propagation");
        Check(session.Player!.Volume == 37 && Math.Abs(session.Player.Rate - 1.25) < 0.01, "volume and playback rate reach engine");
        session.SeekLyricCommand.Execute(session.Lines[2]);
        Check(session.Position == 4000 && session.ActiveIndex == 2, "lyric click seeks unadjusted timestamp immediately");
        await Task.Delay(350);
        long pausedTime = session.Player.Time;
        var output = window.VideoOutputHandle;
        for (int i = 0; i < 5; i++) { window.SetFullscreen(true); await Task.Delay(80); window.SetFullscreen(false); await Task.Delay(80); }
        Check(window.VideoOutputHandle == output && session.Player.State == VLCState.Paused && Math.Abs(session.Player.Time - pausedTime) < 300,
            "five fullscreen roundtrips retain output, paused state and time");
        window.SetFullscreen(true);
        try { await Until(() => window.ControlsHidden, "fullscreen idle timer", 8000); }
        catch (TimeoutException)
        {
            Console.WriteLine($"IDLE DIAGNOSTIC: fullscreen={window.IsFullscreen}, seeking={((PlaybackControlBar)window.FindName("Controls")).IsSeeking}, leftButton={Mouse.LeftButton}");
            throw;
        }
        Check(window.ControlsHidden, "fullscreen controls hide after idle");
        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, Environment.TickCount, Key.F10) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Check(!window.ControlsHidden, "fullscreen activity restores controls");
        var key = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        window.RaiseEvent(key);
        Check(!window.IsFullscreen, "Escape returns from fullscreen");
        for (int i = 0; i < 5; i++)
        {
            var pip = window.ShowPip();
            await Task.Delay(80);
            Check(Find<ListBox>(pip).Single().SelectedIndex == session.ActiveIndex, $"PiP cycle {i + 1} follows shared active line");
            pip.CloseImmediately();
        }
        var lyrics = (LyricsView)window.FindName("MainLyrics");
        var rows = Find<ListBox>(lyrics).Single();
        rows.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
        Check(!lyrics.IsFollowing, "manual wheel interrupts automatic centering");
        await Task.Delay(3200);
        Check(lyrics.IsFollowing, "manual scrolling returns to follow after three seconds");
        session.Seek(8000);
        session.Seek(12000);
        session.Seek(2000);
        Check(session.ActiveIndex == 1 && lyrics.IsFollowing, "rapid seeking immediately resynchronizes without queued animations");
        window.Width = 1000;
        window.Height = 620;
        await Task.Delay(150);
        Check(window.ActualWidth >= 1000 && rows.ActualWidth >= 200, "minimum window size retains readable lyrics layout");
        window.Width = 1360;
        window.Height = 860;
        await session.PlayFileAsync(mp3);
        await Until(() => session.IsPlaying && session.Length > 0, "MP3 audio playback");
        Check(session.Lines.Count == 4 && session.Lines[0].Text.Contains('\n') && session.Lines[1].Text == "...", "SRT multiline and long-pause state shared in playback");
        await session.PlayFileAsync(mkv);
        await Until(() => session.IsPlaying && session.HasVideo && session.Length > 0, "MKV video playback");
        await Until(() => DisplayedFrames(session) > 2, "MKV displayed frames");
        Check(session.Lines.Count == 0, "media without lyrics plays normally");
        await session.PlayFileAsync(wmv);
        await Until(() => session.IsPlaying && session.HasVideo && session.Length > 0, "WMV video playback");
        await Until(() => DisplayedFrames(session) > 2, "WMV displayed frames");
        Check(session.IsPlaying, "WMV video and audio playback");

        string watched = Path.Combine(folder, "99 - Watcher test.mp3");
        string renamed = Path.Combine(folder, "99 - Renamed test.MP3");
        try
        {
            File.Copy(mp3, watched, true);
            await Until(() => session.Items.Any(x => x.Path == watched), "watch create");
            File.Move(watched, renamed, true);
            await Until(() => session.Items.Any(x => x.Path == renamed) && session.Items.All(x => x.Path != watched), "watch rename");
            File.Delete(renamed);
            await Until(() => session.Items.Count == 4, "watch delete");
            Check(session.Items.Count == 4, "debounced watcher detects create, rename and removal including uppercase extension");
        }
        finally { File.Delete(watched); File.Delete(renamed); }
        session.Search = "Evening";
        Check(session.FilteredItems.Cast<object>().Count() == 1, "case insensitive filename search");
        await session.PlayFileAsync(mp4);
        await Until(() => session.Length > 0 && session.IsPlaying, "filtered playback");
        await session.NavigateAsync(1);
        await Until(() => session.CurrentPath == mp3 && session.IsPlaying, "filtered next");
        Check(session.CurrentPath == mp3, "next follows full library while filter remains active");
        await session.PlayFileAsync(mp4);
        await Until(() => session.Length > 0 && session.IsPlaying, "auto-next source");
        session.Seek(session.Length - 500);
        await Until(() => session.CurrentPath == mp3 && session.IsPlaying, "automatic next", 10000);
        Check(session.CurrentPath == mp3 && session.Search == "Evening", "automatic next follows full library without changing filter");
        session.Search = "";
        await session.PlayFileAsync(wmv);
        await Until(() => session.Length > 0 && session.IsPlaying, "last item source");
        session.Seek(session.Length - 500);
        await Until(() => session.Player.State == VLCState.Ended, "last item completion");
        await Task.Delay(200);
        Check(session.CurrentPath == wmv && !session.NextCommand.CanExecute(null), "last item ends without wrapping");
        await session.RunAsync(() => Task.WhenAll(session.PlayFileAsync(mp4), session.PlayFileAsync(mp3)));
        await Until(() => session.CurrentPath == mp3 && session.IsPlaying && !session.IsLoading, "rapid media switching");
        Check(session.Lines[0].Text.Contains("두 번째"), "latest media switch wins over stale lyric load");
        string before = session.CurrentPath!;
        await session.PlayFileAsync(Path.Combine(folder, "missing.mp4"));
        Check(session.CurrentPath == before && session.Status.Contains("찾을 수"), "missing file leaves current session intact with inline error");
        await session.PlayFileAsync("unsupported.txt");
        Check(session.CurrentPath == before && session.Status.Contains("지원 형식"), "unsupported format rejected without replacing current media");
        await session.StopAsync();
        Check(!session.HasMedia && !session.IsPlaying && session.Position == 0 && session.ActiveIndex == -1, "stop resets transport and lyric highlight");
        session.TogglePlayback();
        await Until(() => session.HasMedia && session.IsPlaying && !session.IsLoading, "replay stopped media");
        Check(session.CurrentPath == before, "play after stop restarts the same media");
        await LargeLyricsCheck(window, session, mp4, folder);
    }

    private static int DisplayedFrames(MainViewModel session)
    {
        using var media = session.Player!.Media;
        return media?.Statistics.DisplayedPictures ?? 0;
    }

    private static async Task LargeLyricsCheck(MainWindow window, MainViewModel session, string source, string folder)
    {
        string path = Path.Combine(folder, "98 - Large lyrics.mp4");
        string lrc = Path.ChangeExtension(path, ".lrc");
        File.Copy(source, path, true);
        await File.WriteAllLinesAsync(lrc, Enumerable.Range(0, 10000).Select(i => $"[00:{i / 1000:00}.{i % 1000:000}]가사 {i} — a line for virtualization"));
        try
        {
            await session.PlayFileAsync(path);
            await Until(() => session.IsPlaying && session.Length > 0, "large lyric playback");
            session.Seek(4000);
            await Task.Delay(500);
            var view = (LyricsView)window.FindName("MainLyrics");
            var rows = Find<ListBox>(view).Single();
            int realized = Find<ListBoxItem>(rows).Count();
            Check(session.Lines.Count == 10000 && realized < 100, $"10000 lyric rows remain virtualized ({realized} realized)");
            var frames = new List<double>();
            var stopwatch = Stopwatch.StartNew();
            double last = 0;
            TimeSpan previousRender = TimeSpan.MinValue;
            EventHandler render = (_, args) =>
            {
                var time = ((RenderingEventArgs)args).RenderingTime;
                if (time == previousRender) return;
                previousRender = time;
                double now = stopwatch.Elapsed.TotalMilliseconds;
                if (last != 0) frames.Add(now - last);
                last = now;
            };
            CompositionTarget.Rendering += render;
            try
            {
                for (int i = 0; i < 12; i++) { session.Seek(2000 + i * 300); await Task.Delay(100); }
            }
            finally { CompositionTarget.Rendering -= render; }
            frames.Sort();
            Console.WriteLine($"RENDER SAMPLE: frames={frames.Count}, median={frames.ElementAtOrDefault(frames.Count / 2):F1}ms, p95={frames.ElementAtOrDefault((int)(frames.Count * .95)):F1}ms (not a GPU FPS guarantee)");
            Check(frames.Count > 5, "UI continues rendering while seeking through large lyrics and video");
            await session.PlayFileAsync(source);
        }
        finally { File.Delete(path); File.Delete(lrc); }
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Find<T>(child)) yield return nested;
        }
    }

    private static async Task Until(Func<bool> condition, string name, int timeout = 10000)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.ElapsedMilliseconds > timeout) throw new TimeoutException(name);
            await Task.Delay(50);
        }
    }
}
