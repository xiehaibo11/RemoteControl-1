using System;
using System.Diagnostics;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    class RequestClearLogHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestClearLog;
                    if (req == null) return;

                    switch (req.LogType)
                    {
                        case eClearLogType.All:
                            ProcessUtil.Run("wevtutil.exe", "cl System", true);
                            ProcessUtil.Run("wevtutil.exe", "cl Security", true);
                            ProcessUtil.Run("wevtutil.exe", "cl Application", true);
                            break;
                        case eClearLogType.System:
                            ProcessUtil.Run("wevtutil.exe", "cl System", true);
                            break;
                        case eClearLogType.Security:
                            ProcessUtil.Run("wevtutil.exe", "cl Security", true);
                            break;
                        case eClearLogType.Application:
                            ProcessUtil.Run("wevtutil.exe", "cl Application", true);
                            break;
                    }

                    var resp = new ResponseClearLog();
                    resp.Result = true;
                    resp.Message = "日志清理完成";
                    session.Send(ePacketType.PACKET_CLEAR_LOG_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseClearLog();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_CLEAR_LOG_RESPONSE, resp);
                }
            });
        }
    }
}
