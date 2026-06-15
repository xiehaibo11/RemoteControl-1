using System;
using System.Threading.Tasks;

namespace RemoteControl.Relay
{
    public partial class RelayServer
    {
        private async Task HandleControllerConnectionAsync(ClientSession session)
        {
            session.Role = "controller";
            string controllerId = session.SessionId;
            _controllers[controllerId] = session;
            Console.WriteLine("[controller] " + session.RemoteEndPoint + " ID=" + controllerId);

            try
            {
                while (IsRunning && session.IsConnected)
                {
                    byte[] packet = await session.ReceivePacketAsync();
                    if (packet == null)
                        break;

                    byte packetType = PacketCodec.GetPacketType(packet);
                    if (packetType == 201)
                    {
                        byte[] listPacket = PacketCodec.BuildClientListResponse(_clients);
                        await session.SendRawAsync(listPacket);
                    }
                    else if (packetType == 203)
                    {
                        SelectClientData selectData = PacketCodec.DecodeSelectClient(packet);
                        ClientSession targetClient;
                        if (selectData != null && _clients.TryGetValue(selectData.ClientId, out targetClient))
                        {
                            session.BoundClient = targetClient;
                            targetClient.BoundController = session;
                        }
                    }
                    else
                    {
                        ClientSession client = session.BoundClient;
                        if (client != null && client.IsConnected)
                            await client.SendRawAsync(packet);
                    }
                }
            }
            catch { }

            _controllers.TryRemove(controllerId, out _);
            if (session.BoundClient != null)
                session.BoundClient.BoundController = null;
            Console.WriteLine("[controller offline] " + controllerId);
            session.Close();
        }
    }
}
