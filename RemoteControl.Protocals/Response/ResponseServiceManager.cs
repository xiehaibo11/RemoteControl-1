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
    }

    public class ResponseServiceManager : ResponseBase
    {
        public List<ServiceInfo> Services;
    }
}
