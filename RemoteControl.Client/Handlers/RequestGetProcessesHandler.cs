using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    partial class RequestGetProcessesHandler : IRequestHandler
    {
        public void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_GET_PROCESSES_REQUEST)
            {
                var req = reqObj as RequestGetProcesses;
                new Thread(() => StartGetProcesses(session, req)) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_KILL_PROCESS_REQUEST)
            {
                var req = reqObj as RequestKillProcesses;
                new Thread(() =>
                {
                    StartKillProcesses(session, req);
                    StartGetProcesses(session, new RequestGetProcesses() { IsSimpleMode = true });
                }) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_SUSPEND_PROCESS_REQUEST)
            {
                var req = reqObj as RequestKillProcesses;
                new Thread(() =>
                {
                    SuspendProcesses(session, req);
                }) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_RESUME_PROCESS_REQUEST)
            {
                var req = reqObj as RequestKillProcesses;
                new Thread(() =>
                {
                    ResumeProcesses(session, req);
                }) { IsBackground = true }.Start();
            }
            else if (reqType == ePacketType.PACKET_SET_PROCESS_PRIORITY_REQUEST)
            {
                var req = reqObj as RequestSetProcessPriority;
                new Thread(() =>
                {
                    SetProcessPriority(session, req);
                }) { IsBackground = true }.Start();
            }
        }
    }
}
