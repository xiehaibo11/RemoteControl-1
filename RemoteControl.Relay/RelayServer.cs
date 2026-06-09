using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RemoteControl.Relay
{
    /// <summary>
    /// 中转服务器核心逻辑
    /// </summary>
    public class RelayServer
    {
        private int _port;
        private Socket _listener;
        private Thread _acceptThread;
        public bool IsRunning { get; private set; }

        // 在线客户端 (被控端)
        private ConcurrentDictionary<string, ClientSession> _clients = new ConcurrentDictionary<string, ClientSession>();
        // 在线控制端
        private ConcurrentDictionary<string, ClientSession> _controllers = new ConcurrentDictionary<string, ClientSession>();

        public RelayServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
            _listener.Listen(100);
            IsRunning = true;

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();

            // 心跳检测线程
            new Thread(HeartbeatLoop) { IsBackground = true }.Start();
        }

        public void Stop()
        {
            IsRunning = false;
            try { _listener?.Close(); } catch { }
        }

        private void AcceptLoop()
        {
            while (IsRunning)
            {
                try
                {
                    var client = _listener.Accept();
                    var session = new ClientSession(client);
                    Console.WriteLine($"[连接] 新连接: {session.RemoteEndPoint}");

                    // 启动接收线程
                    new Thread(() => HandleSession(session)) { IsBackground = true }.Start();
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                        Console.WriteLine($"[错误] Accept: {ex.Message}");
                }
            }
        }

        private void HandleSession(ClientSession session)
        {
            try
            {
                // 等待握手包(第一个包必须是握手)
                var packet = session.ReceivePacket();
                if (packet == null)
                {
                    session.Close();
                    return;
                }

                var handshake = PacketCodec.DecodeHandshake(packet);
                if (handshake == null)
                {
                    Console.WriteLine($"[错误] 无效握手包: {session.RemoteEndPoint}");
                    session.Close();
                    return;
                }

                if (handshake.Role == "client")
                {
                    HandleClientConnection(session, handshake);
                }
                else if (handshake.Role == "controller")
                {
                    HandleControllerConnection(session);
                }
                else
                {
                    Console.WriteLine($"[错误] 未知角色: {handshake.Role}");
                    session.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] HandleSession: {ex.Message}");
                session.Close();
            }
        }

        /// <summary>
        /// 处理被控客户端连接
        /// </summary>
        private void HandleClientConnection(ClientSession session, HandshakeData handshake)
        {
            session.Role = "client";
            session.HostName = handshake.HostName;
            session.OnlineAvatar = handshake.OnlineAvatar;
            session.AppPath = handshake.AppPath;
            session.OnlineTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string clientId = session.SessionId;
            _clients[clientId] = session;
            Console.WriteLine($"[上线] 客户端: {session.HostName} ({session.RemoteEndPoint}) ID={clientId}");

            // 通知所有控制端有新客户端上线
            NotifyControllersClientOnline(session);

            // 持续接收数据，转发给绑定的控制端
            try
            {
                while (IsRunning && session.IsConnected)
                {
                    var packet = session.ReceivePacket();
                    if (packet == null) break;

                    // 转发给绑定的控制端
                    if (session.BoundController != null && session.BoundController.IsConnected)
                    {
                        session.BoundController.SendRaw(packet);
                    }
                }
            }
            catch { }

            // 下线处理
            _clients.TryRemove(clientId, out _);
            Console.WriteLine($"[下线] 客户端: {session.HostName} ID={clientId}");
            NotifyControllersClientOffline(clientId);
            session.Close();
        }

        /// <summary>
        /// 处理控制端连接
        /// </summary>
        private void HandleControllerConnection(ClientSession session)
        {
            session.Role = "controller";
            string controllerId = session.SessionId;
            _controllers[controllerId] = session;
            Console.WriteLine($"[连接] 控制端: {session.RemoteEndPoint} ID={controllerId}");

            try
            {
                while (IsRunning && session.IsConnected)
                {
                    var packet = session.ReceivePacket();
                    if (packet == null) break;

                    // 解析包类型
                    byte packetType = PacketCodec.GetPacketType(packet);

                    if (packetType == 201) // CYCLER_RELAY_CLIENT_LIST_REQUEST
                    {
                        // 返回在线客户端列表
                        var listPacket = PacketCodec.BuildClientListResponse(_clients);
                        session.SendRaw(listPacket);
                    }
                    else if (packetType == 203) // CYCLER_RELAY_SELECT_CLIENT
                    {
                        // 选择控制目标
                        var selectData = PacketCodec.DecodeSelectClient(packet);
                        if (selectData != null && _clients.TryGetValue(selectData.ClientId, out var targetClient))
                        {
                            // 双向绑定
                            session.BoundClient = targetClient;
                            targetClient.BoundController = session;
                            Console.WriteLine($"[绑定] 控制端{controllerId} -> 客户端{selectData.ClientId}");
                        }
                    }
                    else
                    {
                        // 其他包转发给绑定的客户端
                        if (session.BoundClient != null && session.BoundClient.IsConnected)
                        {
                            session.BoundClient.SendRaw(packet);
                        }
                    }
                }
            }
            catch { }

            // 断开处理
            _controllers.TryRemove(controllerId, out _);
            if (session.BoundClient != null)
            {
                session.BoundClient.BoundController = null;
            }
            Console.WriteLine($"[断开] 控制端: {controllerId}");
            session.Close();
        }

        /// <summary>
        /// 通知所有控制端有客户端上线
        /// </summary>
        private void NotifyControllersClientOnline(ClientSession client)
        {
            var packet = PacketCodec.BuildClientOnline(client);
            foreach (var kv in _controllers)
            {
                try { kv.Value.SendRaw(packet); } catch { }
            }
        }

        /// <summary>
        /// 通知所有控制端有客户端下线
        /// </summary>
        private void NotifyControllersClientOffline(string clientId)
        {
            var packet = PacketCodec.BuildClientOffline(clientId);
            foreach (var kv in _controllers)
            {
                try { kv.Value.SendRaw(packet); } catch { }
            }
        }

        /// <summary>
        /// 心跳检测 - 清理断开的连接
        /// </summary>
        private void HeartbeatLoop()
        {
            while (IsRunning)
            {
                Thread.Sleep(30000);
                var deadClients = new List<string>();
                foreach (var kv in _clients)
                {
                    if (!kv.Value.IsConnected)
                        deadClients.Add(kv.Key);
                }
                foreach (var id in deadClients)
                {
                    if (_clients.TryRemove(id, out var s))
                    {
                        Console.WriteLine($"[清理] 死连接客户端: {id}");
                        NotifyControllersClientOffline(id);
                        s.Close();
                    }
                }

                var deadControllers = new List<string>();
                foreach (var kv in _controllers)
                {
                    if (!kv.Value.IsConnected)
                        deadControllers.Add(kv.Key);
                }
                foreach (var id in deadControllers)
                {
                    _controllers.TryRemove(id, out _);
                    Console.WriteLine($"[清理] 死连接控制端: {id}");
                }

                Console.WriteLine($"[状态] 客户端:{_clients.Count} 控制端:{_controllers.Count}");
            }
        }
    }
}
