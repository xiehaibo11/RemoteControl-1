using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmCaptureVideo
    {
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;
            button.Checked = !button.Checked;
            if (button.Checked)
            {
                this.toolStripSplitButton2.Enabled = false;
                button.Text = "停止摄像头";
                RequestStartCaptureVideo req = new RequestStartCaptureVideo();
                req.Fps = _fps;
                oSession.Send(ePacketType.PACKET_START_CAPTURE_VIDEO_REQUEST, req);
            }
            else
            {
                this.toolStripSplitButton2.Enabled = true;
                button.Text = "开始摄像头";
                oSession.Send(ePacketType.PACKET_STOP_CAPTURE_VIDEO_REQUEST, null);
            }
        }

        private void toolStripButtonSave_ButtonClick(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                string fileName = "";
                using (Bitmap bmp = new Bitmap(this.pictureBox1.Image))
                {
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Filter = "*.bmp|*.bmp|*.jpg;*.jpeg|*.jpg;*.jpeg|*.*|*.*";
                    dialog.FilterIndex = 1;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        fileName = dialog.FileName;
                        try
                        {
                            bmp.Save(fileName);
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

        private void 实时保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.saveInRealTime = !this.saveInRealTime;
            this.实时保存ToolStripMenuItem.Checked = !this.实时保存ToolStripMenuItem.Checked;
        }

        private void toolStripMenuItemFPS_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item != null && item.Tag != null)
            {
                var parent = this.toolStripSplitButton2;
                for (int i = 0; i < parent.DropDownItems.Count; i++)
                {
                    var mItem = parent.DropDownItems[i] as ToolStripMenuItem;
                    if (mItem != null)
                    {
                        mItem.Checked = false;
                    }
                }

                _fps = Convert.ToInt32(item.Tag);
                item.Checked = true;
            }
        }

        private void toolStripMenuItemCaptureAudio_Click(object sender, EventArgs e)
        {
            toolStripMenuItemCaptureAudio.Checked = !toolStripMenuItemCaptureAudio.Checked;
            _captureAudio = toolStripMenuItemCaptureAudio.Checked;
            if (_captureAudio)
            {
                RequestStartCaptureAudio req = new RequestStartCaptureAudio();
                this.oSession.Send(ePacketType.PACKET_START_CAPTURE_AUDIO_REQUEST, req);
            }
            else
            {
                this.oSession.Send(ePacketType.PACKET_STOP_CAPTURE_AUDIO_REQUEST, null);
            }
        }

        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            ToolStripSplitButton btn = sender as ToolStripSplitButton;
            if (btn != null)
            {
                btn.ShowDropDown();
            }
        }

        private void FrmCaptureVideo_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (oSession != null)
            {
                oSession.Send(ePacketType.PACKET_STOP_CAPTURE_VIDEO_REQUEST, null);
                oSession.Send(ePacketType.PACKET_STOP_CAPTURE_AUDIO_REQUEST, null);
            }
        }
    }
}
