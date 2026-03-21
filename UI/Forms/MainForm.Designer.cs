namespace LyricsPlayer.UI.Forms {
	partial class MainForm {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

        private void InitializeComponent() {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            tmrSync = new System.Windows.Forms.Timer(components);
            label1 = new Label();
            splitContainer2 = new SplitContainer();
            splitContainer1 = new SplitContainer();
            videoView = new LibVLCSharp.WinForms.VideoView();
            pnlVideoControls = new Panel();
            btnPrev = new Button();
            btnPlayPause = new Button();
            btnNext = new Button();
            btnStop = new Button();
            tbPosition = new LyricsPlayer.UI.Controls.ModernSlider();
            lblTime = new Label();
            labelVolume = new Label();
            tbVolume = new LyricsPlayer.UI.Controls.ModernSlider();
            cmbRate = new ComboBox();
            pnlLyricsHost = new Panel();
            btnPipMode = new Button();
            lbPlaylist = new LyricsPlayer.UI.Controls.ModernListBox();
            ctxPlaylist = new ContextMenuStrip(components);
            menuOpenFileLocation = new ToolStripMenuItem();
            menuRemoveFromList = new ToolStripMenuItem();
            menuRefreshList = new ToolStripMenuItem();
            pnlPlaylistFooter = new Panel();
            btnOpenFolder = new Button();
            pnlPlaylistHeader = new Panel();
            txtSearch = new TextBox();
            lblPlaylistInfo = new Label();
            label3 = new Label();
            panel1 = new Panel();
            label4 = new Label();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)videoView).BeginInit();
            pnlVideoControls.SuspendLayout();
            pnlLyricsHost.SuspendLayout();
            ctxPlaylist.SuspendLayout();
            pnlPlaylistFooter.SuspendLayout();
            pnlPlaylistHeader.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tmrSync
            // 
            tmrSync.Interval = 15;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label1.ForeColor = Color.Gainsboro;
            label1.Location = new Point(12, 37);
            label1.Name = "label1";
            label1.Padding = new Padding(0, 5, 0, 10);
            label1.Size = new Size(57, 35);
            label1.TabIndex = 3;
            label1.Text = "Playlist";
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(12, 72);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(lbPlaylist);
            splitContainer2.Panel2.Controls.Add(pnlPlaylistFooter);
            splitContainer2.Panel2.Controls.Add(pnlPlaylistHeader);
            splitContainer2.Size = new Size(960, 548);
            splitContainer2.SplitterDistance = 680;
            splitContainer2.TabIndex = 6;
            // 
            // splitContainer1
            // 
            splitContainer1.BackColor = Color.FromArgb(40, 40, 40);
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(videoView);
            splitContainer1.Panel1.Controls.Add(pnlVideoControls);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pnlLyricsHost);
            splitContainer1.Size = new Size(680, 548);
            splitContainer1.SplitterDistance = 331;
            splitContainer1.TabIndex = 6;
            // 
            // videoView
            // 
            videoView.BackColor = Color.Black;
            videoView.Dock = DockStyle.Fill;
            videoView.Location = new Point(0, 0);
            videoView.MediaPlayer = null;
            videoView.Name = "videoView";
            videoView.Size = new Size(680, 281);
            videoView.TabIndex = 0;
            // 
            // pnlVideoControls
            // 
            pnlVideoControls.BackColor = Color.FromArgb(30, 30, 30);
            pnlVideoControls.Controls.Add(btnPrev);
            pnlVideoControls.Controls.Add(btnPlayPause);
            pnlVideoControls.Controls.Add(btnNext);
            pnlVideoControls.Controls.Add(btnStop);
            pnlVideoControls.Controls.Add(tbPosition);
            pnlVideoControls.Controls.Add(lblTime);
            pnlVideoControls.Controls.Add(labelVolume);
            pnlVideoControls.Controls.Add(tbVolume);
            pnlVideoControls.Controls.Add(cmbRate);
            pnlVideoControls.Dock = DockStyle.Bottom;
            pnlVideoControls.Location = new Point(0, 281);
            pnlVideoControls.Name = "pnlVideoControls";
            pnlVideoControls.Padding = new Padding(5);
            pnlVideoControls.Size = new Size(680, 50);
            pnlVideoControls.TabIndex = 1;
            // 
            // btnPrev
            // 
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.Font = new Font("Segoe UI", 12F);
            btnPrev.ForeColor = Color.White;
            btnPrev.Location = new Point(5, 5);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(35, 35);
            btnPrev.TabIndex = 3;
            btnPrev.Text = "❮❮";
            btnPrev.Click += btnPrev_Click;
            // 
            // btnPlayPause
            // 
            btnPlayPause.FlatAppearance.BorderSize = 0;
            btnPlayPause.FlatStyle = FlatStyle.Flat;
            btnPlayPause.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnPlayPause.ForeColor = Color.DeepSkyBlue;
            btnPlayPause.Location = new Point(40, 5);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(40, 35);
            btnPlayPause.TabIndex = 0;
            btnPlayPause.Text = "▶";
            btnPlayPause.Click += btnPlayPause_Click;
            // 
            // btnNext
            // 
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.Font = new Font("Segoe UI", 12F);
            btnNext.ForeColor = Color.White;
            btnNext.Location = new Point(80, 5);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(35, 35);
            btnNext.TabIndex = 4;
            btnNext.Text = "❯❯";
            btnNext.Click += btnNext_Click;
            // 
            // btnStop
            // 
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Font = new Font("Segoe UI", 10F);
            btnStop.ForeColor = Color.White;
            btnStop.Location = new Point(120, 5);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(35, 35);
            btnStop.TabIndex = 5;
            btnStop.Text = "■";
            btnStop.Click += btnStop_Click;
            // 
            // tbPosition
            // 
            tbPosition.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbPosition.BackColor = Color.Transparent;
            tbPosition.HighlightColor = Color.DeepSkyBlue;
            tbPosition.Location = new Point(161, 12);
            tbPosition.Maximum = 1000F;
            tbPosition.Minimum = 0F;
            tbPosition.Name = "tbPosition";
            tbPosition.Size = new Size(250, 25);
            tbPosition.TabIndex = 1;
            tbPosition.ThumbColor = Color.White;
            tbPosition.TrackColor = Color.FromArgb(40, 255, 255, 255);
            tbPosition.Value = 0F;
            tbPosition.Scroll += tbPosition_Scroll;
            tbPosition.MouseDown += tbPosition_MouseDown;
            tbPosition.MouseUp += tbPosition_MouseUp;
            // 
            // lblTime
            // 
            lblTime.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTime.ForeColor = Color.Silver;
            lblTime.Location = new Point(417, 13);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(85, 20);
            lblTime.TabIndex = 2;
            lblTime.Text = "00:00/00:00";
            lblTime.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelVolume
            // 
            labelVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelVolume.ForeColor = Color.White;
            labelVolume.Location = new Point(508, 15);
            labelVolume.Name = "labelVolume";
            labelVolume.Size = new Size(25, 20);
            labelVolume.TabIndex = 6;
            labelVolume.Text = "🔊";
            labelVolume.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tbVolume
            // 
            tbVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tbVolume.BackColor = Color.Transparent;
            tbVolume.HighlightColor = Color.DeepSkyBlue;
            tbVolume.Location = new Point(533, 15);
            tbVolume.Maximum = 100F;
            tbVolume.Minimum = 0F;
            tbVolume.Name = "tbVolume";
            tbVolume.Size = new Size(70, 20);
            tbVolume.TabIndex = 7;
            tbVolume.ThumbColor = Color.White;
            tbVolume.TrackColor = Color.FromArgb(40, 255, 255, 255);
            tbVolume.Value = 80F;
            tbVolume.Scroll += tbVolume_Scroll;
            // 
            // cmbRate
            // 
            cmbRate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbRate.BackColor = Color.FromArgb(45, 45, 45);
            cmbRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRate.FlatStyle = FlatStyle.Flat;
            cmbRate.ForeColor = Color.White;
            cmbRate.FormattingEnabled = true;
            cmbRate.Items.AddRange(new object[] { "0.5x", "0.75x", "1.0x", "1.25x", "1.5x", "2.0x" });
            cmbRate.Location = new Point(607, 13);
            cmbRate.Name = "cmbRate";
            cmbRate.Size = new Size(65, 23);
            cmbRate.TabIndex = 8;
            cmbRate.SelectedIndexChanged += cmbRate_SelectedIndexChanged;
            // 
            // pnlLyricsHost
            // 
            pnlLyricsHost.BackColor = Color.FromArgb(28, 28, 28);
            pnlLyricsHost.Controls.Add(btnPipMode);
            pnlLyricsHost.Dock = DockStyle.Fill;
            pnlLyricsHost.Location = new Point(0, 0);
            pnlLyricsHost.Name = "pnlLyricsHost";
            pnlLyricsHost.Size = new Size(680, 213);
            pnlLyricsHost.TabIndex = 2;
            // 
            // btnPipMode
            // 
            btnPipMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPipMode.BackColor = Color.FromArgb(60, 60, 60);
            btnPipMode.Cursor = Cursors.Hand;
            btnPipMode.FlatAppearance.BorderSize = 0;
            btnPipMode.FlatAppearance.MouseOverBackColor = Color.DeepSkyBlue;
            btnPipMode.FlatStyle = FlatStyle.Flat;
            btnPipMode.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnPipMode.ForeColor = Color.White;
            btnPipMode.Location = new Point(620, 10);
            btnPipMode.Name = "btnPipMode";
            btnPipMode.Size = new Size(50, 26);
            btnPipMode.TabIndex = 4;
            btnPipMode.Text = "PIP";
            btnPipMode.UseVisualStyleBackColor = false;
            btnPipMode.Click += btnPipMode_Click;
            // 
            // lbPlaylist
            // 
            lbPlaylist.BackColor = Color.FromArgb(32, 32, 32);
            lbPlaylist.BorderStyle = BorderStyle.None;
            lbPlaylist.ContextMenuStrip = ctxPlaylist;
            lbPlaylist.CurrentPlayingPath = "";
            lbPlaylist.Dock = DockStyle.Fill;
            lbPlaylist.DrawMode = DrawMode.OwnerDrawFixed;
            lbPlaylist.DurationColor = Color.Gray;
            lbPlaylist.ForeColor = Color.Gainsboro;
            lbPlaylist.FormattingEnabled = true;
            lbPlaylist.HoverBackColor = Color.FromArgb(40, 255, 255, 255);
            lbPlaylist.ItemHeight = 50;
            lbPlaylist.Location = new Point(0, 65);
            lbPlaylist.Name = "lbPlaylist";
            lbPlaylist.PlayingBackColor = Color.FromArgb(60, 0, 191, 255);
            lbPlaylist.SelectedBackColor = Color.FromArgb(80, 0, 120, 215);
            lbPlaylist.Size = new Size(276, 443);
            lbPlaylist.TabIndex = 3;
            lbPlaylist.TextColor = Color.Gainsboro;
            lbPlaylist.DoubleClick += lbPlaylist_DoubleClick;
            // 
            // ctxPlaylist
            // 
            ctxPlaylist.BackColor = Color.FromArgb(45, 45, 45);
            ctxPlaylist.ForeColor = Color.White;
            ctxPlaylist.Items.AddRange(new ToolStripItem[] { menuOpenFileLocation, menuRemoveFromList, menuRefreshList });
            ctxPlaylist.Name = "ctxPlaylist";
            ctxPlaylist.Size = new Size(155, 70);
            // 
            // menuOpenFileLocation
            // 
            menuOpenFileLocation.Name = "menuOpenFileLocation";
            menuOpenFileLocation.Size = new Size(154, 22);
            menuOpenFileLocation.Text = "파일 위치 열기";
            menuOpenFileLocation.Click += menuOpenFileLocation_Click;
            // 
            // menuRemoveFromList
            // 
            menuRemoveFromList.Name = "menuRemoveFromList";
            menuRemoveFromList.Size = new Size(154, 22);
            menuRemoveFromList.Text = "목록에서 제거";
            menuRemoveFromList.Click += menuRemoveFromList_Click;
            // 
            // menuRefreshList
            // 
            menuRefreshList.Name = "menuRefreshList";
            menuRefreshList.Size = new Size(154, 22);
            menuRefreshList.Text = "새로고침";
            menuRefreshList.Click += menuRefreshList_Click;
            // 
            // pnlPlaylistFooter
            // 
            pnlPlaylistFooter.BackColor = Color.FromArgb(35, 35, 35);
            pnlPlaylistFooter.Controls.Add(btnOpenFolder);
            pnlPlaylistFooter.Dock = DockStyle.Bottom;
            pnlPlaylistFooter.Location = new Point(0, 508);
            pnlPlaylistFooter.Name = "pnlPlaylistFooter";
            pnlPlaylistFooter.Padding = new Padding(5);
            pnlPlaylistFooter.Size = new Size(276, 40);
            pnlPlaylistFooter.TabIndex = 5;
            // 
            // btnOpenFolder
            // 
            btnOpenFolder.BackColor = Color.FromArgb(50, 50, 50);
            btnOpenFolder.Dock = DockStyle.Fill;
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnOpenFolder.ForeColor = Color.Silver;
            btnOpenFolder.Location = new Point(5, 5);
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new Size(266, 30);
            btnOpenFolder.TabIndex = 0;
            btnOpenFolder.Text = "📂 탐색기로 폴더 열기";
            btnOpenFolder.UseVisualStyleBackColor = false;
            btnOpenFolder.Click += btnOpenFolder_Click;
            // 
            // pnlPlaylistHeader
            // 
            pnlPlaylistHeader.BackColor = Color.FromArgb(35, 35, 35);
            pnlPlaylistHeader.Controls.Add(txtSearch);
            pnlPlaylistHeader.Controls.Add(lblPlaylistInfo);
            pnlPlaylistHeader.Dock = DockStyle.Top;
            pnlPlaylistHeader.Location = new Point(0, 0);
            pnlPlaylistHeader.Name = "pnlPlaylistHeader";
            pnlPlaylistHeader.Padding = new Padding(8);
            pnlPlaylistHeader.Size = new Size(276, 65);
            pnlPlaylistHeader.TabIndex = 4;
            // 
            // txtSearch
            // 
            txtSearch.BackColor = Color.FromArgb(45, 45, 45);
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.Dock = DockStyle.Bottom;
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.ForeColor = Color.White;
            txtSearch.Location = new Point(8, 39);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = " 검색...";
            txtSearch.Size = new Size(260, 18);
            txtSearch.TabIndex = 0;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // lblPlaylistInfo
            // 
            lblPlaylistInfo.AutoSize = true;
            lblPlaylistInfo.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblPlaylistInfo.ForeColor = Color.Gray;
            lblPlaylistInfo.Location = new Point(8, 10);
            lblPlaylistInfo.Name = "lblPlaylistInfo";
            lblPlaylistInfo.Size = new Size(55, 15);
            lblPlaylistInfo.TabIndex = 1;
            lblPlaylistInfo.Text = "ITEMS: 0";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Top;
            label3.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label3.ForeColor = Color.DeepSkyBlue;
            label3.Location = new Point(12, 12);
            label3.Name = "label3";
            label3.Size = new Size(121, 25);
            label3.TabIndex = 8;
            label3.Text = "Lyrics Player";
            // 
            // panel1
            // 
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label2);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(12, 620);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(0, 10, 0, 0);
            panel1.Size = new Size(960, 35);
            panel1.TabIndex = 9;
            // 
            // label4
            // 
            label4.Dock = DockStyle.Right;
            label4.Font = new Font("Segoe UI", 8.5F);
            label4.ForeColor = Color.Gray;
            label4.Location = new Point(710, 10);
            label4.Name = "label4";
            label4.Size = new Size(250, 25);
            label4.TabIndex = 9;
            label4.Text = "Developed by tharu & AI Agent";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            label2.Dock = DockStyle.Left;
            label2.Font = new Font("Segoe UI", 8.5F);
            label2.ForeColor = Color.Gray;
            label2.Location = new Point(0, 10);
            label2.Name = "label2";
            label2.Size = new Size(700, 25);
            label2.TabIndex = 8;
            label2.Text = "팁: 가사 파일(LRC, SRT)은 영상과 동일한 이름으로 같은 폴더에 넣어주세요.";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(24, 24, 24);
            ClientSize = new Size(984, 667);
            Controls.Add(splitContainer2);
            Controls.Add(label1);
            Controls.Add(label3);
            Controls.Add(panel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(800, 600);
            Name = "MainForm";
            Padding = new Padding(12);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lyrics Player";
            Load += Form1_Load;
            DragDrop += MainForm_DragDrop;
            DragEnter += MainForm_DragEnter;
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)videoView).EndInit();
            pnlVideoControls.ResumeLayout(false);
            pnlLyricsHost.ResumeLayout(false);
            ctxPlaylist.ResumeLayout(false);
            pnlPlaylistFooter.ResumeLayout(false);
            pnlPlaylistHeader.ResumeLayout(false);
            pnlPlaylistHeader.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Timer tmrSync;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Panel pnlLyricsHost;
		private System.Windows.Forms.Button btnPipMode;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;

		private LibVLCSharp.WinForms.VideoView videoView;
		private System.Windows.Forms.Panel pnlVideoControls;
		private System.Windows.Forms.Button btnPrev;
		private System.Windows.Forms.Button btnPlayPause;
		private System.Windows.Forms.Button btnNext;
		private System.Windows.Forms.Button btnStop;
		
		private LyricsPlayer.UI.Controls.ModernSlider tbPosition;
		private System.Windows.Forms.Label lblTime;
		private LyricsPlayer.UI.Controls.ModernSlider tbVolume;
		
		private System.Windows.Forms.ComboBox cmbRate;
		private System.Windows.Forms.Label labelVolume;

		private System.Windows.Forms.Panel pnlPlaylistHeader;
		private System.Windows.Forms.TextBox txtSearch;
		private System.Windows.Forms.Label lblPlaylistInfo;
		
		private LyricsPlayer.UI.Controls.ModernListBox lbPlaylist;
		private System.Windows.Forms.Panel pnlPlaylistFooter;
		private System.Windows.Forms.Button btnOpenFolder;

		private System.Windows.Forms.ContextMenuStrip ctxPlaylist;
		private System.Windows.Forms.ToolStripMenuItem menuOpenFileLocation;
		private System.Windows.Forms.ToolStripMenuItem menuRemoveFromList;
		private System.Windows.Forms.ToolStripMenuItem menuRefreshList;
	}
}
