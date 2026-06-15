using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using RemoteControl.Protocals;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using log4net;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Audio;
using RemoteControl.Audio.Codecs;
using RemoteControl.Protocals.Utilities;
using RemoteControl.Protocals.Relay;
using RemoteControl.Server.Utils;
namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void refreshClientCountShow()
        {
            if (!string.IsNullOrEmpty(hostFilterKeyword) && this.InternetTreeNode != null)
            {
                this.toolStripStatusLabel1.Text = "自动上线：" + this.clientCount + "台  筛选：" + this.InternetTreeNode.Nodes.Count + "台";
                return;
            }

            this.toolStripStatusLabel1.Text = "自动上线：" + this.clientCount + "台";
        }

        /// <summary>
        /// 新建文本文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (this.listView1.Tag == null)
            {
                MessageBox.Show("无法在该目录下创建文件!");
                return;
            }
            var frm = new FrmInputFileOrDir();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            if (this.currentSession != null)
            {
                RequestCreateFileOrDir req = new RequestCreateFileOrDir();
                req.PathType = Protocals.ePathType.File;
                req.Path = System.IO.Path.Combine(this.listView1.Tag.ToString(), frm.InputText);
                this.currentSession.Send(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// 新建文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            if (this.listView1.Tag == null)
            {
                MessageBox.Show("无法在该目录下创建文件!");
                return;
            }
            var frm = new FrmInputFileOrDir();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            if (this.currentSession != null)
            {
                RequestCreateFileOrDir req = new RequestCreateFileOrDir();
                req.PathType = Protocals.ePathType.Directory;
                req.Path = System.IO.Path.Combine(this.listView1.Tag.ToString(), frm.InputText);
                this.currentSession.Send(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// 删除文件或文件件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            if (MessageBox.Show("确定要删除选择项?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Cancel)
                return;

            if (this.currentSession != null)
            {
                ListViewItem selectedItem = this.listView1.SelectedItems[0];
                ListViewItemFileOrDirTag tag = selectedItem.Tag as ListViewItemFileOrDirTag;
                if (tag == null)
                    return;

                RequestDeleteFileOrDir req = new RequestDeleteFileOrDir();
                req.PathType = tag.IsFile ? Protocals.ePathType.File : Protocals.ePathType.Directory;
                req.Path = tag.Path;
                this.currentSession.Send(ePacketType.PACKET_DELETE_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
                return;

            string remoteFileDir = this.listView1.Tag as string;
            if (remoteFileDir == null)
            {
                MsgBox.Info("当前目录无法上传文件!");
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string localFilePath = ofd.FileName;
            StartUploadFile(this.currentSession, localFilePath, remoteFileDir);
        }

        private void StartUploadFile(SocketSession session, string localFilePath, string remoteFileDir)
        {
            if (session == null)
                return;
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                MsgBox.Info("本地文件不存在！");
                return;
            }
            if (string.IsNullOrEmpty(remoteFileDir))
            {
                MsgBox.Info("当前目录无法上传文件!");
                return;
            }

            if (remoteFileDir.EndsWith("\\"))
                remoteFileDir = remoteFileDir.TrimEnd('\\');
            string remoteFilePath = remoteFileDir + "\\" + System.IO.Path.GetFileName(localFilePath);
            RequestStartUploadHeader req = new RequestStartUploadHeader();
            req.From = localFilePath;
            req.To = remoteFilePath;
            string fileId = Guid.NewGuid().ToString();
            req.Id = fileId;
            session.Send(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, req);

            FileStream fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            uploadDic.Add(fileId, fs);
            long fileSize = fs.Length;

            var frm = new FrmDownload(() =>
                {
                    RequestStopUpload reqStop = new RequestStopUpload();
                    reqStop.Id = fileId;
                    session.Send(ePacketType.PACKET_STOP_UPLOAD_REQUEST, reqStop);
                }, localFilePath, remoteFilePath, fileSize);
            uploadFrmDic.Add(fileId, frm);
            frm.Text = "上传文件";

            new Thread(() =>
            {
                frm.ShowDialog();
            }) { IsBackground = true }.Start();

            new Thread(() => { DoUploadFileInternal(session, fileId); }) { IsBackground = true }.Start();
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
    }
}
