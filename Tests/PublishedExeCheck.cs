using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Program
{
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);

    private static int PublishedExeCheck()
    {
        string executable = Path.GetFullPath("artifacts/publish/audit-win-x64/LyricsPlayer.exe");
        string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LyricsPlayer", "settings.json");
        byte[]? original = File.Exists(settings) ? File.ReadAllBytes(settings) : null;
        using var first = Process.Start(new ProcessStartInfo(executable)
        {
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            ArgumentList = { Path.GetFullPath("artifacts/test-media/01 - Evening light.mp4") }
        })!;
        try
        {
            var timeout = Stopwatch.StartNew();
            while (!first.HasExited && first.MainWindowHandle == IntPtr.Zero && timeout.ElapsedMilliseconds < 15000)
            { Thread.Sleep(50); first.Refresh(); }
            Check(!first.HasExited && first.MainWindowHandle != IntPtr.Zero, "published self-contained EXE creates a real desktop window");
            using var second = Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = Path.GetDirectoryName(executable)! })!;
            Check(second.WaitForExit(5000) && second.ExitCode == 0 && !first.HasExited, "published single-instance EXE preserves the original process");
            Thread.Sleep(1500);
            Check(!first.HasExited, "published EXE remains alive after opening generated MP4");
            PostMessage(first.MainWindowHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Check(first.WaitForExit(15000) && first.ExitCode == 0, "published EXE closes normally through WM_CLOSE");
            Check(original is null ? !File.Exists(settings) : File.ReadAllBytes(settings).SequenceEqual(original), "EXE smoke leaves personal settings unchanged");
            return 0;
        }
        finally
        {
            if (!first.HasExited)
            {
                PostMessage(first.MainWindowHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                if (!first.WaitForExit(15000)) first.Kill();
            }
        }
    }
}
