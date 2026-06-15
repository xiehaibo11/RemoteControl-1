using System;
using System.IO;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        /// <summary>
        /// 复制按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("不支持文件夹的复制！");
                return;
            }

            PasteInfo pi = toolStripButton8.Tag as PasteInfo;
            if (pi == null)
            {
                pi = new PasteInfo();
                toolStripButton8.Tag = pi;
            }
            pi.IsDeleteSourceFile = false;
            pi.SourceFilePath = path;
        }

        /// <summary>
        /// 剪切按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("不支持文件夹的剪切！");
                return;
            }

            PasteInfo pi = toolStripButton8.Tag as PasteInfo;
            if (pi == null)
            {
                pi = new PasteInfo();
                toolStripButton8.Tag = pi;
            }
            pi.IsDeleteSourceFile = true;
            pi.SourceFilePath = path;
        }

        /// <summary>
        /// 粘贴按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            PasteInfo pi = toolStripButton8.Tag as PasteInfo;
            if (pi == null)
            {
                MsgBox.Info("请先复制或移动一个文件！");
                return;
            }

            string pastMode = pi.IsDeleteSourceFile ? "移动" : "复制";
            string curDir = this.listView1.Tag as string;
            if (curDir == null)
            {
                MsgBox.Info("不能" + pastMode + "到当前目录！");
                return;
            }
            if (MsgBox.Question("确定要" + pastMode + "文件" + pi.SourceFilePath + "?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                return;
            string fileName = Path.GetFileName(pi.SourceFilePath);
            string destPath = Path.Combine(curDir, fileName);
            if (pi.IsDeleteSourceFile)
            {
                RequestMoveFile req = new RequestMoveFile();
                req.SourceFile = pi.SourceFilePath;
                req.DestinationFile = destPath;
                this.currentSession.Send(ePacketType.PACKET_MOVE_FILE_OR_DIR_REQUEST, req);
            }
            else
            {
                RequestCopyFile req = new RequestCopyFile();
                req.SourceFile = pi.SourceFilePath;
                req.DestinationFile = destPath;
                this.currentSession.Send(ePacketType.PACKET_COPY_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// listivew节点是否为文件
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsListViewItemAFile(ListViewItem item, out string path)
        {
            path = null;
            if (item == null)
                return false;

            ListViewItemFileOrDirTag tag = item.Tag as ListViewItemFileOrDirTag;
            if (tag == null || string.IsNullOrEmpty(tag.Path))
                return false;

            path = tag.Path;
            if (tag.IsFile == false)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// listview按键监控
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                toolStripButton9.PerformClick();
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                toolStripButton7.PerformClick();
            }
            else if (e.KeyCode == Keys.X && e.Control)
            {
                toolStripButton12.PerformClick();
            }
            else if (e.KeyCode == Keys.V && e.Control)
            {
                toolStripButton8.PerformClick();
            }
            else if (e.KeyCode == Keys.F2)
            {
                toolStripButton2.PerformClick();
            }
        }

        /// <summary>
        /// 重命名按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("不支持文件夹的重命名！");
                return;
            }

            string oldName = Path.GetFileName(path);
            var frm = new FrmRename(oldName);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                RequestRenameFile req = new RequestRenameFile();
                req.SourceFile = path;
                req.DestinationFileName = frm.NewName;
                this.currentSession.Send(ePacketType.PACKET_RENAME_FILE_REQUEST, req);
            }
        }
    }
}
