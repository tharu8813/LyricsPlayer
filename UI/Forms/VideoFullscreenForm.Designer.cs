namespace LyricsPlayer.Forms {
    partial class VideoFullscreenForm {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            pnlBottom = new Panel();
            tbPosition = new LyricsPlayer.UI.Controls.ModernSlider();
            btnPlayPause = new Button();
            lblTime = new Label();
            splitContainer1 = new SplitContainer();
            pnlVideoArea = new Panel();
            pnlLyricsHost = new Panel();
            pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // pnlBottom
            // 
            pnlBottom.BackColor = Color.FromArgb(25, 25, 25);
            pnlBottom.Controls.Add(tbPosition);
            pnlBottom.Controls.Add(btnPlayPause);
            pnlBottom.Controls.Add(lblTime);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 660);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new Padding(16, 0, 16, 0);
            pnlBottom.Size = new Size(1280, 60);
            pnlBottom.TabIndex = 2;
            // 
            // tbPosition
            // 
            tbPosition.BackColor = Color.Transparent;
            tbPosition.Dock = DockStyle.Fill;
            tbPosition.HighlightColor = Color.DeepSkyBlue;
            tbPosition.Location = new Point(66, 0);
            tbPosition.Maximum = 1000F;
            tbPosition.Name = "tbPosition";
            tbPosition.Size = new Size(1038, 60);
            tbPosition.TabIndex = 0;
            tbPosition.ThumbColor = Color.White;
            tbPosition.TrackColor = Color.FromArgb(60, 255, 255, 255);
            tbPosition.Value = 0F;
            tbPosition.Scroll += tbPosition_Scroll;
            tbPosition.MouseDown += tbPosition_MouseDown;
            tbPosition.MouseUp += tbPosition_MouseUp;
            // 
            // btnPlayPause
            // 
            btnPlayPause.Dock = DockStyle.Left;
            btnPlayPause.FlatAppearance.BorderSize = 0;
            btnPlayPause.FlatStyle = FlatStyle.Flat;
            btnPlayPause.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            btnPlayPause.ForeColor = Color.DeepSkyBlue;
            btnPlayPause.Location = new Point(16, 0);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(50, 60);
            btnPlayPause.TabIndex = 1;
            btnPlayPause.Text = "II";
            btnPlayPause.Click += btnPlayPause_Click;
            // 
            // lblTime
            // 
            lblTime.Dock = DockStyle.Right;
            lblTime.Font = new Font("Segoe UI", 11F);
            lblTime.ForeColor = Color.Gainsboro;
            lblTime.Location = new Point(1104, 0);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(160, 60);
            lblTime.TabIndex = 2;
            lblTime.Text = "00:00 / 00:00";
            lblTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(pnlVideoArea);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pnlLyricsHost);
            splitContainer1.Size = new Size(1280, 660);
            splitContainer1.SplitterDistance = 962;
            splitContainer1.TabIndex = 0;
            // 
            // pnlVideoArea
            // 
            pnlVideoArea.BackColor = Color.Black;
            pnlVideoArea.Dock = DockStyle.Fill;
            pnlVideoArea.Location = new Point(0, 0);
            pnlVideoArea.Name = "pnlVideoArea";
            pnlVideoArea.Size = new Size(962, 660);
            pnlVideoArea.TabIndex = 1;
            // 
            // pnlLyricsHost
            // 
            pnlLyricsHost.BackColor = Color.FromArgb(30, 30, 30);
            pnlLyricsHost.Dock = DockStyle.Fill;
            pnlLyricsHost.Location = new Point(0, 0);
            pnlLyricsHost.Name = "pnlLyricsHost";
            pnlLyricsHost.Size = new Size(314, 660);
            pnlLyricsHost.TabIndex = 2;
            // 
            // VideoFullscreenForm
            // 
            BackColor = Color.FromArgb(28, 28, 28);
            ClientSize = new Size(1280, 720);
            Controls.Add(splitContainer1);
            Controls.Add(pnlBottom);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "VideoFullscreenForm";
            Text = "Fullscreen Player";
            pnlBottom.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnPlayPause;
        private LyricsPlayer.UI.Controls.ModernSlider tbPosition;
        private System.Windows.Forms.Label lblTime;
        private SplitContainer splitContainer1;
        private Panel pnlVideoArea;
        private Panel pnlLyricsHost;
    }
}