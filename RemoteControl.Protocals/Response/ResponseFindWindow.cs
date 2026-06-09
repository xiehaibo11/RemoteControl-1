using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class WindowInfo
    {
        public string Title;
        public string ProcessName;
        public int ProcessId;
        public string Handle;
    }

    public class ResponseFindWindow : ResponseBase
    {
        public List<WindowInfo> Windows;
    }
}
