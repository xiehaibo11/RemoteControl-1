using System;
using System.Collections.Generic;
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

        private Socket _relaySocket;
        private SocketSession _relaySession;
        private Thread _recvThread;
        private bool _isRunning = false;
        private string _currentClientId = null;

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectedEventArgs> ClientDisconnected;
        public event EventHandler<ClientListChangedEventArgs> ClientListChanged;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;
        public event EventHandler RelayDisconnected;

        public bool IsConnected { get { return _isRunning && _relaySocket != null && _relaySocket.Connected; } }

        public void Start(string relayIP, int relayPort)
        {
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
            _relaySession.Send(ePacketType.CYCLER_RELAY_SELECT_CLIENT, select);
        }

        public void RefreshClientList()
        {
            if (_relaySession != null)
                _relaySession.Send(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);
        }

        public void Stop()
        {
            _isRunning = false;
            try { if (_relaySocket != null) _relaySocket.Close(); } catch { }
            _relaySocket = null;
            _relaySession = null;
            _currentClientId = null;
            lock (_virtualClientsLock)
                _virtualClients.Clear();
        }
    }
}
