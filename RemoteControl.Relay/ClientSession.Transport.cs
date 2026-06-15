using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RemoteControl.Relay
{
    public partial class ClientSession
    {
        public void ConfigureSocket()
        {
            try { _socket.NoDelay = true; } catch { }
            try { _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); } catch { }
            try { _socket.ReceiveBufferSize = 64 * 1024; } catch { }
            try { _socket.SendBufferSize = 64 * 1024; } catch { }
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
                LastSeenUtc = DateTime.UtcNow;
                return fullPacket;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]> ReceivePacketAsync()
        {
            try
            {
                byte[] lenBuf = new byte[4];
                if (!await ReceiveExactAsync(lenBuf, 0, lenBuf.Length))
                    return null;

                int packetLength = BitConverter.ToInt32(lenBuf, 0);
                if (packetLength <= 4 || packetLength > 10 * 1024 * 1024)
                    return null;

                int remainLen = packetLength - 4;
                byte[] remain = new byte[remainLen];
                if (!await ReceiveExactAsync(remain, 0, remainLen))
                    return null;

                byte[] fullPacket = new byte[packetLength];
                Buffer.BlockCopy(lenBuf, 0, fullPacket, 0, 4);
                Buffer.BlockCopy(remain, 0, fullPacket, 4, remainLen);
                LastSeenUtc = DateTime.UtcNow;
                return fullPacket;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> ReceiveExactAsync(byte[] buffer, int offset, int count)
        {
            int received = 0;
            while (received < count)
            {
                int r = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer, offset + received, count - received),
                    SocketFlags.None);
                if (r <= 0)
                    return false;
                received += r;
            }
            return true;
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

        public async Task SendRawAsync(byte[] packet)
        {
            if (packet == null || packet.Length == 0)
                return;

            await _sendSemaphore.WaitAsync();
            try
            {
                int sent = 0;
                while (sent < packet.Length)
                {
                    int s = await _socket.SendAsync(
                        new ArraySegment<byte>(packet, sent, packet.Length - sent),
                        SocketFlags.None);
                    if (s <= 0)
                        break;
                    sent += s;
                }
            }
            catch { }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public void Close()
        {
            _closed = true;
            try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket?.Close(); } catch { }
        }
    }
}
