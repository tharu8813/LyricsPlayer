using LyricsPlayer.Helpers;
using LyricsPlayer.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace LyricsPlayer.UI.Rendering {
    public sealed class LyricsRenderer : IDisposable {
        private readonly Panel _host;
        private readonly Control _canvas;

        public Font NormalFont { get; set; } = new Font("Segoe UI", 11f, FontStyle.Regular);
        public Font HighlightFont { get; set; } = new Font("Segoe UI", 13f, FontStyle.Bold);
        public Color NormalForeColor { get; set; } = Color.FromArgb(180, 255, 255, 255);
        public Color HighlightForeColor { get; set; } = Color.DeepSkyBlue;

        // BackgroundColor 변경 시 host에 즉시 반영
        private Color _backgroundColor = Color.FromArgb(24, 24, 24);
        public Color BackgroundColor {
            get => _backgroundColor;
            set { _backgroundColor = value; _host.BackColor = value; }
        }

        public bool EnableScrollAnimation { get; set; } = true;
        public int ScrollAnimationMs { get; set; } = 100;

        // 타이머 Interval과 직접 연동
        public int ScrollStepIntervalMs {
            get => _scrollTimer.Interval;
            set => _scrollTimer.Interval = Math.Max(1, value);
        }
        public int ManualScrollResumeMs {
            get => _manualScrollTimer.Interval;
            set => _manualScrollTimer.Interval = Math.Max(1, value);
        }

        private readonly System.Windows.Forms.Timer _scrollTimer;
        private readonly System.Windows.Forms.Timer _manualScrollTimer;
        private bool _isManualScrolling = false;
        private bool _isLoading = false;
        private bool _disposed = false;

        // 스크롤바 관련
        private bool _isDraggingScrollBar = false;
        private bool _isMouseOverThumb = false;
        private readonly float _scrollBarWidth = 8;
        private readonly float _scrollBarMargin = 4;
        private RectangleF _scrollBarThumbRect;
        private bool _showScrollBar = false;

        // StringFormat 캐싱 - Paint마다 생성 방지
        private readonly StringFormat _centerFormat = new StringFormat {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.Word
        };
        private readonly StringFormat _rightFormat = new StringFormat {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        private readonly StringFormat _loadingFormat = new StringFormat {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        public bool IsLoading {
            get => _isLoading;
            set { _isLoading = value; _canvas.Invalidate(); }
        }

        private float _currentScrollTop = 0;
        private float _targetScrollTop = 0;
        private int _highlightIndex = -1;
        private readonly List<LyricLayout> _layouts = new();
        private float _totalContentHeight = 0;

        public Action<int> OnLyricClicked { get; set; }
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
            _host.BackColor = _backgroundColor;

            _canvas = new Control { Dock = DockStyle.Fill };
            _canvas.EnableDoubleBuffer();
            _host.Controls.Add(_canvas);

            _canvas.Paint += OnCanvasPaint;
            _canvas.MouseWheel += OnMouseWheel;
            _canvas.MouseDown += OnCanvasMouseDown;
            _canvas.MouseMove += OnCanvasMouseMove;
            _canvas.MouseUp += OnCanvasMouseUp;
            _canvas.MouseLeave += OnCanvasMouseLeave;
            _canvas.Resize += (s, e) => RecalculateLayout();

            _scrollTimer = new System.Windows.Forms.Timer { Interval = 10 };
            _scrollTimer.Tick += OnScrollTimerTick;

            _manualScrollTimer = new System.Windows.Forms.Timer { Interval = 3000 };
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
            if (Lines.Count == 0) { _totalContentHeight = 0; return; }

            float y = 40;
            int idx = 0;
            float canvasWidth = _canvas.ClientSize.Width;

            foreach (var line in Lines) {
                string text = string.IsNullOrWhiteSpace(line.Text) ? " " : line.Text;

                // CreateGraphics() 대신 TextRenderer 사용 - DPI 안전, 리소스 정리 불필요
                var size = TextRenderer.MeasureText(text, HighlightFont,
                    new Size((int)canvasWidth - 100, 0),
                    TextFormatFlags.WordBreak);

                _layouts.Add(new LyricLayout {
                    Line = line,
                    Bounds = new RectangleF(0, y, canvasWidth, size.Height + 15),
                    Index = idx++
                });
                y += size.Height + 15;
            }

            _totalContentHeight = y + 40;
        }

        private void OnCanvasPaint(object sender, PaintEventArgs e) {
            if (_disposed) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float viewHeight = _canvas.ClientSize.Height;
            float viewWidth = _canvas.ClientSize.Width;

            if (_isLoading) {
                using var brush = new SolidBrush(Color.Silver);
                e.Graphics.DrawString("가사 불러오는 중...", HighlightFont, brush,
                    _canvas.ClientRectangle, _loadingFormat);
                return;
            }

            if (Lines.Count == 0) {
                using var brush = new SolidBrush(Color.Gray);
                e.Graphics.DrawString(
                    "재생 중인 미디어가 없습니다.\n오른쪽 재생목록의 항목을 더블클릭하여 재생하세요.",
                    NormalFont, brush, _canvas.ClientRectangle, _loadingFormat);
                return;
            }

            float visibleTop = -_currentScrollTop;
            float visibleBottom = visibleTop + viewHeight;

            foreach (var layout in _layouts) {
                if (layout.Bounds.Bottom < visibleTop || layout.Bounds.Top > visibleBottom)
                    continue;

                bool isHighlighted = layout.Index == _highlightIndex;
                bool isHovered = layout.Index == _hoverIndex;

                var drawRect = layout.Bounds;
                drawRect.Offset(0, _currentScrollTop);

                if (isHighlighted) {
                    using var b = new SolidBrush(HighlightBackColor);
                    e.Graphics.FillRectangle(b, 0, drawRect.Top, viewWidth, drawRect.Height);
                } else if (isHovered) {
                    using var b = new SolidBrush(HoverBackColor);
                    e.Graphics.FillRectangle(b, 0, drawRect.Top, viewWidth, drawRect.Height);
                }

                Font font = isHighlighted ? HighlightFont : NormalFont;
                Color color = isHighlighted ? HighlightForeColor : NormalForeColor;

                using var brush = new SolidBrush(color);
                e.Graphics.DrawString(layout.Line.Text, font, brush, drawRect, _centerFormat);

                if (isHovered) {
                    string timeline = layout.Line.GetTimelineString();
                    using var timelineBrush = new SolidBrush(Color.FromArgb(130, color));
                    var timelineRect = new RectangleF(viewWidth - 130, drawRect.Top, 120, drawRect.Height);
                    e.Graphics.DrawString(timeline, TimelineFont, timelineBrush, timelineRect, _rightFormat);
                }
            }

            DrawScrollBar(e.Graphics, viewHeight, viewWidth);
        }

        private void DrawScrollBar(Graphics g, float viewHeight, float viewWidth) {
            if (_totalContentHeight <= viewHeight) return;

            float trackHeight = viewHeight - (_scrollBarMargin * 2);
            float thumbHeight = Math.Max(30, (viewHeight / _totalContentHeight) * trackHeight);
            float scrollRange = _totalContentHeight - viewHeight;
            float scrollPercent = -_currentScrollTop / scrollRange;
            float thumbY = _scrollBarMargin + (scrollPercent * (trackHeight - thumbHeight));
            float barX = viewWidth - _scrollBarWidth - _scrollBarMargin;

            _scrollBarThumbRect = new RectangleF(barX, thumbY, _scrollBarWidth, thumbHeight);

            using var trackBrush = new SolidBrush(Color.FromArgb(15, 255, 255, 255));
            g.FillRoundedRectangle(trackBrush,
                new RectangleF(barX, _scrollBarMargin, _scrollBarWidth, trackHeight),
                _scrollBarWidth / 2);

            if (_isDraggingScrollBar || _isMouseOverThumb) {
                using var highlightBrush = new SolidBrush(Color.FromArgb(40, HighlightForeColor));
                g.FillRoundedRectangle(highlightBrush,
                    new RectangleF(barX, _scrollBarMargin, _scrollBarWidth,
                        thumbY - _scrollBarMargin + (thumbHeight / 2)),
                    _scrollBarWidth / 2);
            }

            int alpha = _isDraggingScrollBar ? 220 : (_isMouseOverThumb ? 180 : (_showScrollBar ? 100 : 40));
            Color thumbColor = _isDraggingScrollBar ? HighlightForeColor
                             : (_isMouseOverThumb ? Color.LightGray : Color.Gray);

            using var thumbBrush = new SolidBrush(Color.FromArgb(alpha, thumbColor));
            g.FillRoundedRectangle(thumbBrush, _scrollBarThumbRect, _scrollBarWidth / 2);
        }

        private void OnCanvasMouseLeave(object sender, EventArgs e) {
            _hoverIndex = -1;
            _showScrollBar = false;
            _isMouseOverThumb = false;
            _canvas.Cursor = Cursors.Default;
            _canvas.Invalidate();
        }

        private void OnCanvasMouseDown(object sender, MouseEventArgs e) {
            if (_scrollBarThumbRect.Contains(e.Location)) {
                _isDraggingScrollBar = true;
                _isManualScrolling = true;
                _manualScrollTimer.Stop();
                _canvas.Invalidate();
                return;
            }

            _isManualScrolling = false;
            _manualScrollTimer.Stop();

            float clickY = e.Y - _currentScrollTop;
            var clicked = _layouts.FirstOrDefault(l => l.Bounds.Contains(e.X, clickY));
            if (clicked != null) OnLyricClicked?.Invoke(clicked.Index);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            _showScrollBar = true;
            bool nowOverThumb = _scrollBarThumbRect.Contains(e.Location);
            if (_isMouseOverThumb != nowOverThumb) {
                _isMouseOverThumb = nowOverThumb;
                _canvas.Cursor = nowOverThumb ? Cursors.Hand : Cursors.Default;
                _canvas.Invalidate();
            }

            if (_isDraggingScrollBar) {
                float viewHeight = _canvas.ClientSize.Height;
                float trackHeight = viewHeight - (_scrollBarMargin * 2);
                float thumbHeight = _scrollBarThumbRect.Height;
                float relativeY = Math.Max(0, Math.Min(trackHeight - thumbHeight, e.Y - _scrollBarMargin));
                float scrollPercent = relativeY / (trackHeight - thumbHeight);
                float scrollRange = _totalContentHeight - viewHeight;
                _targetScrollTop = -(scrollPercent * scrollRange);
                if (!_scrollTimer.Enabled) _scrollTimer.Start();
                _canvas.Invalidate();
                return;
            }

            float virtualY = e.Y - _currentScrollTop;
            int newHover = -1;
            for (int i = 0; i < _layouts.Count; i++) {
                if (_layouts[i].Bounds.Contains(e.X, virtualY)) { newHover = i; break; }
            }
            if (_hoverIndex != newHover) {
                _hoverIndex = newHover;
                _canvas.Invalidate();
            }
        }

        private void OnCanvasMouseUp(object sender, MouseEventArgs e) {
            if (!_isDraggingScrollBar) return;
            _isDraggingScrollBar = false;
            _manualScrollTimer.Start();
            _canvas.Cursor = _scrollBarThumbRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
            _canvas.Invalidate();
        }

        private void OnMouseWheel(object sender, MouseEventArgs e) {
            _manualScrollTimer.Stop();
            _isManualScrolling = true;
            _showScrollBar = true;
            float newTarget = _targetScrollTop + (e.Delta * 1.5f);
            float minTop = Math.Min(0, _canvas.ClientSize.Height - _totalContentHeight);
            _targetScrollTop = Math.Max(minTop, Math.Min(0, newTarget));
            if (!_scrollTimer.Enabled) _scrollTimer.Start();
            _manualScrollTimer.Start();
        }

        private void OnManualScrollTimerTick(object sender, EventArgs e) {
            _manualScrollTimer.Stop();
            _isManualScrolling = false;
            _showScrollBar = false;
            if (_highlightIndex >= 0) CenterOnIndex(_highlightIndex, snap: false);
            _canvas.Invalidate();
        }

        public void HighlightIndex(int newIndex, int previousIndex) {
            if (_highlightIndex == newIndex) return;
            _highlightIndex = newIndex;
            if (!_isManualScrolling && _highlightIndex >= 0)
                CenterOnIndex(_highlightIndex, snap: !EnableScrollAnimation);
            _canvas.Invalidate();
        }

        private void CenterOnIndex(int index, bool snap) {
            if (index < 0 || index >= _layouts.Count) return;
            var layout = _layouts[index];
            float targetY = (_canvas.ClientSize.Height / 2f)
                          - (layout.Bounds.Top + layout.Bounds.Height / 2f);
            float minTop = Math.Min(0, _canvas.ClientSize.Height - _totalContentHeight);
            targetY = Math.Max(minTop, Math.Min(0, targetY));

            if (snap || !EnableScrollAnimation) {
                _currentScrollTop = targetY;
                _targetScrollTop = targetY;
                _canvas.Invalidate();
            } else {
                _targetScrollTop = targetY;
                if (!_scrollTimer.Enabled) _scrollTimer.Start();
            }
        }

        private void OnScrollTimerTick(object sender, EventArgs e) {
            float diff = _targetScrollTop - _currentScrollTop;
            if (Math.Abs(diff) > 0.2f) {
                _currentScrollTop += diff * 0.25f;
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
            if (_disposed) return;
            _disposed = true;

            // 이벤트 해제
            _canvas.Paint -= OnCanvasPaint;
            _canvas.MouseWheel -= OnMouseWheel;
            _canvas.MouseDown -= OnCanvasMouseDown;
            _canvas.MouseMove -= OnCanvasMouseMove;
            _canvas.MouseUp -= OnCanvasMouseUp;
            _canvas.MouseLeave -= OnCanvasMouseLeave;

            _scrollTimer.Dispose();
            _manualScrollTimer.Dispose();

            // StringFormat 캐시 해제
            _centerFormat.Dispose();
            _rightFormat.Dispose();
            _loadingFormat.Dispose();

            // Font 해제
            NormalFont?.Dispose();
            HighlightFont?.Dispose();
            TimelineFont?.Dispose();
        }
    }
}