using System;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestClipboardHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_CLIPBOARD_GET_REQUEST)
            {
                HandleGetClipboard(session);
            }
            else if (reqType == ePacketType.PACKET_CLIPBOARD_SET_REQUEST)
            {
                HandleSetClipboard(session, reqObj as RequestClipboardSet);
            }
        }

        private void HandleGetClipboard(SocketSession session)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseClipboardGet();
                try
                {
                    string text = null;
                    var thread = new Thread(() =>
                    {
                        if (Clipboard.ContainsText())
                            text = Clipboard.GetText();
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();

                    resp.Text = text ?? "";
                    resp.Result = true;
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }
                session.Send(ePacketType.PACKET_CLIPBOARD_GET_RESPONSE, resp);
            });
        }

        private void HandleSetClipboard(SocketSession session, RequestClipboardSet req)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseClipboardSet();
                try
                {
                    var thread = new Thread(() =>
                    {
                        Clipboard.SetText(req.Text ?? "");
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    resp.Result = true;
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }
                session.Send(ePacketType.PACKET_CLIPBOARD_SET_RESPONSE, resp);
            });
        }
    }
}
