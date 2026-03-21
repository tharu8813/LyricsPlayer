using System.Runtime.InteropServices;

namespace LyricsPlayer.UI.Forms {
    public partial class LyricsPipForm : Form {
        // WinAPI 상수 - 클래스 레벨로 통합
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int WM_NCHITTEST = 0x84;
        private const int HT_CAPTION = 0x2;
        private const int HTCLIENT = 0x1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int RESIZE_AREA = 5;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

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

            // 드래그 이동 - 공통 핸들러 연결
            panel1.MouseDown += OnDragMouseDown;
            LyricsHostPanel.MouseDown += OnDragMouseDown;
        }

        public void UpdateStatus(string title, string currentTime, string totalTime) {
            if (InvokeRequired) {
                Invoke(() => UpdateStatus(title, currentTime, totalTime));
                return;
            }
            lblTitle.Text = title;
            lblTime.Text = $"{currentTime} / {totalTime}";
        }

        // panel1_MouseDown + LyricsHostPanel_MouseDown 통합
        private void OnDragMouseDown(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);

            if (m.Msg != WM_NCHITTEST || (int)m.Result != HTCLIENT) return;

            var clientPoint = this.PointToClient(new Point(m.LParam.ToInt32()));
            int x = clientPoint.X;
            int y = clientPoint.Y;
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            if (y <= RESIZE_AREA) {
                if (x <= RESIZE_AREA) m.Result = (IntPtr)HTTOPLEFT;
                else if (x >= w - RESIZE_AREA) m.Result = (IntPtr)HTTOPRIGHT;
                else m.Result = (IntPtr)HTTOP;
            } else if (y >= h - RESIZE_AREA) {
                if (x <= RESIZE_AREA) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (x >= w - RESIZE_AREA) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else m.Result = (IntPtr)HTBOTTOM;
            } else if (x <= RESIZE_AREA) {
                m.Result = (IntPtr)HTLEFT;
            } else if (x >= w - RESIZE_AREA) {
                m.Result = (IntPtr)HTRIGHT;
            }
        }

        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(LyricsPipForm));

            closeButton = new Button();
            LyricsHostPanel = new Panel();
            panel1 = new Panel();
            statusPanel = new Panel();
            lblTitle = new Label();
            lblTime = new Label();

            panel1.SuspendLayout();
            statusPanel.SuspendLayout();
            SuspendLayout();

            // closeButton
            closeButton.BackColor = Color.Transparent;
            closeButton.Dock = DockStyle.Right;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Font = new Font("Arial", 10F, FontStyle.Bold);
            closeButton.ForeColor = Color.Gainsboro;
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(35, 28);
            closeButton.Text = "X";
            closeButton.UseVisualStyleBackColor = false;
            closeButton.Click += (s, e) => this.Close(); // 한 줄짜리 메서드 람다로 대체

            // LyricsHostPanel
            LyricsHostPanel.BackColor = Color.FromArgb(24, 24, 24);
            LyricsHostPanel.Dock = DockStyle.Fill;
            LyricsHostPanel.Name = "LyricsHostPanel";

            // panel1 (드래그 바)
            panel1.BackColor = Color.FromArgb(32, 32, 32);
            panel1.Controls.Add(closeButton);
            panel1.Dock = DockStyle.Top;
            panel1.Name = "panel1";
            panel1.Size = new Size(599, 30);

            // statusPanel
            statusPanel.BackColor = Color.FromArgb(40, 0, 0, 0);
            statusPanel.Controls.Add(lblTime);
            statusPanel.Controls.Add(lblTitle);
            statusPanel.Dock = DockStyle.Bottom;
            statusPanel.Name = "statusPanel";
            statusPanel.Padding = new Padding(10, 0, 10, 0);
            statusPanel.Size = new Size(599, 28);

            // lblTitle
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitle.ForeColor = Color.DeepSkyBlue;
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Title";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // lblTime
            lblTime.Dock = DockStyle.Right;
            lblTime.Font = new Font("Segoe UI Semibold", 8.5F);
            lblTime.ForeColor = Color.Gainsboro;
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(140, 28);
            lblTime.Text = "00:00 / 00:00";
            lblTime.TextAlign = ContentAlignment.MiddleRight;

            // Form
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