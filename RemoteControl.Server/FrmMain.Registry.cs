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
        private void treeView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Left)
                return;

            if (this.currentSession == null)
                return;

            TreeViewHitTestInfo ti = this.treeView2.HitTest(e.Location);
            if (ti != null && ti.Node != null)
            {
                RequestViewRegistryKey req = new RequestViewRegistryKey();
                if (ti.Node.Level == 0)
                {
                    return;
                }
                else if (ti.Node.Level == 1)
                {
                    // 根节点
                    eRegistryHive keyRoot = (eRegistryHive)Enum.Parse(typeof(eRegistryHive), ti.Node.Tag as string);
                    req.KeyRoot = keyRoot;
                    req.KeyPath = null;
                }
                else
                {
                    // 非根节点
                    req = ti.Node.Tag as RequestViewRegistryKey;
                }
                // 在listview上标注当前的key节点
                this.listView2.Tag = req;
                this.textBoxRegistryPath.Text = "计算机\\" + req.KeyRoot + "\\" + req.KeyPath;
                this.currentSession.Send(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, req);
            }
        }

        /// <summary>
        /// 注册表项右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView2_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button!= System.Windows.Forms.MouseButtons.Right)
                return;

            ContextMenuStrip cms = new System.Windows.Forms.ContextMenuStrip();
            cms.Items.Add("切换", null, (o, args) => {
                if (this.currentSession == null)
                    return;

                TreeViewHitTestInfo ti = this.treeView2.HitTest(e.Location);
                if (ti != null && ti.Node != null)
                {
                    RequestViewRegistryKey req = new RequestViewRegistryKey();
                    if (ti.Node.Level == 0)
                    {
                        return;
                    }
                    else if (ti.Node.Level == 1)
                    {
                        // 根节点
                        eRegistryHive keyRoot = (eRegistryHive)Enum.Parse(typeof(eRegistryHive), ti.Node.Tag as string);
                        req.KeyRoot = keyRoot;
                        req.KeyPath = null;
                    }
                    else
                    {
                        // 非根节点
                        req = ti.Node.Tag as RequestViewRegistryKey;
                    }
                    var frm = new FrmInputUrl();
                    frm.Text = "请输入注册表相对地址";
                    frm.ShowDialog();
                    if (frm.InputText!=null)
                    {
                        if (req.KeyPath == null)
                        {
                            req.KeyPath = frm.InputText;
                        }
                        else
                        {
                            req.KeyPath += "\\" + frm.InputText;
                        }
                        this.currentSession.Send(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, req);
                    }
                }

                
            });
            cms.Show(sender as TreeView, e.Location);

        }

        /// <summary>
        /// 注册表值操作菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView2_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.currentSession == null)
                return;

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                var key = this.listView2.Tag as RequestViewRegistryKey;
                if (key != null)
                {
                    if (this.listView2.SelectedItems.Count > 0)
                    {
                        string valueName = this.listView2.SelectedItems[0].Text;
                        var req = new RequestOpeRegistryValueName();
                        req.KeyRoot = key.KeyRoot;
                        req.KeyPath = key.KeyPath;
                        req.ValueName = valueName;

                        ContextMenuStrip cms = new ContextMenuStrip();
                        cms.Items.Add("删除", null, (o, args) =>
                        {
                            req.Operation = OpeType.Delete;
                            this.currentSession.Send(ePacketType.PACKET_OPE_REGISTRY_VALUE_NAME_REQUEST, req);
                        });
                        cms.Show(this.listView2, e.Location);
                    }
                    else
                    {
                        ContextMenuStrip cms = new ContextMenuStrip();
                        cms.Items.Add("刷新", null, (o, args) =>
                        {
                            this.currentSession.Send(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, key);
                        });
                        cms.Show(this.listView2, e.Location);
                    }
                }
            }
        }

    }
}
