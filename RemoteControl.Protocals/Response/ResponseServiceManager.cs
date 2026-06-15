using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class ServiceInfo
    {
        public string ServiceName;
        public string DisplayName;
        public string Status;
        public string StartType;
        public string Type;
        public int PID;
        public string Description;
    }

    public class ResponseServiceManager : ResponseBase
    {
        public List<ServiceInfo> Services;
    }
}
