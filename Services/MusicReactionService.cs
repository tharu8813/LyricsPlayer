using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using LyricsPlayer.Core;

namespace LyricsPlayer.Services;

public static class MusicReactionService
{
    public static Task<MusicReactionTimeline> AnalyzeAsync(LibVLC engine, string path, CancellationToken token) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        using var decoder = new MediaPlayer(engine);
        using var media = new Media(engine, new Uri(Path.GetFullPath(path)));
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = new MusicReactionTimeline.Builder();
        IntPtr buffer = IntPtr.Zero;
        int capacity = 0;
        short[] samples = [];
        AudioPre pre = (IntPtr _, out IntPtr pcm, nuint size) =>
        {
            pcm = IntPtr.Zero;
            try
            {
                token.ThrowIfCancellationRequested();
                int bytes = checked((int)size);
                if (bytes < 0 || bytes > 16 * 1024 * 1024) throw new IOException("Invalid PCM buffer size.");
                if (bytes > capacity) { buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)bytes); capacity = bytes; }
                pcm = buffer;
            }
            catch (Exception ex) { completed.TrySetException(ex); }
        };
        AudioPost post = (_, pcm, channels, rate, count, bits, bytes, _) =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (channels != 1 || rate != MusicReactionTimeline.SampleRate || bits != 16 || bytes != (nuint)count * 2)
                    throw new IOException("Unexpected analysis PCM format.");
                int length = checked((int)count);
                if (samples.Length < length) samples = new short[length];
                Marshal.Copy(pcm, samples, 0, length);
                builder.Append(samples.AsSpan(0, length));
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
        try
        {
            if (!decoder.Play(media)) throw new IOException("오디오 분석을 시작할 수 없습니다.");
            completed.Task.WaitAsync(TimeSpan.FromMinutes(2), token).GetAwaiter().GetResult();
            token.ThrowIfCancellationRequested();
        }
        finally
        {
            decoder.Stop();
            Marshal.FreeHGlobal(buffer);
            GC.KeepAlive(pre);
            GC.KeepAlive(post);
        }
        return builder.Finish();
    }, token);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioPre(IntPtr data, out IntPtr buffer, nuint bytes);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioPost(IntPtr data, IntPtr buffer, uint channels, uint rate, uint samples, uint bits, nuint bytes, long pts);
}
