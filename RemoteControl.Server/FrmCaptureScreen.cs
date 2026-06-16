using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using log4net;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmCaptureScreen : FrmBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FrmCaptureScreen));
        private SocketSession oSession;
        private bool _isCaptureMouse = false;
        private bool _isCaptureKeyboard = false;
        private readonly object _screenFrameLock = new object();
        private ResponseStartGetScreen _latestScreenResponse;
        private int _screenUpdateScheduled = 0;
        private int _stopCaptureSent = 0;
        private bool _isClosing = false;

        private const int DefaultCaptureFps = 5;

        public FrmCaptureScreen(SocketSession session)
        {
            InitializeComponent();
            this.oSession = session;
        }

        private void SendStopCaptureRequest()
        {
            if (Interlocked.Exchange(ref _stopCaptureSent, 1) == 0 && oSession != null)
            {
                oSession.Send(ePacketType.PACKET_STOP_CAPTURE_SCREEN_REQUEST, null);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;
            button.Checked = !button.Checked;
            if (button.Checked)
            {
                RequestStartGetScreen req = new RequestStartGetScreen();
                req.fps = DefaultCaptureFps;
                Interlocked.Exchange(ref _stopCaptureSent, 0);
                oSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
            }
            else
            {
                SendStopCaptureRequest();
            }
        }

        public void HandleScreen(ResponseStartGetScreen resp)
        {
            if (resp == null || _isClosing || this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            lock (_screenFrameLock)
            {
                _latestScreenResponse = resp;
            }

            if (Interlocked.CompareExchange(ref _screenUpdateScheduled, 1, 0) == 0)
            {
                try
                {
                    this.BeginInvoke(new Action(RenderLatestScreen));
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        private void RenderLatestScreen()
        {
            ResponseStartGetScreen resp = null;
            bool hasMoreFrames = false;

            try
            {
                lock (_screenFrameLock)
                {
                    resp = _latestScreenResponse;
                    _latestScreenResponse = null;
                }

                if (resp == null || !resp.Result)
                {
                    return;
                }

                Image image = resp.GetImage();
                if (image == null)
                {
                    return;
                }

                Image oldImage = this.pictureBox1.Image;
                this.pictureBox1.Image = image;
                if (oldImage != null)
                {
                    oldImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("HandleScreen", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _screenUpdateScheduled, 0);
                lock (_screenFrameLock)
                {
                    hasMoreFrames = _latestScreenResponse != null;
                }

                if (hasMoreFrames && !_isClosing && Interlocked.CompareExchange(ref _screenUpdateScheduled, 1, 0) == 0)
                {
                    try
                    {
                        this.BeginInvoke(new Action(RenderLatestScreen));
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
        }

        private void FrmCaptureScreen_Load(object sender, EventArgs e)
        {
            this.toolStripMenuItemFPS15.Visible = false;
            this.toolStripMenuItemFPS60.Visible = false;
            // Panel增加滚动条
            this.panel1.AutoScroll = true;
            // 根据图像大小，自动调节控件和Image的尺寸
            this.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            SetupMenuStrip();
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                string fileName = "";
                // 直接从picturebox中调用save()的话，容易出现“GDI+ 发生一般性错误”。
                // 此处用bitmap对象中专一次
                using (Bitmap bmp = new Bitmap(this.pictureBox1.Image))
                {
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Filter = "*.bmp|*.bmp|*.jpg;*.jpeg|*.jpg;*.jpeg|*.*|*.*";
                    dialog.FilterIndex = 1;
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        fileName = dialog.FileName;
                        try
                        {
                            using (var ms = new MemoryStream())
                            {
                                bmp.Save(ms, ImageFormat.Jpeg);
                                System.IO.File.WriteAllBytes(fileName, ms.ToArray());
                            }
                            MsgBox.Info("保存成功!");
                        }
                        catch (Exception ex)
                        {
                            MsgBox.Info("保存失败，" + ex.Message);
                        }
                    }
                }
            }
            else
            {
                MsgBox.Info("暂无图像，无法保存！");
            }
        }

    }
}
