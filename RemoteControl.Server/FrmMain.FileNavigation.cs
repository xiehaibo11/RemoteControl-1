using System;
using System.IO;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hitTestInfo = this.listView1.HitTest(e.Location);
            if (hitTestInfo != null && hitTestInfo.Item != null)
            {
                ListViewItemFileOrDirTag tag = hitTestInfo.Item.Tag as ListViewItemFileOrDirTag;
                if (tag == null)
                    return;

                if (!tag.IsFile)
                {
                    if (this.currentSession != null)
                    {
                        this.listView1.Tag = tag.Path;
                        RequestGetSubFilesOrDirs req = new RequestGetSubFilesOrDirs();
                        req.parentDir = tag.Path;
                        this.currentSession.Send(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, req);
                    }
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (this.listView1.Tag != null)
            {
                string dir = this.listView1.Tag as string;
                if (this.currentSession != null)
                {
                    DirectoryInfo parentDirInfo = Directory.GetParent(dir);
                    if (parentDirInfo != null)
                    {
                        string parent = parentDirInfo.FullName;
                        RequestGetSubFilesOrDirs req = new RequestGetSubFilesOrDirs();
                        req.parentDir = parent;
                        this.currentSession.Send(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, req);
                        this.listView1.Tag = parent;
                    }
                    else
                    {
                        RequestDriveList(this.currentSession);
                    }
                }
            }
        }
    }
}
