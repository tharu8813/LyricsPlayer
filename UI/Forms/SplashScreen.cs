using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace LyricsPlayer.UI.Forms {
    public partial class SplashScreen : Form {
        public SplashScreen() {
            InitializeComponent();
        }

        protected override async void OnLoad(EventArgs e) {
            base.OnLoad(e);

            await Task.Run(() => {
                try {
                    Thread.Sleep(500); //바로 꺼지면 섭섭해서 넣었음ㅋ
                    LibVLCSharp.Shared.Core.Initialize();
                } catch (Exception ex) {
                    MessageBox.Show("LibVLC 초기화 중 오류가 발생했습니다: " + ex.Message);
                }
            });

            this.Close();
        }
    }
}
