using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Relay;

namespace RemoteControl.Server
{
    class RemoteControlServer
    {
        private Socket _relaySocket;
        private SocketSession _relaySession;
        private Thread _recvThread;
        private bool _isRunning = false;

        // 虚拟客户端会话(通过Relay中转)
        private Dictionary<string, SocketSession> _virtualClients = new Dictionary<string, SocketSession>();
        private string _currentClientId = null;

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectedEventArgs> ClientDisconnected;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        public RemoteControlServer()
        {
        }

        /// <summary>
        /// 连接到Relay中转服务器
        /// </summary>
        public void Start(string relayIP, int relayPort)
        {
            _relaySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _relaySocket.Connect(new IPEndPoint(IPAddress.Parse(relayIP), relayPort));

            _relaySession = new SocketSession(_relaySocket.RemoteEndPoint.ToString(), _relaySocket);

            // 发送控制端握手
            var handshake = new RelayHandshake();
            handshake.Role = "controller";
            _relaySession.Send(ePacketType.CYCLER_RELAY_HANDSHAKE, handshake);

            _isRunning = true;

            // 请求在线客户端列表
            _relaySession.Send(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);

            // 启动接收线程
            _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "RelayRecvThread" };
            _recvThread.Start();
        }

        /// <summary>
        /// 兼容旧接口(已弃用本地监听)
        /// </summary>
        public void Start(List<string> lstIP, int iServerPort)
        {
            string relayIP = Settings.CurrentSettings.RelayServerIP;
            int relayPort = Settings.CurrentSettings.RelayServerPort;
            if (string.IsNullOrEmpty(relayIP))
            {
                throw new Exception("请先在设置中配置Relay服务器地址!");
            }
            Start(relayIP, relayPort);
        }

        /// <summary>
        /// 选择控制某个客户端
        /// </summary>
        public void SelectClient(string clientId)
        {
            if (_relaySession == null || string.IsNullOrEmpty(clientId))
                return;
            _currentClientId = clientId;
            var select = new RelaySelectClient();
            select.ClientId = clientId;
            _relaySession.Send(ePacketType.CYCLER_RELAY_SELECT_CLIENT, select);
        }

        /// <summary>
        /// 刷新在线列表
        /// </summary>
        public void RefreshClientList()
        {
            if (_relaySession != null)
                _relaySession.Send(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);
        }

        private void RecvLoop()
        {
            byte[] buffer = new byte[1024];
            int recvSize = -1;
            List<byte> data = new List<byte>();

            while (_isRunning)
            {
                try
                {
                    recvSize = _relaySocket.Receive(buffer);
                    if (recvSize < 1)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    for (int i = 0; i < recvSize; i++)
                    {
                        data.Add(buffer[i]);
                    }

                    while (data.Count >= 4)
                    {
                        int packetLength = BitConverter.ToInt32(data.ToArray(), 0);
                        if (data.Count < packetLength)
                        {
                            break;
                        }
                        // 包含长度头的完整包(packetLength字节)
                        byte[] fullPacket = new byte[packetLength];
                        data.CopyTo(0, fullPacket, 0, packetLength);
                        data.RemoveRange(0, packetLength);

                        ProcessPacket(fullPacket);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Relay接收异常: " + ex.Message);
                    _isRunning = false;
                    break;
                }
            }
        }

        private void ProcessPacket(byte[] packet)
        {
            ePacketType packetType;
            object obj;
            CodecFactory.Instance.DecodeObject(packet, out packetType, out obj);

            if (packetType == ePacketType.CYCLER_RELAY_CLIENT_LIST_RESPONSE)
            {
                // 收到在线客户端列表
                var resp = obj as RelayClientListResponse;
                if (resp != null && resp.Clients != null)
                {
                    foreach (var info in resp.Clients)
                    {
                        if (!_virtualClients.ContainsKey(info.ClientId))
                        {
                            // 创建虚拟会话
                            var session = CreateVirtualSession(info);
                            _virtualClients[info.ClientId] = session;
                            RaiseClientConnected(session);
                        }
                    }
                }
            }
            else if (packetType == ePacketType.CYCLER_RELAY_CLIENT_ONLINE)
            {
                // 新客户端上线
                var online = obj as RelayClientOnline;
                if (online != null && !_virtualClients.ContainsKey(online.ClientId))
                {
                    var info = new RelayClientInfo
                    {
                        ClientId = online.ClientId,
                        HostName = online.HostName,
                        IP = online.IP,
                        OnlineAvatar = online.OnlineAvatar
                    };
                    var session = CreateVirtualSession(info);
                    _virtualClients[online.ClientId] = session;
                    RaiseClientConnected(session);
                }
            }
            else if (packetType == ePacketType.CYCLER_RELAY_CLIENT_OFFLINE)
            {
                // 客户端下线
                var offline = obj as RelayClientOffline;
                if (offline != null && _virtualClients.ContainsKey(offline.ClientId))
                {
                    var session = _virtualClients[offline.ClientId];
                    _virtualClients.Remove(offline.ClientId);
                    RaiseClientDisconnected(session);
                }
            }
            else
            {
                // 其他包 = 来自被控客户端的响应，转发到PacketReceived
                if (PacketReceived != null && _currentClientId != null && _virtualClients.ContainsKey(_currentClientId))
                {
                    var args = new PacketReceivedEventArgs();
                    args.PacketType = packetType;
                    args.Obj = obj;
                    args.Session = _virtualClients[_currentClientId];
                    try
                    {
                        PacketReceived(this, args);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("PacketReceived事件异常: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 创建虚拟会话(用于UI展示，实际通信走Relay)
        /// </summary>
        private SocketSession CreateVirtualSession(RelayClientInfo info)
        {
            // SocketId = ClientId，用于_virtualClients查找和SelectClient
            var session = new SocketSession(info.ClientId, _relaySocket);
            session.SetHostName(!string.IsNullOrEmpty(info.HostName) ? info.HostName : info.IP);
            session.SetAppPath(info.AppPath);
            session.SetOnlineAvatar(info.OnlineAvatar);
            return session;
        }

        private void RaiseClientConnected(SocketSession session)
        {
            try
            {
                if (ClientConnected != null)
                    ClientConnected(this, new ClientConnectedEventArgs(session));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ClientConnected事件异常: " + ex.Message);
            }
        }

        private void RaiseClientDisconnected(SocketSession session)
        {
            try
            {
                if (ClientDisconnected != null)
                    ClientDisconnected(this, new ClientConnectedEventArgs(session));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ClientDisconnected事件异常: " + ex.Message);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            try { if (_relaySocket != null) _relaySocket.Close(); } catch { }
            _virtualClients.Clear();
        }
    }
}
