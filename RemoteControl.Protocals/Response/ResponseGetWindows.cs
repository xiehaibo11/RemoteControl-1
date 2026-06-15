using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class WindowDetailInfo
    {
        public string Title;
        public string ProcessName;
        public int ProcessId;
        public int ThreadId;
        public string Handle;
        public bool IsVisible;
        public string WindowState;
        public string ClassName;
        public string Bounds;
    }

    public class ResponseGetWindows : ResponseBase
    {
        public List<WindowDetailInfo> Windows;
        public int TotalCount;
        public string CollectedAt;
    }
}
