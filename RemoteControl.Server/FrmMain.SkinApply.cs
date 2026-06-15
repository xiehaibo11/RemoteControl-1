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
        private void actChangeSkin(string sSkinFile)
        {
            try
            {
                if (string.IsNullOrEmpty(sSkinFile) || !System.IO.File.Exists(sSkinFile))
                    return;
                if (!EnsureSkinEngine())
                    return;

                this.skinEngine1.SkinFile = sSkinFile;
                Settings.CurrentSettings.SkinPath = sSkinFile;
                if (this.ToolStripMenuItemSkins != null)
                {
                    UpdateSkinMenuChecked(this.ToolStripMenuItemSkins.DropDownItems, Settings.CurrentSettings.SkinPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("actChangeSkin Error: " + ex.Message);
            }
        }

        private bool EnsureSkinEngine()
        {
            if (this.skinEngine1 != null)
                return true;

            try
            {
                this.skinEngine1 = new Sunisoft.IrisSkin.SkinEngine((System.ComponentModel.Component)this);
                this.skinEngine1.SerialNumber = "";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EnsureSkinEngine Error: " + ex.Message);
                return false;
            }
        }

        private bool UpdateSkinMenuChecked(ToolStripItemCollection items, string skinPath)
        {
            bool hasCheckedChild = false;
            if (items == null)
                return false;

            for (int j = 0; j < items.Count; j++)
            {
                var item = items[j] as ToolStripMenuItem;
                if (item == null)
                    continue;

                bool itemChecked = item.Tag != null && item.Tag.ToString() == skinPath;
                bool childChecked = false;
                if (item.DropDownItems.Count > 0)
                {
                    childChecked = UpdateSkinMenuChecked(item.DropDownItems, skinPath);
                }
                item.Checked = itemChecked || childChecked;
                hasCheckedChild = hasCheckedChild || item.Checked;
            }

            return hasCheckedChild;
        }

    }
}
