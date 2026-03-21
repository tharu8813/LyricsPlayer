namespace LyricsPlayer.Helpers {
    public static class MessageHelper {
        private const string AppTitle = "LyricsPlayer";

        /// <summary>
        /// 사용자에게 정보 메시지를 표시하고 콘솔에 기록합니다.
        /// </summary>
        public static void ShowInfo(string text) {
            LogToConsole("INFO", text);
            MessageBox.Show(text, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 사용자에게 경고 메시지를 표시하고 콘솔에 기록합니다.
        /// </summary>
        public static void ShowWarning(string text) {
            LogToConsole("WARNING", text);
            MessageBox.Show(text, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 사용자에게 오류 메시지를 표시하고 콘솔에 기록합니다.
        /// </summary>
        public static void ShowError(string text, Exception ex = null) {
            string fullMessage = text + (ex != null ? $"\n\n상세 정보: {ex.Message}" : "");
            LogToConsole("ERROR", fullMessage);
            MessageBox.Show(fullMessage, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 콘솔에 로그를 기록합니다. (디버깅 확인용)
        /// </summary>
        public static void LogToConsole(string level, string text) {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {text}");
        }
    }
}
