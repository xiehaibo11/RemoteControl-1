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
        private void initSkinMenus()
        {
            this.ToolStripMenuItemSkins.DropDownItems.Clear();
            RSCApplication.lstSkins = RSCApplication.GetAllSkinFiles();
            if (RSCApplication.lstSkins.Count > 0)
            {
                Dictionary<string, ToolStripMenuItem> skinGroups = new Dictionary<string, ToolStripMenuItem>(StringComparer.OrdinalIgnoreCase);
                int iSkinCount = RSCApplication.lstSkins.Count;
                for (int i = 0; i < iSkinCount; i++)
                {
                    string sSkinFile = RSCApplication.lstSkins[i];
                    string familyKey = GetSkinFamilyKey(sSkinFile);
                    ToolStripMenuItem groupMenu;
                    if (!skinGroups.TryGetValue(familyKey, out groupMenu))
                    {
                        groupMenu = new ToolStripMenuItem(GetSkinFamilyDisplayName(familyKey));
                        skinGroups.Add(familyKey, groupMenu);
                        this.ToolStripMenuItemSkins.DropDownItems.Add(groupMenu);
                    }

                    string sSkinName = GetSkinVariantDisplayName(sSkinFile, familyKey);
                    ToolStripMenuItem menuSkin = new ToolStripMenuItem(sSkinName, null, (o, e) =>
                        {
                            ToolStripMenuItem m = o as ToolStripMenuItem;
                            string sFile = m.Tag as string;
                            actChangeSkin(sFile);
                        });
                    menuSkin.Tag = sSkinFile;
                    groupMenu.DropDownItems.Add(menuSkin);
                }
            }
            var tools = RSCApplication.GetAllTools();
            if (tools.Count > 0)
            {
                for (int i = 0; i < tools.Count; i++)
                {
                    string tool = tools[i];
                    string menuText = System.IO.Path.GetFileNameWithoutExtension(tool);
                    Bitmap bmp = System.Drawing.Icon.ExtractAssociatedIcon(tool).ToBitmap();
                    ToolStripMenuItem menuItem = new ToolStripMenuItem(menuText, bmp, (o, e) =>
                    {
                        ToolStripMenuItem m = o as ToolStripMenuItem;
                        string sFile = m.Tag as string;
                        ProcessUtil.Run(sFile, "", false);
                    });
                    menuItem.Tag = tool;
                    this.ToolStripMenuItemTools.DropDownItems.Add(menuItem);
                }
            }

            Dictionary<ePathType, string> paths = new Dictionary<ePathType,string>();
            paths.Add(ePathType.APP_DIR, "根目录");
            paths.Add(ePathType.AVATAR_DIR,"头像目录");
            paths.Add(ePathType.SKINS_DIR,"皮肤目录");
            paths.Add(ePathType.TOOL_DIR,"工具目录");
            foreach (var pair in paths)
	        {
                string path = RSCApplication.GetPath(pair.Key);
                string menuText = pair.Value;
                ToolStripMenuItem menuItem = new ToolStripMenuItem(menuText, null, (o, e) =>
                {
                    ToolStripMenuItem m = o as ToolStripMenuItem;
                    string sFile = m.Tag as string;
                    ProcessUtil.RunByCmdStart("explorer.exe", sFile, true);
                    //ProcessUtil.Run("explorer.exe", sFile, false);
                });
                menuItem.Tag = path;
                this.ToolStripMenuItemUsualFolders.DropDownItems.Add(menuItem);
	        }
        }

    }
}
