using System.Drawing.Drawing2D;

namespace LyricsPlayer.UI.Controls {
    public class ModernListBox : ListBox {
        private int _hoverIndex = -1;

        public Color HoverBackColor { get; set; } = Color.FromArgb(40, 255, 255, 255);
        public Color PlayingBackColor { get; set; } = Color.FromArgb(60, 0, 191, 255);
        public Color SelectedBackColor { get; set; } = Color.FromArgb(80, 0, 120, 215);
        public Color TextColor { get; set; } = Color.Gainsboro;
        public Color DurationColor { get; set; } = Color.Gray;

        public string CurrentPlayingPath { get; set; } = "";

        // 폰트 캐싱 - OnDrawItem마다 생성 방지
        private Font _normalFont;
        private Font _boldFont;

        public ModernListBox() {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw, true);

            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.ItemHeight = 50;
            this.BorderStyle = BorderStyle.None;
            this.BackColor = Color.FromArgb(32, 32, 32);
            // DoubleBuffered 중복 제거 - SetStyle로 이미 처리됨

            RebuildFonts();

            this.MouseMove += OnMouseMove;
            this.MouseLeave += OnMouseLeave;
        }

        // Font가 바뀔 때 캐시 재생성
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            RebuildFonts();
        }

        private void RebuildFonts() {
            _normalFont?.Dispose();
            _boldFont?.Dispose();
            _normalFont = new Font(this.Font.FontFamily, 9.5f, FontStyle.Regular);
            _boldFont = new Font(this.Font.FontFamily, 9.5f, FontStyle.Bold);
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            int index = this.IndexFromPoint(e.Location);
            if (index != _hoverIndex) {
                _hoverIndex = index;
                this.Invalidate();
            }
        }

        private void OnMouseLeave(object sender, EventArgs e) {
            _hoverIndex = -1;
            this.Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            // 배경 지우기 무시 (깜빡임 방지)
        }

        protected override void OnPaint(PaintEventArgs e) {
            using var b = new SolidBrush(this.BackColor);
            e.Graphics.FillRectangle(b, this.ClientRectangle);

            for (int i = 0; i < Items.Count; i++) {
                var rect = GetItemRectangle(i);
                if (!e.ClipRectangle.IntersectsWith(rect)) continue;

                var state = DrawItemState.Default;
                if (SelectedIndex == i) state |= DrawItemState.Selected;
                OnDrawItem(new DrawItemEventArgs(e.Graphics, this.Font, rect, i, state));
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e) {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            // dynamic 대신 직접 캐스팅 - 타입 안전 + 빠름
            if (Items[e.Index] is not PlaylistItem item) return;

            var g = e.Graphics;
            var rect = e.Bounds;

            bool isPlaying = item.Path == CurrentPlayingPath;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isHovered = e.Index == _hoverIndex;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. 배경
            Color backColor = isPlaying ? PlayingBackColor
                            : isSelected ? SelectedBackColor
                            : isHovered ? HoverBackColor
                            : this.BackColor;

            using (var b = new SolidBrush(backColor))
                g.FillRectangle(b, rect);

            // 2. 제목 텍스트 - 캐싱된 폰트 사용
            Font titleFont = isPlaying ? _boldFont : _normalFont;
            Color titleColor = (isPlaying || isHovered) ? Color.White : TextColor;
            var titleRect = new Rectangle(rect.X + 15, rect.Y, rect.Width - 80, rect.Height);
            TextRenderer.DrawText(g, item.Title, titleFont, titleRect, titleColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // 3. 재생시간 텍스트
            string duration = item.Duration ?? "--:--";
            var durationRect = new Rectangle(rect.Right - 70, rect.Y, 60, rect.Height);
            TextRenderer.DrawText(g, duration, this.Font, durationRect, DurationColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);

            // 4. 재생 중 왼쪽 강조선
            if (isPlaying) {
                using var accent = new SolidBrush(Color.DeepSkyBlue);
                g.FillRectangle(accent, rect.X, rect.Y + 5, 4, rect.Height - 10);
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                this.MouseMove -= OnMouseMove;
                this.MouseLeave -= OnMouseLeave;
                _normalFont?.Dispose();
                _boldFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class PlaylistItem {
        public string Title { get; set; } = "";
        public string Path { get; set; } = "";
        public string Duration { get; set; } = "00:00";
        public override string ToString() => Title;
    }
}