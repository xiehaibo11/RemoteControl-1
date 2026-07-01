using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
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

            foreach (string groupName in dashboardKnownGroups)
            {
                if (!counts.ContainsKey(groupName))
                    counts[groupName] = 0;
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
                if (!counts.ContainsKey(groupName) && !dashboardKnownGroups.Contains(groupName))
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
    }
}
