using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LyricsPlayer.Models;
using LyricsPlayer.Core;
using LyricsPlayer.ViewModels;

namespace LyricsPlayer.Views;

public partial class LyricsView : UserControl
{
    private ScrollViewer? _scroll;
    private readonly DispatcherTimer _resume = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _manual, _allowBring, _centering;
    private MainViewModel? _session;
    private ScaleTransform? _pulse;
    private bool _rendering;
    private TimeSpan _lastFrame;
    private long _activationAt;
    public bool IsFollowing => !_manual;
    public bool IsMusicAnimating => _rendering;
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
        _manual = false;
        _scroll = FindChild<ScrollViewer>(Rows);
        _session = DataContext as MainViewModel;
        if (_session is not null)
        {
            _session.SeekCompleted += Resync;
            _session.PropertyChanged += SessionChanged;
            _session.AudioReset += ResetMotion;
        }
        Center(false);
        FindPulse();
        SystemParameters.StaticPropertyChanged += MotionPreferenceChanged;
        ApplyMotionPreference(SystemParameters.ClientAreaAnimation);
        UpdateRendering();
    }
    private void Detach(object sender, RoutedEventArgs args)
    {
        if (_session is not null)
        {
            _session.SeekCompleted -= Resync;
            _session.PropertyChanged -= SessionChanged;
            _session.AudioReset -= ResetMotion;
        }
        if (_rendering) CompositionTarget.Rendering -= RenderMusic;
        _rendering = false;
        SystemParameters.StaticPropertyChanged -= MotionPreferenceChanged;
        ResetPulse();
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
    private void ActiveChanged(object sender, SelectionChangedEventArgs args) { if (!_manual) Center(true); FindPulse(); ApplyMotionPreference(SystemParameters.ClientAreaAnimation); ApplyMusicLevel(); }
    private void PreventBring(object sender, RequestBringIntoViewEventArgs args) { if (!_allowBring) args.Handled = true; }
    private void ViewResized(object sender, SizeChangedEventArgs args) { if (!_manual) Center(false); }

    private void WordsResized(object sender, SizeChangedEventArgs args)
    {
        if (sender is ContentPresenter { TemplatedParent: ListBoxItem row } && row.Template.FindName("Frame", row) is Border frame)
        {
            double margin = Math.Max(18, args.NewSize.Height * 0.11);
            frame.Padding = new Thickness(18, margin, 18, margin);
        }
    }

    private void ResetPulse()
    {
        if (_pulse is null) return;
        _pulse.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _pulse.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _pulse.ScaleX = _pulse.ScaleY = 1;
        _pulse = null;
    }

    private void FindPulse()
    {
        var row = Rows.SelectedIndex < 0 ? null : Rows.ItemContainerGenerator.ContainerFromIndex(Rows.SelectedIndex) as ListBoxItem;
        var border = row?.Template.FindName("Pulse", row) as Border;
        var pulse = border?.RenderTransform as ScaleTransform;
        if (pulse is { IsFrozen: true }) { pulse = pulse.Clone(); border!.RenderTransform = pulse; }
        if (!ReferenceEquals(_pulse, pulse)) { ResetPulse(); _pulse = pulse; }
    }

    private void SessionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(MainViewModel.Position) or nameof(MainViewModel.IsPlaying) or nameof(MainViewModel.MusicReactionReady)) UpdateRendering();
    }

    private void ResetMotion() { _activationAt = Environment.TickCount64; ResetPulse(); FindPulse(); UpdateRendering(); ApplyMusicLevel(); }
    private void MotionPreferenceChanged(object? sender, PropertyChangedEventArgs args)
    { if (args.PropertyName == nameof(SystemParameters.ClientAreaAnimation)) { UpdateRendering(); ApplyMotionPreference(SystemParameters.ClientAreaAnimation); } }

    private void ApplyMotionPreference(bool animations)
    {
        if (animations) return;
        foreach (var row in FindChild<VirtualizingStackPanel>(Rows)?.Children.OfType<ListBoxItem>() ?? Enumerable.Empty<ListBoxItem>())
        {
            if (row.Template.FindName("Words", row) is ContentPresenter words)
            { words.BeginAnimation(OpacityProperty, null); words.Opacity = row.IsSelected ? 1 : 0.34; }
            if (row.Template.FindName("Role", row) is Border { RenderTransform: ScaleTransform role } border)
            {
                if (role.IsFrozen) { role = role.Clone(); border.RenderTransform = role; }
                role.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                role.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                role.ScaleX = role.ScaleY = row.IsSelected ? 1.08 : 1;
            }
        }
    }

    private void UpdateRendering()
    {
        bool active = IsLoaded && _session is { MusicReactionMoving: true } && SystemParameters.ClientAreaAnimation;
        if (active == _rendering) return;
        _rendering = active;
        if (active) { _lastFrame = TimeSpan.MinValue; _activationAt = Environment.TickCount64; CompositionTarget.Rendering += RenderMusic; }
        else
        {
            CompositionTarget.Rendering -= RenderMusic;
            if (!SystemParameters.ClientAreaAnimation) { ResetPulse(); return; }
            if (_pulse is not null)
            {
                var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(MusicReactionTimeline.ReleaseSeconds))
                { EasingFunction = new ExponentialEase { Exponent = Math.Log(10), EasingMode = EasingMode.EaseOut } };
                _pulse.BeginAnimation(ScaleTransform.ScaleXProperty, animation, HandoffBehavior.SnapshotAndReplace);
                _pulse.BeginAnimation(ScaleTransform.ScaleYProperty, animation, HandoffBehavior.SnapshotAndReplace);
            }
        }
    }

    private void RenderMusic(object? sender, EventArgs args)
    {
        var frame = ((RenderingEventArgs)args).RenderingTime;
        if (frame == _lastFrame) return;
        _lastFrame = frame;
        if (!SystemParameters.ClientAreaAnimation) { UpdateRendering(); ResetPulse(); return; }
        ApplyMusicLevel();
    }

    private void ApplyMusicLevel()
    {
        if (!_rendering) return;
        FindPulse();
        if (_pulse is null) return;
        if (_pulse.HasAnimatedProperties)
        {
            _pulse.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _pulse.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }
        long now = Environment.TickCount64;
        double target = _session?.MusicReactionLevel ?? 0;
        double scale = 1 + target * Math.Clamp((now - _activationAt) / 60d, 0, 1) * MusicReactionTimeline.Strength;
        _pulse.ScaleX = _pulse.ScaleY = scale;
    }

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
                // A virtualized jump must not animate back across unrealized rows.
                previous = _scroll.VerticalOffset;
                fast = true;
            }
            if (row is null) return;
            double target = Math.Clamp(_scroll.VerticalOffset + row.TranslatePoint(new Point(), _scroll).Y
                + row.ActualHeight / 2 - _scroll.ViewportHeight / 2, 0, _scroll.ScrollableHeight);
            if (!animate || !SystemParameters.ClientAreaAnimation)
            { SetValue(OffsetProperty, target); _scroll.ScrollToVerticalOffset(target); return; }
            SetValue(OffsetProperty, target);
            BeginAnimation(OffsetProperty, new DoubleAnimation(previous, target, TimeSpan.FromMilliseconds(fast ? 150 : 360))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } }, HandoffBehavior.SnapshotAndReplace);
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
