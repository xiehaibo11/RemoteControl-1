#!/bin/bash
# ============================================
# RemoteControl Relay 中转服务器一键部署脚本
# 服务器编号: #8432 / MyServer
# ============================================

set -e

RELAY_PORT=10010
INSTALL_DIR="/opt/remotecontrol-relay"
SERVICE_NAME="rc-relay"

echo "=== RemoteControl Relay 部署脚本 ==="
echo ""

# 1. 检测系统类型
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
    echo "[信息] 检测到系统: $PRETTY_NAME"
else
    echo "[错误] 无法检测操作系统"
    exit 1
fi

# 2. 安装 .NET 6 SDK
echo ""
echo "[步骤1] 安装 .NET 6 运行时..."

if command -v dotnet &> /dev/null; then
    echo "[信息] dotnet 已安装: $(dotnet --version)"
else
    case $OS in
        ubuntu|debian)
            # 添加 Microsoft 包源
            wget https://packages.microsoft.com/config/$OS/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            apt-get update
            apt-get install -y dotnet-sdk-6.0
            ;;
        centos|rhel|fedora)
            dnf install -y dotnet-sdk-6.0
            ;;
        *)
            # 通用安装方式
            echo "[信息] 使用通用安装脚本..."
            wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
            chmod +x dotnet-install.sh
            ./dotnet-install.sh --channel 6.0
            export PATH="$HOME/.dotnet:$PATH"
            echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
            rm dotnet-install.sh
            ;;
    esac
fi

echo "[信息] dotnet 版本: $(dotnet --version)"

# 3. 创建安装目录
echo ""
echo "[步骤2] 创建安装目录..."
mkdir -p $INSTALL_DIR
cd $INSTALL_DIR

# 4. 创建项目文件
echo ""
echo "[步骤3] 创建项目文件..."

cat > RemoteControl.Relay.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>RemoteControl.Relay</RootNamespace>
    <AssemblyName>RemoteControl.Relay</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
EOF

cat > Program.cs << 'EOF'
using System;
using System.Net;

namespace RemoteControl.Relay
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 10010;
            if (args.Length > 0 && int.TryParse(args[0], out int p))
            {
                port = p;
            }

            Console.WriteLine("=== RemoteControl Relay Server ===");
            Console.WriteLine($"监听端口: {port}");

            var server = new RelayServer(port);
            server.Start();

            Console.WriteLine("服务已启动，按 Ctrl+C 退出...");
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                server.Stop();
            };

            while (server.IsRunning)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
EOF

cat > RelayServer.cs << 'RELAYEOF'
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RemoteControl.Relay
{
    public class RelayServer
    {
        private int _port;
        private Socket _listener;
        private Thread _acceptThread;
        public bool IsRunning { get; private set; }

        private ConcurrentDictionary<string, ClientSession> _clients = new ConcurrentDictionary<string, ClientSession>();
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

            NotifyControllersClientOnline(session);

            try
            {
                while (IsRunning && session.IsConnected)
                {
                    var packet = session.ReceivePacket();
                    if (packet == null) break;

                    if (session.BoundController != null && session.BoundController.IsConnected)
                    {
                        session.BoundController.SendRaw(packet);
                    }
                }
            }
            catch { }

            _clients.TryRemove(clientId, out _);
            Console.WriteLine($"[下线] 客户端: {session.HostName} ID={clientId}");
            NotifyControllersClientOffline(clientId);
            session.Close();
        }

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

                    byte packetType = PacketCodec.GetPacketType(packet);

                    if (packetType == 201) // CYCLER_RELAY_CLIENT_LIST_REQUEST
                    {
                        var listPacket = PacketCodec.BuildClientListResponse(_clients);
                        session.SendRaw(listPacket);
                    }
                    else if (packetType == 203) // CYCLER_RELAY_SELECT_CLIENT
                    {
                        var selectData = PacketCodec.DecodeSelectClient(packet);
                        if (selectData != null && _clients.TryGetValue(selectData.ClientId, out var targetClient))
                        {
                            session.BoundClient = targetClient;
                            targetClient.BoundController = session;
                            Console.WriteLine($"[绑定] 控制端{controllerId} -> 客户端{selectData.ClientId}");
                        }
                    }
                    else
                    {
                        if (session.BoundClient != null && session.BoundClient.IsConnected)
                        {
                            session.BoundClient.SendRaw(packet);
                        }
                    }
                }
            }
            catch { }

            _controllers.TryRemove(controllerId, out _);
            if (session.BoundClient != null)
            {
                session.BoundClient.BoundController = null;
            }
            Console.WriteLine($"[断开] 控制端: {controllerId}");
            session.Close();
        }

        private void NotifyControllersClientOnline(ClientSession client)
        {
            var packet = PacketCodec.BuildClientOnline(client);
            foreach (var kv in _controllers)
            {
                try { kv.Value.SendRaw(packet); } catch { }
            }
        }

        private void NotifyControllersClientOffline(string clientId)
        {
            var packet = PacketCodec.BuildClientOffline(clientId);
            foreach (var kv in _controllers)
            {
                try { kv.Value.SendRaw(packet); } catch { }
            }
        }

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
RELAYEOF

cat > ClientSession.cs << 'EOF'
using System;
using System.Net;
using System.Net.Sockets;

namespace RemoteControl.Relay
{
    public class ClientSession
    {
        private Socket _socket;
        private object _sendLock = new object();

        public string SessionId { get; private set; }
        public string Role { get; set; } = "";
        public string HostName { get; set; } = "";
        public string AppPath { get; set; } = "";
        public string OnlineAvatar { get; set; } = "";
        public string OnlineTime { get; set; } = "";
        public string RemoteEndPoint { get; private set; }

        public ClientSession BoundController { get; set; }
        public ClientSession BoundClient { get; set; }

        public bool IsConnected
        {
            get
            {
                try { return _socket != null && _socket.Connected; }
                catch { return false; }
            }
        }

        public ClientSession(Socket socket)
        {
            _socket = socket;
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            RemoteEndPoint = socket.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public byte[] ReceivePacket()
        {
            try
            {
                byte[] lenBuf = new byte[4];
                int received = 0;
                while (received < 4)
                {
                    int r = _socket.Receive(lenBuf, received, 4 - received, SocketFlags.None);
                    if (r <= 0) return null;
                    received += r;
                }

                int packetLength = BitConverter.ToInt32(lenBuf, 0);
                if (packetLength <= 4 || packetLength > 10 * 1024 * 1024) return null;

                int remainLen = packetLength - 4;
                byte[] remain = new byte[remainLen];
                received = 0;
                while (received < remainLen)
                {
                    int r = _socket.Receive(remain, received, remainLen - received, SocketFlags.None);
                    if (r <= 0) return null;
                    received += r;
                }

                byte[] fullPacket = new byte[packetLength];
                Buffer.BlockCopy(lenBuf, 0, fullPacket, 0, 4);
                Buffer.BlockCopy(remain, 0, fullPacket, 4, remainLen);
                return fullPacket;
            }
            catch
            {
                return null;
            }
        }

        public void SendRaw(byte[] packet)
        {
            lock (_sendLock)
            {
                try
                {
                    int sent = 0;
                    while (sent < packet.Length)
                    {
                        int s = _socket.Send(packet, sent, packet.Length - sent, SocketFlags.None);
                        if (s <= 0) break;
                        sent += s;
                    }
                }
                catch { }
            }
        }

        public void Close()
        {
            try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket?.Close(); } catch { }
        }
    }
}
EOF

cat > PacketCodec.cs << 'EOF'
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RemoteControl.Relay
{
    public class HandshakeData
    {
        public string Role { get; set; } = "";
        public string HostName { get; set; } = "";
        public string AppPath { get; set; } = "";
        public string OnlineAvatar { get; set; } = "";
    }

    public class SelectClientData
    {
        public string ClientId { get; set; } = "";
    }

    public static class PacketCodec
    {
        const byte CYCLER_RELAY_HANDSHAKE = 200;
        const byte CYCLER_RELAY_CLIENT_LIST_REQUEST = 201;
        const byte CYCLER_RELAY_CLIENT_LIST_RESPONSE = 202;
        const byte CYCLER_RELAY_SELECT_CLIENT = 203;
        const byte CYCLER_RELAY_CLIENT_ONLINE = 204;
        const byte CYCLER_RELAY_CLIENT_OFFLINE = 205;

        public static byte GetPacketType(byte[] fullPacket)
        {
            if (fullPacket == null || fullPacket.Length < 5) return 0;
            return fullPacket[4];
        }

        private static string GetBodyJson(byte[] fullPacket)
        {
            if (fullPacket == null || fullPacket.Length <= 5) return null;
            return Encoding.UTF8.GetString(fullPacket, 5, fullPacket.Length - 5);
        }

        private static byte[] BuildPacket(byte packetType, object body)
        {
            byte[] bodyBytes = body != null
                ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body))
                : new byte[0];

            int packetLength = 4 + 1 + bodyBytes.Length;
            byte[] packet = new byte[packetLength];

            Buffer.BlockCopy(BitConverter.GetBytes(packetLength), 0, packet, 0, 4);
            packet[4] = packetType;
            if (bodyBytes.Length > 0)
                Buffer.BlockCopy(bodyBytes, 0, packet, 5, bodyBytes.Length);

            return packet;
        }

        public static HandshakeData DecodeHandshake(byte[] fullPacket)
        {
            byte type = GetPacketType(fullPacket);
            if (type != CYCLER_RELAY_HANDSHAKE) return null;

            string json = GetBodyJson(fullPacket);
            if (string.IsNullOrEmpty(json)) return null;

            try { return JsonConvert.DeserializeObject<HandshakeData>(json); }
            catch { return null; }
        }

        public static SelectClientData DecodeSelectClient(byte[] fullPacket)
        {
            string json = GetBodyJson(fullPacket);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<SelectClientData>(json); }
            catch { return null; }
        }

        public static byte[] BuildClientListResponse(ConcurrentDictionary<string, ClientSession> clients)
        {
            var list = new List<object>();
            foreach (var kv in clients)
            {
                var s = kv.Value;
                list.Add(new
                {
                    ClientId = s.SessionId,
                    HostName = s.HostName,
                    IP = s.RemoteEndPoint,
                    AppPath = s.AppPath,
                    OnlineAvatar = s.OnlineAvatar,
                    OnlineTime = s.OnlineTime
                });
            }
            return BuildPacket(CYCLER_RELAY_CLIENT_LIST_RESPONSE, new { Clients = list });
        }

        public static byte[] BuildClientOnline(ClientSession client)
        {
            return BuildPacket(CYCLER_RELAY_CLIENT_ONLINE, new
            {
                ClientId = client.SessionId,
                HostName = client.HostName,
                IP = client.RemoteEndPoint,
                OnlineAvatar = client.OnlineAvatar
            });
        }

        public static byte[] BuildClientOffline(string clientId)
        {
            return BuildPacket(CYCLER_RELAY_CLIENT_OFFLINE, new { ClientId = clientId });
        }
    }
}
EOF

echo "[信息] 项目文件创建完成"

# 5. 编译项目
echo ""
echo "[步骤4] 编译项目..."
dotnet restore
dotnet publish -c Release -o ./publish

# 6. 开放防火墙端口
echo ""
echo "[步骤5] 配置防火墙..."
if command -v ufw &> /dev/null; then
    ufw allow $RELAY_PORT/tcp
    echo "[信息] UFW 已放行端口 $RELAY_PORT"
elif command -v firewall-cmd &> /dev/null; then
    firewall-cmd --permanent --add-port=$RELAY_PORT/tcp
    firewall-cmd --reload
    echo "[信息] firewalld 已放行端口 $RELAY_PORT"
else
    echo "[警告] 未检测到防火墙工具，请手动放行端口 $RELAY_PORT"
fi

# 7. 创建 systemd 服务
echo ""
echo "[步骤6] 创建系统服务..."
cat > /etc/systemd/system/$SERVICE_NAME.service << SERVICEEOF
[Unit]
Description=RemoteControl Relay Server
After=network.target

[Service]
Type=simple
WorkingDirectory=$INSTALL_DIR/publish
ExecStart=$INSTALL_DIR/publish/RemoteControl.Relay $RELAY_PORT
Restart=always
RestartSec=5
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
SERVICEEOF

systemctl daemon-reload
systemctl enable $SERVICE_NAME
systemctl start $SERVICE_NAME

# 8. 验证服务状态
echo ""
echo "[步骤7] 验证服务状态..."
sleep 2
systemctl status $SERVICE_NAME --no-pager

echo ""
echo "============================================"
echo " 部署完成!"
echo "============================================"
echo ""
echo " 服务器编号: #8432 / MyServer"
echo " 监听端口: $RELAY_PORT"
echo " 服务名称: $SERVICE_NAME"
echo " 安装目录: $INSTALL_DIR"
echo ""
echo " 管理命令:"
echo "   查看状态: systemctl status $SERVICE_NAME"
echo "   查看日志: journalctl -u $SERVICE_NAME -f"
echo "   重启服务: systemctl restart $SERVICE_NAME"
echo "   停止服务: systemctl stop $SERVICE_NAME"
echo ""
echo " 下一步操作:"
echo "   在控制端设置中填写:"
echo "   - Relay服务IP: $(curl -s ifconfig.me || echo '你的公网IP')"
echo "   - Relay端口: $RELAY_PORT"
echo "============================================"
