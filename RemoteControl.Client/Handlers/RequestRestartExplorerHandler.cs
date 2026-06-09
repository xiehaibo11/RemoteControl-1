using System;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    class RequestRestartExplorerHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    ProcessUtil.Run("taskkill.exe", "/f /im explorer.exe", true);
                    System.Threading.Thread.Sleep(1000);
                    ProcessUtil.Run("explorer.exe", "", false);
                }
                catch (Exception ex)
                {
                    DoOutput("重启Explorer异常: " + ex.Message);
                }
            });
        }
    }
}
