using System.Windows.Forms;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public partial class FrmHostInfo
    {
        private void AddDiskTab(ResponseGetHostInfo resp)
        {
            var tabDisk = new TabPage("磁盘");
            if (resp.Disks != null && resp.Disks.Count > 0)
            {
                var lv = CreateDetailsListView();
                lv.Columns.Add("挂载", 60);
                lv.Columns.Add("文件系统", 80);
                lv.Columns.Add("总量", 80);
                lv.Columns.Add("可用", 80);
                lv.Columns.Add("使用率", 80);
                foreach (var d in resp.Disks)
                {
                    var item = new ListViewItem(d.Mount);
                    item.SubItems.Add(d.FileSystem);
                    item.SubItems.Add(d.TotalGB + " GB");
                    item.SubItems.Add(d.AvailableGB + " GB");
                    item.SubItems.Add(d.UsagePercent + "%");
                    lv.Items.Add(item);
                }
                tabDisk.Controls.Add(lv);
            }
            _tabControl.TabPages.Add(tabDisk);
        }

        private void AddNetworkTab(ResponseGetHostInfo resp)
        {
            var tabNet = new TabPage("网络");
            if (resp.NetworkAdapters != null && resp.NetworkAdapters.Count > 0)
            {
                var lv = CreateDetailsListView();
                lv.Columns.Add("适配器", 160);
                lv.Columns.Add("MAC地址", 140);
                lv.Columns.Add("IP地址", 140);
                lv.Columns.Add("状态", 80);
                foreach (var na in resp.NetworkAdapters)
                {
                    var item = new ListViewItem(na.Name ?? "");
                    item.SubItems.Add(na.MacAddress ?? "");
                    item.SubItems.Add(na.IPAddresses != null ? string.Join(", ", na.IPAddresses.ToArray()) : "");
                    item.SubItems.Add(na.Status ?? "");
                    lv.Items.Add(item);
                }
                tabNet.Controls.Add(lv);
            }
            else
            {
                tabNet.Controls.Add(CreateEmptyLabel("未获取到网络适配器信息"));
            }
            _tabControl.TabPages.Add(tabNet);
        }

        private void AddInstalledSoftwareTab(ResponseGetHostInfo resp)
        {
            var tabSoft = new TabPage("软件安装");
            if (resp.InstalledSoftware != null && resp.InstalledSoftware.Count > 0)
            {
                var lv = CreateDetailsListView();
                lv.Columns.Add("软件名称", 240);
                lv.Columns.Add("版本", 120);
                lv.Columns.Add("安装日期", 120);
                foreach (var sw in resp.InstalledSoftware)
                {
                    var item = new ListViewItem(sw.Name ?? "");
                    item.SubItems.Add(sw.Version ?? "");
                    item.SubItems.Add(sw.InstallDate ?? "");
                    lv.Items.Add(item);
                }
                tabSoft.Controls.Add(lv);
            }
            else
            {
                tabSoft.Controls.Add(CreateEmptyLabel("未获取到已安装软件信息"));
            }
            _tabControl.TabPages.Add(tabSoft);
        }

        private void AddSecuritySoftwareTab(ResponseGetHostInfo resp)
        {
            var tabSec = new TabPage("安全软件");
            if (resp.SecuritySoftware != null && resp.SecuritySoftware.Count > 0)
            {
                var lv = CreateDetailsListView();
                lv.Columns.Add("软件名称", 280);
                lv.Columns.Add("状态", 120);
                foreach (var sec in resp.SecuritySoftware)
                {
                    var item = new ListViewItem(sec.Name ?? "");
                    item.SubItems.Add(sec.IsEnabled ? "运行中" : "已关闭");
                    lv.Items.Add(item);
                }
                tabSec.Controls.Add(lv);
            }
            else
            {
                tabSec.Controls.Add(CreateEmptyLabel("未检测到安全软件"));
            }
            _tabControl.TabPages.Add(tabSec);
        }
    }
}
