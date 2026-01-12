namespace LyricsPlayer {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            tmrSync = new System.Windows.Forms.Timer(components);
            label1 = new Label();
            splitContainer2 = new SplitContainer();
            splitContainer1 = new SplitContainer();
            player = new AxWMPLib.AxWindowsMediaPlayer();
            pnlLyricsHost = new Panel();
            btnPipMode = new Button();
            dataGridView1 = new DataGridView();
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
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label1.ForeColor = Color.White;
            label1.Location = new Point(10, 25);
            label1.Name = "label1";
            label1.Padding = new Padding(0, 0, 0, 8);
            label1.Size = new Size(66, 29);
            label1.TabIndex = 3;
            label1.Text = "Playlist";
            // 
            // splitContainer2
            // 
            splitContainer2.BorderStyle = BorderStyle.FixedSingle;
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(10, 54);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(dataGridView1);
            splitContainer2.Size = new Size(845, 482);
            splitContainer2.SplitterDistance = 580;
            splitContainer2.TabIndex = 6;
            // 
            // splitContainer1
            // 
            splitContainer1.BackColor = Color.FromArgb(32, 32, 32);
            splitContainer1.BorderStyle = BorderStyle.FixedSingle;
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
            splitContainer1.Size = new Size(580, 482);
            splitContainer1.SplitterDistance = 351;
            splitContainer1.SplitterWidth = 1;
            splitContainer1.TabIndex = 6;
            // 
            // player
            // 
            player.Dock = DockStyle.Fill;
            player.Enabled = true;
            player.Location = new Point(0, 0);
            player.Name = "player";
            player.OcxState = (AxHost.State)resources.GetObject("player.OcxState");
            player.Size = new Size(578, 349);
            player.TabIndex = 0;
            // 
            // pnlLyricsHost
            // 
            pnlLyricsHost.BackColor = Color.FromArgb(32, 32, 32);
            pnlLyricsHost.Controls.Add(btnPipMode);
            pnlLyricsHost.Dock = DockStyle.Fill;
            pnlLyricsHost.Location = new Point(0, 0);
            pnlLyricsHost.Margin = new Padding(5);
            pnlLyricsHost.Name = "pnlLyricsHost";
            pnlLyricsHost.Size = new Size(578, 128);
            pnlLyricsHost.TabIndex = 2;
            // 
            // btnPipMode
            // 
            btnPipMode.BackColor = Color.FromArgb(60, 60, 60);
            btnPipMode.FlatAppearance.BorderSize = 0;
            btnPipMode.FlatStyle = FlatStyle.Flat;
            btnPipMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnPipMode.ForeColor = Color.Gainsboro;
            btnPipMode.Location = new Point(3, 4);
            btnPipMode.Name = "btnPipMode";
            btnPipMode.Size = new Size(50, 25);
            btnPipMode.TabIndex = 4;
            btnPipMode.Text = "PIP";
            btnPipMode.UseVisualStyleBackColor = false;
            btnPipMode.Click += btnPipMode_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(50, 50, 52);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = Color.Gainsboro;
            dataGridViewCellStyle1.Padding = new Padding(8);
            dataGridViewCellStyle1.SelectionBackColor = Color.DeepSkyBlue;
            dataGridViewCellStyle1.SelectionForeColor = Color.White;
            dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.BackgroundColor = Color.FromArgb(45, 45, 48);
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.GridColor = Color.FromArgb(60, 60, 60);
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Margin = new Padding(5);
            dataGridView1.MultiSelect = false;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowTemplate.Height = 40;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(259, 480);
            dataGridView1.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Top;
            label3.ForeColor = Color.White;
            label3.Location = new Point(10, 10);
            label3.Name = "label3";
            label3.Size = new Size(161, 15);
            label3.TabIndex = 8;
            label3.Text = "Lyrics Player (가사 플레이어)";
            // 
            // panel1
            // 
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label2);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(10, 536);
            panel1.Name = "panel1";
            panel1.Size = new Size(845, 23);
            panel1.TabIndex = 9;
            // 
            // label4
            // 
            label4.Dock = DockStyle.Fill;
            label4.ForeColor = Color.White;
            label4.Location = new Point(638, 0);
            label4.Name = "label4";
            label4.Size = new Size(207, 23);
            label4.TabIndex = 9;
            label4.Text = "by. tharu(Ji Back Min) and AI";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            label2.Dock = DockStyle.Left;
            label2.ForeColor = Color.White;
            label2.Location = new Point(0, 0);
            label2.Name = "label2";
            label2.Size = new Size(638, 23);
            label2.TabIndex = 8;
            label2.Text = "* 비디오 폴더에 \"LyricsPlayer\" 으로 영상과 자막(LRC, SRT) 파일을 같은 이름으로 넣어주세요.";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(50, 50, 50);
            ClientSize = new Size(865, 569);
            Controls.Add(splitContainer2);
            Controls.Add(label1);
            Controls.Add(label3);
            Controls.Add(panel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Padding = new Padding(10);
            StartPosition = FormStartPosition.CenterParent;
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

        #endregion
        private System.Windows.Forms.Timer tmrSync;
		private Label label1;
		private SplitContainer splitContainer2;
		private SplitContainer splitContainer1;
		private Panel pnlLyricsHost;
		private Button btnPipMode;
		private DataGridView dataGridView1;
		private Label label3;
		private Panel panel1;
		private Label label2;
		private Label label4;
		public AxWMPLib.AxWindowsMediaPlayer player;
	}
}