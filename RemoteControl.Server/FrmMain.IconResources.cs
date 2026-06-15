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
        private void initIcons()
        {
            string sFileName = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\shell32.dll";
            int iIconCount = Win32API.ExtractIconEx(sFileName, -1, null, null, 0);
            IntPtr[] pLargeIcons = new IntPtr[iIconCount];
            IntPtr[] pSmallIcons = new IntPtr[iIconCount];
            Win32API.ExtractIconEx(sFileName, 0, pLargeIcons, pSmallIcons, iIconCount);
            for (int i = 0; i < iIconCount; i++)
			{
                this.imageList1.Images.Add(Icon.FromHandle(pLargeIcons[i]));
			}

            Dictionary<string,View> viewDic = new Dictionary<string,View>();
            viewDic.Add("大图标", View.LargeIcon);
            viewDic.Add("详情", View.Details);
            viewDic.Add("小图标", View.SmallIcon);
            viewDic.Add("列表", View.List);
            viewDic.Add("平铺", View.Tile);
            this.toolStripSplitButton1.Click += (o, args) => this.toolStripSplitButton1.ShowDropDown();
            foreach (var viewItem in viewDic)
	        {
                ToolStripItem tsi = this.toolStripSplitButton1.DropDownItems.Add(viewItem.Key, null, (o, args) =>
                    {
                        ToolStripItem i = o as ToolStripItem;
                        View v = (View)i.Tag;
                        this.listView1.View = v;
                    });
                tsi.Tag = viewItem.Value;
	        }

            var avatars = RSCApplication.GetAllAvatarFiles();
            for (int i = 0; i < avatars.Count; i++)
            {
                string avatarPath = avatars[i];
                string avatarFileName = System.IO.Path.GetFileName(avatarPath);
                this.imageList2.Images.Add(avatarFileName, Image.FromFile(avatarPath));
            }
        }

    }
}
