using System;
using System.Threading;
using System.Windows.Forms;
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
                    if (req == null) return;

                    resp.RequestMessage = req.Message;
                    string reply = string.Empty;
                    Thread inputThread = new Thread(() =>
                    {
                        reply = ShowInputDialog(req.Message ?? string.Empty, "远程聊天");
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

        private string ShowInputDialog(string message, string title)
        {
            string result = "";
            using (var form = new Form())
            {
                form.Text = title;
                form.Size = new System.Drawing.Size(400, 180);
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label();
                lbl.Text = message;
                lbl.Location = new System.Drawing.Point(10, 10);
                lbl.Size = new System.Drawing.Size(370, 50);
                form.Controls.Add(lbl);

                var txt = new TextBox();
                txt.Location = new System.Drawing.Point(10, 70);
                txt.Size = new System.Drawing.Size(370, 22);
                form.Controls.Add(txt);

                var btnOk = new Button();
                btnOk.Text = "确定";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Location = new System.Drawing.Point(210, 100);
                form.Controls.Add(btnOk);

                var btnCancel = new Button();
                btnCancel.Text = "取消";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new System.Drawing.Point(300, 100);
                form.Controls.Add(btnCancel);

                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    result = txt.Text;
                }
            }
            return result;
        }
    }
}
