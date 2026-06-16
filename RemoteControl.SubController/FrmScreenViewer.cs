using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Response;

namespace RemoteControl.SubController
{
    internal class FrmScreenViewer : Form
    {
        private PictureBox pictureBox;
        private ToolStrip toolStrip;
        private SocketSession _session;
        private int _fps;
        private readonly object _frameLock = new object();
        private ResponseStartGetScreen _latestFrame;
        private int _updateScheduled = 0;

        public FrmScreenViewer(SocketSession session, int fps)
        {
            _session = session;
            _fps = fps;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "屏幕监控 - " + (_session.HostName ?? _session.SocketId);
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            toolStrip = new ToolStrip();
            var btnSave = new ToolStripButton("保存截图");
            btnSave.Click += (s, e) => SaveImage();
            toolStrip.Items.Add(btnSave);

            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;

            pictureBox = new PictureBox();
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox.Location = new Point(0, 0);
            panel.Controls.Add(pictureBox);

            this.Controls.Add(panel);
            this.Controls.Add(toolStrip);
            this.FormClosing += FrmScreenViewer_FormClosing;
        }

        public void HandleScreen(ResponseStartGetScreen resp)
        {
            if (resp == null || this.IsDisposed) return;

            lock (_frameLock)
            {
                _latestFrame = resp;
            }

            if (Interlocked.CompareExchange(ref _updateScheduled, 1, 0) == 0)
            {
                try { this.BeginInvoke(new Action(RenderFrame)); }
                catch { }
            }
        }

        private void RenderFrame()
        {
            ResponseStartGetScreen resp;
            lock (_frameLock)
            {
                resp = _latestFrame;
                _latestFrame = null;
            }

            try
            {
                if (resp == null || !resp.Result) return;
                Image image = resp.GetImage();
                if (image == null) return;

                Image old = pictureBox.Image;
                pictureBox.Image = image;
                if (old != null) old.Dispose();
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _updateScheduled, 0);
                bool hasMore;
                lock (_frameLock) { hasMore = _latestFrame != null; }
                if (hasMore && Interlocked.CompareExchange(ref _updateScheduled, 1, 0) == 0)
                {
                    try { this.BeginInvoke(new Action(RenderFrame)); } catch { }
                }
            }
        }

        private void SaveImage()
        {
            if (pictureBox.Image == null)
            {
                MessageBox.Show("暂无图像", "提示"); return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JPEG|*.jpg|PNG|*.png";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    using (var bmp = new Bitmap(pictureBox.Image))
                        bmp.Save(dlg.FileName);
                }
            }
        }

        private void FrmScreenViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_session != null)
                _session.Send(ePacketType.PACKET_STOP_CAPTURE_SCREEN_REQUEST, null);
        }
    }
}
