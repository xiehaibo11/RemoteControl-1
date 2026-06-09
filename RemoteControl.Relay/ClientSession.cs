using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RemoteControl.Relay
{
    /// <summary>
    /// 表示一个连接会话(客户端或控制端)
    /// </summary>
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

        // 双向绑定
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

        /// <summary>
        /// 接收一个完整数据包
        /// 协议格式: [4字节packetLength(含自身)][1字节packetType][body]
        /// packetLength = 4 + 1 + body.Length
        /// </summary>
        public byte[] ReceivePacket()
        {
            try
            {
                // 读取4字节长度头
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
        
                // 读取剩余数据 (packetLength - 4 字节)
                int remainLen = packetLength - 4;
                byte[] remain = new byte[remainLen];
                received = 0;
                while (received < remainLen)
                {
                    int r = _socket.Receive(remain, received, remainLen - received, SocketFlags.None);
                    if (r <= 0) return null;
                    received += r;
                }
        
                // 返回完整包 [长度头+其余数据]
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

        /// <summary>
        /// 发送原始数据包(已含长度头)
        /// </summary>
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
