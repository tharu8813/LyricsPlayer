using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.UI.Rendering;

namespace LyricsPlayer.Forms {
    public partial class VideoFullscreenForm : System.Windows.Forms.Form {
        private readonly MediaPlayer _mp;
        private readonly MediaLyricsSync _sync;
        private readonly LyricsRenderer _fsRenderer;
        private readonly LibVLCSharp.WinForms.VideoView _mainVideoView;
        private readonly System.Windows.Forms.Timer _hideTimer;

        private bool _isUserSeeking = false;

        public VideoFullscreenForm(LibVLC libVLC, MediaPlayer mp, MediaLyricsSync sync,
                                   LibVLCSharp.WinForms.VideoView mainVideoView) {
            _mp = mp;
            _sync = sync;
            _mainVideoView = mainVideoView;

            InitializeComponent();
            SetupFullscreen();

            _fsRenderer = new LyricsRenderer(pnlLyricsHost) {
                BackgroundColor = Color.FromArgb(30, 30, 30),
                NormalForeColor = Color.FromArgb(200, 255, 255, 255),
                HighlightForeColor = Color.DeepSkyBlue
            };
            _sync.AddRenderer(_fsRenderer);

            _hideTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _hideTimer.Tick += OnHideTimerTick;

            _sync.OnStatusUpdated += Sync_OnStatusUpdated;
            _mainVideoView.MouseMove += OnActivity;
            _mainVideoView.MouseDoubleClick += MainVideoView_DoubleClick;
            this.pnlBottom.MouseMove += OnActivity;

            this.Shown += OnShown;
            this.KeyDown += OnKeyDown;
        }

        private void OnShown(object sender, EventArgs e) {
            pnlLyricsHost.Width = (int)(this.ClientSize.Width * 0.20);
            _mainVideoView.Parent = pnlVideoArea;
            _mainVideoView.Dock = DockStyle.Fill;
            OnActivity(null, null!);
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) this.Close();
            if (e.KeyCode == Keys.Space) btnPlayPause.PerformClick();
        }

        private void OnHideTimerTick(object sender, EventArgs e) => HideControls();

        private void MainVideoView_DoubleClick(object? sender, MouseEventArgs e) => this.Close();

        private void SetupFullscreen() {
            Screen currentScreen = Screen.FromControl(this);
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = currentScreen.Bounds;
            this.TopMost = true;
        }

        private void Sync_OnStatusUpdated(string title, string cur, string tot) {
            if (_isUserSeeking || this.IsDisposed) return;
            try {
                this.Invoke(() => {
                    lblTime.Text = $"{cur} / {tot}";
                    if (_mp.Length > 0)
                        tbPosition.Value = (float)((double)_mp.Time / _mp.Length * tbPosition.Maximum);
                });
            } catch (ObjectDisposedException) { }
        }

        private void OnActivity(object? sender, MouseEventArgs e) {
            ShowControls();
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void ShowControls() {
            pnlBottom.Visible = true;
            this.Cursor = Cursors.Default;
        }

        private void HideControls() {
            if (!_isUserSeeking && !this.IsDisposed)
                pnlBottom.Visible = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            // 이벤트 해제
            this.Shown -= OnShown;
            this.KeyDown -= OnKeyDown;
            _hideTimer.Tick -= OnHideTimerTick;
            _mainVideoView.MouseMove -= OnActivity;
            _mainVideoView.MouseDoubleClick -= MainVideoView_DoubleClick;
            this.pnlBottom.MouseMove -= OnActivity;

            _sync.OnStatusUpdated -= Sync_OnStatusUpdated;
            _sync.RemoveRenderer(_fsRenderer);
            _fsRenderer.Dispose();
            _hideTimer.Dispose();

            base.OnFormClosing(e);
        }

        private void btnPlayPause_Click(object sender, EventArgs e) {
            if (_mp.IsPlaying) { _mp.Pause(); btnPlayPause.Text = "▶"; } else { _mp.Play(); btnPlayPause.Text = "⏸"; }
        }

        private void tbPosition_Scroll(object sender, EventArgs e) {
            if (_mp.Length <= 0) return;
            _isUserSeeking = true;
            long targetTime = (long)((double)tbPosition.Value / tbPosition.Maximum * _mp.Length);
            _mp.Time = targetTime;
        }

        private void tbPosition_MouseUp(object sender, MouseEventArgs e) => _isUserSeeking = false;
        private void tbPosition_MouseDown(object sender, MouseEventArgs e) => _isUserSeeking = true;

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            pnlLyricsHost.Width = (int)(this.ClientSize.Width * 0.20);
        }
    }
}