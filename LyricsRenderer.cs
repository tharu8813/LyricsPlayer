namespace LyricsPlayer {
	public sealed class LyricsRenderer {
		private readonly Panel _host;
		private readonly FlowLayoutPanel _contentPanel;
		private readonly List<Label> _labels = new();

		public Font NormalFont { get; set; } = new Font("Segoe UI", 10f, FontStyle.Regular);
		public Font HighlightFont { get; set; } = new Font("Segoe UI", 12f, FontStyle.Bold);
		public Color NormalForeColor { get; set; } = Color.Gainsboro;
		public Color HighlightForeColor { get; set; } = Color.DeepSkyBlue;
		public Color BackgroundColor { get; set; } = Color.FromArgb(45, 45, 48);
		public Color HoverBackColor { get; set; } = Color.FromArgb(50, Color.White);
		public bool EnableScrollAnimation { get; set; } = true;
		public int ScrollAnimationMs { get; set; } = 120;
		public int ScrollStepIntervalMs { get; set; } = 15;
		public int ManualScrollResumeMs { get; set; } = 3000;

		private System.Windows.Forms.Timer _scrollTimer;
		private System.Windows.Forms.Timer _resizeTimer;
		private System.Windows.Forms.Timer _manualScrollTimer;
		private bool _isManualScrolling = false;
		private int _scrollStart;
		private int _scrollTarget;
		private int _scrollElapsed;
		private int _contentHeight;

		public Action<int> OnLyricClicked { get; set; }
		public Action OnNeedsRecenter { get; set; }

		public IReadOnlyList<LyricLine> Lines { get; private set; } // Now set externally

		public LyricsRenderer(Panel host) {
			_host = host ?? throw new ArgumentNullException(nameof(host));
			_host.BackColor = BackgroundColor;
			_host.EnableDoubleBuffer();

			_contentPanel = new FlowLayoutPanel {
				AutoSize = true,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Margin = new Padding(0),
				Padding = new Padding(0, 16, 0, 16),
				BackColor = Color.Transparent
			};
			_contentPanel.EnableDoubleBuffer();
			_host.Controls.Add(_contentPanel);

			_resizeTimer = new System.Windows.Forms.Timer { Interval = 100 };
			_resizeTimer.Tick += OnResizeTimerTick;

			_manualScrollTimer = new System.Windows.Forms.Timer { Interval = ManualScrollResumeMs };
			_manualScrollTimer.Tick += OnManualScrollTimerTick;

			_host.MouseWheel += OnMouseWheel;
			_contentPanel.MouseWheel += OnMouseWheel;
		}

		public void LoadLines(IReadOnlyList<LyricLine> lines) {
			Lines = lines ?? Array.Empty<LyricLine>();
			BuildLabels();
			StopScrollAnimation();
			_contentPanel.Top = 0;
			_isManualScrolling = false;
		}

		private void BuildLabels() {
			_host.SuspendLayout();
			_contentPanel.SuspendLayout();
			try {
				foreach(var lbl in _labels) {
					lbl.MouseWheel -= OnMouseWheel;
					lbl.Dispose();
				}
				_labels.Clear();
				_contentPanel.Controls.Clear();

				for(int i = 0; i < Lines.Count; i++) {
					int localIndex = i; // Capture the value of i for this iteration
					var line = Lines[i];
					var lbl = new Label {
						AutoSize = false, // We will calculate height manually
						Width = 4000, // Use a large fixed width
						Text = string.IsNullOrEmpty(line.Text) ? " " : line.Text,
						TextAlign = ContentAlignment.MiddleCenter,
						Margin = new Padding(0),
						Padding = new Padding(0, 2, 0, 2),
						ForeColor = NormalForeColor,
						Font = NormalFont,
						BackColor = Color.Transparent,
						Cursor = Cursors.Hand
					};
					// Calculate and set height manually to support multi-line text
					lbl.Height = TextRenderer.MeasureText(lbl.Text, lbl.Font, new Size(lbl.Width, int.MaxValue), TextFormatFlags.WordBreak).Height + lbl.Padding.Vertical + 5;

					lbl.Click += (s, e) => {
						_isManualScrolling = false;
						_manualScrollTimer.Stop();
						OnLyricClicked?.Invoke(localIndex);
					};
					lbl.MouseEnter += (s, e) => { lbl.BackColor = Color.Black; };
					lbl.MouseLeave += (s, e) => { lbl.BackColor = Color.Transparent; };
					lbl.MouseWheel += OnMouseWheel;
					_labels.Add(lbl);
					_contentPanel.Controls.Add(lbl);
				}

				_host.Resize -= OnHostResize;
				_host.Resize += OnHostResize;
				UpdateLayout();
			} finally {
				_contentPanel.ResumeLayout(false);
				_host.ResumeLayout(true);
			}

			// Recalculate the precise content height by summing up all label heights
			if(Lines.Count > 0 && _labels.Count > 0) {
				_contentHeight = _labels.Sum(l => l.Height) + _contentPanel.Padding.Top + _contentPanel.Padding.Bottom;
			} else {
				_contentHeight = 0;
			}
		}

		private void OnMouseWheel(object sender, MouseEventArgs e) {
			_manualScrollTimer.Stop();
			StopScrollAnimation();
			_isManualScrolling = true;

			int newTop = _contentPanel.Top + (e.Delta / 3);

			int maxTop = 0;
			int minTop = _host.Height - _contentHeight;
			if(_contentHeight < _host.Height) minTop = 0;

			_contentPanel.Top = Math.Max(minTop, Math.Min(maxTop, newTop));

			_manualScrollTimer.Interval = ManualScrollResumeMs;
			_manualScrollTimer.Start();
		}

		private void OnManualScrollTimerTick(object sender, EventArgs e) {
			_manualScrollTimer.Stop();
			_isManualScrolling = false;
			OnNeedsRecenter?.Invoke(); // Signal that manual scroll ended, and highlight should reset or re-center
		}

		private void OnHostResize(object sender, EventArgs e) {
			_resizeTimer.Stop();
			_resizeTimer.Start();
		}

		private void OnResizeTimerTick(object sender, EventArgs e) {
			_resizeTimer.Stop();
			ForceUpdateLayout();
		}

		public void ForceUpdateLayout() {
			UpdateLayout();
			OnNeedsRecenter?.Invoke(); // Signal that layout updated, and highlight should reset or re-center
		}

		private void UpdateLayout() {
			// With fixed-width labels, we only need to center the content panel.
			_contentPanel.Left = (_host.ClientSize.Width / 2) - (4000 / 2);
		}

		public void HighlightIndex(int newIndex, int previousIndex) {
			if(_labels.Count == 0) return; // No labels to highlight

			// Reset previous highlight if valid
			if(previousIndex >= 0 && previousIndex < _labels.Count) {
				ResetHighlight(previousIndex);
			}

			// Apply new highlight if valid
			if(newIndex >= 0 && newIndex < _labels.Count) {
				var cur = _labels[newIndex];
				cur.Font = HighlightFont;
				cur.ForeColor = HighlightForeColor;
				cur.BackColor = Color.Transparent;
				CenterOnLabel(newIndex);
			} else // If newIndex is invalid, ensure no highlight is active
			  {
				// No need to call CenterOnLabel if no highlight
			}
		}

		private void ResetHighlight(int index) {
			if(index >= 0 && index < _labels.Count) {
				var prev = _labels[index];
				prev.Font = NormalFont;
				prev.ForeColor = NormalForeColor;
				prev.BackColor = Color.Transparent;
			}
		}

		private void CenterOnLabel(int index) {
			if(index < 0 || index >= _labels.Count) return;
			if(_isManualScrolling) return;
			var lbl = _labels[index];
			int targetY = (_host.ClientSize.Height / 2) - (lbl.Top + lbl.Height / 2);

			ScrollTo(targetY, snap: !EnableScrollAnimation);
		}

		private void ScrollTo(int targetY, bool snap) {
			if(_contentPanel.Controls.Count == 0) return;

			int maxTop = 0;
			int minTop = _host.Height - _contentHeight;
			if(_contentHeight < _host.Height) minTop = 0;
			targetY = Math.Max(minTop, Math.Min(maxTop, targetY));

			if(_scrollTimer != null && _scrollTimer.Enabled && _scrollTarget == targetY) {
				return;
			}

			StopScrollAnimation();

			if(snap || !EnableScrollAnimation) {
				_contentPanel.Top = targetY;
				return;
			}

			_scrollStart = _contentPanel.Top;
			_scrollTarget = targetY;
			_scrollElapsed = 0;

			if(_scrollTimer == null) {
				_scrollTimer = new System.Windows.Forms.Timer { Interval = ScrollStepIntervalMs };
				_scrollTimer.Tick += (_, __) => AnimateScrollStep();
			}

			_scrollTimer.Start();
		}

		private void AnimateScrollStep() {
			_scrollElapsed += ScrollStepIntervalMs;
			double t = Math.Min(1.0, (double)_scrollElapsed / ScrollAnimationMs);
			t = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t; // easeInOutQuad

			int y = (int)Math.Round(_scrollStart + (_scrollTarget - _scrollStart) * t);
			_contentPanel.Top = y;

			if(_scrollElapsed >= ScrollAnimationMs) {
				StopScrollAnimation();
				_contentPanel.Top = _scrollTarget;
			}
		}

		private void StopScrollAnimation() {
			_scrollTimer?.Stop();
		}
	}
}