using System.Runtime.InteropServices;

namespace LyricsPlayer {
    internal static class Program {
        private const string MutexName = "LyricsPlayer_SingleInstance";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main() {
            using var mutex = new System.Threading.Mutex(true, MutexName, out bool isNewInstance);

            if (!isNewInstance) {
                BringExistingWindowToFront();
                return;
            }

            ApplicationConfiguration.Initialize();

            // SplashScreen 표시 및 초기화 대기
            using (var splash = new UI.Forms.SplashScreen()) {
                splash.ShowDialog();
            }

            Application.Run(new UI.Forms.MainForm());
        }

        private static void BringExistingWindowToFront() {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var existing = System.Diagnostics.Process
                .GetProcessesByName(currentProcess.ProcessName)
                .FirstOrDefault(p => p.Id != currentProcess.Id && p.MainWindowHandle != IntPtr.Zero);

            if (existing == null) return;

            ShowWindow(existing.MainWindowHandle, SW_RESTORE);
            SetForegroundWindow(existing.MainWindowHandle);
        }
    }
}