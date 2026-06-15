using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestGetNetworkConnections
    {
        public bool IncludeUDP = true;
        public string Filter;
    }
}
