using System;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void RemoveClient(SocketSession oClient)
        {
            if (oClient == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<SocketSession>(RemoveClient), oClient);
                return;
            }
            TreeNode internetNode = this.InternetTreeNode;
            if (internetNode == null)
                return;
            for (int i = onlineClientSessions.Count - 1; i >= 0; i--)
            {
                if (onlineClientSessions[i].SocketId == oClient.SocketId)
                {
                    onlineClientSessions.RemoveAt(i);
                }
            }
            for (int i = internetNode.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = internetNode.Nodes[i];
                SocketSession session = node.Tag as SocketSession;
                if (session != null && session.SocketId == oClient.SocketId)
                {
                    internetNode.Nodes.RemoveAt(i);
                }
            }
            // 仅当下线的客户端是当前选中客户端时才清空
            if (this.currentSession != null && this.currentSession.SocketId == oClient.SocketId)
            {
                this.currentSession = null;
                UpdateSelectedClientInfo(null);
            }
            RemoveHostDashboardClient(oClient);
            RemoveDashboardGroup(oClient);
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
            doOutput((oClient.HostName ?? oClient.SocketId) + " 下线了！");
        }
    }
}
