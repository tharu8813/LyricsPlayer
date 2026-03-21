using System.Drawing.Drawing2D;
using System.Reflection;

namespace LyricsPlayer.Helpers {
    public static class ControlExtensions {
        /// <summary>
        /// WinForms 컨트롤에 런타임으로 DoubleBuffered를 켭니다.
        /// </summary>
        public static void EnableDoubleBuffer(this Control control) {
            var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(control, true, null);
        }

        /// <summary>
        /// 둥근 모서리 사각형을 그립니다.
        /// </summary>
        public static void FillRoundedRectangle(this Graphics g, Brush brush, RectangleF rect, float radius) {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            using var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
