using AxWMPLib;
using System.Diagnostics;

using LyricsPlayer.Models;
using LyricsPlayer.Parsers;
using LyricsPlayer.UI.Rendering;

namespace LyricsPlayer.Core {
	/// <summary>
	/// Windows Media Player와 LRC 가사를 동기화하여
	/// 현재 라인을 하이라이트하고 자연스럽게 스크롤합니다.
	/// </summary>
	public sealed class MediaLyricsSync {
		private readonly AxWindowsMediaPlayer _player;
		private readonly System.Windows.Forms.Timer _tmr;
		private readonly List<LyricsRenderer> _renderers = new();

		private List<LyricLine> _lines = new();
		private double _offsetMs = 0; // 오프셋 보정(필요시 ±)

		// 이진 탐색용 캐시
		private double[] _msArray = Array.Empty<double>();

		// 현재 하이라이트된 가사 인덱스 (모든 렌더러에 공통)
		private int _currentHighlightIndex = -1;
		private int _previousHighlightIndex = -1;

		// 옵션
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

		// 외부 이벤트 노출
		public Action<int> OnLyricClicked { get; set; }
		public Action<string, string, string> OnStatusUpdated { get; set; }

		public MediaLyricsSync(AxWindowsMediaPlayer player, System.Windows.Forms.Timer tmrSync) {
			_player = player ?? throw new ArgumentNullException(nameof(player));
			_tmr = tmrSync ?? throw new ArgumentNullException(nameof(tmrSync));

			_player.PlayStateChange += Player_PlayStateChange;
			_tmr.Tick += (s, e) => TickSync();
		}

		public void AddRenderer(LyricsRenderer renderer) {
			if(renderer == null) return;
			if(!_renderers.Contains(renderer)) {
				_renderers.Add(renderer);
				renderer.OnLyricClicked = (idx) => OnLyricClicked?.Invoke(idx);
				if(_lines.Count > 0) {
					renderer.LoadLines(_lines);
					renderer.HighlightIndex(_currentHighlightIndex, _previousHighlightIndex);
				}
			}
		}

		public void RemoveRenderer(LyricsRenderer renderer) {
			if(renderer == null) return;
			if(_renderers.Contains(renderer)) {
				_renderers.Remove(renderer);
				renderer.OnLyricClicked = null;
			}
		}

		public void LoadMedia(string mediaPath, string lyricsPath = null) {
			if(!System.IO.File.Exists(mediaPath)) return;

			string text = string.Empty;
			string ext = null;

			if(!string.IsNullOrEmpty(lyricsPath) && System.IO.File.Exists(lyricsPath)) {
				text = System.IO.File.ReadAllText(lyricsPath);
				ext = System.IO.Path.GetExtension(lyricsPath).ToLowerInvariant();
			} else {
				var guessLrc = System.IO.Path.ChangeExtension(mediaPath, ".lrc");
				var guessSrt = System.IO.Path.ChangeExtension(mediaPath, ".srt");

				if(System.IO.File.Exists(guessLrc)) {
					text = System.IO.File.ReadAllText(guessLrc);
					ext = ".lrc";
				} else if(System.IO.File.Exists(guessSrt)) {
					text = System.IO.File.ReadAllText(guessSrt);
					ext = ".srt";
				}
			}

			if(!string.IsNullOrEmpty(text)) {
				if(ext == ".srt")
					_lines = SrtParser.Parse(text);
				else {
					_lines = LrcParser.Parse(text);
					for(int i = 0; i < _lines.Count - 1; i++) {
						_lines[i].EndTime = _lines[i + 1].StartTime;
					}
				}
			} else {
				_lines = new List<LyricLine>();
			}

			_lines = InsertPauses(_lines);

			foreach(var renderer in _renderers) {
				renderer.LoadLines(_lines);
			}

			_msArray = _lines.Select(x => x.StartTime.TotalMilliseconds).ToArray();

			if(AutoStartTimer)
				_tmr.Start();
		}

		private List<LyricLine> InsertPauses(List<LyricLine> lines, double gapThresholdSeconds = 3.0) {
			if(lines == null || lines.Count == 0) return lines ?? new List<LyricLine>();
			if(!lines[0].EndTime.HasValue) return lines;

			var result = new List<LyricLine>();
			if(lines[0].StartTime.TotalSeconds > gapThresholdSeconds) {
				result.Add(new LyricLine(TimeSpan.FromSeconds(1), "..."));
			}

			for(int i = 0; i < lines.Count - 1; i++) {
				result.Add(lines[i]);
				var currentLine = lines[i];
				var nextLine = lines[i + 1];
				if(currentLine.EndTime.HasValue) {
					var gap = nextLine.StartTime - currentLine.EndTime.Value;
					if(gap.TotalSeconds > gapThresholdSeconds) {
						var insertTime = currentLine.EndTime.Value + TimeSpan.FromSeconds(1);
						result.Add(new LyricLine(insertTime, "..."));
					}
				}
			}

			result.Add(lines[^1]);
			return result;
		}

		private void TickSync() {
			// 곡 정보 업데이트
			if(_player.currentMedia != null) {
				OnStatusUpdated?.Invoke(
					_player.currentMedia.name,
					_player.Ctlcontrols.currentPositionString,
					_player.currentMedia.durationString
				);
			}

			if(_lines.Count == 0) return;

			double curMs = (_player.Ctlcontrols.currentPosition * 1000.0) + _offsetMs;
			if(curMs < 0) curMs = 0;

			int idx = FindCurrentIndex(curMs);
			_previousHighlightIndex = _currentHighlightIndex;

			if(idx < 0 || string.IsNullOrWhiteSpace(_lines[idx].Text)) {
				_currentHighlightIndex = -1;
			} else {
				_currentHighlightIndex = idx;
			}

			foreach(var renderer in _renderers) {
				renderer.HighlightIndex(_currentHighlightIndex, _previousHighlightIndex);
			}
		}

		private int FindCurrentIndex(double curMs) {
			if(_msArray.Length == 0) return -1;
			int lo = 0, hi = _msArray.Length - 1, ans = 0;
			while(lo <= hi) {
				int mid = (lo + hi) >> 1;
				if(_msArray[mid] <= curMs) {
					ans = mid;
					lo = mid + 1;
				} else hi = mid - 1;
			}
			return ans;
		}

		private void Player_PlayStateChange(object sender, _WMPOCXEvents_PlayStateChangeEvent e) {
			switch(e.newState) {
				case 3: if(AutoStartTimer && !_tmr.Enabled) _tmr.Start(); break;
				case 1: case 2: case 8: if(_tmr.Enabled) _tmr.Stop(); break;
			}
		}
	}
}