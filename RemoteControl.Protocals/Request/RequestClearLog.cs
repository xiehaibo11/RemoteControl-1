using System;

namespace RemoteControl.Protocals.Request
{
    public enum eClearLogType
    {
        All = 0,
        System,
        Security,
        Application
    }

    public class RequestClearLog
    {
        public eClearLogType LogType;
    }
}
