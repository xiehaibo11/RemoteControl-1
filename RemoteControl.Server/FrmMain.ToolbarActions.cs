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
        private void toolStripButtonCaptureVideo_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择客户端！");
                return;
            }
            var frm = new FrmCaptureVideo(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionVideoHandlers.ContainsKey(sessionId))
            {
                this.sessionVideoHandlers.Add(sessionId, frm.HandleScreen);
            }
            else
            {
                this.sessionVideoHandlers[sessionId] = frm.HandleScreen;
            }
            frm.Show();;
        }

        private void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            var frm = new FrmSettings();
            frm.Owner = this;
            frm.Show();
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.SaveSettings();
        }

        private void 配置服务程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButtonSettings_Click(null, null);
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAbout())
            {
                frm.ShowDialog();
            }
        }

        /// <summary>
        /// 树控件右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Right)
                return;

            TreeViewHitTestInfo ti = treeView1.HitTest(e.Location);
            if (ti != null && 
                ti.Node != null &&
                ti.Node.Level == 1)
            {
                SocketSession client = ti.Node.Tag as SocketSession;
                if (client != null)
                {
                    this.treeView1.SelectedNode = ti.Node;
                    this.currentSession = client;
                    RSCApplication.oRemoteControlServer.SelectClient(client.SocketId);
                    contextMenuStripClient.Show(this.treeView1, e.Location);
                }
            }
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// 双击注册表项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
    }
}
