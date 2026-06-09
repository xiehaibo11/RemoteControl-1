using System;

namespace RemoteControl.Protocals.Request
{
    public enum eStartupType
    {
        Registry = 0,
        RunKey
    }

    public class RequestWriteStartup
    {
        public eStartupType StartupType;
    }
}
