using WMPLib;

namespace LyricsPlayer {
	public partial class MainForm : Form {
		private MediaLyricsSync _sync;
		private LyricsRenderer _mainRenderer;
		private LyricsPipForm _pipForm;
		private IWMPPlaylist _playlist;
		private FileSystemWatcher _watcher;

		public MainForm() {
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e) {
			this.Shown += MainForm_Shown;

			_sync = new MediaLyricsSync(player, tmrSync);
			_mainRenderer = new LyricsRenderer(pnlLyricsHost);
			_sync.AddRenderer(_mainRenderer);
			_sync.OnLyricClicked = (idx) => { player.Ctlcontrols.currentPosition = _sync.Lines[idx].StartTime.TotalSeconds; };

			_sync.SyncOffsetMs = 0;

			player.uiMode = "full";
			_playlist = player.playlistCollection.newPlaylist("MyPlaylist");

			string videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
			var mediaPath = Path.Combine(videosPath, "LyricsPlayer");

			if(!Directory.Exists(mediaPath)) {
				Directory.CreateDirectory(mediaPath);
			}

			var mediaFiles = Directory.GetFiles(mediaPath, "*.mp4")
									  .Concat(Directory.GetFiles(mediaPath, "*.mp3"))
									  .Concat(Directory.GetFiles(mediaPath, "*.wmv"));

			foreach(var file in mediaFiles) {
				_playlist.appendItem(player.newMedia(file));
			}

			player.currentPlaylist = _playlist;

			// DataGridView 컬럼 수동 설정
			dataGridView1.AutoGenerateColumns = false;
			dataGridView1.Columns.Clear();

			var titleColumn = new DataGridViewTextBoxColumn {
				Name = "Title",
				HeaderText = "제목",
				DataPropertyName = "name", // IWMPMedia.name 속성과 바인딩
				AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
			};
			dataGridView1.Columns.Add(titleColumn);

			var durationColumn = new DataGridViewTextBoxColumn {
				Name = "Duration",
				HeaderText = "시간",
				DataPropertyName = "durationString", // IWMPMedia.durationString 속성과 바인딩
				Width = 100
			};
			dataGridView1.Columns.Add(durationColumn);


			// DataGridView is now configured in the designer.
			// We just need to set the data source here.
			var playlistItems = new System.Collections.Generic.List<IWMPMedia>();
			if(_playlist != null) {
				for(int i = 0; i < _playlist.count; i++) {
					playlistItems.Add(_playlist.Item[i]);
				}
			}
			dataGridView1.DataSource = playlistItems;

			dataGridView1.CellDoubleClick += DataGridView1_CellDoubleClick;
			player.CurrentItemChange += Player_CurrentItemChange;

			_watcher = new FileSystemWatcher(mediaPath);
			_watcher.NotifyFilter = NotifyFilters.FileName;
			_watcher.Created += OnFileSystemChanged;
			_watcher.Deleted += OnFileSystemChanged;
			_watcher.Renamed += OnFileSystemChanged;
			_watcher.EnableRaisingEvents = true;
		}

		private void OnFileSystemChanged(object sender, FileSystemEventArgs e) {
			var ext = Path.GetExtension(e.FullPath).ToLowerInvariant();
			if(ext == ".mp4" || ext == ".mp3" || ext == ".wmv") {
				if(InvokeRequired) {
					Invoke(new Action(RefreshPlaylist));
				} else {
					RefreshPlaylist();
				}
			}
		}

		private void RefreshPlaylist() {
			string videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
			var mediaPath = Path.Combine(videosPath, "LyricsPlayer");
			var mediaFiles = Directory.GetFiles(mediaPath, "*.mp4")
									  .Concat(Directory.GetFiles(mediaPath, "*.mp3"))
									  .Concat(Directory.GetFiles(mediaPath, "*.wmv"))
									  .ToList();

			var currentPlaylistFiles = new List<string>();
			for(int i = 0; i < _playlist.count; i++) {
				currentPlaylistFiles.Add(_playlist.Item[i].sourceURL);
			}

			foreach(var file in mediaFiles) {
				if(!currentPlaylistFiles.Contains(file, StringComparer.OrdinalIgnoreCase)) {
					_playlist.appendItem(player.newMedia(file));
				}
			}

			for(int i = _playlist.count - 1; i >= 0; i--) {
				var item = _playlist.Item[i];
				if(!mediaFiles.Contains(item.sourceURL, StringComparer.OrdinalIgnoreCase)) {
					_playlist.removeItem(item);
				}
			}

			var playlistItems = new System.Collections.Generic.List<IWMPMedia>();
			for(int i = 0; i < _playlist.count; i++) {
				playlistItems.Add(_playlist.Item[i]);
			}
			dataGridView1.DataSource = null;
			dataGridView1.DataSource = playlistItems;
		}

		private void Player_CurrentItemChange(object sender, AxWMPLib._WMPOCXEvents_CurrentItemChangeEvent e) {
			var media = player.Ctlcontrols.currentItem;
			if(media != null) {
				label1.Text = media.name;
				_sync.LoadMedia(media.sourceURL);

				foreach(DataGridViewRow row in dataGridView1.Rows) {
					if(row.DataBoundItem is IWMPMedia item && item.sourceURL == media.sourceURL) {
						row.Selected = true;
						dataGridView1.CurrentCell = row.Cells[0];
						break;
					}
				}
			}
		}

		private void DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
			if(e.RowIndex >= 0) {
				var media = (IWMPMedia)dataGridView1.Rows[e.RowIndex].DataBoundItem;
				player.Ctlcontrols.playItem(media);
			}
		}

		private void MainForm_Shown(object sender, EventArgs e) {
			_mainRenderer.ForceUpdateLayout();

			if(_playlist.count > 0) {
				var firstItem = _playlist.Item[0];
				if(firstItem != null) {
					label1.Text = firstItem.name;
					_sync.LoadMedia(firstItem.sourceURL);
				}
				player.Ctlcontrols.play();
			}
		}

		private void btnPipMode_Click(object sender, EventArgs e) {
			if(_pipForm == null || _pipForm.IsDisposed) {
				_pipForm = new LyricsPipForm();
				var pipRenderer = new LyricsRenderer(_pipForm.LyricsHostPanel);
				_sync.AddRenderer(pipRenderer);
				_pipForm.FormClosed += (s, args) => _sync.RemoveRenderer(pipRenderer);
				_pipForm.Show();
			} else {
				_pipForm.Activate();
			}
		}
	}
}
