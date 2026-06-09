using System;

namespace RemoteControl.Protocals.Response
{
    public class ResponseChangeConfig : ResponseBase
    {
        public string ServerIP = "";
        public int ServerPort;
        public string ServiceName = "";
        public string OnlineAvatar = "";
        public bool IsHide;
        public bool RestartClient;
    }
}
