using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using LyricsPlayer.Core;
using LyricsPlayer.Services;

internal static partial class Program
{
    private static int BenchmarkChecks()
    {
        int result = 1;
        var app = new LyricsPlayer.App();
        app.InitializeComponent();
        var window = new Window { Width = 400, Height = 160, Title = "LyricsPlayer · 분석 성능 검사", Content = "격리된 합성 PCM을 분석하고 UI 응답 시간을 측정합니다." };
        var measurements = new List<object>();
        window.Loaded += async (_, _) =>
        {
            try
            {
                string folder = Path.GetFullPath("artifacts/audit-benchmark");
                Directory.CreateDirectory(folder);
                await using var playback = await Task.Run(() => new MediaPlaybackService());
                foreach (int seconds in new[] { 10, 180, 1800, 7200 })
                {
                    string path = Path.Combine(folder, $"tone-{seconds}.wav");
                    await Task.Run(() => WriteWave(path, seconds));
                    using var process = Process.GetCurrentProcess();
                    var cpu = process.TotalProcessorTime;
                    long initialPrivate = process.PrivateMemorySize64;
                    long maximumPrivate = initialPrivate;
                    long maximumManaged = GC.GetTotalMemory(false);
                    var latencies = new List<double>();
                    var elapsed = Stopwatch.StartNew();
                    double previous = 0;
                    var pulse = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(20) };
                    pulse.Tick += (_, _) =>
                    {
                        double now = elapsed.Elapsed.TotalMilliseconds;
                        if (previous > 0) latencies.Add(now - previous);
                        previous = now;
                        process.Refresh();
                        maximumPrivate = Math.Max(maximumPrivate, process.PrivateMemorySize64);
                        maximumManaged = Math.Max(maximumManaged, GC.GetTotalMemory(false));
                    };
                    pulse.Start();
                    var start = Stopwatch.StartNew();
                    await playback.PlayAsync(path, CancellationToken.None);
                    await Until(() => playback.Player.IsPlaying, "long-file native playback");
                    double playbackStartMs = start.Elapsed.TotalMilliseconds;
                    start.Restart();
                    var reaction = await MusicReactionService.AnalyzeAsync(playback.Engine, path, CancellationToken.None);
                    double analysisMs = start.Elapsed.TotalMilliseconds;
                    pulse.Stop();
                    var measurement = MusicReactionService.LastMeasurement;
                    Check(Math.Abs(reaction.FrameCount - seconds * 30) <= 1, $"{seconds}s real decode yields expected envelope length");
                    Check(latencies.Count > 0 || analysisMs < 100, $"{seconds}s analysis leaves dispatcher responsive");
                    var cache = new MusicReactionCache();
                    var key = MusicReactionCache.Identify(path);
                    cache.Store(key, reaction);
                    start.Restart();
                    Check(ReferenceEquals(cache.Find(MusicReactionCache.Identify(path)), reaction), $"{seconds}s cache hit");
                    double hitMs = start.Elapsed.TotalMilliseconds;
                    await playback.StopAsync();
                    latencies.Sort();
                    process.Refresh();
                    var sample = new
                    {
                        durationSeconds = seconds, format = "8000Hz mono S16 WAV, synthetic 65Hz beat",
                        playbackStartMs, analysisMs,
                        decoderAndCallbackMs = measurement.DecodeAndCallback.TotalMilliseconds,
                        pcmProcessingMs = measurement.PcmProcessing.TotalMilliseconds,
                        normalizationMs = measurement.Normalization.TotalMilliseconds,
                        nativeAndOverheadEstimateMs = Math.Max(0, (measurement.DecodeAndCallback - measurement.PcmProcessing).TotalMilliseconds),
                        cacheHitMs = hitMs, envelopeBytes = reaction.FrameCount * sizeof(double),
                        initialPrivateBytes = initialPrivate, peakPrivateBytes = maximumPrivate, peakManagedBytes = maximumManaged,
                        cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds,
                        dispatcherIntervalP95Ms = latencies.ElementAtOrDefault((int)(latencies.Count * 0.95)),
                        dispatcherIntervalMaximumMs = latencies.LastOrDefault(), dispatcherSamples = latencies.Count
                    };
                    measurements.Add(sample);
                    Console.WriteLine(JsonSerializer.Serialize(sample));
                    File.Delete(path);
                }
                File.WriteAllText(Path.Combine(folder, "results.json"), JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
                result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { window.Close(); }
        };
        app.Run(window);
        return result;
    }

    private static void WriteWave(string path, int seconds)
    {
        const int rate = 8000;
        int bytes = checked(seconds * rate * 2);
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(bytes + 36);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
        writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
        byte[] second = new byte[rate * 2];
        for (int i = 0; i < rate; i++)
        {
            short value = (short)(12000 * Math.Sin(2 * Math.PI * 65 * i / rate) * Math.Exp(-8 * (i % 4000) / (double)rate));
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(second.AsSpan(i * 2), value);
        }
        for (int i = 0; i < seconds; i++) writer.Write(second);
    }
}
