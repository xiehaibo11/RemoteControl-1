using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Relay;

namespace RemoteControl.SubController
{
    internal class SubControllerRelay
    {
        private readonly object _virtualClientsLock = new object();
        private readonly Dictionary<string, SocketSession> _virtualClients = new Dictionary<string, SocketSession>();

        private Socket _relaySocket;
        private SocketSession _relaySession;
        private Thread _recvThread;
        private bool _isRunning = false;
        private string _currentClientId = null;
        private string _lastRelayIP;
        private int _lastRelayPort;
        private bool _autoReconnect = true;
        private bool _reconnecting = false;

        public event EventHandler<ClientEventArgs> ClientConnected;
        public event EventHandler<ClientEventArgs> ClientDisconnected;
        public event EventHandler<ClientListEventArgs> ClientListChanged;
        public event EventHandler<PacketEventArgs> PacketReceived;
        public event EventHandler RelayDisconnected;

        public bool IsConnected { get { return _isRunning && _relaySocket != null && _relaySocket.Connected; } }

        public void Start(string relayIP, int relayPort)
        {
            _lastRelayIP = relayIP;
            _lastRelayPort = relayPort;
            _autoReconnect = true;
            ConnectRelay(relayIP, relayPort, 5000);
        }

        public void SelectClient(string clientId)
        {
            if (_relaySession == null || string.IsNullOrEmpty(clientId))
                return;
            _currentClientId = clientId;
            var select = new RelaySelectClient();
            select.ClientId = clientId;
            _relaySession.Send(ePacketType.CYCLER_RELAY_SELECT_CLIENT, select);
        }

        public void RefreshClientList()
        {
            if (_relaySession != null)
                _relaySession.Send(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);
        }

        public List<SocketSession> GetClientSnapshot()
        {
            lock (_virtualClientsLock)
                return _virtualClients.Values.ToList();
        }

        public void Stop()
        {
            _autoReconnect = false;
            _isRunning = false;
            try { if (_relaySocket != null) _relaySocket.Close(); } catch { }
            _relaySocket = null;
            _relaySession = null;
            _currentClientId = null;
            lock (_virtualClientsLock)
                _virtualClients.Clear();
        }

        public void TryAutoReconnect()
        {
            if (!_autoReconnect || _reconnecting)
                return;
            if (string.IsNullOrEmpty(_lastRelayIP))
                return;

            _reconnecting = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                int retryCount = 0;
                while (_autoReconnect && !_isRunning && retryCount < 60)
                {
                    retryCount++;
                    Thread.Sleep(3000);
                    try
                    {
                        ConnectRelay(_lastRelayIP, _lastRelayPort, 5000);
                        _reconnecting = false;
                        return;
                    }
                    catch { }
                }
                _reconnecting = false;
            });
        }

        private void ConnectRelay(string relayIP, int relayPort, int timeoutMs)
        {
            Stop();
            _autoReconnect = true;

            _relaySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _relaySocket.NoDelay = true;
            _relaySocket.ReceiveBufferSize = 64 * 1024;
            _relaySocket.SendBufferSize = 64 * 1024;

            EndPoint ep = CreateEndpoint(relayIP, relayPort);
            IAsyncResult result = _relaySocket.BeginConnect(ep, null, null);
            if (!result.AsyncWaitHandle.WaitOne(timeoutMs, false))
            {
                try { _relaySocket.Close(); } catch { }
                throw new TimeoutException("Relay connect timed out.");
            }
            _relaySocket.EndConnect(result);

            _relaySession = new SocketSession(_relaySocket.RemoteEndPoint.ToString(), _relaySocket);

            var handshake = new RelayHandshake();
            handshake.Role = "controller";
            _relaySession.Send(ePacketType.CYCLER_RELAY_HANDSHAKE, handshake);

            _isRunning = true;
            _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "SubRelayRecv" };
            _recvThread.Start();

            _relaySession.Send(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);
        }

        private static EndPoint CreateEndpoint(string relayIP, int relayPort)
        {
            IPAddress address;
            if (IPAddress.TryParse(relayIP, out address))
                return new IPEndPoint(address, relayPort);

            IPAddress[] addresses = Dns.GetHostAddresses(relayIP);
            IPAddress ipv4 = addresses.FirstOrDefault(m => m.AddressFamily == AddressFamily.InterNetwork);
            if (ipv4 != null)
                return new IPEndPoint(ipv4, relayPort);
            if (addresses.Length > 0)
                return new IPEndPoint(addresses[0], relayPort);
            throw new SocketException();
        }

        private void RecvLoop()
        {
            byte[] buffer = new byte[64 * 1024];
            List<byte> data = new List<byte>(128 * 1024);

            while (_isRunning)
            {
                try
                {
                    int recvSize = _relaySocket.Receive(buffer);
                    if (recvSize < 1) { _isRunning = false; break; }

                    for (int i = 0; i < recvSize; i++)
                        data.Add(buffer[i]);

                    while (data.Count >= 4)
                    {
                        int packetLength = data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
                        if (packetLength < 5 || packetLength > 10 * 1024 * 1024)
                            throw new InvalidOperationException("Invalid packet length");
                        if (data.Count < packetLength) break;

                        byte[] fullPacket = new byte[packetLength];
                        data.CopyTo(0, fullPacket, 0, packetLength);
                        data.RemoveRange(0, packetLength);

                        try { ProcessPacket(fullPacket); }
                        catch { }
                    }
                }
                catch
                {
                    _isRunning = false;
                    break;
                }
            }

            RaiseEvent(RelayDisconnected);
        }

        private void ProcessPacket(byte[] packet)
        {
            ePacketType packetType;
            object obj;
            CodecFactory.Instance.DecodeObject(packet, out packetType, out obj);

            if (packetType == ePacketType.CYCLER_RELAY_CLIENT_LIST_RESPONSE)
            {
                ApplyClientListResponse(obj as RelayClientListResponse);
            }
            else if (packetType == ePacketType.CYCLER_RELAY_CLIENT_ONLINE)
            {
                ApplyClientOnline(obj as RelayClientOnline);
            }
            else if (packetType == ePacketType.CYCLER_RELAY_CLIENT_OFFLINE)
            {
                ApplyClientOffline(obj as RelayClientOffline);
            }
            else
            {
                SocketSession session = null;
                lock (_virtualClientsLock)
                {
                    if (_currentClientId != null && _virtualClients.ContainsKey(_currentClientId))
                        session = _virtualClients[_currentClientId];
                }

                if (PacketReceived != null && session != null)
                {
                    var args = new PacketEventArgs { PacketType = packetType, Obj = obj, Session = session };
                    try { PacketReceived(this, args); } catch { }
                }
            }
        }

        private void ApplyClientListResponse(RelayClientListResponse resp)
        {
            if (resp == null || resp.Clients == null) return;
            var added = new List<SocketSession>();
            var removed = new List<SocketSession>();
            var incomingIds = new HashSet<string>();

            lock (_virtualClientsLock)
            {
                foreach (var info in resp.Clients)
                {
                    if (info == null || string.IsNullOrEmpty(info.ClientId)) continue;
                    incomingIds.Add(info.ClientId);
                    SocketSession session;
                    if (_virtualClients.TryGetValue(info.ClientId, out session))
                        UpdateSession(session, info);
                    else
                    {
                        session = new SocketSession(info.ClientId, _relaySocket);
                        UpdateSession(session, info);
                        _virtualClients[info.ClientId] = session;
                        added.Add(session);
                    }
                }
                foreach (string id in _virtualClients.Keys.ToList())
                {
                    if (!incomingIds.Contains(id))
                    {
                        removed.Add(_virtualClients[id]);
                        _virtualClients.Remove(id);
                    }
                }
            }

            if (ClientListChanged != null)
                ClientListChanged(this, new ClientListEventArgs(added, removed));
        }

        private void ApplyClientOnline(RelayClientOnline online)
        {
            if (online == null || string.IsNullOrEmpty(online.ClientId)) return;
            SocketSession session;
            bool isNew = false;
            lock (_virtualClientsLock)
            {
                if (!_virtualClients.TryGetValue(online.ClientId, out session))
                {
                    session = new SocketSession(online.ClientId, _relaySocket);
                    _virtualClients[online.ClientId] = session;
                    isNew = true;
                }
                ApplyOnlineInfo(session, online);
            }
            if (isNew && ClientConnected != null)
                ClientConnected(this, new ClientEventArgs(session));
            else if (!isNew && ClientListChanged != null)
                ClientListChanged(this, new ClientListEventArgs(new List<SocketSession>(), new List<SocketSession>()));
        }

        private void ApplyClientOffline(RelayClientOffline offline)
        {
            if (offline == null || string.IsNullOrEmpty(offline.ClientId)) return;
            SocketSession session = null;
            lock (_virtualClientsLock)
            {
                if (_virtualClients.ContainsKey(offline.ClientId))
                {
                    session = _virtualClients[offline.ClientId];
                    _virtualClients.Remove(offline.ClientId);
                }
            }
            if (session != null && ClientDisconnected != null)
                ClientDisconnected(this, new ClientEventArgs(session));
        }

        private static void UpdateSession(SocketSession session, RelayClientInfo info)
        {
            string hostName = !string.IsNullOrEmpty(info.HostName) ? info.HostName : info.IP;
            session.SetHostName(hostName);
            session.SetExternalIP(info.IP);
            session.SetAppPath(info.AppPath);
            session.SetOnlineAvatar(info.OnlineAvatar);
            session.SetClientInfo(info.UserName, info.LocalIP, info.OSVersion, info.Privilege, info.CameraStatus);
            session.SetBossExInfo(info.Antivirus, info.OnlineQQ, info.TG, info.WX, info.UserStatus, info.Region, info.ISP);
            session.Touch();
        }

        private static void ApplyOnlineInfo(SocketSession session, RelayClientOnline online)
        {
            string hostName = !string.IsNullOrEmpty(online.HostName) ? online.HostName : online.IP;
            session.SetHostName(hostName);
            session.SetExternalIP(online.IP);
            session.SetAppPath(online.AppPath);
            session.SetOnlineAvatar(online.OnlineAvatar);
            session.SetClientInfo(online.UserName, online.LocalIP, online.OSVersion, online.Privilege, online.CameraStatus);
            session.SetBossExInfo(online.Antivirus, online.OnlineQQ, online.TG, online.WX, online.UserStatus, online.Region, online.ISP);
            session.Touch();
        }

        private static void RaiseEvent(EventHandler handler)
        {
            if (handler != null) try { handler(null, EventArgs.Empty); } catch { }
        }
    }

    internal class ClientEventArgs : EventArgs
    {
        public SocketSession Client { get; private set; }
        public ClientEventArgs(SocketSession client) { Client = client; }
    }

    internal class ClientListEventArgs : EventArgs
    {
        public List<SocketSession> Added { get; private set; }
        public List<SocketSession> Removed { get; private set; }
        public ClientListEventArgs(List<SocketSession> added, List<SocketSession> removed)
        {
            Added = added ?? new List<SocketSession>();
            Removed = removed ?? new List<SocketSession>();
        }
    }

    internal class PacketEventArgs : EventArgs
    {
        public SocketSession Session;
        public ePacketType PacketType;
        public object Obj;
    }
}
