using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void SelectDashboardGroup(string groupName)
        {
            activeDashboardGroup = NormalizeDashboardGroupName(groupName);
            ShowDashboardHome();
        }

        private void ShowPluginDashboardNotice()
        {
            MessageBox.Show("插件入口已保留，但当前没有独立插件页。可执行工具请从右键菜单或工具目录进入。",
                APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string NormalizeDashboardGroupName(string groupName)
        {
            return string.IsNullOrWhiteSpace(groupName) ? DefaultDashboardGroupName : groupName.Trim();
        }

        private string GetDashboardGroupName(SocketSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return DefaultDashboardGroupName;

            string groupName;
            if (dashboardSessionGroups.TryGetValue(session.SocketId, out groupName))
                return NormalizeDashboardGroupName(groupName);

            string remark = session.Remark;
            if (!string.IsNullOrEmpty(remark) && remark.Contains("/"))
            {
                string parsedGroup = remark.Split('/')[0];
                if (!string.IsNullOrWhiteSpace(parsedGroup))
                    return NormalizeDashboardGroupName(parsedGroup);
            }

            return DefaultDashboardGroupName;
        }

        private void SetDashboardGroup(SocketSession session, string groupName)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return;

            string normalized = NormalizeDashboardGroupName(groupName);
            if (string.Equals(normalized, DefaultDashboardGroupName, StringComparison.OrdinalIgnoreCase))
                dashboardSessionGroups.Remove(session.SocketId);
            else
                dashboardSessionGroups[session.SocketId] = normalized;

            RefreshHostDashboard();
        }

        private void RemoveDashboardGroup(SocketSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return;

            dashboardSessionGroups.Remove(session.SocketId);
        }

        private bool SessionMatchesDashboardGroup(SocketSession session)
        {
            return string.Equals(
                GetDashboardGroupName(session),
                activeDashboardGroup,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool SessionMatchesDashboardView(SocketSession session)
        {
            return SessionMatchesFilter(session) && SessionMatchesDashboardGroup(session);
        }

        private void EnsureDashboardGroupTab(string groupName, int count)
        {
            Button button;
            if (!dashboardGroupTabs.TryGetValue(groupName, out button))
            {
                string capturedGroup = groupName;
                button = CreateDashboardTab("", delegate { SelectDashboardGroup(capturedGroup); });
                button.Tag = groupName;
                dashboardGroupTabs[groupName] = button;
                dashboardTabsPanel.Controls.Add(button);
            }

            button.Text = groupName + "(" + count + ")";
            ApplyDashboardTabState(button, string.Equals(groupName, activeDashboardGroup, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyDashboardTabState(Button button, bool active)
        {
            if (button == null)
                return;

            button.BackColor = active ? Color.FromArgb(226, 242, 255) : Color.White;
            button.FlatAppearance.BorderColor = active ? Color.FromArgb(88, 160, 220) : Color.FromArgb(210, 210, 210);
        }
    }
}
