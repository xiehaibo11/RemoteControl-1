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
        private void HandleFileListPackets(PacketReceivedEventArgs e)
        {
            if (e.PacketType == ePacketType.PACKET_GET_DRIVES_RESPONSE)
            {
                ResponseGetDrives resp = e.Obj as ResponseGetDrives;
                if (resp == null || resp.drives == null)
                    return;

                this.UpdateUI(() =>
                {
                    SetFileListColumnsForDrives();
                    this.listView1.Items.Clear();
                    this.listView1.Tag = null;
                    for (int i = 0; i < resp.drives.Count; i++)
                    {
                        string drive = resp.drives[i];
                        ListViewItem item = new ListViewItem(new string[] { drive, "", "", "" }, 7);
                        ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                        tag.IsFile = false;
                        tag.Path = drive;
                        item.Tag = tag;
                        this.listView1.Items.Add(item);
                    }

                });
            }
            else if (e.PacketType == ePacketType.PACKET_GET_DRIVES_EX_RESPONSE)
            {
                ResponseGetDrivesEx resp = e.Obj as ResponseGetDrivesEx;
                if (resp == null || resp.Drives == null)
                    return;

                this.UpdateUI(() =>
                {
                    SetFileListColumnsForDrives();
                    this.listView1.Items.Clear();
                    this.listView1.Tag = null;
                    for (int i = 0; i < resp.Drives.Count; i++)
                    {
                        DriveInfoEx drive = resp.Drives[i];
                        string displayName = string.IsNullOrEmpty(drive.VolumeLabel)
                            ? drive.Name
                            : string.Format("{0} ({1})", drive.Name, drive.VolumeLabel);
                        ListViewItem item = new ListViewItem(
                            new string[]
                            {
                                displayName,
                                drive.DriveType,
                                GetDriveSizeDesc(drive.TotalSize),
                                GetDriveSizeDesc(drive.FreeSpace)
                            }, 7);
                        ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                        tag.IsFile = false;
                        tag.Path = drive.Name;
                        item.Tag = tag;
                        this.listView1.Items.Add(item);
                    }
                });
            }
            else if (e.PacketType == ePacketType.PACKET_GET_SUBFILES_OR_DIRS_RESPONSE)
            {
                ResponseGetSubFilesOrDirs resp = e.Obj as ResponseGetSubFilesOrDirs;
                if (resp == null)
                    return;

                List<DirectoryProperty> dirs = resp.dirs ?? new List<DirectoryProperty>();
                List<FileProperty> files = resp.files ?? new List<FileProperty>();
                this.UpdateUI(() =>
                    {
                        SetFileListColumnsForFiles();
                        this.listView1.Items.Clear();
                        for (int i = 0; i < dirs.Count; i++)
                        {
                            var dirObj = dirs[i];
                            if (dirObj == null || string.IsNullOrEmpty(dirObj.DirPath))
                                continue;

                            string path = dirObj.DirPath;
                            string itemText = System.IO.Path.GetFileName(path);
                            ListViewItem item = new ListViewItem(new string[] { itemText, "", dirObj.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss"),"<文件夹>" }, 3);
                            ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                            tag.IsFile = false;
                            tag.Path = path;
                            item.Tag = tag;
                            this.listView1.Items.Add(item);
                        }
                        for (int i = 0; i < files.Count; i++)
                        {
                            var fileObj = files[i];
                            if (fileObj == null || string.IsNullOrEmpty(fileObj.FilePath))
                                continue;

                            string path = fileObj.FilePath;
                            string itemText = System.IO.Path.GetFileName(path);
                            string extension = System.IO.Path.GetExtension(path).ToLower();
                            if (!this.imageList1.Images.ContainsKey(extension))
                            {
                                this.imageList1.Images.Add(extension, CommonUtil.GetIcon(extension, true));
                            }
                            ListViewItem item = new ListViewItem(new string[] { itemText, GetFileSizeDesc(fileObj.Size), fileObj.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss"), "<文件>" }, extension);
                            ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                            tag.IsFile = true;
                            tag.Path = path;
                            item.Tag = tag;
                            this.listView1.Items.Add(item);
                        }
                    });
            }
        }
    }
}
