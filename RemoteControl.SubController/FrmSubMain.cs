using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.SubController
{
    public partial class FrmSubMain : Form
    {
        internal SubControllerRelay Relay { get; private set; }
        private SocketSession _currentSession;
        private NotifyIcon onlineNotifyIcon;

        public FrmSubMain()
        {
            InitializeComponent();
            Relay = new SubControllerRelay();
        }

        private void FrmSubMain_Load(object sender, EventArgs e)
        {
            this.Text = "副控管理端";
            InitDashboard();
            InitContextMenu();
            InitGroups();
            InitializeOnlineNotifyIcon();
            SubscribeRelayEvents();
            AutoConnect();
        }

        private void FrmSubMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("确定退出副控管理端?", "退出",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (onlineNotifyIcon != null)
            {
                onlineNotifyIcon.Visible = false;
                onlineNotifyIcon.Dispose();
                onlineNotifyIcon = null;
            }
            Relay.Stop();
        }

        private void SubscribeRelayEvents()
        {
            Relay.ClientConnected += OnClientConnected;
            Relay.ClientDisconnected += OnClientDisconnected;
            Relay.ClientListChanged += OnClientListChanged;
            Relay.PacketReceived += OnPacketReceived;
            Relay.RelayDisconnected += OnRelayDisconnected;
        }

        private void AutoConnect()
        {
            var cfg = SubControllerConfig.Current;
            if (!string.IsNullOrEmpty(cfg.RelayServerIP))
            {
                try
                {
                    Relay.Start(cfg.RelayServerIP, cfg.RelayServerPort);
                    UpdateConnectionStatus(true);
                }
                catch (Exception ex)
                {
                    UpdateConnectionStatus(false);
                    labelStatus.Text = "连接失败: " + ex.Message;
                }
            }
            else
            {
                UpdateConnectionStatus(false);
                labelStatus.Text = "请先配置Relay地址（右键 → 设置）";
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (connected)
            {
                labelStatus.Text = "已连接 | " + SubControllerConfig.Current.RelayServerIP
                    + ":" + SubControllerConfig.Current.RelayServerPort;
                labelStatus.ForeColor = Color.LimeGreen;
                btnConnect.Text = "断开";
            }
            else
            {
                if (labelStatus.ForeColor != Color.OrangeRed)
                    labelStatus.Text = "未连接";
                labelStatus.ForeColor = Color.OrangeRed;
                btnConnect.Text = "连接";
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (Relay.IsConnected)
            {
                Relay.Stop();
                UpdateConnectionStatus(false);
                RefreshDashboard();
            }
            else
            {
                var cfg = SubControllerConfig.Current;
                if (string.IsNullOrEmpty(cfg.RelayServerIP))
                {
                    ShowSettings();
                    return;
                }
                try
                {
                    Relay.Start(cfg.RelayServerIP, cfg.RelayServerPort);
                    UpdateConnectionStatus(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("连接Relay失败: " + ex.Message, "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateConnectionStatus(false);
                }
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowSettings();
        }

        private void ShowSettings()
        {
            using (var frm = new FrmSubSettings())
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    // 重连
                    if (Relay.IsConnected)
                    {
                        Relay.Stop();
                        UpdateConnectionStatus(false);
                    }
                    AutoConnect();
                }
            }
        }

        private void OnClientConnected(object sender, ClientEventArgs e)
        {
            SafeInvoke(() =>
            {
                UpsertDashboardClient(e.Client);
                UpdateClientCount();
                ShowClientOnlineNotification(e.Client);
            });
        }

        private void OnClientDisconnected(object sender, ClientEventArgs e)
        {
            SafeInvoke(() =>
            {
                RemoveDashboardClient(e.Client.SocketId);
                UpdateClientCount();
            });
        }

        private void OnClientListChanged(object sender, ClientListEventArgs e)
        {
            SafeInvoke(() =>
            {
                foreach (var c in e.Removed) RemoveDashboardClient(c.SocketId);
                foreach (var c in e.Added)
                {
                    UpsertDashboardClient(c);
                    ShowClientOnlineNotification(c);
                }
                RefreshDashboard();
                UpdateClientCount();
            });
        }

        private void OnRelayDisconnected(object sender, EventArgs e)
        {
            SafeInvoke(() =>
            {
                UpdateConnectionStatus(false);
                labelStatus.Text = "Relay已断开，尝试重连...";
                Relay.TryAutoReconnect();
            });
        }

        private void UpdateClientCount()
        {
            int count = Relay.GetClientSnapshot().Count;
            labelClientCount.Text = "在线: " + count;
        }

        private void SafeInvoke(Action action)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(action);
            else
                action();
        }

        private void InitializeOnlineNotifyIcon()
        {
            if (onlineNotifyIcon != null)
                return;

            onlineNotifyIcon = new NotifyIcon();
            onlineNotifyIcon.Icon = this.Icon == null ? SystemIcons.Information : this.Icon;
            onlineNotifyIcon.Text = "副控管理端";
            onlineNotifyIcon.Visible = true;
        }

        private void ShowClientOnlineNotification(SocketSession session)
        {
            if (session == null)
                return;

            try
            {
                InitializeOnlineNotifyIcon();
                onlineNotifyIcon.ShowBalloonTip(
                    3000,
                    "主机上线",
                    GetSessionLabel(session) + " 已上线",
                    ToolTipIcon.Info);
            }
            catch
            {
            }
        }

        private string GetSessionLabel(SocketSession session)
        {
            if (session == null)
                return "未知主机";
            if (!string.IsNullOrEmpty(session.HostName))
                return session.HostName;
            if (!string.IsNullOrEmpty(session.GetExternalIP()))
                return session.GetExternalIP();
            return session.SocketId ?? "未知主机";
        }
    }
}
