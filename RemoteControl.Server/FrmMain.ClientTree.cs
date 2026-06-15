using System;
using System.Windows.Forms;
using RemoteControl.Protocals;

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
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
            doOutput(GetClientDisplayText(oClient) + " 上线了！");
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
    }
}
