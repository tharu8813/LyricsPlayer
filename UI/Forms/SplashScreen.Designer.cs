namespace LyricsPlayer.UI.Forms {
    partial class SplashScreen {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            lblStatus = new Label();
            progressBar1 = new ProgressBar();
            lblTitle = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(23, 114);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(110, 15);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "LibVLC 초기화 중...";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(23, 137);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(350, 23);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 1;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(82, 39);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(156, 32);
            lblTitle.TabIndex = 2;
            lblTitle.Text = "Lyrics Player";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.icon;
            pictureBox1.Location = new Point(31, 31);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(45, 45);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // SplashScreen
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(45, 45, 48);
            ClientSize = new Size(400, 198);
            Controls.Add(pictureBox1);
            Controls.Add(lblTitle);
            Controls.Add(progressBar1);
            Controls.Add(lblStatus);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SplashScreen";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SplashScreen";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblTitle;
        private PictureBox pictureBox1;
    }
}
