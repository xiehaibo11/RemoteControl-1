using System;

namespace RemoteControl.Protocals.Request
{
    public enum eRunFileMode
    {
        Show = 0,
        Hide,
        Elevate
    }

    public class RequestRunFile
    {
        public string FilePath;
        public eRunFileMode Mode;
    }
}
