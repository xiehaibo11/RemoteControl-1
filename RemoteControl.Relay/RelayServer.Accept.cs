using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteControl.Relay
{
    public partial class RelayServer
    {
        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (IsRunning && !token.IsCancellationRequested)
            {
                try
                {
                    Socket client = await _listener.AcceptAsync();
                    var session = new ClientSession(client);
                    session.ConfigureSocket();

                    if ((_clients.Count + _controllers.Count) < 20 ||
                        (_clients.Count + _controllers.Count) % 100 == 0)
                    {
                        Console.WriteLine("[connect] " + session.RemoteEndPoint);
                    }

                    _ = HandleSessionAsync(session);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                        Console.WriteLine("[error] Accept: " + ex.Message);
                }
            }
        }

        private async Task HandleSessionAsync(ClientSession session)
        {
            try
            {
                byte[] packet = await session.ReceivePacketAsync();
                if (packet == null)
                {
                    session.Close();
                    return;
                }

                HandshakeData handshake = PacketCodec.DecodeHandshake(packet);
                if (handshake == null)
                {
                    Console.WriteLine("[error] Invalid handshake: " + session.RemoteEndPoint);
                    session.Close();
                    return;
                }

                if (handshake.Role == "client")
                {
                    await HandleClientConnectionAsync(session, handshake);
                }
                else if (handshake.Role == "controller")
                {
                    await HandleControllerConnectionAsync(session);
                }
                else
                {
                    Console.WriteLine("[error] Unknown role: " + handshake.Role);
                    session.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[error] HandleSession: " + ex.Message);
                session.Close();
            }
        }
    }
}
