using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Relay;

namespace RemoteControl.Server
{
    partial class RemoteControlServer
    {
        private readonly object _virtualClientsLock = new object();
        private readonly Dictionary<string, SocketSession> _virtualClients = new Dictionary<string, SocketSession>();
        private readonly object _relaySendLock = new object();

        private Socket _relaySocket;
        private SocketSession _relaySession;
        private Thread _recvThread;
        private bool _isRunning = false;
        private string _currentClientId = null;
        private string _lastRelayIP;
        private int _lastRelayPort;
        private bool _autoReconnect = true;
        private bool _reconnecting = false;

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectedEventArgs> ClientDisconnected;
        public event EventHandler<ClientListChangedEventArgs> ClientListChanged;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;
        public event EventHandler RelayDisconnected;

        public bool IsConnected { get { return _isRunning && _relaySocket != null && _relaySocket.Connected; } }

        public void Start(string relayIP, int relayPort)
        {
            _lastRelayIP = relayIP;
            _lastRelayPort = relayPort;
            _autoReconnect = true;
            Start(relayIP, relayPort, 5000);
        }

        public void Start(List<string> lstIP, int iServerPort)
        {
            string relayIP = Settings.CurrentSettings.RelayServerIP;
            int relayPort = Settings.CurrentSettings.RelayServerPort;
            if (string.IsNullOrEmpty(relayIP))
                throw new Exception("请先在设置中配置Relay服务器地址!");
            Start(relayIP, relayPort);
        }

        public void SelectClient(string clientId)
        {
            if (_relaySession == null || string.IsNullOrEmpty(clientId))
                return;

            _currentClientId = clientId;
            var select = new RelaySelectClient();
            select.ClientId = clientId;
            SendRelayPacket(ePacketType.CYCLER_RELAY_SELECT_CLIENT, select);
        }

        public void RefreshClientList()
        {
            if (_relaySession != null)
            {
                StartupTrace.Write("Relay client list requested");
                SendRelayPacket(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);
            }
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

        /// <summary>
        /// 当Relay连接断开后，自动尝试重连
        /// </summary>
        internal void TryAutoReconnect()
        {
            if (!_autoReconnect || _reconnecting)
                return;
            if (string.IsNullOrEmpty(_lastRelayIP))
                return;

            _reconnecting = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                int retryCount = 0;
                while (retryCount < 60)
                {
                    retryCount++;
                    Thread.Sleep(3000);
                    try
                    {
                        StartupTrace.Write("Auto-reconnect attempt #" + retryCount);
                        Start(_lastRelayIP, _lastRelayPort, 5000);
                        _autoReconnect = true; // 恢复自动重连标记
                        StartupTrace.Write("Auto-reconnect succeeded");
                        _reconnecting = false;
                        return;
                    }
                    catch (Exception ex)
                    {
                        StartupTrace.Write("Auto-reconnect failed: " + ex.Message);
                        _autoReconnect = true; // 确保循环继续
                    }
                }
                _reconnecting = false;
            });
        }
    }
}
