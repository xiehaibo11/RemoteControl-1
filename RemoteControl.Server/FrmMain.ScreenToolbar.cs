using System;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择客户端！");
                return;
            }

            // 确保 Relay 绑定指向当前客户端
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(this.currentSession.SocketId);

            var frm = new FrmCaptureScreen(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionScreenHandlers.ContainsKey(sessionId))
            {
                this.sessionScreenHandlers.Add(sessionId, frm.HandleScreen);
            }
            else
            {
                this.sessionScreenHandlers[sessionId] = frm.HandleScreen;
            }
            frm.Show();

            RequestStartGetScreen req = new RequestStartGetScreen();
            req.fps = 5;
            this.currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }
    }
}
