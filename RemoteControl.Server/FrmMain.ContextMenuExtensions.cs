using System;
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
    }
}
