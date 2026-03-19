namespace LyricsPlayer.UI.Forms {
	public partial class LyricsPipForm : Form {
		private const int WM_NCLBUTTONDOWN = 0xA1;
		private const int HT_CAPTION = 0x2;

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		private Button closeButton;
		public Panel LyricsHostPanel;
		private Panel panel1;
		private Panel statusPanel;
		private Label lblTitle;
		private Label lblTime;

		public LyricsPipForm() {
			InitializeComponent();

			this.MinimumSize = new Size(300, 120);
			this.MaximumSize = new Size(1200, 800);
			this.LyricsHostPanel.MouseDown += LyricsHostPanel_MouseDown;
		}

		public void UpdateStatus(string title, string currentTime, string totalTime) {
			if (InvokeRequired) {
				Invoke(new Action(() => UpdateStatus(title, currentTime, totalTime)));
				return;
			}
			lblTitle.Text = title;
			lblTime.Text = $"{currentTime} / {totalTime}";
		}

		private void panel1_MouseDown(object sender, MouseEventArgs e) {
			if(e.Button == MouseButtons.Left) {
				ReleaseCapture();
				SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}

		private void LyricsHostPanel_MouseDown(object sender, MouseEventArgs e) {
			if(e.Button == MouseButtons.Left) {
				ReleaseCapture();
				SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}

		private void closeButton_Click(object sender, EventArgs e) {
			this.Close();
		}

		protected override void WndProc(ref Message m) {
			const int WM_NCHITTEST = 0x84;
			const int HTCLIENT = 0x1;
			const int HTLEFT = 10;
			const int HTRIGHT = 11;
			const int HTTOP = 12;
			const int HTTOPLEFT = 13;
			const int HTTOPRIGHT = 14;
			const int HTBOTTOM = 15;
			const int HTBOTTOMLEFT = 16;
			const int HTBOTTOMRIGHT = 17;

			base.WndProc(ref m);

			if(m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT) {
				var screenPoint = new Point(m.LParam.ToInt32());
				var clientPoint = this.PointToClient(screenPoint);
				int resizeArea = 5;

				if(clientPoint.Y <= resizeArea) {
					if(clientPoint.X <= resizeArea)
						m.Result = (IntPtr)HTTOPLEFT;
					else if(clientPoint.X >= this.ClientSize.Width - resizeArea)
						m.Result = (IntPtr)HTTOPRIGHT;
					else
						m.Result = (IntPtr)HTTOP;
				} else if(clientPoint.Y >= this.ClientSize.Height - resizeArea) {
					if(clientPoint.X <= resizeArea)
						m.Result = (IntPtr)HTBOTTOMLEFT;
					else if(clientPoint.X >= this.ClientSize.Width - resizeArea)
						m.Result = (IntPtr)HTBOTTOMRIGHT;
					else
						m.Result = (IntPtr)HTBOTTOM;
				} else if(clientPoint.X <= resizeArea) {
					m.Result = (IntPtr)HTLEFT;
				} else if(clientPoint.X >= this.ClientSize.Width - resizeArea) {
					m.Result = (IntPtr)HTRIGHT;
				}
			}
		}

		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LyricsPipForm));
			closeButton = new Button();
			LyricsHostPanel = new Panel();
			panel1 = new Panel();
			statusPanel = new Panel();
			lblTitle = new Label();
			lblTime = new Label();
			panel1.SuspendLayout();
			statusPanel.SuspendLayout();
			SuspendLayout();
			// 
			// closeButton
			// 
			closeButton.BackColor = Color.Transparent;
			closeButton.Dock = DockStyle.Right;
			closeButton.FlatAppearance.BorderSize = 0;
			closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
			closeButton.FlatStyle = FlatStyle.Flat;
			closeButton.Font = new Font("Arial", 10F, FontStyle.Bold);
			closeButton.ForeColor = Color.Gainsboro;
			closeButton.Location = new Point(562, 0);
			closeButton.Name = "closeButton";
			closeButton.Size = new Size(35, 28);
			closeButton.TabIndex = 0;
			closeButton.Text = "X";
			closeButton.UseVisualStyleBackColor = false;
			closeButton.Click += closeButton_Click;
			// 
			// LyricsHostPanel
			// 
			LyricsHostPanel.BackColor = Color.FromArgb(24, 24, 24);
			LyricsHostPanel.Dock = DockStyle.Fill;
			LyricsHostPanel.Location = new Point(3, 33);
			LyricsHostPanel.Name = "LyricsHostPanel";
			LyricsHostPanel.Size = new Size(599, 109);
			LyricsHostPanel.TabIndex = 0;
			// 
			// panel1
			// 
			panel1.BackColor = Color.FromArgb(32, 32, 32);
			panel1.Controls.Add(closeButton);
			panel1.Dock = DockStyle.Top;
			panel1.Location = new Point(3, 3);
			panel1.Name = "panel1";
			panel1.Size = new Size(599, 30);
			panel1.TabIndex = 2;
			panel1.MouseDown += panel1_MouseDown;
			// 
			// statusPanel
			// 
			statusPanel.BackColor = Color.FromArgb(40, 0, 0, 0);
			statusPanel.Controls.Add(lblTime);
			statusPanel.Controls.Add(lblTitle);
			statusPanel.Dock = DockStyle.Bottom;
			statusPanel.Location = new Point(3, 142);
			statusPanel.Name = "statusPanel";
			statusPanel.Padding = new Padding(10, 0, 10, 0);
			statusPanel.Size = new Size(599, 28);
			statusPanel.TabIndex = 3;
			// 
			// lblTitle
			// 
			lblTitle.Dock = DockStyle.Fill;
			lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			lblTitle.ForeColor = Color.DeepSkyBlue;
			lblTitle.Location = new Point(10, 0);
			lblTitle.Name = "lblTitle";
			lblTitle.Size = new Size(579, 28);
			lblTitle.TabIndex = 0;
			lblTitle.Text = "Title";
			lblTitle.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// lblTime
			// 
			lblTime.Dock = DockStyle.Right;
			lblTime.Font = new Font("Segoe UI Semibold", 8.5F);
			lblTime.ForeColor = Color.Gainsboro;
			lblTime.Location = new Point(449, 0);
			lblTime.Name = "lblTime";
			lblTime.Size = new Size(140, 28);
			lblTime.TabIndex = 1;
			lblTime.Text = "00:00 / 00:00";
			lblTime.TextAlign = ContentAlignment.MiddleRight;
			// 
			// LyricsPipForm
			// 
			BackColor = Color.FromArgb(45, 45, 48);
			ClientSize = new Size(605, 173);
			Controls.Add(LyricsHostPanel);
			Controls.Add(statusPanel);
			Controls.Add(panel1);
			FormBorderStyle = FormBorderStyle.None;
			Icon = (Icon)resources.GetObject("$this.Icon");
			Name = "LyricsPipForm";
			Padding = new Padding(3);
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Lyrics PIP";
			TopMost = true;
			panel1.ResumeLayout(false);
			statusPanel.ResumeLayout(false);
			ResumeLayout(false);

		}
	}
}