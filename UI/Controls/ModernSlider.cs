using LyricsPlayer.Helpers;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LyricsPlayer.UI.Controls {
    public class ModernSlider : Control {
        private const float TrackPadding = 5f;
        private const float TrackBarHeight = 4f;

        private float _value = 0;
        private float _maximum = 100;
        private float _minimum = 0;
        private bool _isDragging = false;
        private bool _isHovered = false;

        public event EventHandler? ValueChanged;
        public event EventHandler? Scroll;

        [Category("Appearance")]
        public Color TrackColor { get; set; } = Color.FromArgb(40, 255, 255, 255);
        [Category("Appearance")]
        public Color HighlightColor { get; set; } = Color.DeepSkyBlue;
        [Category("Appearance")]
        public Color ThumbColor { get; set; } = Color.White;

        public float Value {
            get => _value;
            set {
                float clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value == clamped) return;
                _value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public float Minimum {
            get => _minimum;
            set {
                _minimum = Math.Min(value, _maximum);
                _value = Math.Max(_value, _minimum);
                Invalidate();
            }
        }

        public float Maximum {
            get => _maximum;
            set {
                _maximum = Math.Max(value, _minimum);
                _value = Math.Min(_value, _maximum);
                Invalidate();
            }
        }

        public ModernSlider() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
            this.BackColor = Color.Transparent;
            this.Height = 20;
        }

        protected override void OnMouseEnter(EventArgs e) {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e) {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                _isDragging = true;
                UpdateValueFromMouse(e.X);
                Scroll?.Invoke(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            if (_isDragging) {
                UpdateValueFromMouse(e.X);
                Scroll?.Invoke(this, EventArgs.Empty);
                base.OnMouseMove(e);
                return;
            }

            // 드래그 중이 아닐 때만 커서 변경
            float thumbX = GetThumbPosition();
            this.Cursor = (e.X >= thumbX - 10 && e.X <= thumbX + 10)
                ? Cursors.Hand
                : Cursors.Default;

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            _isDragging = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        private void UpdateValueFromMouse(int mouseX) {
            float trackWidth = Width - (TrackPadding * 2);
            float percent = (mouseX - TrackPadding) / trackWidth;
            Value = _minimum + (percent * (_maximum - _minimum));
        }

        private float GetThumbPosition() {
            float trackWidth = Width - (TrackPadding * 2);
            float range = _maximum - _minimum;
            float percent = range > 0 ? (Value - _minimum) / range : 0;
            return TrackPadding + (percent * trackWidth);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float y = (Height - TrackBarHeight) / 2f;
            float trackWidth = Width - (TrackPadding * 2);
            float thumbPos = GetThumbPosition();
            float radius = TrackBarHeight / 2;

            // 배경 트랙
            using var trackBrush = new SolidBrush(TrackColor);
            e.Graphics.FillRoundedRectangle(trackBrush,
                new RectangleF(TrackPadding, y, trackWidth, TrackBarHeight), radius);

            // 진행된 부분
            if (Value > _minimum) {
                using var highlightBrush = new SolidBrush(HighlightColor);
                e.Graphics.FillRoundedRectangle(highlightBrush,
                    new RectangleF(TrackPadding, y, thumbPos - TrackPadding, TrackBarHeight), radius);
            }

            // 썸(Thumb)
            float thumbSize = _isDragging ? 12f : (_isHovered ? 10f : 8f);
            float thumbY = (Height - thumbSize) / 2f;
            int alpha = _isDragging ? 255 : (_isHovered ? 200 : 150);
            Color thumbColor = _isDragging ? HighlightColor : ThumbColor;

            using var thumbBrush = new SolidBrush(Color.FromArgb(alpha, thumbColor));
            e.Graphics.FillEllipse(thumbBrush,
                thumbPos - (thumbSize / 2), thumbY, thumbSize, thumbSize);
        }
    }
}