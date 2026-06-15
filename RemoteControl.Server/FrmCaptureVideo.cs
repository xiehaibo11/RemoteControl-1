using System;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmCaptureVideo : FrmBase
    {
        private SocketSession oSession;
        private bool saveInRealTime = false;
        private int _fps = 2;
        private bool _captureAudio = false;

        public FrmCaptureVideo(SocketSession session)
        {
            InitializeComponent();
            this.oSession = session;
            if (this.oSession != null)
            {
                this.Text = "摄像头查看 - " + this.oSession.SocketId;
            }
        }

        private void FrmCaptureScreen_Load(object sender, EventArgs e)
        {
            // Panel增加滚动条
            this.panel1.AutoScroll = true;
            // 根据图像大小，自动调节控件和Image的尺寸
            this.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
        }
    }
}
