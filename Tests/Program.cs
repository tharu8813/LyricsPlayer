using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.Parsers;
using LyricsPlayer.Services;
using LyricsPlayer.ViewModels;
using LyricsPlayer.Views;

internal static partial class Program
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
            if (args.Contains("--published-exe")) return PublishedExeCheck();
            if (args.Contains("--benchmark")) return BenchmarkChecks();
            if (args.Contains("--runtime") || args.Contains("--audio-runtime"))
                return RuntimeChecks(args.Contains("--hold"), args.Contains("--audio-runtime"));
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
        using var reference = System.Text.Json.JsonDocument.Parse(File.ReadAllText("Tests/Fixtures/canvas-reaction-reference.json"));
        var step = new double[60];
        Array.Fill(step, 1, 10, 5);
        var envelope = new MusicReactionTimeline(step);
        double Difference(string name, MusicReactionTimeline timeline) => reference.RootElement.GetProperty(name).EnumerateArray()
            .Select((value, index) => Math.Abs(value.GetDouble() - timeline.LevelAt(index * 1000d / 30))).Max();
        Check(Difference("step", envelope) < 1e-6, "attack/release matches Playlist Canvas numerical reference");
        short[] pcm = new short[32000];
        for (int i = 0; i < pcm.Length; i++)
        {
            double time = i / 8000d;
            double bass = time >= 0.5 && time < 2 ? 12000 * Math.Exp(-15 * ((time - 0.5) % 0.5)) * Math.Sin(2 * Math.PI * 65 * time) : 0;
            double treble = time >= 2 && time < 2.5 ? 10000 * Math.Sin(2 * Math.PI * 1000 * time) : 0;
            pcm[i] = (short)(bass + treble);
        }
        var builder = new MusicReactionTimeline.Builder();
        for (int i = 0; i < pcm.Length; i += 137) builder.Append(pcm.AsSpan(i, Math.Min(137, pcm.Length - i)));
        var spectrum = builder.Finish();
        double error = Difference("pcm", spectrum);
        Check(spectrum.FrameCount == 120 && error < 0.0001, $"streaming low-band DFT matches Playlist Canvas FFT (max error={error:E2})");
        Check(spectrum.LevelAt(-100) == 0 && spectrum.LevelAt(10000) == 0 && spectrum.LevelAt(double.NaN) == 0, "reaction time boundaries remain safe");
        Check(new MusicReactionTimeline(new double[60]).LevelAt(500) == 0, "silent track remains still after normalization");
        Check(envelope.LevelAt(400) > 0.9 && envelope.LevelAt(700) < envelope.LevelAt(500), "bass rises promptly and decays gradually");
        var clock = new PlaybackClock();
        Check(clock.Read(1000, 0, true, 1) == 1000 && clock.Read(1000, 120, true, 1) == 1120,
            "playback advances between repeated VLC time values");
        Check(clock.Read(1250, 250, true, 1) == 1250, "fresh VLC time reanchors interpolation");
        clock.Read(1250, 300, false, 1);
        Check(clock.Estimate(900) == 1300, "paused playback clock stays fixed");
        clock.Read(1250, 1000, true, 1);
        Check(clock.Estimate(1100) == 1400, "resume does not include paused wall-clock time");
        clock.Reset(2000, 0, true);
        clock.Read(2000, 100, true, 2);
        Check(clock.Estimate(200) == 2300, "playback rate changes preserve continuity");
        clock.Reset(5000, 0, true);
        Check(clock.Read(1000, 100, true, 1, true) == 5100, "seek advances without adopting stale native time");
        Check(clock.Estimate(5000) == 5300, "stalled native input cannot extrapolate indefinitely");
        clock.Reset(1000, 0, true);
        Check(clock.Read(1200, 250, true, 1) == 1250 && clock.Estimate(350) == 1330,
            "small native clock corrections settle gradually instead of rewinding lyrics");
        AuditPureChecks();
    }
    private static int RuntimeChecks(bool hold, bool audioOnly)
    {
        int result = 1;
        foreach (string name in new[] { "99 - Watcher test.mp3", "99 - Renamed test.MP3", "97 - Recovery.mp3", "98 - Large lyrics.mp4", "98 - Large lyrics.lrc" })
            File.Delete(Path.Combine("artifacts/test-media", name));
        var app = new LyricsPlayer.App();
        app.InitializeComponent();
        var window = new MainWindow(libraryFolder: Path.GetFullPath("artifacts/test-media"));
        window.Title = "LyricsPlayer · 자동 검사 (일시적 창)";
        app.MainWindow = window;
        window.Loaded += async (_, _) =>
        {
            try
            {
                var session = (MainViewModel)window.DataContext;
                await Until(() => !session.IsLoading && session.Player is not null, "engine initialization");
                Check(window.VideoOutputHandle != IntPtr.Zero, "WPF VideoView owns native output HWND");
                if (!audioOnly)
                {
                    var handle = window.VideoOutputHandle;
                    for (int i = 0; i < 3; i++) { window.SetFullscreen(true); await Task.Delay(120); window.SetFullscreen(false); await Task.Delay(120); }
                    Check(window.VideoOutputHandle == handle, "fullscreen prototype retains one native output handle");
                    var pip = window.ShowPip();
                    Check(ReferenceEquals(pip.DataContext, session) && pip.Topmost, "PiP shares session and stays on top");
                    pip.CloseImmediately();
                }
                await AudioChecks(window, session);
                await AnalysisSafetyChecks(window, session);
                await AuditRuntimeChecks(window, session);
                if (!audioOnly) await PlaybackChecks(window, session);
                else await LargeLyricsCheck(window, session, Path.GetFullPath("artifacts/test-media/01 - Evening light.mp4"), Path.GetFullPath("artifacts/test-media"));
                if (hold)
                {
                    await session.PlayFileAsync(Path.GetFullPath("artifacts/test-media/audio-reaction/Rhythm.mp3"));
                    await Task.Delay(1500);
                    var lyrics = (LyricsView)window.FindName("MainLyrics");
                    var preview = new RenderTargetBitmap((int)lyrics.ActualWidth, (int)lyrics.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    var canvas = new DrawingVisual();
                    using (var drawing = canvas.RenderOpen())
                    {
                        var bounds = new Rect(0, 0, lyrics.ActualWidth, lyrics.ActualHeight);
                        drawing.DrawRectangle((Brush)lyrics.FindResource("Surface"), null, bounds);
                        drawing.DrawRectangle(new VisualBrush(lyrics), null, bounds);
                    }
                    preview.Render(canvas);
                    var png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(preview));
                    using (var file = File.Create("artifacts/bass-pulse-preview.png")) png.Save(file);
                    Console.WriteLine("HOLD: ready for visual inspection for 60 seconds");
                    await Task.Delay(60000);
                }
                result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { window.Close(); }
        };
        app.Run(window);
        Check(((MainViewModel)window.DataContext).Player?.NativeReference == IntPtr.Zero, "native player released after window shutdown");
        if (result == 0) Console.WriteLine($"{_checks} checks passed, including shutdown.");
        return result;
    }

    private static async Task AudioChecks(MainWindow window, MainViewModel session)
    {
        string path = Path.GetFullPath("artifacts/test-media/audio-reaction/Reaction.mp3");
        session.Volume = 30;
        var elapsed = Stopwatch.StartNew();
        await session.PlayFileAsync(path);
        await Until(() => session.IsPlaying && session.Length > 0, "real playback with shared-engine analysis");
        Console.WriteLine($"PLAYBACK PREPARE: {elapsed.ElapsedMilliseconds}ms (analysis is independent)");
        await Until(() => session.ReactionState != ReactionStatus.Analyzing, "background reaction completion");
        Check(session.MusicReactionReady, "background analysis completes without holding playback preparation");
        await Task.Delay(450);
        var view = (LyricsView)window.FindName("MainLyrics");
        var rows = Find<ListBox>(view).Single();
        session.Seek(1000);
        await Task.Delay(100);
        Check(session.MusicReactionLevel < 0.05, $"treble-only passage stays below 0.625% scale change (level={session.MusicReactionLevel:F5})");
        var pip = window.ShowPip();
        await Task.Delay(100);
        session.Seek(3500);
        await Task.Delay(70);
        var active = (ListBoxItem)rows.ItemContainerGenerator.ContainerFromIndex(session.ActiveIndex);
        var pulse = (Border)active.Template.FindName("Pulse", active);
        var scale = (ScaleTransform)pulse.RenderTransform;
        Check(active.ActualWidth >= pulse.ActualWidth * 1.215, "row reserves space for current emphasis and music scaling");
        Check(!SystemParameters.ClientAreaAnimation || scale.ScaleX > 1.015 && scale.ScaleX <= 1.125,
            $"current lyric follows normalized bass envelope (scale={scale.ScaleX:F3})");
        var pipRows = Find<ListBox>(pip).Single();
        var pipActive = (ListBoxItem)pipRows.ItemContainerGenerator.ContainerFromIndex(session.ActiveIndex);
        var pipScale = (ScaleTransform)((Border)pipActive.Template.FindName("Pulse", pipActive)).RenderTransform;
        Check(!SystemParameters.ClientAreaAnimation || Math.Abs(pipScale.ScaleX - scale.ScaleX) < 0.03,
            "main and PiP sample the same audio-clock envelope");
        Check(!SystemParameters.ClientAreaAnimation || Math.Abs(scale.ScaleX - (1 + session.MusicReactionLevel * MusicReactionTimeline.Strength)) < 0.02,
            "rendered scale follows audio time without a queued pulse");
        session.TogglePlayback();
        await Until(() => !session.IsPlaying, "pause music reaction");
        Check(!view.IsMusicAnimating, "pause removes the rendering subscription");
        await Task.Delay(450);
        Check(Math.Abs(scale.ScaleX - 1) < 0.0001 && Math.Abs(pipScale.ScaleX - 1) < 0.0001,
            "pause gently returns main and PiP to base size");
        pip.CloseImmediately();
        Check(Find<ListBoxItem>(rows).Where(row => !row.IsSelected).All(row =>
            ((ScaleTransform)((Border)row.Template.FindName("Pulse", row)).RenderTransform).ScaleX == 1),
            "previous lyric containers reset their music scale");
        session.TogglePlayback();
        session.Seek(7000);
        await Task.Delay(100);
        Check(session.IsPlaying && session.MusicReactionLevel < 0.01, "seek immediately selects silence instead of replaying old beats");
        await session.StopAsync();
        Check(!session.MusicReactionReady && !view.IsMusicAnimating, "stop clears reaction data and rendering subscription");
        await TimingChecks(window, session);
    }

    private static async Task TimingChecks(MainWindow window, MainViewModel session)
    {
        await session.PlayFileAsync(Path.GetFullPath("artifacts/test-media/01 - Evening light.mp4"));
        await Until(() => session.IsPlaying && session.Length > 0 && session.Player!.Time > 600, "timing fixture starts");
        int intermediate = 0;
        long previousNative = session.Player!.Time, previousPosition = session.Position;
        for (int i = 0; i < 35; i++)
        {
            await Task.Delay(15);
            long native = session.Player.Time;
            if (native == previousNative && session.Position > previousPosition) intermediate++;
            previousNative = native; previousPosition = session.Position;
        }
        Check(intermediate >= 5, $"lyrics clock advances between native updates ({intermediate} intermediate steps)");
        session.Seek(1450);
        var watch = Stopwatch.StartNew();
        var reports = new List<string>();
        while (session.ActiveIndex != 1 && watch.ElapsedMilliseconds < 2000)
        {
            reports.Add($"{watch.ElapsedMilliseconds}:{session.Player.Time}/{session.Position}");
            await Task.Delay(20);
        }
        Console.WriteLine("LYRIC CLOCK ms:native/estimated " + string.Join(", ", reports));
        Check(watch.ElapsedMilliseconds < 500, $"lyric crosses 1800ms boundary without waiting for next coarse report ({watch.ElapsedMilliseconds}ms)");
        session.Seek(1000);
        await Task.Delay(80);
        Check(session.Position > 1030 && session.Position < 1150, "seek continues moving during native resynchronization");
        session.TogglePlayback();
        await Until(() => !session.IsPlaying, "timing pause");
        long paused = session.Position;
        await Task.Delay(180);
        Check(Math.Abs(session.Position - paused) < 3, "pause freezes interpolated lyric position");
        session.Rate = 2;
        session.TogglePlayback();
        await Until(() => session.IsPlaying, "double-speed resume");
        long start = session.Position;
        await Task.Delay(260);
        long advanced = session.Position - start;
        Check(advanced > 320 && advanced < 750, $"lyric clock follows 2x playback ({advanced}ms in 260ms)");
        session.Rate = 1;
        await session.StopAsync();
    }

    private static async Task AnalysisSafetyChecks(MainWindow window, MainViewModel session)
    {
        string folder = Path.GetFullPath("artifacts/test-media");
        await using (var playback = await Task.Run(() => new MediaPlaybackService()))
        {
            foreach (string file in new[] { "01 - Evening light.mp4", "02 - Quiet room.mp3", "03 - Window seat.mkv", "04 - After hours.wmv" })
            {
                var result = await MusicReactionService.AnalyzeAsync(playback.Engine, Path.Combine(folder, file), CancellationToken.None);
                Check(result.FrameCount >= 520 && result.FrameCount <= 550, $"audio-only analysis supports {Path.GetExtension(file)} ({result.FrameCount} frames)");
            }
            using var cancellation = new CancellationTokenSource(10);
            bool cancelled = false;
            try { await MusicReactionService.AnalyzeAsync(playback.Engine, Path.Combine(folder, "01 - Evening light.mp4"), cancellation.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "analysis cancellation releases its native decoder");
        }
        var first = session.RunAsync(() => session.PlayFileAsync(Path.Combine(folder, "audio-reaction/Reaction.mp3")));
        await Task.Delay(40);
        string latest = Path.Combine(folder, "02 - Quiet room.mp3");
        await session.PlayFileAsync(latest);
        await first;
        await Until(() => session.MusicReactionReady, "latest track reaction ready");
        Check(session.CurrentPath == latest && session.Lines[0].Text.Contains("두 번째") && session.MusicReactionReady,
            "rapid switching rejects the cancelled track's reaction and lyrics");
        await session.PlayFileAsync(Path.Combine(folder, "01 - Evening light.mp4"));
        await Until(() => session.HasVideo && DisplayedFrames(session) > 2, "video remains attached during analysis");
        Check(DisplayedFrames(session) > 2, "audio analysis preserves main-player video output");
        Check(!((LyricsView)window.FindName("MainLyrics")).IsMusicAnimating, "silent media does not keep a rendering subscription alive");
        await session.StopAsync();
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
        await Until(() => Math.Abs(session.Player!.Time - 4000) < 350 && session.ActiveIndex == 2, "lyric seek acknowledgement");
        Check(Math.Abs(session.Position - 4000) < 350, "lyric click follows acknowledged unadjusted timestamp");
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
        await Until(() => session.ActiveIndex == 1 && lyrics.IsFollowing, "rapid seek acknowledgement");
        Check(session.ActiveIndex == 1 && lyrics.IsFollowing, "rapid seeking resynchronizes after native acknowledgement");
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
            await FileAction(() => File.Move(watched, renamed, true));
            await Until(() => session.Items.Any(x => x.Path == renamed) && session.Items.All(x => x.Path != watched), "watch rename");
            await FileAction(() => File.Delete(renamed));
            await Until(() => session.Items.Count == 4, "watch delete");
            Check(session.Items.Count == 4, "debounced watcher detects create, rename and removal including uppercase extension");
        }
        finally { await FileAction(() => File.Delete(watched)); await FileAction(() => File.Delete(renamed)); }
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
        session.TogglePlayback();
        await Until(() => session.IsPlaying && session.Player.Time < 2000, "restart ended media");
        Check(session.CurrentPath == wmv, "play after final Ended restarts the same track without wrapping");
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
                for (int i = 0; i < 100; i++) { session.Seek(2000 + i % 13 * 300); await Task.Delay(30); }
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
