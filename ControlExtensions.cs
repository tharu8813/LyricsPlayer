using System.Reflection;

namespace LyricsPlayer {
	public static class ControlExtensions {
		/// <summary>
		/// WinForms 컨트롤에 런타임으로 DoubleBuffered를 켭니다.
		/// (디자이너에는 노출되지 않는 속성)
		/// </summary>
		public static void EnableDoubleBuffer(this Control control) {
			var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
			prop?.SetValue(control, true, null);
		}
	}
}
