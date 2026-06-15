using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestSetProcessPriority
    {
        public int ProcessId;
        /// <summary>
        /// 优先级: Idle, BelowNormal, Normal, AboveNormal, High, RealTime
        /// </summary>
        public string PriorityClass;
    }
}
