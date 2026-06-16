using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmCaptureVideo
    {
        public void HandleScreen(ResponseStartCaptureVideo resp)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseStartCaptureVideo>(HandleScreen), resp);
                return;
            }

            _receivedFirstFrame = true;
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Stop();
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }

            if (resp == null)
            {
                this.toolStripStatusLabel1.Text = "摄像头返回异常：空响应";
                return;
            }

            if (!resp.Result)
            {
                this.toolStripStatusLabel1.Text = "摄像头启动失败：" + resp.Message;
                this.toolStripStatusLabel2.Text = "返回时间：" + DateTime.Now;
                this.toolStripButton2.Checked = false;
                this.toolStripButton2.Text = "开始摄像头";
                this.toolStripSplitButton2.Enabled = true;
                return;
            }

            Image image = null;
            try
            {
                image = resp.GetImage();
            }
            catch (Exception ex)
            {
                this.toolStripStatusLabel1.Text = "摄像头图像解析失败：" + ex.Message;
                this.toolStripStatusLabel2.Text = "返回时间：" + DateTime.Now;
                return;
            }

            if (image == null)
            {
                this.toolStripStatusLabel1.Text = "摄像头暂无画面";
                this.toolStripStatusLabel2.Text = "返回时间：" + DateTime.Now;
                return;
            }

            Image oldImage = this.pictureBox1.Image;
            this.pictureBox1.Image = image;
            if (oldImage != null)
            {
                oldImage.Dispose();
            }

            this.toolStripStatusLabel1.Text = "摄像头采集时间：" + resp.CollectTime;
            this.toolStripStatusLabel2.Text = "图像返回时间：" + DateTime.Now;
            if (this.saveInRealTime)
                SaveCaptureFrame(resp);
        }

        private void SaveCaptureFrame(ResponseStartCaptureVideo resp)
        {
            try
            {
                if (!System.IO.Directory.Exists("CaptureVideo"))
                {
                    System.IO.Directory.CreateDirectory("CaptureVideo");
                }
                string dir = Application.StartupPath + "\\CaptureVideo\\" + oSession.SocketId.Replace(":", "-") + "\\";
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                string filename = dir + resp.CollectTime.ToString("yyyyMMddHHmmssfff") + ".jpg";
                System.IO.File.WriteAllBytes(filename, resp.ImageData);
            }
            catch (Exception)
            {
            }
        }
    }
}
