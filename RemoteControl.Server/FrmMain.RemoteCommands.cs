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
        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            if (this.currentSession != null)
            {
                ListViewItem selectedItem = this.listView1.SelectedItems[0];
                ListViewItemFileOrDirTag tag = selectedItem.Tag as ListViewItemFileOrDirTag;
                if (tag == null)
                    return;

                if (tag.IsFile == false)
                {
                    MessageBox.Show("暂时不支持文件夹下载！");
                    return;
                }

                string fileName = System.IO.Path.GetFileName(tag.Path);
                var sfd = new SaveFileDialog();
                sfd.FileName = fileName;
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                var req = new RequestStartDownload();
                req.Path = tag.Path;
                req.SavePath = sfd.FileName;
                this.currentSession.Send(ePacketType.PACKET_START_DOWNLOAD_REQUEST, req);
            }
        }

        /// <summary>
        /// 发送cmd命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendCommand_Click(object sender, EventArgs e)
        {
            RequestCommand req = new RequestCommand();
            req.Command = this.textBoxCommandRequest.Text;

            PostRequstWithCurrentSession(ePacketType.PACKET_COMMAND_REQUEST, req);

            this.textBoxCommandRequest.Clear();
        }

        /// <summary>
        /// 回车发送命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxCommandRequest_KeyUp(object sender, KeyEventArgs e)
        {
            bool sendCommand = false;
            if (this.sendCommandHotKey == SendCommandHotKey.Enter)
            {
                if (!e.Control && e.KeyCode == Keys.Enter)
                {
                    sendCommand = true;
                }
            }
            else if (this.sendCommandHotKey == SendCommandHotKey.CtrlEnter)
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    sendCommand = true;
                }
            }

            if (sendCommand)
            {
                buttonSendCommand_Click(null, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 对当前会话发送数据包
        /// </summary>
        /// <param name="packetType"></param>
        /// <param name="reqObj"></param>
        private void PostRequstWithCurrentSession(ePacketType packetType, object reqObj)
        {
            if (this.currentSession == null)
                return;
            this.currentSession.Send(packetType, reqObj);
        }

        /// <summary>
        /// 选择发送命令模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSelectSendCmdMode_Click(object sender, EventArgs e)
        {
            ContextMenuStrip cms = new ContextMenuStrip();

            ToolStripMenuItem enterMenuItem = new ToolStripMenuItem("Enter 发送", null, (o, args) =>
                {
                    for (int i = 0; i < cms.Items.Count; i++)
                    {
                        var control = cms.Items[i];
                        if (control is ToolStripMenuItem)
                        {
                            (control as ToolStripMenuItem).Checked = false;
                        }
                    }
                    (o as ToolStripMenuItem).Checked = true;
                    this.sendCommandHotKey = SendCommandHotKey.Enter;
                });
            enterMenuItem.Checked = this.sendCommandHotKey == SendCommandHotKey.Enter;
            cms.Items.Add(enterMenuItem);

            ToolStripMenuItem altEnterMenuItem = new ToolStripMenuItem("Ctrl+Enter 发送", null, (o, args) =>
            {
                for (int i = 0; i < cms.Items.Count; i++)
                {
                    var control = cms.Items[i];
                    if (control is ToolStripMenuItem)
                    {
                        (control as ToolStripMenuItem).Checked = false;
                    }
                }
                (o as ToolStripMenuItem).Checked = true;
                this.sendCommandHotKey = SendCommandHotKey.CtrlEnter;
            });
            altEnterMenuItem.Checked = this.sendCommandHotKey == SendCommandHotKey.CtrlEnter;
            cms.Items.Add(altEnterMenuItem);
            Point location = this.buttonSelectSendCmdMode.PointToClient(Control.MousePosition);
            cms.Show(this.buttonSelectSendCmdMode, location);
        }

        /// <summary>
        /// 当前会话是否有效
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentSessionValid()
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择一台计算机");
                return false;
            }

            return true;
        }

    }
}
