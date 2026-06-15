using System;
using System.Runtime.InteropServices;

namespace RemoteControl.Client.Handlers
{
    partial class RequestGetProcessesHandler
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        const uint THREAD_SUSPEND_RESUME = 0x0002;
    }
}
