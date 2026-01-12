using AxWMPLib;
using System.Diagnostics;

namespace LyricsPlayer {
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
		private int _previousHighlightIndex = -1; // Added this field

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

		// 외부에서 가사 클릭 이벤트를 받을 수 있도록 노출
		public Action<int> OnLyricClicked { get; set; }

		public MediaLyricsSync(AxWindowsMediaPlayer player, System.Windows.Forms.Timer tmrSync) {
			_player = player ?? throw new ArgumentNullException(nameof(player));
			_tmr = tmrSync ?? throw new ArgumentNullException(nameof(tmrSync));

			// 플레이 상태 변화에 맞춰 타이머 ON/OFF
			_player.PlayStateChange += Player_PlayStateChange;

			_tmr.Tick += (s, e) => TickSync();
		}

		public void AddRenderer(LyricsRenderer renderer) {
			if(renderer == null) return;
			if(!_renderers.Contains(renderer)) {
				_renderers.Add(renderer);
				renderer.OnLyricClicked = (idx) => OnLyricClicked?.Invoke(idx); // 클릭 이벤트 연결

				// 이미 로드된 가사가 있다면 새로 추가된 렌더러에도 로드
				if(_lines.Count > 0) {
					renderer.LoadLines(_lines);
					renderer.HighlightIndex(_currentHighlightIndex, _previousHighlightIndex); // Pass both indices
				}
			}
		}

		public void RemoveRenderer(LyricsRenderer renderer) {
			if(renderer == null) return;
			if(_renderers.Contains(renderer)) {
				_renderers.Remove(renderer);
				renderer.OnLyricClicked = null; // 이벤트 연결 해제
			}
		}

		/// <summary>
		/// 비디오와 LRC, SRT 파일 경로를 로드합니다.
		/// </summary>
		public void LoadMedia(string mediaPath, string lyricsPath = null) {
			Debug.WriteLine($"[MediaLyricsSync] Loading media: {mediaPath}");
			if(!System.IO.File.Exists(mediaPath))
				throw new System.IO.FileNotFoundException("Media file not found.", mediaPath);

			//_player.URL = mediaPath; // This is now handled by the playlist

			string text = string.Empty;
			string ext = null;

			if(!string.IsNullOrEmpty(lyricsPath) && System.IO.File.Exists(lyricsPath)) {
				Debug.WriteLine($"[MediaLyricsSync] Loading specific lyrics file: {lyricsPath}");
				text = System.IO.File.ReadAllText(lyricsPath);
				ext = System.IO.Path.GetExtension(lyricsPath).ToLowerInvariant();
			} else {
				// 자동 탐색: 같은 이름 .lrc or .srt
				var guessLrc = System.IO.Path.ChangeExtension(mediaPath, ".lrc");
				Debug.WriteLine($"[MediaLyricsSync] Guessing LRC path: {guessLrc}");
				var guessSrt = System.IO.Path.ChangeExtension(mediaPath, ".srt");
				Debug.WriteLine($"[MediaLyricsSync] Guessing SRT path: {guessSrt}");

				if(System.IO.File.Exists(guessLrc)) {
					Debug.WriteLine("[MediaLyricsSync] Found LRC file.");
					text = System.IO.File.ReadAllText(guessLrc);
					ext = ".lrc";
				} else if(System.IO.File.Exists(guessSrt)) {
					Debug.WriteLine("[MediaLyricsSync] Found SRT file.");
					text = System.IO.File.ReadAllText(guessSrt);
					ext = ".srt";
				} else {
					Debug.WriteLine("[MediaLyricsSync] No lyrics file found.");
				}
			}

			if(!string.IsNullOrEmpty(text)) {
				if(ext == ".srt")
					_lines = SrtParser.Parse(text);
				else
					_lines = LrcParser.Parse(text);
				Debug.WriteLine($"[MediaLyricsSync] Parsed {_lines.Count} lyric lines.");
			} else {
				_lines = new List<LyricLine>();
				Debug.WriteLine("[MediaLyricsSync] No lyrics to parse.");
			}

			// gap 채우기
			_lines = InsertPauses(_lines);

			// 모든 렌더러에 가사 로드
			foreach(var renderer in _renderers) {
				renderer.LoadLines(_lines);
			}

			_msArray = _lines.Select(x => x.StartTime.TotalMilliseconds).ToArray();

			if(AutoStartTimer)
				_tmr.Start();
		}


		/// <summary>
		/// 가사 라인 사이에 긴 공백이 있을 경우 "..." 라인을 삽입합니다.
		/// 이 기능은 EndTime이 있는 SRT 포맷에만 적용됩니다.
		/// </summary>
		/// <param name="lines">원본 가사 라인들</param>
		/// <param name="gapThresholdSeconds">공백을 감지할 최소 시간 (초)</param>
		private List<LyricLine> InsertPauses(List<LyricLine> lines, double gapThresholdSeconds = 3.0) {
			if(lines == null || lines.Count == 0)
				return lines ?? new List<LyricLine>();

			// "..." 삽입은 SRT 파일(EndTime.HasValue가 true)에만 적용.
			// 첫 라인에 EndTime이 없다면 LRC로 간주하고 아무 작업도 하지 않음.
			if(!lines[0].EndTime.HasValue) {
				return lines;
			}

			var result = new List<LyricLine>();

			// 영상 시작과 첫 가사 사이의 간격 확인
			if(lines[0].StartTime.TotalSeconds > gapThresholdSeconds) {
				result.Add(new LyricLine(TimeSpan.FromSeconds(1), "..."));
			}

			// 가사 간 간격 확인
			for(int i = 0; i < lines.Count - 1; i++) {
				result.Add(lines[i]);

				var currentLine = lines[i];
				var nextLine = lines[i + 1];

				// EndTime이 있는 SRT 포맷의 경우에만 간격 검사
				if(currentLine.EndTime.HasValue) {
					var gap = nextLine.StartTime - currentLine.EndTime.Value;
					if(gap.TotalSeconds > gapThresholdSeconds) {
						var insertTime = currentLine.EndTime.Value + TimeSpan.FromSeconds(1);
						result.Add(new LyricLine(insertTime, "..."));
					}
				}
			}

			result.Add(lines[^1]); // 마지막 라인 추가
			return result;
		}

		/// <summary>
		/// 수동으로 재생 시작(원하면 사용)
		/// </summary>
		public void Play() => _player.Ctlcontrols.play();

		/// <summary>
		/// 수동 일시정지
		/// </summary>
		public void Pause() => _player.Ctlcontrols.pause();

		/// <summary>
		/// 현재 재생 위치(ms)를 기준으로 하이라이트 업데이트
		/// </summary>
		private void TickSync() {
			if(_lines.Count == 0) return;

			double curMs = (_player.Ctlcontrols.currentPosition * 1000.0) + _offsetMs;
			if(curMs < 0) curMs = 0;

			int idx = FindCurrentIndex(curMs);

			// 이전 하이라이트 인덱스 저장
			_previousHighlightIndex = _currentHighlightIndex;

			// 현재 하이라이트 인덱스 결정
			if(idx < 0) {
				_currentHighlightIndex = -1;
			} else if(string.IsNullOrWhiteSpace(_lines[idx].Text)) { // 비어있는 가사 라인 처리
				_currentHighlightIndex = -1;
			} else {
				_currentHighlightIndex = idx;
			}

			if(_previousHighlightIndex != _currentHighlightIndex) {
				Debug.WriteLine($"[MediaLyricsSync] Tick: curMs={curMs}, newIndex={_currentHighlightIndex}");
			}

			// 모든 렌더러에 하이라이트 업데이트
			foreach(var renderer in _renderers) {
				renderer.HighlightIndex(_currentHighlightIndex, _previousHighlightIndex);
			}
		}

		/// <summary>
		/// 현재 시간이 포함되는 라인 인덱스(다음 라인 직전까지 유효).
		/// 예: t ∈ [ts[i], ts[i+1]) -> i
		/// </summary>
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
			// 3 = Playing, 2 = Paused, 1 = Stopped, 10 = Ready 등
			switch(e.newState) {
				case 3: // Playing
					if(AutoStartTimer && !_tmr.Enabled) _tmr.Start();
					break;
				case 1: // Stopped
				case 2: // Paused
				case 8: // MediaEnded
					if(_tmr.Enabled) _tmr.Stop();
					break;
			}
		}
	}
}