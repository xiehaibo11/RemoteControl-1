using System;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            ToolStripButton tsButton = sender as ToolStripButton;
            tsButton.Checked = !tsButton.Checked;
            if (tsButton.Checked)
            {
                string relayIP = Settings.CurrentSettings.RelayServerIP;
                int relayPort = Settings.CurrentSettings.RelayServerPort;
                if (string.IsNullOrEmpty(relayIP))
                {
                    doOutput("请先在设置中配置Relay服务器地址!");
                    tsButton.Checked = false;
                    return;
                }
                try
                {
                    RSCApplication.oRemoteControlServer.Start(relayIP, relayPort);
                    this.Text = APP_TITLE;
                    SetRelayStatus(true);
                    ScheduleClientListRefresh();
                    doOutput("已连接中转服务器");
                }
                catch (Exception ex)
                {
                    Logger.Debug("连接Relay服务器失败: " + ex);
                    SetRelayStatus(false);
                    doOutput("连接Relay服务器失败，请检查配置");
                    tsButton.Checked = false;
                }
            }
            else
            {
                RSCApplication.oRemoteControlServer.Stop();
                this.Text = APP_TITLE;
                SetRelayStatus(false);
                doOutput("已断开中转服务器连接！");
                this.clientCount = 0;
                refreshClientCountShow();
            }
        }
    }
}
