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
        private void 刷新ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.PostRequstWithCurrentSession(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses());
        }

        private void 刷新急速ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.PostRequstWithCurrentSession(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses(){IsSimpleMode = true});
        }

        private void 结束进程ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> processIds = GetSelectedProcessIds();
            if (processIds.Count < 1)
            {
                MsgBox.Info("请先选择进程！");
                return;
            }
            RequestKillProcesses req = new RequestKillProcesses();
            req.ProcessIds = processIds;

            this.PostRequstWithCurrentSession(ePacketType.PACKET_KILL_PROCESS_REQUEST, req);
        }

        private void 挂起进程ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendSelectedProcessOperation(ePacketType.PACKET_SUSPEND_PROCESS_REQUEST);
        }

        private void 恢复进程ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendSelectedProcessOperation(ePacketType.PACKET_RESUME_PROCESS_REQUEST);
        }

        private void SendSelectedProcessOperation(ePacketType packetType)
        {
            List<string> processIds = GetSelectedProcessIds();
            if (processIds.Count < 1)
            {
                MsgBox.Info("请先选择进程！");
                return;
            }

            RequestKillProcesses req = new RequestKillProcesses();
            req.ProcessIds = processIds;
            this.PostRequstWithCurrentSession(packetType, req);
        }

        private void SetProcessPriority(string priorityClass)
        {
            List<string> processIds = GetSelectedProcessIds();
            if (processIds.Count < 1)
            {
                MsgBox.Info("请先选择进程！");
                return;
            }

            int processId;
            if (!int.TryParse(processIds[0], out processId))
            {
                MsgBox.Info("进程ID无效！");
                return;
            }

            RequestSetProcessPriority req = new RequestSetProcessPriority();
            req.ProcessId = processId;
            req.PriorityClass = priorityClass;
            this.PostRequstWithCurrentSession(ePacketType.PACKET_SET_PROCESS_PRIORITY_REQUEST, req);
        }

        private List<string> GetSelectedProcessIds()
        {
            List<string> processIds = new List<string>();
            for (int i = 0; i < this.listView3.SelectedItems.Count; i++)
            {
                var item = this.listView3.SelectedItems[i];
                if (item.SubItems.Count > 1)
                {
                    string processId = item.SubItems[1].Text.Trim();
                    if (!string.IsNullOrEmpty(processId))
                        processIds.Add(processId);
                }
            }
            return processIds;
        }

    }
}
