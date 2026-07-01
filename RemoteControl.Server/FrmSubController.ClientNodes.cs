using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmSubController
    {
        #region 服务器事件

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, ClientConnectedEventArgs>(OnClientConnected), sender, e);
                return;
            }
            if (e.Client != null) AddSessionNode(e.Client);
            UpdateStatus();
            if (e.Client != null)
                LogMessage("主机上线: " + GetSessionLabel(e.Client));
        }

        private void OnClientDisconnected(object sender, ClientConnectedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, ClientConnectedEventArgs>(OnClientDisconnected), sender, e);
                return;
            }
            if (e.Client != null) RemoveSessionNode(e.Client);
            UpdateStatus();
        }

        private void OnClientListChanged(object sender, ClientListChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, ClientListChangedEventArgs>(OnClientListChanged), sender, e);
                return;
            }
            treeViewClients.BeginUpdate();
            try
            {
                if (e.RemovedClients != null)
                    foreach (var c in e.RemovedClients) RemoveSessionNode(c);
                if (e.AddedClients != null)
                    foreach (var c in e.AddedClients)
                    {
                        AddSessionNode(c);
                        LogMessage("主机上线: " + GetSessionLabel(c));
                    }
            }
            finally
            {
                treeViewClients.EndUpdate();
            }
            UpdateStatus();
        }

        #endregion

        #region 客户端节点管理

        private void AddSessionNode(SocketSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return;
            if (_sessionIndex.ContainsKey(session.SocketId))
            {
                UpdateNodeText(session);
                return;
            }
            _sessions.Add(session);
            _sessionIndex[session.SocketId] = session;
            TreeNode parentNode = treeViewClients.Nodes["online"];

            // 按分组放置
            string group = session.Remark ?? "";
            if (!string.IsNullOrEmpty(group) && group.Contains("/"))
                group = group.Split('/')[0];
            TreeNode groupNode = FindOrCreateGroupNode(parentNode, group);

            TreeNode node = groupNode.Nodes.Add(session.SocketId, FormatNodeText(session));
            node.Tag = session;
            _nodeIndex[session.SocketId] = node;
            groupNode.Expand();
        }

        private void RemoveSessionNode(SocketSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return;
            _sessions.RemoveAll(s => s.SocketId == session.SocketId);
            _sessionIndex.Remove(session.SocketId);
            TreeNode node;
            if (_nodeIndex.TryGetValue(session.SocketId, out node))
            {
                _nodeIndex.Remove(session.SocketId);
                if (node.TreeView != null) node.Remove();
            }
            if (_currentSession != null && _currentSession.SocketId == session.SocketId)
            {
                _currentSession = null;
                listViewInfo.Items.Clear();
            }
        }

        private void UpdateNodeText(SocketSession session)
        {
            TreeNode node;
            if (_nodeIndex.TryGetValue(session.SocketId, out node))
                node.Text = FormatNodeText(session);
        }

        private string FormatNodeText(SocketSession session)
        {
            string host = session.HostName ?? session.GetExternalIP();
            string remark = string.IsNullOrEmpty(session.Remark) ? "" : " [" + session.Remark + "]";
            return host + remark;
        }

        private TreeNode FindOrCreateGroupNode(TreeNode parent, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return parent;
            foreach (TreeNode n in parent.Nodes)
            {
                if (n.Text == groupName && n.Tag == null)
                    return n;
            }
            TreeNode gn = parent.Nodes.Add(groupName);
            gn.Tag = null; // marker for group node (not a session)
            gn.ForeColor = Color.DarkBlue;
            gn.NodeFont = new Font(treeViewClients.Font, FontStyle.Bold);
            return gn;
        }

        private void UpdateStatus()
        {
            labelStatus.Text = "在线: " + _sessions.Count;
        }

        #endregion
    }
}
