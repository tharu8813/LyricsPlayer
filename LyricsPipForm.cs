namespace LyricsPlayer {
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

		public LyricsPipForm() {
			InitializeComponent();

			this.MinimumSize = new Size(200, 100);
			this.MaximumSize = new Size(1000, 700);
			this.LyricsHostPanel.MouseDown += LyricsHostPanel_MouseDown;
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
			panel1.SuspendLayout();
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
			closeButton.Location = new Point(582, 0);
			closeButton.Name = "closeButton";
			closeButton.Size = new Size(35, 28);
			closeButton.TabIndex = 0;
			closeButton.Text = "X";
			closeButton.UseVisualStyleBackColor = false;
			closeButton.Click += closeButton_Click;
			// 
			// LyricsHostPanel
			// 
			LyricsHostPanel.BackColor = Color.FromArgb(32, 32, 32);
			LyricsHostPanel.BorderStyle = BorderStyle.FixedSingle;
			LyricsHostPanel.Dock = DockStyle.Fill;
			LyricsHostPanel.Location = new Point(3, 33);
			LyricsHostPanel.Margin = new Padding(5);
			LyricsHostPanel.Name = "LyricsHostPanel";
			LyricsHostPanel.Size = new Size(619, 137);
			LyricsHostPanel.TabIndex = 0;
			// 
			// panel1
			// 
			panel1.BackColor = Color.FromArgb(32, 32, 32);
			panel1.BorderStyle = BorderStyle.FixedSingle;
			panel1.Controls.Add(closeButton);
			panel1.Dock = DockStyle.Top;
			panel1.Location = new Point(3, 3);
			panel1.Name = "panel1";
			panel1.Size = new Size(619, 30);
			panel1.TabIndex = 2;
			panel1.MouseDown += panel1_MouseDown;
			// 
			// LyricsPipForm
			// 
			BackColor = Color.DimGray;
			ClientSize = new Size(625, 173);
			Controls.Add(LyricsHostPanel);
			Controls.Add(panel1);
			FormBorderStyle = FormBorderStyle.None;
			Icon = (Icon)resources.GetObject("$this.Icon");
			Name = "LyricsPipForm";
			Padding = new Padding(3);
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Lyrics PIP";
			TopMost = true;
			panel1.ResumeLayout(false);
			ResumeLayout(false);

		}
	}
}