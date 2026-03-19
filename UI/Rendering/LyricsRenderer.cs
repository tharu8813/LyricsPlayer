using System.Drawing.Drawing2D;
using System.Drawing.Text;

using LyricsPlayer.Models;
using LyricsPlayer.Helpers;

namespace LyricsPlayer.UI.Rendering {
	public sealed class LyricsRenderer : IDisposable {
		private readonly Panel _host;
		private readonly Control _canvas;

		public Font NormalFont { get; set; } = new Font("Segoe UI", 11f, FontStyle.Regular);
		public Font HighlightFont { get; set; } = new Font("Segoe UI", 13f, FontStyle.Bold);
		public Color NormalForeColor { get; set; } = Color.FromArgb(180, 255, 255, 255);
		public Color HighlightForeColor { get; set; } = Color.DeepSkyBlue;
		public Color BackgroundColor { get; set; } = Color.FromArgb(24, 24, 24);
		
		public bool EnableScrollAnimation { get; set; } = true;
		public int ScrollAnimationMs { get; set; } = 150;
		public int ScrollStepIntervalMs { get; set; } = 15;
		public int ManualScrollResumeMs { get; set; } = 3000;

		private System.Windows.Forms.Timer _scrollTimer;
		private System.Windows.Forms.Timer _manualScrollTimer;
		private bool _isManualScrolling = false;
		
		private float _currentScrollTop = 0;
		private float _targetScrollTop = 0;
		private float _scrollStartTop = 0;
		private int _scrollElapsed = 0;

		private int _highlightIndex = -1;
		private List<LyricLayout> _layouts = new();
		private float _totalContentHeight = 0;

		public Action<int> OnLyricClicked { get; set; }
		public Action OnNeedsRecenter { get; set; }

		public IReadOnlyList<LyricLine> Lines { get; private set; } = Array.Empty<LyricLine>();

		private class LyricLayout {
			public LyricLine Line { get; set; }
			public RectangleF Bounds { get; set; }
			public int Index { get; set; }
		}

		private int _hoverIndex = -1;
		public Color HighlightBackColor { get; set; } = Color.FromArgb(40, 0, 191, 255);
		public Color HoverBackColor { get; set; } = Color.FromArgb(20, 255, 255, 255);
		public Font TimelineFont { get; set; } = new Font("Segoe UI", 8.5f, FontStyle.Regular);

		public LyricsRenderer(Panel host) {
			_host = host ?? throw new ArgumentNullException(nameof(host));
			_host.BackColor = BackgroundColor;

			_canvas = new Control {
				Dock = DockStyle.Fill,
			};
			_canvas.EnableDoubleBuffer();
			_host.Controls.Add(_canvas);

			_canvas.Paint += OnCanvasPaint;
			_canvas.MouseWheel += OnMouseWheel;
			_canvas.MouseDown += OnCanvasMouseDown;
			_canvas.MouseMove += OnCanvasMouseMove;
			_canvas.MouseLeave += (s, e) => { _hoverIndex = -1; _canvas.Invalidate(); };
			_canvas.Resize += (s, e) => RecalculateLayout();

			_scrollTimer = new System.Windows.Forms.Timer { Interval = ScrollStepIntervalMs };
			_scrollTimer.Tick += OnScrollTimerTick;

			_manualScrollTimer = new System.Windows.Forms.Timer { Interval = ManualScrollResumeMs };
			_manualScrollTimer.Tick += OnManualScrollTimerTick;
		}

		public void LoadLines(IReadOnlyList<LyricLine> lines) {
			Lines = lines ?? Array.Empty<LyricLine>();
			_highlightIndex = -1;
			_currentScrollTop = 0;
			_targetScrollTop = 0;
			_isManualScrolling = false;
			RecalculateLayout();
			_canvas.Invalidate();
		}

		private void RecalculateLayout() {
			_layouts.Clear();
			if(Lines.Count == 0) {
				_totalContentHeight = 0;
				return;
			}

			using var g = _canvas.CreateGraphics();
			float y = 40; // 상단 여백 확대
			int idx = 0;
			float canvasWidth = _canvas.ClientSize.Width;

			foreach(var line in Lines) {
				string text = string.IsNullOrWhiteSpace(line.Text) ? " " : line.Text;
				// 최대 너비를 고려한 텍스트 크기 측정 (양옆 여백 100px 확보)
				var size = g.MeasureString(text, HighlightFont, (int)canvasWidth - 100);
				
				_layouts.Add(new LyricLayout {
					Line = line,
					Bounds = new RectangleF(0, y, canvasWidth, size.Height + 15),
					Index = idx++
				});
				y += size.Height + 15;
			}

			_totalContentHeight = y + 40; // 하단 여백 포함
		}

		private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
			float virtualY = e.Y - _currentScrollTop;
			int newHover = -1;
			for(int i = 0; i < _layouts.Count; i++) {
				if(_layouts[i].Bounds.Contains(e.X, virtualY)) {
					newHover = i;
					break;
				}
			}

			if(_hoverIndex != newHover) {
				_hoverIndex = newHover;
				_canvas.Invalidate();
			}
		}

		private void OnCanvasPaint(object sender, PaintEventArgs e) {
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			float viewHeight = _canvas.ClientSize.Height;
			float viewWidth = _canvas.ClientSize.Width;

			float visibleTop = -_currentScrollTop;
			float visibleBottom = visibleTop + viewHeight;

			foreach(var layout in _layouts) {
				if(layout.Bounds.Bottom < visibleTop || layout.Bounds.Top > visibleBottom)
					continue;

				bool isHighlighted = layout.Index == _highlightIndex;
				bool isHovered = layout.Index == _hoverIndex;

				var drawRect = layout.Bounds;
				drawRect.Offset(0, _currentScrollTop);

				// 배경 하이라이트 (현재 가사 또는 호버 가사)
				if(isHighlighted) {
					using var b = new SolidBrush(HighlightBackColor);
					e.Graphics.FillRectangle(b, 0, drawRect.Top, viewWidth, drawRect.Height);
				} else if(isHovered) {
					using var b = new SolidBrush(HoverBackColor);
					e.Graphics.FillRectangle(b, 0, drawRect.Top, viewWidth, drawRect.Height);
				}

				// 가사 텍스트
				Font font = isHighlighted ? HighlightFont : NormalFont;
				Color color = isHighlighted ? HighlightForeColor : NormalForeColor;
				using var brush = new SolidBrush(color);
				var format = new StringFormat {
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					Trimming = StringTrimming.Word
				};
				e.Graphics.DrawString(layout.Line.Text, font, brush, drawRect, format);

				// 호버 시 타임라인 표시
				if(isHovered) {
					string timeline = layout.Line.GetTimelineString();
					using var timelineBrush = new SolidBrush(Color.FromArgb(130, color));
					// 타임라인 위치 조정 (우측 정렬)
					var timelineRect = new RectangleF(viewWidth - 130, drawRect.Top, 120, drawRect.Height);
					var timelineFormat = new StringFormat {
						Alignment = StringAlignment.Far,
						LineAlignment = StringAlignment.Center
					};
					e.Graphics.DrawString(timeline, TimelineFont, timelineBrush, timelineRect, timelineFormat);
				}
			}
		}

		private void OnCanvasMouseDown(object sender, MouseEventArgs e) {
			_isManualScrolling = false;
			_manualScrollTimer.Stop();

			float clickY = e.Y - _currentScrollTop;
			var clicked = _layouts.FirstOrDefault(l => l.Bounds.Contains(e.X, clickY));
			if(clicked != null) {
				OnLyricClicked?.Invoke(clicked.Index);
			}
		}

		private void OnMouseWheel(object sender, MouseEventArgs e) {
			_manualScrollTimer.Stop();
			_isManualScrolling = true;

			float scrollAmount = e.Delta * 1.5f; 
			float newTarget = _targetScrollTop + scrollAmount;
			
			float minTop = Math.Min(0, _canvas.ClientSize.Height - _totalContentHeight);
			_targetScrollTop = Math.Max(minTop, Math.Min(0, newTarget));

			if(!_scrollTimer.Enabled) {
				_scrollTimer.Start();
			}

			_manualScrollTimer.Start();
		}

		private void OnManualScrollTimerTick(object sender, EventArgs e) {
			_manualScrollTimer.Stop();
			_isManualScrolling = false;
			if(_highlightIndex >= 0) {
				CenterOnIndex(_highlightIndex, snap: false);
			}
		}

		public void HighlightIndex(int newIndex, int previousIndex) {
			if(_highlightIndex == newIndex) return;
			_highlightIndex = newIndex;
			
			if(!_isManualScrolling && _highlightIndex >= 0) {
				CenterOnIndex(_highlightIndex, snap: !EnableScrollAnimation);
			}
			_canvas.Invalidate();
		}

		private void CenterOnIndex(int index, bool snap) {
			if(index < 0 || index >= _layouts.Count) return;
			
			var layout = _layouts[index];
			float targetY = (_canvas.ClientSize.Height / 2f) - (layout.Bounds.Top + layout.Bounds.Height / 2f);
			
			float minTop = Math.Min(0, _canvas.ClientSize.Height - _totalContentHeight);
			targetY = Math.Max(minTop, Math.Min(0, targetY));

			if(snap || !EnableScrollAnimation) {
				_currentScrollTop = targetY;
				_targetScrollTop = targetY;
				_canvas.Invalidate();
			} else {
				_targetScrollTop = targetY;
				if(!_scrollTimer.Enabled) _scrollTimer.Start();
			}
		}

		private void OnScrollTimerTick(object sender, EventArgs e) {
			float diff = _targetScrollTop - _currentScrollTop;
			if(Math.Abs(diff) > 0.5f) {
				_currentScrollTop += diff * 0.15f; 
				_canvas.Invalidate();
			} else {
				_currentScrollTop = _targetScrollTop;
				_scrollTimer.Stop();
				_canvas.Invalidate();
			}
		}

		public void ForceUpdateLayout() {
			RecalculateLayout();
			_canvas.Invalidate();
		}

		public void Dispose() {
			_scrollTimer?.Dispose();
			_manualScrollTimer?.Dispose();
			NormalFont?.Dispose();
			HighlightFont?.Dispose();
			TimelineFont?.Dispose();
		}
	}
}