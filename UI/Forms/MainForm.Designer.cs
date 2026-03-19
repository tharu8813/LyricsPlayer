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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			tmrSync = new System.Windows.Forms.Timer(components);
			label1 = new System.Windows.Forms.Label();
			splitContainer2 = new System.Windows.Forms.SplitContainer();
			splitContainer1 = new System.Windows.Forms.SplitContainer();
			player = new AxWMPLib.AxWindowsMediaPlayer();
			pnlLyricsHost = new System.Windows.Forms.Panel();
			btnPipMode = new System.Windows.Forms.Button();
			dataGridView1 = new System.Windows.Forms.DataGridView();
			label3 = new System.Windows.Forms.Label();
			panel1 = new System.Windows.Forms.Panel();
			label4 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
			splitContainer2.Panel1.SuspendLayout();
			splitContainer2.Panel2.SuspendLayout();
			splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)player).BeginInit();
			pnlLyricsHost.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
			panel1.SuspendLayout();
			SuspendLayout();
			// 
			// tmrSync
			// 
			tmrSync.Interval = 33;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.BackColor = Color.Transparent;
			label1.Dock = DockStyle.Top;
			label1.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
			label1.ForeColor = Color.Gainsboro;
			label1.Location = new Point(12, 40);
			label1.Name = "label1";
			label1.Padding = new Padding(0, 5, 0, 10);
			label1.Size = new Size(56, 35);
			label1.TabIndex = 3;
			label1.Text = "Playlist";
			// 
			// splitContainer2
			// 
			splitContainer2.Dock = DockStyle.Fill;
			splitContainer2.Location = new Point(12, 75);
			splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			splitContainer2.Panel1.Controls.Add(splitContainer1);
			// 
			// splitContainer2.Panel2
			// 
			splitContainer2.Panel2.Controls.Add(dataGridView1);
			splitContainer2.Size = new Size(960, 545);
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
			splitContainer1.Panel1.Controls.Add(player);
			// 
			// splitContainer1.Panel2
			// 
			splitContainer1.Panel2.Controls.Add(pnlLyricsHost);
			splitContainer1.Size = new Size(680, 545);
			splitContainer1.SplitterDistance = 380;
			splitContainer1.TabIndex = 6;
			// 
			// player
			// 
			player.Dock = DockStyle.Fill;
			player.Enabled = true;
			player.Location = new Point(0, 0);
			player.Name = "player";
			player.OcxState = (AxHost.State)resources.GetObject("player.OcxState");
			player.Size = new Size(680, 380);
			player.TabIndex = 0;
			// 
			// pnlLyricsHost
			// 
			pnlLyricsHost.BackColor = Color.FromArgb(28, 28, 28);
			pnlLyricsHost.Controls.Add(btnPipMode);
			pnlLyricsHost.Dock = DockStyle.Fill;
			pnlLyricsHost.Location = new Point(0, 0);
			pnlLyricsHost.Name = "pnlLyricsHost";
			pnlLyricsHost.Size = new Size(680, 161);
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
			// dataGridView1
			// 
			dataGridView1.AllowUserToAddRows = false;
			dataGridView1.AllowUserToDeleteRows = false;
			dataGridView1.AllowUserToResizeColumns = false;
			dataGridView1.AllowUserToResizeRows = false;
			dataGridView1.BackgroundColor = Color.FromArgb(32, 32, 32);
			dataGridView1.BorderStyle = BorderStyle.None;
			dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.None;
			dataGridView1.ColumnHeadersVisible = false;
			dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = Color.FromArgb(32, 32, 32);
			dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
			dataGridViewCellStyle1.ForeColor = Color.Silver;
			dataGridViewCellStyle1.Padding = new Padding(10, 0, 0, 0);
			dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(0, 120, 215);
			dataGridViewCellStyle1.SelectionForeColor = Color.White;
			dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
			dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
			dataGridView1.Dock = DockStyle.Fill;
			dataGridView1.GridColor = Color.FromArgb(45, 45, 45);
			dataGridView1.Location = new Point(0, 0);
			dataGridView1.MultiSelect = false;
			dataGridView1.Name = "dataGridView1";
			dataGridView1.ReadOnly = true;
			dataGridView1.RowHeadersVisible = false;
			dataGridView1.RowTemplate.Height = 45;
			dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			dataGridView1.Size = new Size(276, 545);
			dataGridView1.TabIndex = 3;
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Dock = DockStyle.Top;
			label3.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
			label3.ForeColor = Color.DeepSkyBlue;
			label3.Location = new Point(12, 12);
			label3.Name = "label3";
			label3.Size = new Size(131, 25);
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
			splitContainer2.Panel1.ResumeLayout(false);
			splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
			splitContainer2.ResumeLayout(false);
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)player).EndInit();
			pnlLyricsHost.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
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
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;
		public AxWMPLib.AxWindowsMediaPlayer player;
	}
}
