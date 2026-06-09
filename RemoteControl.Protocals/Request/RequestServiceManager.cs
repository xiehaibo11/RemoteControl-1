using System;

namespace RemoteControl.Protocals.Request
{
    public enum eServiceAction
    {
        List = 0,
        Start,
        Stop,
        Delete
    }

    public class RequestServiceManager
    {
        public eServiceAction Action;
        public string ServiceName;
    }
}
