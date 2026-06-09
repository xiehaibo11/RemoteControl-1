using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestChangeConfig
    {
        public string ServerIP = "";
        public int ServerPort;
        public string ServiceName = "";
        public string OnlineAvatar = "";
        public bool IsHide;
        public bool RestartClient;
    }
}
