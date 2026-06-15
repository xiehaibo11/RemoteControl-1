using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void RefreshHostDashboard()
        {
            if (hostListView == null)
                return;

            hostListView.BeginUpdate();
            try
            {
                hostListView.Items.Clear();
                foreach (SocketSession session in onlineClientSessions)
                {
                    if (!SessionMatchesDashboardView(session))
                        continue;
                    hostListView.Items.Add(CreateHostListItem(session));
                }
            }
            finally
            {
                hostListView.EndUpdate();
            }
            UpdateDashboardCounters();
        }

        private void UpsertHostDashboardClient(SocketSession session)
        {
            if (hostListView == null || session == null || string.IsNullOrEmpty(session.SocketId))
                return;

            ListViewItem item = FindHostListItem(session.SocketId);
            if (!SessionMatchesDashboardView(session))
            {
                if (item != null)
                    hostListView.Items.Remove(item);
                UpdateDashboardCounters();
                return;
            }

            if (item == null)
            {
                hostListView.Items.Add(CreateHostListItem(session));
            }
            else
            {
                ApplyHostListItem(item, session);
            }
            UpdateDashboardCounters();
        }

        private void RemoveHostDashboardClient(SocketSession session)
        {
            if (hostListView == null || session == null || string.IsNullOrEmpty(session.SocketId))
                return;

            ListViewItem item = FindHostListItem(session.SocketId);
            if (item != null)
                hostListView.Items.Remove(item);
            UpdateDashboardCounters();
        }

        private ListViewItem FindHostListItem(string socketId)
        {
            foreach (ListViewItem item in hostListView.Items)
            {
                SocketSession session = item.Tag as SocketSession;
                if (session != null && session.SocketId == socketId)
                    return item;
            }
            return null;
        }

        private ListViewItem CreateHostListItem(SocketSession session)
        {
            ListViewItem item = new ListViewItem();
            ApplyHostListItem(item, session);
            return item;
        }

        private void ApplyHostListItem(ListViewItem item, SocketSession session)
        {
            string[] values = BuildHostRowValues(session);
            if (item.Text != values[0])
                item.Text = values[0];
            item.ImageKey = "computer";
            while (item.SubItems.Count < values.Length)
                item.SubItems.Add("");
            for (int i = 1; i < values.Length; i++)
            {
                if (item.SubItems[i].Text != values[i])
                    item.SubItems[i].Text = values[i];
            }
            item.Tag = session;
        }

        private string[] BuildHostRowValues(SocketSession session)
        {
            if (session == null)
                return new string[]
                {
                    "未知主机", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-"
                };

            return new string[]
            {
                GetClientDisplayText(session),
                SafeText(session.UserName),
                SafeText(session.GetExternalIP()),
                SafeText(session.LocalIP),
                SafeText(session.Location),
                SafeText(session.OSVersion),
                SafeText(session.Privilege),
                SafeText(session.CameraStatus),
                SafeText(session.Antivirus),
                SafeText(session.OnlineQQ),
                SafeText(session.TG),
                SafeText(session.WX),
                SafeText(session.UserStatus),
                SafeText(session.Region),
                SafeText(session.ISP),
                SafeText(session.Remark),
                FormatLastActive(session.LastActiveTime)
            };
        }

        private static string SafeText(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static string FormatLastActive(DateTime value)
        {
            if (value == DateTime.MinValue)
                return "-";
            TimeSpan span = DateTime.Now - value;
            if (span.TotalMinutes < 1)
                return "刚刚";
            if (span.TotalHours < 1)
                return ((int)span.TotalMinutes) + " 分钟前";
            return value.ToString("HH:mm:ss");
        }

        private void UpdateDashboardCounters()
        {
            if (dashboardTabsPanel == null)
                return;

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (SocketSession session in onlineClientSessions)
            {
                string groupName = GetDashboardGroupName(session);
                if (!counts.ContainsKey(groupName))
                    counts[groupName] = 0;
                counts[groupName]++;
            }

            int defaultCount = counts.ContainsKey(DefaultDashboardGroupName) ? counts[DefaultDashboardGroupName] : 0;
            if (dashboardDefaultTab != null)
            {
                dashboardDefaultTab.Text = DefaultDashboardGroupName + "(" + defaultCount + ")";
                ApplyDashboardTabState(dashboardDefaultTab,
                    string.Equals(activeDashboardGroup, DefaultDashboardGroupName, StringComparison.OrdinalIgnoreCase));
            }

            List<string> groups = new List<string>(counts.Keys);
            groups.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string groupName in groups)
            {
                if (string.Equals(groupName, DefaultDashboardGroupName, StringComparison.OrdinalIgnoreCase))
                    continue;
                EnsureDashboardGroupTab(groupName, counts[groupName]);
            }

            List<string> staleGroups = new List<string>();
            foreach (string groupName in dashboardGroupTabs.Keys)
            {
                if (!counts.ContainsKey(groupName))
                    staleGroups.Add(groupName);
            }
            foreach (string groupName in staleGroups)
            {
                Button button = dashboardGroupTabs[groupName];
                dashboardGroupTabs.Remove(groupName);
                if (button != null)
                {
                    dashboardTabsPanel.Controls.Remove(button);
                    button.Dispose();
                }
            }
        }

        private void hostListView_MouseUp(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = hostListView.HitTest(e.Location);
            if (hit.Item != null)
            {
                hit.Item.Selected = true;
                SelectDashboardSession(hit.Item.Tag as SocketSession);
            }
            if (e.Button == MouseButtons.Right && contextMenuStripClient != null)
                contextMenuStripClient.Show(hostListView, e.Location);
        }

        private void hostListView_DoubleClick(object sender, EventArgs e)
        {
            if (hostListView.SelectedItems.Count < 1)
                return;
            SelectDashboardSession(hostListView.SelectedItems[0].Tag as SocketSession);
            onMenuFileManager(sender, e);
        }

        private void SelectDashboardSession(SocketSession session)
        {
            if (session == null)
                return;

            this.currentSession = session;
            session.Touch();
            UpdateSelectedClientInfo(session);
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(session.SocketId);
            UpsertHostDashboardClient(session);
        }
    }
}
