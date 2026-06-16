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
                            Console.WriteLine("[bind] controller=" + controllerId + " -> client=" + selectData.ClientId);
                        }
                        else
                        {
                            Console.WriteLine("[bind-fail] clientId=" + (selectData == null ? "<null>" : selectData.ClientId) + " not found");
                        }
                    }
                    else if (packetType == 206 || packetType == 207)
                    {
                        RelayDataFrameData frame = PacketCodec.DecodeRelayDataFrame(packet);
                        ClientSession targetClient;
                        if (frame != null && !string.IsNullOrEmpty(frame.ClientId) &&
                            _clients.TryGetValue(frame.ClientId, out targetClient) &&
                            targetClient.IsConnected)
                        {
                            targetClient.BoundController = session;
                            if (frame.Payload != null && frame.Payload.Length > 0)
                                await targetClient.SendRawAsync(frame.Payload);
                        }
                        else
                        {
                            Console.WriteLine("[forward-skip] invalid target clientId=" +
                                (frame == null ? "<null>" : frame.ClientId));
                        }
                    }
                    else
                    {
                        ClientSession client = session.BoundClient;
                        if (client != null && client.IsConnected)
                        {
                            await client.SendRawAsync(packet);
                        }
                        else if (client == null)
                        {
                            Console.WriteLine("[forward-skip] no bound client, type=" + packetType);
                        }
                    }
                }
            }
            catch { }

            _controllers.TryRemove(controllerId, out _);
            if (session.BoundClient != null)
                session.BoundClient.BoundController = null;
            foreach (var kv in _clients)
            {
                if (kv.Value.BoundController == session)
                    kv.Value.BoundController = null;
            }
            Console.WriteLine("[controller offline] " + controllerId);
            session.Close();
        }
    }
}
