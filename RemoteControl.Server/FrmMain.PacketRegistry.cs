using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using RemoteControl.Protocals;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using log4net;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Audio;
using RemoteControl.Audio.Codecs;
using RemoteControl.Protocals.Utilities;
using RemoteControl.Protocals.Relay;
using RemoteControl.Server.Utils;
namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void HandleRegistryPackets(PacketReceivedEventArgs e)
        {
            if (e.PacketType == ePacketType.PACKET_VIEW_REGISTRY_KEY_RESPONSE)
            {
                // 查看注册表项
                var resp = e.Obj as ResponseViewRegistryKey;
                this.UpdateUI(() =>
                {
                    try
                    {
                        // 清除右侧value值列表
                        this.listView2.Items.Clear();
                        if (resp.KeyNames != null)
                        {
                            TreeView tv = this.treeView2;
                            // 查找根节点
                            TreeNode rootNode = null;
                            for (int j = 0; j < tv.Nodes[0].Nodes.Count; j++)
                            {
                                TreeNode node = tv.Nodes[0].Nodes[j];
                                string str = node.Tag.ToString();
                                eRegistryHive erh = (eRegistryHive)Enum.Parse(typeof(eRegistryHive), str);
                                if (erh == resp.KeyRoot)
                                {
                                    rootNode = node;
                                    break;
                                }
                            }
                            if (rootNode == null)
                            {
                                doOutput("未找到Registry根节点");
                                return;
                            }
                            TreeNode curNode = rootNode;
                            if (resp.KeyPath != null)
                            {
                                // 查找目标的节点
                                string[] keyNames = resp.KeyPath.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < keyNames.Length; i++)
                                {
                                    var keyName = keyNames[i];
                                    var found = false;
                                    for (int k = 0; k < curNode.Nodes.Count; k++)
                                    {
                                        var node = curNode.Nodes[k];
                                        if (node.Text == keyName)
                                        {
                                            found = true;
                                            curNode = node;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        TreeNode node = new TreeNode(keyName, 0, 0);
                                        List<string> curKeys = new List<string>();
                                        for (int ii = 0; ii <= i; ii++)
                                        {
                                            curKeys.Add(keyNames[ii]);
                                        }
                                        node.Tag = new RequestViewRegistryKey() {
                                            KeyRoot = resp.KeyRoot,
                                            KeyPath = string.Join("\\",curKeys)
                                        };
                                        curNode.Nodes.Add(node);
                                        curNode = node;
                                    }
                                }
                            }
                            // 清除目标节点的子节点
                            curNode.Nodes.Clear();
                            // 重新添加目标节点的子节点
                            for (int i = 0; i < resp.KeyNames.Length; i++)
                            {
                                string keyName = resp.KeyNames[i];
                                TreeNode node = new TreeNode(keyName, 0, 0);
                                string newKeyPath = resp.KeyPath + @"\" + keyName;
                                newKeyPath = newKeyPath.TrimStart('\\');
                                node.Tag = new RequestViewRegistryKey(){
                                    KeyRoot = resp.KeyRoot,
                                    KeyPath = newKeyPath
                                };
                                curNode.Nodes.Add(node);
                            }
                            curNode.Expand();
                            tv.SelectedNode = curNode;
                            this.listView2.Tag = curNode.Tag;

                            this.textBoxRegistryPath.Text = "计算机\\" + resp.KeyRoot + "\\" + resp.KeyPath;
                        }
                        if (resp.ValueNames != null)
                        {
                            // 添加右侧value值列表
                            int valueNameLen = resp.ValueNames.Length;
                            for (int i = 0; i < valueNameLen; i++)
                            {
                                ListViewItem item = new ListViewItem(new string[]{
                                    resp.ValueNames[i],
                                    resp.ValueKinds[i].ToString(),
                                    resp.Values[i].ToString()
                                },resp.ValueKinds[i].ToString());
                                this.listView2.Items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("", ex);
                    }
                });
            }
        }
    }
}
