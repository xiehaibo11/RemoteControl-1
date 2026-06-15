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
        private string GetFileSizeDesc(long size)
        {
            string result = string.Empty;

            if (size > 1000 * 1000 * 1000)
            {
                result = (size * 1.0 / 1000 / 1000 / 1000).ToString("0.000") + " GB";
            }
            else if (size > 1000 * 1000)
            {
                result = (size * 1.0 / 1000 / 1000).ToString("0.000") + " MB";
            }
            else if (size > 1000)
            {
                result = (size * 1.0 / 1000).ToString("0.000") + " KB";
            }
            else
            {
                result = size + " byte";
            }

            return result;
        }

        private string GetDriveSizeDesc(long size)
        {
            if (size <= 0)
                return string.Empty;

            return GetFileSizeDesc(size);
        }

        private void SetFileListColumnsForFiles()
        {
            this.columnHeader2.Text = "大小(KB)";
            this.columnHeader3.Text = "修改日期";
            this.columnHeader7.Text = "类型";
        }

        private void SetFileListColumnsForDrives()
        {
            this.columnHeader2.Text = "类型";
            this.columnHeader3.Text = "总大小";
            this.columnHeader7.Text = "可用空间";
        }

        private void RequestDriveList(SocketSession session)
        {
            if (session == null)
                return;

            session.Send(ePacketType.PACKET_GET_DRIVES_EX_REQUEST, null);
        }

        private void RefreshCurrentFileView()
        {
            if (this.currentSession == null)
                return;

            string currentDir = this.listView1.Tag as string;
            if (string.IsNullOrEmpty(currentDir))
            {
                RequestDriveList(this.currentSession);
                return;
            }

            RequestGetSubFilesOrDirs req = new RequestGetSubFilesOrDirs();
            req.parentDir = currentDir;
            this.currentSession.Send(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, req);
        }

    }
}
