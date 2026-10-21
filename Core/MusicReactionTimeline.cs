namespace LyricsPlayer.Core;

/// <summary>A compact, audio-clock envelope with Playlist Canvas's default bass-scale response.</summary>
public sealed class MusicReactionTimeline
{
    public const int SampleRate = 8000, FramesPerSecond = 30;
    public const double Strength = 0.125, AttackSeconds = 0.06, ReleaseSeconds = 0.4;
    private readonly double[] _levels;
    public int FrameCount => _levels.Length;

    public MusicReactionTimeline(IReadOnlyList<double> energy)
    {
        double[] clean = energy.Select(value => double.IsFinite(value) ? Math.Max(0, value) : 0).ToArray();
        double[] sorted = clean.Order().ToArray();
        double Percentile(double fraction)
        {
            if (sorted.Length == 0) return 0;
            double index = (sorted.Length - 1) * fraction;
            int first = (int)index;
            return sorted[first] + (sorted[Math.Min(first + 1, sorted.Length - 1)] - sorted[first]) * (index - first);
        }
        double floor = Percentile(0.2), span = Math.Max(1e-6, Percentile(0.95) - floor);
        bool inaudible = sorted.Length == 0 || sorted[^1] < 0.001;
        double attack = 1 - Math.Exp(-Math.Log(10) / (FramesPerSecond * AttackSeconds));
        double release = 1 - Math.Exp(-Math.Log(10) / (FramesPerSecond * ReleaseSeconds));
        _levels = new double[energy.Count];
        double previous = 0;
        for (int i = 0; i < energy.Count; i++)
        {
            // Below -60 dBFS, track normalization would magnify quantization noise.
            double target = inaudible ? 0 : Math.Clamp((clean[i] - floor) / span, 0, 1);
            previous += (target - previous) * (target > previous ? attack : release);
            _levels[i] = previous;
        }
    }

    public double LevelAt(double milliseconds)
    {
        double frame = milliseconds * FramesPerSecond / 1000;
        if (!double.IsFinite(frame) || frame < 0 || frame >= _levels.Length) return 0;
        int first = (int)frame;
        return _levels[first] + (_levels[Math.Min(first + 1, _levels.Length - 1)] - _levels[first]) * (frame - first);
    }

    /// <summary>Only four low FFT bands are needed; compute their DFT bins without an FFT dependency.</summary>
    public sealed class Builder
    {
        private const int WindowSize = 512, FirstBin = 2, LastBin = 5;
        private readonly double[] _ring = new double[WindowSize];
        private readonly double[,] _cos = new double[4, WindowSize], _sin = new double[4, WindowSize];
        private readonly List<double> _energy = [];
        private long _samples;

        public Builder()
        {
            for (int bin = FirstBin; bin <= LastBin; bin++)
                for (int i = 0; i < WindowSize; i++)
                {
                    double window = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (WindowSize - 1));
                    _cos[bin - FirstBin, i] = window * Math.Cos(2 * Math.PI * bin * i / WindowSize);
                    _sin[bin - FirstBin, i] = window * Math.Sin(2 * Math.PI * bin * i / WindowSize);
                }
        }

        public void Append(ReadOnlySpan<short> mono)
        { foreach (short sample in mono) AppendSample(sample / 32768d); }

        private void AppendSample(double sample)
        {
            _ring[_samples % WindowSize] = sample;
            _samples++;
            if (_samples < (long)_energy.Count * SampleRate / FramesPerSecond + WindowSize / 2) return;
            Span<double> spectrum = stackalloc double[4];
            for (int bin = FirstBin; bin <= LastBin; bin++)
            {
                double real = 0, imaginary = 0;
                for (int i = 0; i < WindowSize; i++)
                {
                    double value = _ring[(_samples + i) % WindowSize];
                    real += value * _cos[bin - FirstBin, i];
                    imaginary += value * _sin[bin - FirstBin, i];
                }
                spectrum[bin - FirstBin] = Math.Sqrt(real * real + imaginary * imaginary) / (WindowSize / 2);
            }
            double energy = 0;
            for (int band = 0; band < 4; band++)
            {
                double lower = 35 * Math.Pow(4000d / 35, band / 24d);
                double upper = 35 * Math.Pow(4000d / 35, (band + 1) / 24d);
                double peak = 0;
                for (int probe = 0; probe < 5; probe++)
                {
                    double position = lower * Math.Pow(upper / lower, probe / 4d) * WindowSize / SampleRate;
                    int first = (int)position;
                    double a = spectrum[Math.Clamp(first - FirstBin, 0, 3)];
                    double b = spectrum[Math.Clamp(first + 1 - FirstBin, 0, 3)];
                    peak = Math.Max(peak, a + (b - a) * (position - first));
                }
                for (int bin = FirstBin; bin <= LastBin; bin++)
                    if (bin * SampleRate / (double)WindowSize >= lower && bin * SampleRate / (double)WindowSize < upper)
                        peak = Math.Max(peak, spectrum[bin - FirstBin]);
                energy += Math.Clamp(peak, 0, 2) / 4;
            }
            _energy.Add(energy);
        }

        public MusicReactionTimeline Finish()
        {
            long frames = (_samples * FramesPerSecond + SampleRate - 1) / SampleRate;
            while (_energy.Count < frames) AppendSample(0);
            return new MusicReactionTimeline(_energy);
        }
    }
}
