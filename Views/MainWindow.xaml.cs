using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using LyricsPlayer.Core;
using LyricsPlayer.Models;
using LyricsPlayer.ViewModels;

namespace LyricsPlayer.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _session;
    private readonly DispatcherTimer _idle = new() { Interval = TimeSpan.FromSeconds(3) };
    private LyricsPipWindow? _pip;
    private bool _closing, _closed, _controlsHidden;
    private WindowState _restoreState;
    private Point? _lastPointer;
    private readonly string? _settingsPath;
    private readonly PlayerSettings _initialSettings;
    private bool? _libraryOverride;
    public bool IsFullscreen { get; private set; }
    public bool ControlsHidden => _controlsHidden;
    public IntPtr VideoOutputHandle => _session.Player?.Hwnd ?? IntPtr.Zero;

    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public MainWindow(string? initialFile = null, string? libraryFolder = null)
    {
        InitializeComponent();
        _settingsPath = libraryFolder is null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LyricsPlayer", "settings.json") : null;
        _initialSettings = PlayerSettings.Load(_settingsPath);
        _session = new MainViewModel(Dispatcher, libraryFolder, settings: _initialSettings);
        DataContext = _session;
        _idle.Tick += IdleElapsed;
        Loaded += async (_, _) => await _session.RunAsync(() => _session.InitializeAsync(initialFile));
        Closing += WindowClosing;
    }

    private void SourceReady(object? sender, EventArgs args)
    {
        int dark = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref dark, sizeof(int));
        var area = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, area.Width);
        MinHeight = Math.Min(MinHeight, area.Height);
        Width = Math.Min(Width, area.Width);
        Height = Math.Min(Height, area.Height);
        Left = Math.Clamp(Left, area.Left, area.Right - Width);
        Top = Math.Clamp(Top, area.Top, area.Bottom - Height);
    }
    private async void OpenFile(object sender, RoutedEventArgs args)
    {
        var dialog = new OpenFileDialog { Title = "미디어 열기", Filter = "미디어|*.mp4;*.mp3;*.wmv;*.mkv", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) await _session.RunAsync(() => _session.PlayFileAsync(dialog.FileName));
    }
    private async void PlaySelected(object sender, RoutedEventArgs args)
    {
        if (LibraryList.SelectedItem is PlaylistItem item)
            await _session.RunAsync(() => _session.PlayFileAsync(item.Path));
    }
    private void PlaylistKeyDown(object sender, KeyEventArgs args)
    { if (args.Key == Key.Enter) { PlaySelected(sender, args); args.Handled = true; } }
    private void RevealSelected(object sender, RoutedEventArgs args) => _session.RevealCommand.Execute(LibraryList.SelectedItem);
    private void RefreshLibrary(object sender, RoutedEventArgs args) => _session.RefreshCommand.Execute(null);
    private void ResetOffset(object sender, RoutedEventArgs args) => _session.SyncOffsetMs = 200;
    private void FilesDragOver(object sender, DragEventArgs args)
    {
        args.Effects = args.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any(MediaFiles.IsSupported)
            ? DragDropEffects.Copy : DragDropEffects.None;
        args.Handled = true;
    }
    private async void FilesDropped(object sender, DragEventArgs args)
    {
        args.Handled = true;
        if (args.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        string? path = files.FirstOrDefault(MediaFiles.IsSupported);
        if (path is null) _session.Status = "지원 형식: MP4, MP3, WMV, MKV";
        else await _session.RunAsync(() => _session.PlayFileAsync(path));
    }
    private void VideoMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.ClickCount == 2) { SetFullscreen(!IsFullscreen); args.Handled = true; }
        else Focus();
        UserActivity(sender, args);
    }
    private void ToggleFullscreen(object? sender, EventArgs args) => SetFullscreen(!IsFullscreen);

    public void SetFullscreen(bool fullscreen)
    {
        if (IsFullscreen == fullscreen || _closing) return;
        // Keep the same VideoView, HWND and MediaPlayer: no output reattachment during playback.
        if (fullscreen)
        {
            _restoreState = WindowState;
            IsFullscreen = true;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Header.Visibility = StatusPanel.Visibility = LibraryPanel.Visibility = Visibility.Collapsed;
            LibraryColumn.Width = LibraryGap.Width = new GridLength(0);
            ContentGrid.Margin = new Thickness(12);
            MediaColumn.Width = new GridLength(2.6, GridUnitType.Star);
            MediaColumn.MinWidth = 220;
            MediaPanel.Visibility = Visibility.Visible;
            FullscreenHint.Visibility = Visibility.Visible;
            Topmost = true;
            WindowState = WindowState.Maximized;
            _idle.Start();
        }
        else
        {
            IsFullscreen = false;
            _idle.Stop();
            Topmost = false;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Header.Visibility = StatusPanel.Visibility = LibraryPanel.Visibility = Visibility.Visible;
            LibraryColumn.Width = new GridLength(270);
            LibraryGap.Width = new GridLength(24);
            ContentGrid.Margin = new Thickness(28, 0, 28, 0);
            MediaColumn.Width = new GridLength(1.08, GridUnitType.Star);
            FullscreenHint.Visibility = Visibility.Collapsed;
            WindowState = _restoreState;
        }
        SetControlsVisible(true);
        UpdateLibraryLayout();
    }

    private void ToggleLibrary(object sender, RoutedEventArgs args)
    { _libraryOverride = LibraryPanel.Visibility != Visibility.Visible; UpdateLibraryLayout(); }

    private void WindowResized(object sender, SizeChangedEventArgs args) => UpdateLibraryLayout();

    private void UpdateLibraryLayout()
    {
        if (LibraryPanel is null || IsFullscreen) return;
        bool compact = ActualWidth < 1180;
        bool show = _libraryOverride ?? !compact;
        LibraryPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        LibraryColumn.Width = new GridLength(show ? 270 : 0);
        LibraryGap.Width = new GridLength(show ? 24 : 0);
        bool screen = !(compact && show);
        MediaPanel.Visibility = screen ? Visibility.Visible : Visibility.Collapsed;
        MediaColumn.MinWidth = screen ? 220 : 0;
        MediaColumn.Width = screen ? new GridLength(1.08, GridUnitType.Star) : new GridLength(0);
    }

    private void UserActivity(object sender, RoutedEventArgs args)
    {
        if (!IsFullscreen || _closing) return;
        if (args is MouseEventArgs mouse && args is not MouseButtonEventArgs)
        {
            Point position = mouse.GetPosition(this);
            if (_lastPointer == position) return;
            _lastPointer = position;
        }
        SetControlsVisible(true);
        _idle.Stop();
        _idle.Start();
    }
    private void IdleElapsed(object? sender, EventArgs args)
    {
        if (Controls.IsSeeking || Mouse.LeftButton == MouseButtonState.Pressed) return;
        _idle.Stop();
        SetControlsVisible(false);
    }
    private void SetControlsVisible(bool visible)
    {
        if (_controlsHidden == !visible) return;
        _controlsHidden = !visible;
        ControlsFrame.IsHitTestVisible = visible;
        var duration = TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? 220 : 0);
        ControlsFrame.BeginAnimation(OpacityProperty, new DoubleAnimation(visible ? 1 : 0, duration));
        ((System.Windows.Media.TranslateTransform)ControlsFrame.RenderTransform).BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(visible ? 0 : 8, duration));
        FullscreenHint.BeginAnimation(OpacityProperty, new DoubleAnimation(visible ? 1 : 0, duration));
    }

    private void WindowKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Handled) return;
        UserActivity(sender, args);
        if (args.Key == Key.Escape && IsFullscreen) { SetFullscreen(false); args.Handled = true; return; }
        if (args.Key == Key.F11) { SetFullscreen(!IsFullscreen); args.Handled = true; return; }
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (args.Key)
            {
                case Key.O: OpenFile(sender, args); break;
                case Key.P: ShowPip(); break;
                case Key.F: if (IsFullscreen) SetFullscreen(false); SearchBox.Focus(); break;
                case Key.Right: _session.NextCommand.Execute(null); break;
                case Key.Left: _session.PreviousCommand.Execute(null); break;
                default: return;
            }
            args.Handled = true;
            return;
        }
        if (IsFullscreen && args.Key == Key.Space && args.OriginalSource is not System.Windows.Controls.Primitives.TextBoxBase and not ComboBox)
        { _session.TogglePlayback(); args.Handled = true; return; }
        if (args.OriginalSource is TextBox or System.Windows.Controls.Primitives.TextBoxBase or ComboBox or Slider or Button) return;
        if (args.Key == Key.Space) { _session.TogglePlayback(); args.Handled = true; }
        else if (args.Key is Key.Left or Key.Right)
        { _session.Seek(_session.Position + (args.Key == Key.Right ? 5000 : -5000)); args.Handled = true; }
    }

    private void OpenPip(object sender, RoutedEventArgs args) => ShowPip();
    public LyricsPipWindow ShowPip()
    {
        if (_pip is null)
        {
            _pip = new LyricsPipWindow(_session);
            _pip.Closed += (_, _) => _pip = null;
            _pip.Show();
        }
        else _pip.Activate();
        return _pip;
    }

    private async void WindowClosing(object? sender, CancelEventArgs args)
    {
        if (_closed) return;
        args.Cancel = true;
        if (_closing) return;
        _closing = true;
        IsEnabled = false;
        _idle.Stop();
        _idle.Tick -= IdleElapsed;
        _pip?.CloseImmediately();
        if (_settingsPath is not null && (_session.Volume != _initialSettings.Volume ||
            _session.SyncOffsetMs != _initialSettings.SyncOffsetMs || _session.MusicReactionEnabled != _initialSettings.MusicReactionEnabled))
        {
            try { new PlayerSettings { Volume = _session.Volume, SyncOffsetMs = _session.SyncOffsetMs, MusicReactionEnabled = _session.MusicReactionEnabled }.Save(_settingsPath); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { System.Diagnostics.Trace.WriteLine($"Settings could not be saved: {ex.Message}"); }
        }
        // Detach only on final shutdown, before LibVLC releases its output handle.
        Video.MediaPlayer = null;
        try { await _session.DisposeAsync(); }
        finally
        {
            Video.Dispose();
            _closed = true;
            await Dispatcher.Yield(DispatcherPriority.Background);
            Close();
        }
    }
}
