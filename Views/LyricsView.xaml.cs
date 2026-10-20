using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LyricsPlayer.Models;
using LyricsPlayer.ViewModels;

namespace LyricsPlayer.Views;

public partial class LyricsView : UserControl
{
    private ScrollViewer? _scroll;
    private readonly DispatcherTimer _resume = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _manual, _allowBring, _centering;
    private MainViewModel? _session;
    public bool IsFollowing => !_manual;
    private static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
        "Offset", typeof(double), typeof(LyricsView), new PropertyMetadata(0d, OffsetChanged));

    public LyricsView()
    {
        InitializeComponent();
        _resume.Tick += Resume;
        Loaded += Attach;
        Unloaded += Detach;
        Rows.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(ThumbStarted));
        Rows.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(ThumbCompleted));
    }

    private static void OffsetChanged(DependencyObject source, DependencyPropertyChangedEventArgs args) =>
        ((LyricsView)source)._scroll?.ScrollToVerticalOffset((double)args.NewValue);

    private void Attach(object sender, RoutedEventArgs args)
    {
        _scroll = FindChild<ScrollViewer>(Rows);
        _session = DataContext as MainViewModel;
        if (_session is not null) _session.SeekCompleted += Resync;
        Center(false);
    }
    private void Detach(object sender, RoutedEventArgs args)
    {
        if (_session is not null) _session.SeekCompleted -= Resync;
        _session = null;
        _resume.Stop();
        StopAnimation();
    }

    private static T? FindChild<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            if (FindChild<T>(child) is { } nested) return nested;
        }
        return null;
    }

    private void StopAnimation()
    {
        double offset = _scroll?.VerticalOffset ?? 0;
        BeginAnimation(OffsetProperty, null);
        SetValue(OffsetProperty, offset);
    }
    private void Manual()
    {
        StopAnimation();
        _manual = true;
        _resume.Stop();
        _resume.Start();
    }
    private void ManualWheel(object sender, MouseWheelEventArgs args) => Manual();
    private void ThumbStarted(object sender, DragStartedEventArgs args) { Manual(); _resume.Stop(); }
    private void ThumbCompleted(object sender, DragCompletedEventArgs args) => _resume.Start();
    private void Resume(object? sender, EventArgs args)
    {
        if (Rows.IsMouseCaptureWithin) return;
        _resume.Stop();
        _manual = false;
        Center(true);
    }
    private void Resync() { _resume.Stop(); _manual = false; Center(true, true); }
    private void ActiveChanged(object sender, SelectionChangedEventArgs args) { if (!_manual) Center(true); }
    private void PreventBring(object sender, RequestBringIntoViewEventArgs args) { if (!_allowBring) args.Handled = true; }
    private void ViewResized(object sender, SizeChangedEventArgs args) { if (!_manual) Center(false); }

    private void Center(bool animate, bool fast = false)
    {
        if (_centering || _scroll is null || Rows.SelectedIndex < 0 || !IsLoaded) return;
        _centering = true;
        try
        {
            double previous = _scroll.VerticalOffset;
            StopAnimation();
            var row = Rows.ItemContainerGenerator.ContainerFromIndex(Rows.SelectedIndex) as ListBoxItem;
            if (row is null)
            {
                _allowBring = true;
                try { Rows.ScrollIntoView(Rows.SelectedItem); Rows.UpdateLayout(); }
                finally { _allowBring = false; }
                row = Rows.ItemContainerGenerator.ContainerFromIndex(Rows.SelectedIndex) as ListBoxItem;
            }
            if (row is null) return;
            double target = Math.Clamp(_scroll.VerticalOffset + row.TranslatePoint(new Point(), _scroll).Y
                + row.ActualHeight / 2 - _scroll.ViewportHeight / 2, 0, _scroll.ScrollableHeight);
            if (!animate || !SystemParameters.ClientAreaAnimation)
            { SetValue(OffsetProperty, target); _scroll.ScrollToVerticalOffset(target); return; }
            SetValue(OffsetProperty, target);
            BeginAnimation(OffsetProperty, new DoubleAnimation(previous, target, TimeSpan.FromMilliseconds(fast ? 150 : 320))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }, HandoffBehavior.SnapshotAndReplace);
        }
        finally { _centering = false; }
    }

    private void RowMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton != MouseButton.Left) return;
        var element = args.OriginalSource as DependencyObject;
        var row = element is null ? null : ItemsControl.ContainerFromElement(Rows, element) as ListBoxItem;
        if (row?.DataContext is not LyricLine line) { Manual(); return; }
        Rows.Focus();
        _session?.SeekLyricCommand.Execute(line);
        args.Handled = true;
    }
    private void RowKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is Key.Up or Key.Down or Key.Home or Key.End)
        {
            if (Rows.Items.Count == 0) return;
            int index = args.Key switch { Key.Home => 0, Key.End => Rows.Items.Count - 1, Key.Up => Rows.SelectedIndex - 1, _ => Rows.SelectedIndex + 1 };
            _session?.SeekLyricCommand.Execute(Rows.Items[Math.Clamp(index, 0, Rows.Items.Count - 1)]);
            args.Handled = true;
        }
        else if (args.Key is Key.PageDown or Key.PageUp) Manual();
    }
}
