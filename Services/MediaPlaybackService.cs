using LibVLCSharp.Shared;

namespace LyricsPlayer.Services;

public sealed class MediaPlaybackService : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;
    public LibVLC Engine { get; }
    public MediaPlayer Player { get; }

    public MediaPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        Engine = new LibVLC("--no-video-title-show", "--no-sub-autodetect-file");
        Player = new MediaPlayer(Engine) { EnableMouseInput = false, EnableKeyInput = false };
    }

    public async Task PlayAsync(string path, CancellationToken token)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                Player.Stop();
                token.ThrowIfCancellationRequested();
                using var media = new Media(Engine, new Uri(path));
                if (!Player.Play(media)) throw new IOException("미디어를 재생할 수 없습니다. 파일 형식과 손상 여부를 확인해 주세요.");
            }, token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { if (!_disposed) await Task.Run(Player.Stop).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            await Task.Run(() => { Player.Stop(); Player.Dispose(); Engine.Dispose(); }).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
