using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using LyricsPlayer.Views;

namespace LyricsPlayer;
internal static class Program
{
    internal const string MutexName = "LyricsPlayer_SingleInstance";
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out bool first);
        if (!first)
        {
            using var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName))
                using (process)
                    if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, 9);
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
            return;
        }
        try
        {
            var app = new App();
            app.InitializeComponent();
            var window = new MainWindow(args.FirstOrDefault());
            app.MainWindow = window;
            app.Run(window);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"LyricsPlayer를 시작할 수 없습니다. 배포 폴더 전체를 확인해 주세요.\n\n{ex.Message}",
                "LyricsPlayer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
