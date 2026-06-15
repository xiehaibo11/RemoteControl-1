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
        private void HandleFileOperationPackets(PacketReceivedEventArgs e)
        {
            if (e.PacketType == ePacketType.PACKET_CREATE_FILE_OR_DIR_RESPONSE)
            {
                ResponseCreateFileOrDir resp = e.Obj as ResponseCreateFileOrDir;
                if (resp == null)
                    return;

                if (resp.Result == false)
                {
                    doOutput(resp.Path + "创建失败，" + resp.Path);
                }

                string path = resp.Path;
                string itemText = System.IO.Path.GetFileName(path);
                ListViewItem item = new ListViewItem(string.Concat(new object[] { itemText, "", "" }), resp.PathType == Protocals.ePathType.File ? 152 : 3);
                ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                tag.IsFile = resp.PathType == Protocals.ePathType.File;
                tag.Path = path;
                item.Tag = tag;
                this.listView1.Items.Add(item);
            }
            else if (e.PacketType == ePacketType.PACKET_DELETE_FILE_OR_DIR_RESPONSE)
            {
                ResponseDeleteFileOrDir resp = e.Obj as ResponseDeleteFileOrDir;
                if (resp == null)
                    return;

                if (resp.Result == false)
                {
                    doOutput(resp.Path + "删除失败，" + resp.Path);
                }

                for (int i = this.listView1.Items.Count - 1; i >= 0; i--)
                {
                    var tag = this.listView1.Items[i].Tag as ListViewItemFileOrDirTag;
                    if (tag != null && resp.Path == tag.Path)
                    {
                        this.listView1.Items.RemoveAt(i);
                    }
                }
            }
            else if (e.PacketType == ePacketType.PACKET_START_DOWNLOAD_HEADER_RESPONSE)
            {
                ResponseStartDownloadHeader downloadHeader = e.Obj as ResponseStartDownloadHeader;
                if (downloadHeader == null || string.IsNullOrEmpty(downloadHeader.SavePath))
                    return;

                string fileName = System.IO.Path.GetFileName(downloadHeader.Path);
                this.DownloadHeader = downloadHeader;

                // 处理资源释放
                if (this.downloadFileStream != null)
                {
                    this.downloadFileStream.Close();
                    this.downloadFileStream = null;
                }
                this.recvSize = 0;
                this.UpdateDownloadProgressAction = null;

                if (downloadHeader.FileSize == 0)
                {
                    try
                    {
                        using (System.IO.File.Create(downloadHeader.SavePath))
                        {
                        }
                        doOutput("下载完成：" + downloadHeader.SavePath);
                    }
                    catch (Exception ex)
                    {
                        doOutput("创建空文件失败：" + ex.Message);
                    }
                    return;
                }

                new Thread(() =>
                {
                    var frm = new FrmDownload(() =>
                    {
                        // 处理资源释放
                        if (this.downloadFileStream != null)
                        {
                            this.downloadFileStream.Close();
                            this.downloadFileStream = null;
                        }
                        this.recvSize = 0;
                        this.UpdateDownloadProgressAction = null;

                        // 发送终止下载请求
                        if (this.currentSession != null)
                            this.currentSession.Send(ePacketType.PACKET_STOP_DOWNLOAD_REQUEST, null);
                    }, downloadHeader.Path, downloadHeader.SavePath, downloadHeader.FileSize);
                    this.DownloadWindow = frm;
                    this.UpdateDownloadProgressAction = frm.UpdateProgress;
                    frm.ShowDialog();
                }) { IsBackground = true }.Start();
            }
            else if (e.PacketType == ePacketType.PACKET_START_DOWNLOAD_RESPONSE)
            {
                ResponseStartDownload resp = e.Obj as ResponseStartDownload;
                if (resp == null || resp.Data == null || this.DownloadHeader == null)
                    return;

                try
                {
                    string localFull = this.DownloadHeader.SavePath;
                    if (!System.IO.File.Exists(localFull))
                    {
                        System.IO.File.Create(localFull).Close();
                    }
                    byte[] data = resp.Data;
                    if (downloadFileStream == null)
                    {
                        downloadFileStream = new FileStream(localFull, FileMode.Open, FileAccess.Write);
                    }
                    downloadFileStream.Write(data, 0, data.Length);
                    this.recvSize += data.Length;

                    // 显示进度
                    if (this.DownloadWindow!=null)
                    {
                        this.DownloadWindow.UpdateProgress(this.recvSize);
                    }

                    // 下载完成
                    if (this.recvSize >= this.DownloadHeader.FileSize)
                    {
                        if (this.DownloadWindow != null)
                            this.DownloadWindow.Close();

                        // 处理资源释放
                        if (this.downloadFileStream != null)
                        {
                            this.downloadFileStream.Close();
                            this.downloadFileStream = null;
                        }
                        this.recvSize = 0;
                        this.UpdateDownloadProgressAction = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_COMMAND_RESPONSE)
            {
                ResponseCommand resp = e.Obj as ResponseCommand;
                if(resp.Result ==false)
                    return;

                this.textBoxCommandResponse.AppendText(resp.CommandResponse + "\r\n");
            }
            else if (e.PacketType == ePacketType.PACKET_GET_PROCESSES_RESPONSE)
            {
                ResponseGetProcesses resp = e.Obj as ResponseGetProcesses;

                new Thread(() => 
                {
                    UpdateProcessListView(resp);

                }) { IsBackground=true }.Start();
            }
            else if (e.PacketType == ePacketType.PACKET_COPY_FILE_OR_DIR_RESPONSE)
            {
                var resp = e.Obj as ResponseCopyFile;
                doOutput("复制" + resp.SourceFile + "成功!");
            }
            else if (e.PacketType == ePacketType.PACKET_MOVE_FILE_OR_DIR_RESPONSE)
            {
                var resp = e.Obj as ResponseMoveFile;
                doOutput("移动" + resp.SourceFile + "成功!");
            }
        }
    }
}
