using System;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void hostListView_MouseUp(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = hostListView.HitTest(e.Location);
            if (hit.Item != null)
            {
                hit.Item.Selected = true;
                SelectDashboardSession(hit.Item.Tag as SocketSession);
            }
            if (e.Button == MouseButtons.Right && contextMenuStripClient != null)
                contextMenuStripClient.Show(hostListView, e.Location);
        }

        private void hostListView_DoubleClick(object sender, EventArgs e)
        {
            if (hostListView.SelectedItems.Count < 1)
                return;
            SelectDashboardSession(hostListView.SelectedItems[0].Tag as SocketSession);
            onMenuFileManager(sender, e);
        }

        private void SelectDashboardSession(SocketSession session)
        {
            if (session == null)
                return;

            this.currentSession = session;
            session.Touch();
            UpdateSelectedClientInfo(session);
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(session.SocketId);
            UpsertHostDashboardClient(session);
        }
    }
}
