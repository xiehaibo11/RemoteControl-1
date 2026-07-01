using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void AddClient(SocketSession oClient)
        {
            if (oClient == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<SocketSession>(AddClient), oClient);
                return;
            }
            TreeNode internetNode = this.InternetTreeNode;
            if (internetNode == null)
                return;
            UpsertOnlineClient(oClient);
            if (SessionMatchesFilter(oClient) && FindClientNode(oClient) == null)
            {
                internetNode.Nodes.Add(CreateClientNode(oClient));
            }
            UpsertHostDashboardClient(oClient);
            QueryAndSetLocation(oClient);
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
            doOutput(GetClientDisplayText(oClient) + " 上线了！");
            ShowClientOnlineNotification(oClient);
        }

        private void UpdateClient(SocketSession client)
        {
            if (client == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<SocketSession>(UpdateClient), client);
                return;
            }

            UpsertOnlineClient(client);
            TreeNode node = FindClientNode(client);
            if (node == null && SessionMatchesFilter(client))
            {
                TreeNode internetNode = this.InternetTreeNode;
                if (internetNode != null)
                    internetNode.Nodes.Add(CreateClientNode(client));
            }
            else if (node != null)
            {
                node.Text = GetClientDisplayText(client);
                node.Tag = client;
            }

            UpsertHostDashboardClient(client);
            QueryAndSetLocation(client);
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
        }

        private void UpsertOnlineClient(SocketSession client)
        {
            if (client == null || string.IsNullOrEmpty(client.SocketId))
                return;
            for (int i = 0; i < onlineClientSessions.Count; i++)
            {
                if (onlineClientSessions[i].SocketId == client.SocketId)
                {
                    onlineClientSessions[i] = client;
                    return;
                }
            }
            onlineClientSessions.Add(client);
        }

        private void SyncClientsFromServerSnapshot()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SyncClientsFromServerSnapshot));
                return;
            }

            RemoteControlServer server = RSCApplication.oRemoteControlServer;
            if (server == null)
                return;

            onlineClientSessions.Clear();
            foreach (SocketSession session in server.GetClientSnapshot())
            {
                UpsertOnlineClient(session);
            }

            RenderClientTree();
            RefreshHostDashboard();
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
        }

        private TreeNode CreateClientNode(SocketSession client)
        {
            TreeNode treeNode = new TreeNode(GetClientDisplayText(client));
            treeNode.Tag = client;
            string avatarKey = client == null ? null : client.OnlineAvatar;
            if (!string.IsNullOrEmpty(avatarKey) && this.treeView1 != null &&
                this.treeView1.ImageList != null && this.treeView1.ImageList.Images.ContainsKey(avatarKey))
            {
                treeNode.ImageKey = avatarKey;
                treeNode.SelectedImageKey = avatarKey;
            }
            else
            {
                treeNode.ImageKey = "qq";
                treeNode.SelectedImageKey = "qq";
            }
            return treeNode;
        }

        private static string GetClientDisplayText(SocketSession client)
        {
            if (client == null)
                return "未知主机";
            if (!string.IsNullOrEmpty(client.HostName))
                return client.HostName;
            string ip = client.GetSocketIPById();
            if (!string.IsNullOrEmpty(ip))
                return ip;
            if (!string.IsNullOrEmpty(client.SocketId))
                return client.SocketId;
            return "未知主机";
        }

        private bool SessionMatchesFilter(SocketSession session)
        {
            if (string.IsNullOrEmpty(hostFilterKeyword))
                return true;

            string keyword = hostFilterKeyword.ToLower();
            return ContainsIgnoreCase(session.SocketId, keyword) ||
                ContainsIgnoreCase(session.HostName, keyword) ||
                ContainsIgnoreCase(session.GetSocketIPById(), keyword) ||
                ContainsIgnoreCase(session.AppPath, keyword);
        }

        private static bool ContainsIgnoreCase(string value, string lowerKeyword)
        {
            return !string.IsNullOrEmpty(value) && value.ToLower().Contains(lowerKeyword);
        }

        private void RenderClientTree()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RenderClientTree));
                return;
            }

            this.InternetTreeNode.Nodes.Clear();
            foreach (SocketSession session in onlineClientSessions)
            {
                if (SessionMatchesFilter(session))
                {
                    this.InternetTreeNode.Nodes.Add(CreateClientNode(session));
                }
            }
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
        }

        private TreeNode FindClientNode(SocketSession oClient)
        {
            if (oClient == null || string.IsNullOrEmpty(oClient.SocketId))
                return null;
            TreeNode internetNode = this.InternetTreeNode;
            if (internetNode == null)
                return null;
            for (int i = internetNode.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = internetNode.Nodes[i];
                SocketSession session = node.Tag as SocketSession;
                if (session != null && session.SocketId == oClient.SocketId)
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// 异步查询客户端外网IP的地理位置，查询完成后更新Dashboard
        /// </summary>
        private void QueryAndSetLocation(SocketSession client)
        {
            if (client == null)
                return;
            string ip = client.GetExternalIP();
            if (string.IsNullOrEmpty(ip) || ip == "-")
                return;
            // 已有位置信息则跳过
            if (!string.IsNullOrEmpty(client.Location) && client.Location != "-")
                return;

            IPLocationUtil.QueryAsync(ip, (location) =>
            {
                client.SetLocation(location);
                // 回调可能在后台线程，需要切回UI线程更新Dashboard
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            UpsertHostDashboardClient(client);
                        }));
                    }
                    catch { }
                }
            });
        }
    }
}
