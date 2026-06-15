using System;
using System.Collections.Generic;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        internal List<SocketSession> GetOnlineSessions()
        {
            return new List<SocketSession>(onlineClientSessions);
        }

        internal void OpenFileManagerForSession(SocketSession session)
        {
            if (session == null)
                return;

            SelectSessionFromSubController(session);
            if (this.tabControl1 != null && this.tabControl1.TabPages.Count > 0)
                this.tabControl1.SelectedIndex = 0;
            RequestDriveList(session);
        }

        internal void RegisterScreenHandler(string sessionId, Action<ResponseStartGetScreen> handler)
        {
            if (string.IsNullOrEmpty(sessionId) || handler == null)
                return;

            sessionScreenHandlers[sessionId] = handler;
        }

        internal void RegisterVideoHandler(string sessionId, Action<ResponseStartCaptureVideo> handler)
        {
            if (string.IsNullOrEmpty(sessionId) || handler == null)
                return;

            sessionVideoHandlers[sessionId] = handler;
        }

        private void SelectSessionFromSubController(SocketSession session)
        {
            this.currentSession = session;
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(session.SocketId);
            UpdateSelectedClientInfo(session);
        }
    }
}
