using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LyricsPlayer.ViewModels;

namespace LyricsPlayer.Views;

public partial class PlaybackControlBar : UserControl
{
    private bool _seeking;
    public event EventHandler? FullscreenRequested;
    public bool IsSeeking => _seeking || PositionSlider.IsMouseCaptureWithin;
    public PlaybackControlBar() => InitializeComponent();
    private void FullscreenClicked(object sender, RoutedEventArgs args) => FullscreenRequested?.Invoke(this, EventArgs.Empty);
    private void SeekStarted(object sender, MouseButtonEventArgs args)
    { _seeking = true; if (DataContext is MainViewModel session) session.IsUserSeeking = true; }
    private void PositionChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
    { if (_seeking) Commit(); }
    private void Commit() { if (DataContext is MainViewModel session) session.Seek((long)PositionSlider.Value); }
    private void EndSeek()
    { if (_seeking) Commit(); _seeking = false; if (DataContext is MainViewModel session) session.IsUserSeeking = false; }
    private void SeekEnded(object sender, MouseButtonEventArgs args) => EndSeek();
    private void SeekCaptureLost(object sender, MouseEventArgs args) => EndSeek();
    private void SeekKeyUp(object sender, KeyEventArgs args)
    { if (args.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown) { Commit(); args.Handled = true; } }
    private void SeekWheel(object sender, MouseWheelEventArgs args)
    { if (DataContext is MainViewModel session) session.Seek(session.Position + Math.Sign(args.Delta) * 5000); args.Handled = true; }
}
