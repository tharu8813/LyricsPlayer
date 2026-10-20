namespace LyricsPlayer.Core;

/// <summary>Interpolates VLC's coarse time reports without predicting through a stalled input.</summary>
public sealed class PlaybackClock
{
    private double _anchor, _rate = 1, _correction;
    private long _anchorAt, _native = -1, _nativeAt;
    private bool _playing, _initialized;

    public double Estimate(long now)
    {
        double elapsed = _playing && _native > 0 ? Math.Max(0, Math.Min(now, _nativeAt + 300) - _anchorAt) : 0;
        return Math.Max(0, _anchor + elapsed * _rate + _correction * Math.Min(1, elapsed / 250));
    }

    public double Read(long native, long now, bool playing, double rate, bool holdNative = false)
    {
        double position = Estimate(now);
        if (!_initialized || (!holdNative && native >= 0 && native != _native))
        {
            double error = native - position;
            bool smooth = _initialized && _playing && playing && Math.Abs(error) <= 100;
            _anchor = smooth ? position : Math.Max(0, native);
            _correction = smooth ? error : 0;
            _anchorAt = _nativeAt = now;
            _native = native;
            _initialized = true;
        }
        else if (_playing != playing || _rate != rate)
        { _anchor = position; _anchorAt = now; _correction = 0; if (_playing != playing) _nativeAt = now; }
        _playing = playing;
        _rate = rate;
        return Estimate(now);
    }

    public void Reset(long position, long now, bool playing = false, double rate = 1)
    {
        _anchor = _native = position;
        _anchorAt = _nativeAt = now;
        _playing = playing;
        _rate = rate;
        _correction = 0;
        _initialized = true;
    }
}
