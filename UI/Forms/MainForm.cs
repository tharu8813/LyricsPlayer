using LibVLCSharp.Shared;
using LyricsPlayer.Core;
using LyricsPlayer.Forms;
using LyricsPlayer.Helpers;
using LyricsPlayer.UI.Controls;
using LyricsPlayer.UI.Rendering;
using System.Diagnostics;

namespace LyricsPlayer.UI.Forms {
    public partial class MainForm : Form {
        private LibVLC _libVLC;
        private MediaPlayer _mp;
        private MediaLyricsSync _sync;
        private LyricsRenderer _mainRenderer;
        private LyricsPipForm _pipForm;
        private List<PlaylistItem> _allFiles = new();
        private FileSystemWatcher _watcher;

        private bool _isUserSeeking = false;
        private string _currentPlayingPath = "";

        public MainForm() {
            LibVLCSharp.Shared.Core.Initialize();
            _libVLC = new LibVLC();
            _mp = new MediaPlayer(_libVLC) {
                EnableMouseInput = false,
                EnableKeyInput = false
            };

            InitializeComponent();
            videoView.MediaPlayer = _mp;
        }

        private void Form1_Load(object sender, EventArgs e) {
            try {
                MessageHelper.LogToConsole("INFO", "애플리케이션 시작 및 LibVLC 초기화 중...");

                _sync = new MediaLyricsSync(_mp, tmrSync);
                _mainRenderer = new LyricsRenderer(pnlLyricsHost);
                _sync.AddRenderer(_mainRenderer);

                cmbRate.SelectedIndex = 2;
                tbVolume.Value = _mp.Volume;

                _sync.OnLyricClicked = (idx) => {
                    if (_mp.IsSeekable) {
                        _mp.Time = (long)_sync.Lines[idx].StartTime.TotalMilliseconds;
                        _mainRenderer.HighlightIndex(idx, -1);
                    }
                };

                // += 로 변경해서 전체화면폼 구독자 유지
                _sync.OnStatusUpdated += OnStatusUpdated;

                _mp.EndReached += (s, ev) => {
                    if (!this.IsDisposed)
                        this.BeginInvoke(PlayNext);
                };

                // 전체화면 전환 - _sync 초기화 후에 등록
                videoView.MouseDoubleClick += (s, e) => {
                    if (_mp.Media != null) {
                        var fs = new VideoFullscreenForm(_libVLC, _mp, _sync, videoView);
                        fs.FormClosed += (fsSender, fsArgs) => {
                            videoView.Parent = splitContainer1.Panel1;
                            videoView.Dock = DockStyle.Fill;
                            videoView.BringToFront();
                        };
                        fs.Show();
                    }
                };

                SetupFileWatcher();
                RefreshPlaylist();

                MessageHelper.LogToConsole("INFO", "초기화 완료.");
            } catch (Exception ex) {
                MessageHelper.ShowError("초기화 중 오류가 발생했습니다.", ex);
            }
        }

        private void OnStatusUpdated(string title, string cur, string total) {
            if (_isUserSeeking || this.IsDisposed) return;
            this.Invoke(() => {
                lblTime.Text = $"{cur}/{total}";
                if (_mp.Length > 0)
                    tbPosition.Value = (float)((double)_mp.Time / _mp.Length * tbPosition.Maximum);

                if (_pipForm != null && !_pipForm.IsDisposed)
                    _pipForm.UpdateStatus(label1.Text, cur, total);
            });
        }

        private void SetupFileWatcher() {
            string videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            var mediaPath = Path.Combine(videosPath, "LyricsPlayer");
            if (!Directory.Exists(mediaPath)) Directory.CreateDirectory(mediaPath);

            _watcher = new FileSystemWatcher(mediaPath) {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _watcher.Created += OnFileSystemChanged;
            _watcher.Deleted += OnFileSystemChanged;
            _watcher.Renamed += OnFileSystemChanged;
        }

        private async void RefreshPlaylist(string filter = "") {
            if (InvokeRequired) { Invoke(() => RefreshPlaylist(filter)); return; }

            try {
                string videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                var mediaPath = Path.Combine(videosPath, "LyricsPlayer");
                if (!Directory.Exists(mediaPath)) return;

                var files = Directory.GetFiles(mediaPath, "*.*")
                    .Where(f => f.EndsWith(".mp4") || f.EndsWith(".mp3") ||
                                f.EndsWith(".wmv") || f.EndsWith(".mkv"))
                    .ToList();

                _allFiles = files.Select(f => new PlaylistItem {
                    Title = Path.GetFileNameWithoutExtension(f),
                    Path = f,
                    Duration = "--:--"
                }).ToList();

                UpdateDisplayedList(filter);

                await Task.Run(() => {
                    foreach (var item in _allFiles.Where(i => i.Duration == "--:--")) {
                        using var media = new Media(_libVLC, new Uri(item.Path));
                        media.Parse();
                        long durationMs = media.Duration;
                        if (durationMs > 0) {
                            item.Duration = TimeSpan.FromMilliseconds(durationMs).ToString(@"mm\:ss");
                            this.Invoke(() => lbPlaylist.Invalidate());
                        }
                    }
                });
            } catch (Exception ex) {
                MessageHelper.LogToConsole("ERROR", $"재생 목록 갱신 오류: {ex.Message}");
            }
        }

        private void UpdateDisplayedList(string filter = "") {
            var filtered = string.IsNullOrEmpty(filter)
                ? _allFiles
                : _allFiles.Where(x => x.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            lbPlaylist.Items.Clear();
            foreach (var item in filtered) lbPlaylist.Items.Add(item);

            lblPlaylistInfo.Text = $"ITEMS: {filtered.Count}";
            lbPlaylist.CurrentPlayingPath = _currentPlayingPath;
        }

        private void PlayFile(string path) {
            if (!File.Exists(path)) return;

            _currentPlayingPath = path;
            lbPlaylist.CurrentPlayingPath = path;
            string title = Path.GetFileNameWithoutExtension(path);
            MessageHelper.LogToConsole("INFO", $"재생 시도: {title}");

            _mp.Play(new Media(_libVLC, new Uri(path)));

            Task.Run(async () => {
                await Task.Delay(500);
                if (!this.IsDisposed) this.Invoke(() => _mp.SetSpu(-1));
            });

            _ = _sync.LoadMedia(path);
            label1.Text = title;
            btnPlayPause.Text = "⏸";
            lbPlaylist.Invalidate();

            if (_pipForm != null && !_pipForm.IsDisposed)
                _pipForm.UpdateStatus(title, "00:00", "00:00");
        }

        private void PlayNext() {
            int idx = lbPlaylist.SelectedIndex;
            if (idx < lbPlaylist.Items.Count - 1) SelectAndPlay(idx + 1);
        }

        private void PlayPrev() {
            int idx = lbPlaylist.SelectedIndex;
            if (idx > 0) SelectAndPlay(idx - 1);
        }

        private void SelectAndPlay(int index) {
            if (index >= 0 && index < lbPlaylist.Items.Count) {
                lbPlaylist.SelectedIndex = index;
                if (lbPlaylist.Items[index] is PlaylistItem item) PlayFile(item.Path);
            }
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
            => this.Invoke(() => RefreshPlaylist(txtSearch.Text));

        protected override void OnFormClosing(FormClosingEventArgs e) {
            _sync.OnStatusUpdated -= OnStatusUpdated;
            _watcher?.Dispose();
            _mainRenderer?.Dispose();
            _sync?.Dispose();
            _mp?.Dispose();
            _libVLC?.Dispose();
            base.OnFormClosing(e);
        }

        private void MainForm_Shown(object sender, EventArgs e) {
            _mainRenderer.ForceUpdateLayout();
            // ClearRenderers() 제거 - mainRenderer를 날리면 안 됨
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
            => UpdateDisplayedList(txtSearch.Text);

        private void btnOpenFolder_Click(object sender, EventArgs e) {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "LyricsPlayer");
            if (Directory.Exists(path)) Process.Start("explorer.exe", path);
        }

        private void lbPlaylist_DoubleClick(object sender, EventArgs e) {
            if (lbPlaylist.SelectedItem is PlaylistItem item) PlayFile(item.Path);
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) {
                string ext = Path.GetExtension(files[0]).ToLower();
                if (new[] { ".mp4", ".mp3", ".wmv", ".mkv" }.Contains(ext))
                    PlayFile(files[0]);
            }
        }

        private void menuOpenFileLocation_Click(object sender, EventArgs e) {
            if (lbPlaylist.SelectedItem is PlaylistItem item)
                Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
        }

        private void menuRemoveFromList_Click(object sender, EventArgs e)
            => MessageHelper.ShowInfo("이 플레이어는 폴더와 실시간 동기화됩니다. 파일을 폴더에서 이동하거나 삭제하면 목록에서 자동으로 제거됩니다.");

        private void menuRefreshList_Click(object sender, EventArgs e)
            => RefreshPlaylist(txtSearch.Text);

        private void btnPlayPause_Click(object sender, EventArgs e) {
            if (_mp.IsPlaying) { _mp.Pause(); btnPlayPause.Text = "▶"; } else if (_mp.Media != null) { _mp.Play(); btnPlayPause.Text = "⏸"; }
        }

        private void btnStop_Click(object sender, EventArgs e) {
            _mp.Stop();
            btnPlayPause.Text = "▶";
            tbPosition.Value = 0;
            lblTime.Text = "00:00/00:00";
            _currentPlayingPath = "";
            lbPlaylist.CurrentPlayingPath = "";
            lbPlaylist.Invalidate();

            if (_pipForm != null && !_pipForm.IsDisposed)
                _pipForm.UpdateStatus("정지됨", "00:00", "00:00");
        }

        private void btnPrev_Click(object sender, EventArgs e) => PlayPrev();
        private void btnNext_Click(object sender, EventArgs e) => PlayNext();

        private void tbVolume_Scroll(object sender, EventArgs e) => _mp.Volume = (int)tbVolume.Value;

        private void cmbRate_SelectedIndexChanged(object sender, EventArgs e) {
            if (cmbRate.SelectedItem?.ToString()?.Replace("x", "") is string rateStr
                && float.TryParse(rateStr, out float rate))
                _mp.SetRate(rate);
        }

        private void tbPosition_Scroll(object sender, EventArgs e) {
            if (_mp.Length <= 0) return;
            _isUserSeeking = true;
            long targetTime = (long)((double)tbPosition.Value / tbPosition.Maximum * _mp.Length);
            _mp.Time = targetTime;
            string cur = TimeSpan.FromMilliseconds(targetTime).ToString(@"mm\:ss");
            string tot = TimeSpan.FromMilliseconds(_mp.Length).ToString(@"mm\:ss");
            lblTime.Text = $"{cur}/{tot}";
            if (_pipForm != null && !_pipForm.IsDisposed)
                _pipForm.UpdateStatus(label1.Text, cur, tot);
        }

        private void tbPosition_MouseUp(object sender, MouseEventArgs e) => _isUserSeeking = false;
        private void tbPosition_MouseDown(object sender, MouseEventArgs e) => _isUserSeeking = true;

        private void btnPipMode_Click(object sender, EventArgs e) {
            if (_pipForm == null || _pipForm.IsDisposed) {
                _pipForm = new LyricsPipForm();
                var pipRenderer = new LyricsRenderer(_pipForm.LyricsHostPanel);
                _sync.AddRenderer(pipRenderer);

                if (_sync.Lines.Count == 0) pipRenderer.LoadLines(null);

                string cur = lblTime.Text.Split('/')[0];
                string tot = lblTime.Text.Contains('/') ? lblTime.Text.Split('/')[1] : "00:00";
                _pipForm.UpdateStatus(
                    label1.Text == "Playlist" ? "대기 중" : label1.Text, cur, tot);

                _pipForm.FormClosed += (s, args) => {
                    _sync.RemoveRenderer(pipRenderer);
                    _pipForm = null;
                };
                _pipForm.Show();
            } else {
                _pipForm.Activate();
            }
        }
    }
}