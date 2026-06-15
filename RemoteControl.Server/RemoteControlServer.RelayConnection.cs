using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Relay;

namespace RemoteControl.Server
{
    partial class RemoteControlServer
    {
        public void Start(string relayIP, int relayPort, int connectTimeoutMilliseconds)
        {
            Stop();

            try
            {
                _relaySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _relaySocket.NoDelay = true;
                _relaySocket.ReceiveBufferSize = 64 * 1024;
                _relaySocket.SendBufferSize = 64 * 1024;
                ConnectWithTimeout(_relaySocket, CreateRelayEndpoint(relayIP, relayPort), connectTimeoutMilliseconds);

                _relaySession = new SocketSession(_relaySocket.RemoteEndPoint.ToString(), _relaySocket);

                var handshake = new RelayHandshake();
                handshake.Role = "controller";
                _relaySession.Send(ePacketType.CYCLER_RELAY_HANDSHAKE, handshake);

                _isRunning = true;
                _relaySession.Send(ePacketType.CYCLER_RELAY_CLIENT_LIST_REQUEST, null);

                _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "RelayRecvThread" };
                _recvThread.Start();
            }
            catch
            {
                Stop();
                throw;
            }
        }

        private static EndPoint CreateRelayEndpoint(string relayIP, int relayPort)
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

        private static void ConnectWithTimeout(Socket socket, EndPoint endPoint, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds < 1)
            {
                socket.Connect(endPoint);
                return;
            }

            IAsyncResult result = socket.BeginConnect(endPoint, null, null);
            WaitHandle waitHandle = result.AsyncWaitHandle;
            try
            {
                if (!waitHandle.WaitOne(timeoutMilliseconds, false))
                {
                    try { socket.Close(); } catch { }
                    throw new TimeoutException("Relay connect timed out.");
                }

                socket.EndConnect(result);
            }
            finally
            {
                waitHandle.Close();
            }
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
                    if (recvSize < 1)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    for (int i = 0; i < recvSize; i++)
                        data.Add(buffer[i]);

                    while (data.Count >= 4)
                    {
                        int packetLength = ReadPacketLength(data);
                        if (packetLength < 5 || packetLength > 10 * 1024 * 1024)
                            throw new InvalidOperationException("Invalid relay packet length: " + packetLength);

                        if (data.Count < packetLength)
                            break;

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

            RaiseRelayDisconnected();
        }

        private void RaiseRelayDisconnected()
        {
            try
            {
                if (RelayDisconnected != null)
                    RelayDisconnected(this, EventArgs.Empty);
            }
            catch { }
        }

        private static int ReadPacketLength(List<byte> data)
        {
            return data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
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
                    var args = new PacketReceivedEventArgs();
                    args.PacketType = packetType;
                    args.Obj = obj;
                    args.Session = session;
                    try { PacketReceived(this, args); }
                    catch (Exception ex) { Console.WriteLine("PacketReceived事件异常: " + ex.Message); }
                }
            }
        }
    }
}
