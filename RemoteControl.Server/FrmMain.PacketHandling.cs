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
            RSCApplication.oRemoteControlServer.PacketReceived += oRemoteControlServer_PacketReceived;
        }

        void oRemoteControlServer_PacketReceived(object sender, PacketReceivedEventArgs e)
        {
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
            HandleCapturePackets(e);
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
                string hostName = resp.HostName;
                e.Session.SetHostName(hostName);
                e.Session.SetAppPath(resp.AppPath);
                e.Session.SetOnlineAvatar(resp.OnlineAvatar);
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
                }));
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
