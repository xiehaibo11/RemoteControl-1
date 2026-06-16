using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.SubController
{
    public class FrmVideoViewer : Form
    {
        private readonly SocketSession _session;
        private PictureBox _pictureBox;
        private Panel _panel;
        private ToolStrip _toolStrip;
        private Image _latestFrame;
        private int _rendering;
        private readonly object _frameLock = new object();

        public FrmVideoViewer(SocketSession session)
        {
            _session = session;
            InitializeUI();
            this.Text = "摄像头查看 - " + (session.HostName ?? session.SocketId);
            this.Load += FrmVideoViewer_Load;
            this.FormClosing += FrmVideoViewer_FormClosing;
        }

        private void InitializeUI()
        {
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            _toolStrip = new ToolStrip();
            var btnSave = new ToolStripButton("保存截图");
            btnSave.Click += BtnSave_Click;
            _toolStrip.Items.Add(btnSave);

            _panel = new Panel();
            _panel.Dock = DockStyle.Fill;
            _panel.AutoScroll = true;

            _pictureBox = new PictureBox();
            _pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            _panel.Controls.Add(_pictureBox);

            this.Controls.Add(_panel);
            this.Controls.Add(_toolStrip);
        }

        private void FrmVideoViewer_Load(object sender, EventArgs e)
        {
            StartCapture();
        }

        private void StartCapture()
        {
            var req = new RequestStartCaptureVideo();
            req.Fps = 3;
            _session.Send(ePacketType.PACKET_START_CAPTURE_VIDEO_REQUEST, req);
        }

        private void FrmVideoViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            _session.Send(ePacketType.PACKET_STOP_CAPTURE_VIDEO_REQUEST, null);

            lock (_frameLock)
            {
                if (_latestFrame != null)
                {
                    _latestFrame.Dispose();
                    _latestFrame = null;
                }
            }
        }

        public void HandleVideo(ResponseStartCaptureVideo resp)
        {
            if (resp == null || resp.ImageData == null || resp.ImageData.Length == 0)
                return;

            Image img;
            try
            {
                img = resp.GetImage();
            }
            catch
            {
                return;
            }

            lock (_frameLock)
            {
                if (_latestFrame != null)
                    _latestFrame.Dispose();
                _latestFrame = img;
            }

            if (Interlocked.CompareExchange(ref _rendering, 1, 0) == 0)
            {
                this.BeginInvoke((Action)RenderFrame);
            }
        }

        private void RenderFrame()
        {
            Image frame;
            lock (_frameLock)
            {
                frame = _latestFrame;
                _latestFrame = null;
            }
            if (frame == null)
            {
                Interlocked.Exchange(ref _rendering, 0);
                return;
            }

            var old = _pictureBox.Image;
            _pictureBox.Image = frame;
            if (old != null)
                old.Dispose();

            Interlocked.Exchange(ref _rendering, 0);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_pictureBox.Image == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG 图片|*.png|JPEG 图片|*.jpg";
                dlg.FileName = "video_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _pictureBox.Image.Save(dlg.FileName);
                }
            }
        }
    }
}
