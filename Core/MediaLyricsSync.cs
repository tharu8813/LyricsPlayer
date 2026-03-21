using LibVLCSharp.Shared;
using LyricsPlayer.Helpers;
using LyricsPlayer.Models;
using LyricsPlayer.Parsers;
using LyricsPlayer.UI.Rendering;

namespace LyricsPlayer.Core {
    public sealed class MediaLyricsSync : IDisposable {
        private readonly MediaPlayer _player;
        private readonly System.Windows.Forms.Timer _tmr;
        private readonly List<LyricsRenderer> _renderers = new();

        private List<LyricLine> _lines = new();
        private double _offsetMs = 200;
        private double[] _msArray = Array.Empty<double>();
        private int _currentHighlightIndex = -1;
        private int _previousHighlightIndex = -1;
        private bool _disposed = false;

        public bool AutoStartTimer { get; set; } = true;

        public int TimerIntervalMs {
            get => _tmr.Interval;
            set => _tmr.Interval = Math.Max(15, value);
        }

        public double SyncOffsetMs {
            get => _offsetMs;
            set => _offsetMs = value;
        }

        public IReadOnlyList<LyricLine> Lines => _lines;

        // event로 변경 → += / -= 가능, 구독자 여럿 지원
        public Action<int> OnLyricClicked { get; set; }
        public event Action<string, string, string> OnStatusUpdated;

        public MediaLyricsSync(MediaPlayer player, System.Windows.Forms.Timer tmrSync) {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _tmr = tmrSync ?? throw new ArgumentNullException(nameof(tmrSync));
            _tmr.Tick += (s, e) => TickSync();
            if (AutoStartTimer) _tmr.Start();
        }

        public void AddRenderer(LyricsRenderer renderer) {
            if (renderer == null || _renderers.Contains(renderer)) return;
            _renderers.Add(renderer);

            // 각 렌더러의 클릭 이벤트를 공통 OnLyricClicked로 연결
            renderer.OnLyricClicked = (idx) => OnLyricClicked?.Invoke(idx);

            if (_lines.Count > 0) {
                renderer.LoadLines(_lines);
                renderer.HighlightIndex(_currentHighlightIndex, _previousHighlightIndex);
            }
        }

        public void RemoveRenderer(LyricsRenderer renderer) {
            if (renderer == null) return;
            _renderers.Remove(renderer);
            renderer.OnLyricClicked = null;
        }

        /// <summary>
        /// 가사 데이터만 초기화 (렌더러 목록은 유지)
        /// </summary>
        public void ClearLyrics() {
            _lines = new List<LyricLine>();
            _msArray = Array.Empty<double>();
            _currentHighlightIndex = -1;
            _previousHighlightIndex = -1;
            foreach (var r in _renderers) r.LoadLines(null);
        }

        /// <summary>
        /// 하위 호환성 유지용 - ClearLyrics()와 동일
        /// </summary>
        public void ClearRenderers() => ClearLyrics();

        public async Task LoadMedia(string mediaPath, string lyricsPath = null) {
            if (_disposed) return;

            try {
                if (!File.Exists(mediaPath)) {
                    MessageHelper.ShowError($"미디어 파일을 찾을 수 없습니다: {mediaPath}");
                    return;
                }

                MessageHelper.LogToConsole("INFO", $"미디어 로드 시도: {Path.GetFileName(mediaPath)}");

                foreach (var r in _renderers) r.IsLoading = true;

                List<LyricLine> loadedLines = await Task.Run(() => {
                    try {
                        string text = string.Empty;
                        string ext = null;

                        if (!string.IsNullOrEmpty(lyricsPath) && File.Exists(lyricsPath)) {
                            text = File.ReadAllText(lyricsPath);
                            ext = Path.GetExtension(lyricsPath).ToLowerInvariant();
                        } else {
                            var guessLrc = Path.ChangeExtension(mediaPath, ".lrc");
                            var guessSrt = Path.ChangeExtension(mediaPath, ".srt");

                            if (File.Exists(guessLrc)) { text = File.ReadAllText(guessLrc); ext = ".lrc"; } else if (File.Exists(guessSrt)) { text = File.ReadAllText(guessSrt); ext = ".srt"; }
                        }

                        if (string.IsNullOrEmpty(text)) return new List<LyricLine>();

                        MessageHelper.LogToConsole("INFO", $"가사 파일 파싱 중... ({ext})");
                        var parsed = ext == ".srt"
                            ? SrtParser.Parse(text)
                            : LrcParser.Parse(text);

                        if (ext != ".srt") {
                            for (int i = 0; i < parsed.Count - 1; i++)
                                parsed[i].EndTime = parsed[i + 1].StartTime;
                        }

                        return InsertPauses(parsed);
                    } catch (Exception ex) {
                        MessageHelper.LogToConsole("ERROR", $"가사 파싱 중 오류: {ex.Message}");
                        return new List<LyricLine>();
                    }
                });

                if (_disposed) return;

                _lines = loadedLines;
                _msArray = _lines.Select(x => x.StartTime.TotalMilliseconds).ToArray();
                _currentHighlightIndex = -1;
                _previousHighlightIndex = -1;

                foreach (var r in _renderers) {
                    r.LoadLines(_lines);
                    r.IsLoading = false;
                }

                if (AutoStartTimer) _tmr.Start();

                MessageHelper.LogToConsole(
                    _lines.Count == 0 ? "WARNING" : "INFO",
                    _lines.Count == 0 ? "가사 없이 미디어만 재생합니다." : $"가사 로드 완료: {_lines.Count} 줄"
                );

            } catch (Exception ex) {
                foreach (var r in _renderers) r.IsLoading = false;
                MessageHelper.ShowError("미디어 로드 중 오류가 발생했습니다.", ex);
            }
        }

        private List<LyricLine> InsertPauses(List<LyricLine> lines, double gapThresholdSeconds = 3.0) {
            if (lines == null || lines.Count == 0) return lines ?? new List<LyricLine>();
            if (!lines[0].EndTime.HasValue) return lines;

            var result = new List<LyricLine>();

            if (lines[0].StartTime.TotalSeconds > gapThresholdSeconds)
                result.Add(new LyricLine(TimeSpan.FromSeconds(1), "..."));

            for (int i = 0; i < lines.Count - 1; i++) {
                result.Add(lines[i]);
                if (lines[i].EndTime.HasValue) {
                    var gap = lines[i + 1].StartTime - lines[i].EndTime.Value;
                    if (gap.TotalSeconds > gapThresholdSeconds) {
                        result.Add(new LyricLine(lines[i].EndTime.Value + TimeSpan.FromSeconds(1), "..."));
                    }
                }
            }

            result.Add(lines[^1]);
            return result;
        }

        private void TickSync() {
            if (_disposed) return;

            var state = _player.State;
            if (state != VLCState.Playing && state != VLCState.Paused && state != VLCState.Buffering) return;

            long curTime = _player.Time;
            long totalTime = _player.Length;

            OnStatusUpdated?.Invoke(
                _player.Media?.Meta(MetadataType.Title) ?? "Unknown",
                FormatTime(curTime),
                FormatTime(totalTime)
            );

            if (_lines.Count == 0) return;

            double curMs = Math.Max(0, curTime + _offsetMs);
            int idx = FindCurrentIndex(curMs);

            _previousHighlightIndex = _currentHighlightIndex;
            _currentHighlightIndex = (idx >= 0 && !string.IsNullOrWhiteSpace(_lines[idx].Text)) ? idx : -1;

            foreach (var r in _renderers)
                r.HighlightIndex(_currentHighlightIndex, _previousHighlightIndex);
        }

        private string FormatTime(long ms)
            => TimeSpan.FromMilliseconds(Math.Max(0, ms)).ToString(@"mm\:ss");

        private int FindCurrentIndex(double curMs) {
            if (_msArray.Length == 0) return -1;
            int lo = 0, hi = _msArray.Length - 1, ans = 0;
            while (lo <= hi) {
                int mid = (lo + hi) >> 1;
                if (_msArray[mid] <= curMs) { ans = mid; lo = mid + 1; } else hi = mid - 1;
            }
            return ans;
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            _tmr.Stop();
            _tmr.Tick -= (s, e) => TickSync();
            _renderers.Clear();
        }
    }
}