using System.Runtime.InteropServices;
using System.Diagnostics;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;

namespace LyricsPlayer.Services;

public static class MusicReactionService
{
    private static readonly SemaphoreSlim DecoderGate = new(1, 1);
    internal static (TimeSpan DecodeAndCallback, TimeSpan PcmProcessing, TimeSpan Normalization) LastMeasurement { get; private set; }

    public static async Task<MusicReactionTimeline> AnalyzeAsync(LibVLC engine, string path, CancellationToken token)
    {
        await DecoderGate.WaitAsync(token).ConfigureAwait(false);
        try { return await Task.Run(() => Analyze(engine, path, token), token).ConfigureAwait(false); }
        finally { DecoderGate.Release(); }
    }

    private static MusicReactionTimeline Analyze(LibVLC engine, string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var decoder = new MediaPlayer(engine);
        using var media = new Media(engine, new Uri(Path.GetFullPath(path)));
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = new MusicReactionTimeline.Builder();
        using var buffers = new PcmBuffers(builder);
        AudioPre pre = (IntPtr _, out IntPtr pcm, nuint size) =>
        {
            pcm = IntPtr.Zero;
            try
            {
                token.ThrowIfCancellationRequested();
                if (!completed.Task.IsCompleted) pcm = buffers.Request(size);
            }
            // VLC 3.0.21 smem SendAudio checks null and returns VLC_EGENERIC before memcpy.
            catch (Exception ex) { completed.TrySetException(ex); }
        };
        AudioPost post = (_, pcm, channels, rate, count, bits, bytes, _) =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (channels != 1 || rate != MusicReactionTimeline.SampleRate || bits != 16 || bytes != (nuint)count * 2)
                    throw new IOException("Unexpected analysis PCM format.");
                buffers.Process(pcm, count, bytes);
            }
            catch (Exception ex) { completed.TrySetException(ex); }
        };
        // smem's no-time-sync disables pacing; this decoder never attaches to audio/video output.
        media.AddOption(":no-video");
        media.AddOption(":no-sout-video");
        media.AddOption(":no-spu");
        media.AddOption(":sout=#transcode{acodec=s16l,channels=1,samplerate=8000}:smem{" +
            $"audio-prerender-callback={Marshal.GetFunctionPointerForDelegate(pre).ToInt64()},audio-postrender-callback={Marshal.GetFunctionPointerForDelegate(post).ToInt64()},no-time-sync" + "}");
        decoder.EndReached += (_, _) => completed.TrySetResult();
        decoder.EncounteredError += (_, _) => completed.TrySetException(new IOException("오디오 반응 분석에 실패했습니다."));
        var elapsed = Stopwatch.StartNew();
        try
        {
            if (!decoder.Play(media)) throw new IOException("오디오 분석을 시작할 수 없습니다.");
            completed.Task.WaitAsync(TimeSpan.FromMinutes(2), token).GetAwaiter().GetResult();
            token.ThrowIfCancellationRequested();
        }
        finally
        {
            // Stop/release joins decoder callbacks before any outstanding buffers are freed.
            try { decoder.Stop(); }
            finally
            {
                decoder.Dispose();
                GC.KeepAlive(pre);
                GC.KeepAlive(post);
            }
        }
        elapsed.Stop();
        var normalization = Stopwatch.StartNew();
        var timeline = builder.Finish();
        LastMeasurement = (elapsed.Elapsed, TimeSpan.FromSeconds(buffers.ProcessingTicks / (double)Stopwatch.Frequency), normalization.Elapsed);
        return timeline;
    }

    internal sealed class PcmBuffers(MusicReactionTimeline.Builder builder) : IDisposable
    {
        private readonly object _sync = new();
        private readonly Dictionary<IntPtr, int> _owned = [];
        private short[] _samples = [];
        private long _bytes;
        private bool _disposed;
        internal long ProcessingTicks { get; private set; }

        internal IntPtr Request(nuint size)
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (size == 0 || size > 16 * 1024 * 1024 || _bytes + (long)size > 32 * 1024 * 1024)
                    throw new IOException("Invalid PCM buffer size.");
                int bytes = checked((int)size);
                IntPtr pcm = Marshal.AllocHGlobal(bytes);
                try { _owned.Add(pcm, bytes); }
                catch { Marshal.FreeHGlobal(pcm); throw; }
                _bytes += bytes;
                return pcm;
            }
        }

        internal void Process(IntPtr pcm, uint count, nuint bytes)
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (!_owned.TryGetValue(pcm, out int capacity) || bytes != (nuint)count * 2 || bytes != (nuint)capacity)
                    throw new IOException("PCM buffer does not match its allocation.");
                try
                {
                    long started = Stopwatch.GetTimestamp();
                    int length = checked((int)count);
                    if (_samples.Length < length) _samples = new short[length];
                    Marshal.Copy(pcm, _samples, 0, length);
                    builder.Append(_samples.AsSpan(0, length));
                    ProcessingTicks += Stopwatch.GetTimestamp() - started;
                }
                finally
                {
                    _owned.Remove(pcm);
                    _bytes -= capacity;
                    Marshal.FreeHGlobal(pcm);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                foreach (var pcm in _owned.Keys) Marshal.FreeHGlobal(pcm);
                _owned.Clear();
                _bytes = 0;
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioPre(IntPtr data, out IntPtr buffer, nuint bytes);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioPost(IntPtr data, IntPtr buffer, uint channels, uint rate, uint samples, uint bits, nuint bytes, long pts);
}
