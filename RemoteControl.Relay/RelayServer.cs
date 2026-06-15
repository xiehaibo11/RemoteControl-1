using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteControl.Relay
{
    public partial class RelayServer
    {
        private const int ListenBacklog = 4096;

        private readonly int _port;
        private readonly IPAddress _bindAddress;
        private readonly ConcurrentDictionary<string, ClientSession> _clients = new ConcurrentDictionary<string, ClientSession>();
        private readonly ConcurrentDictionary<string, ClientSession> _controllers = new ConcurrentDictionary<string, ClientSession>();

        private Socket _listener;
        private CancellationTokenSource _cts;

        public bool IsRunning { get; private set; }

        public RelayServer(int port)
            : this(port, IPAddress.Any)
        {
        }

        public RelayServer(int port, IPAddress bindAddress)
        {
            _port = port;
            _bindAddress = bindAddress ?? IPAddress.Any;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Bind(new IPEndPoint(_bindAddress, _port));
            _listener.Listen(ListenBacklog);
            IsRunning = true;

            Task.Run(() => AcceptLoopAsync(_cts.Token));
            Task.Run(() => HeartbeatLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            IsRunning = false;
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Close(); } catch { }

            foreach (var kv in _clients)
                kv.Value.Close();
            foreach (var kv in _controllers)
                kv.Value.Close();
        }

        private static void LogScale(string prefix, int count, string detail)
        {
            if (count <= 20 || count % 100 == 0)
                Console.WriteLine(prefix + count + " " + detail);
        }
    }
}
