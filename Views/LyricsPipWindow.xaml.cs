using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LyricsPlayer.ViewModels;

namespace LyricsPlayer.Views;

public partial class LyricsPipWindow : Window
{
    private bool _closeImmediately, _closing;
    public LyricsPipWindow(MainViewModel session)
    {
        InitializeComponent();
        DataContext = session;
        Loaded += (_, _) =>
        {
            if (!SystemParameters.ClientAreaAnimation) return;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
            ((TranslateTransform)Contents.RenderTransform).BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        };
        Closing += AnimateClose;
    }
    private void ClosePip(object sender, RoutedEventArgs args) => Close();
    public void CloseImmediately() { _closeImmediately = true; Close(); }
    private void PipKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape) { Close(); args.Handled = true; }
        else if (args.Key == Key.Space && DataContext is MainViewModel session) { session.TogglePlayback(); args.Handled = true; }
    }
    private void AnimateClose(object? sender, CancelEventArgs args)
    {
        if (_closeImmediately || !SystemParameters.ClientAreaAnimation) return;
        args.Cancel = true;
        if (_closing) return;
        _closing = true;
        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140));
        animation.Completed += (_, _) => CloseImmediately();
        BeginAnimation(OpacityProperty, animation);
    }
}
