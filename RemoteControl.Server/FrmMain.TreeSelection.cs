using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SocketSession session = e.Node.Tag as SocketSession;
            if (session != null)
            {
                this.currentSession = session;
                UpdateSelectedClientInfo(session);
                var mousePos = Control.MousePosition;
                var tv = sender as TreeView;
                var loc = tv.PointToClient(mousePos);
                loc.Offset(10, 0);
                this.toolTip1.Show(session.HostName, tv, loc, 2000);

                RSCApplication.oRemoteControlServer.SelectClient(session.SocketId);
            }
        }

        private void treeView1_MouseHover(object sender, EventArgs e)
        {
        }

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeViewHitTestInfo hitTestInfo = this.treeView1.HitTest(e.Location);
            if (hitTestInfo != null && hitTestInfo.Node != null)
            {
                SocketSession session = hitTestInfo.Node.Tag as SocketSession;
                if (session != null)
                {
                    if (session != this.currentSession)
                    {
                        if (this.currentSession != null)
                        {
                            if (MsgBox.Question("是否要切换当前连接?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                            {
                                return;
                            }
                        }
                        this.currentSession = session;
                        UpdateSelectedClientInfo(session);
                    }
                    RequestDriveList(session);
                }
                else
                {
                    UpdateSelectedClientInfo(null);
                }
            }
        }
    }
}
