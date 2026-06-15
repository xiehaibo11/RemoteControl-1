using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class NetworkConnectionInfo
    {
        public string Protocol;
        public string LocalAddress;
        public int LocalPort;
        public string RemoteAddress;
        public int RemotePort;
        public string Status;
        public int ProcessId;
        public string ProcessName;
        public string GeoLocation;
    }

    public class ResponseGetNetworkConnections : ResponseBase
    {
        public List<NetworkConnectionInfo> Connections;
        public int TcpCount;
        public int UdpCount;
        public string CollectedAt;
    }
}
