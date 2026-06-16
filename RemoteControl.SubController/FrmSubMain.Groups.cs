using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.SubController
{
    partial class FrmSubMain
    {
        private FlowLayoutPanel groupTabsPanel;
        private Button groupDefaultTab;
        private readonly Dictionary<string, Button> groupTabs = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> sessionGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string activeGroup = DefaultGroupName;
        private const string DefaultGroupName = "默认";

        private void InitGroups()
        {
            BuildGroupTabsPanel();
        }

        private void BuildGroupTabsPanel()
        {
            groupTabsPanel = new FlowLayoutPanel();
            groupTabsPanel.Dock = DockStyle.Bottom;
            groupTabsPanel.Height = 27;
            groupTabsPanel.Padding = new Padding(4, 2, 4, 2);
            groupTabsPanel.BackColor = Color.FromArgb(245, 245, 245);
            groupTabsPanel.WrapContents = false;

            groupDefaultTab = CreateGroupTab("默认(0)", delegate { SelectGroup(DefaultGroupName); });
            groupDefaultTab.Tag = DefaultGroupName;
            groupTabsPanel.Controls.Add(groupDefaultTab);

            this.Controls.Add(groupTabsPanel);
            groupTabsPanel.BringToFront();
        }

        private Button CreateGroupTab(string text, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.Height = 23;
            button.Margin = new Padding(0, 0, 6, 0);
            button.Padding = new Padding(6, 0, 6, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210);
            button.BackColor = Color.White;
            button.Font = new Font("Microsoft YaHei", 9F);
            button.Click += click;
            return button;
        }

        private void SelectGroup(string groupName)
        {
            activeGroup = NormalizeGroupName(groupName);
            RefreshDashboard();
        }

        private static string NormalizeGroupName(string groupName)
        {
            return string.IsNullOrWhiteSpace(groupName) ? DefaultGroupName : groupName.Trim();
        }

        private string GetSessionGroupName(SocketSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return DefaultGroupName;

            string groupName;
            if (sessionGroups.TryGetValue(session.SocketId, out groupName))
                return NormalizeGroupName(groupName);

            string remark = session.Remark;
            if (!string.IsNullOrEmpty(remark) && remark.Contains("/"))
            {
                string parsedGroup = remark.Split('/')[0];
                if (!string.IsNullOrWhiteSpace(parsedGroup))
                    return NormalizeGroupName(parsedGroup);
            }

            return DefaultGroupName;
        }

        private void SetSessionGroup(SocketSession session, string groupName)
        {
            if (session == null || string.IsNullOrEmpty(session.SocketId))
                return;

            string normalized = NormalizeGroupName(groupName);
            if (string.Equals(normalized, DefaultGroupName, StringComparison.OrdinalIgnoreCase))
                sessionGroups.Remove(session.SocketId);
            else
                sessionGroups[session.SocketId] = normalized;

            RefreshDashboard();
        }

        private void RemoveSessionGroup(string socketId)
        {
            if (!string.IsNullOrEmpty(socketId))
                sessionGroups.Remove(socketId);
        }

        private bool SessionMatchesGroup(SocketSession session)
        {
            return string.Equals(
                GetSessionGroupName(session),
                activeGroup,
                StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateGroupCounters()
        {
            if (groupTabsPanel == null)
                return;

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var clients = Relay.GetClientSnapshot();
            foreach (var session in clients)
            {
                string gn = GetSessionGroupName(session);
                if (!counts.ContainsKey(gn))
                    counts[gn] = 0;
                counts[gn]++;
            }

            int defaultCount = counts.ContainsKey(DefaultGroupName) ? counts[DefaultGroupName] : 0;
            if (groupDefaultTab != null)
            {
                groupDefaultTab.Text = DefaultGroupName + "(" + defaultCount + ")";
                ApplyGroupTabState(groupDefaultTab,
                    string.Equals(activeGroup, DefaultGroupName, StringComparison.OrdinalIgnoreCase));
            }

            var groups = new List<string>(counts.Keys);
            groups.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string gn in groups)
            {
                if (string.Equals(gn, DefaultGroupName, StringComparison.OrdinalIgnoreCase))
                    continue;
                EnsureGroupTab(gn, counts[gn]);
            }

            var staleGroups = new List<string>();
            foreach (string gn in groupTabs.Keys)
            {
                if (!counts.ContainsKey(gn))
                    staleGroups.Add(gn);
            }
            foreach (string gn in staleGroups)
            {
                Button button = groupTabs[gn];
                groupTabs.Remove(gn);
                if (button != null)
                {
                    groupTabsPanel.Controls.Remove(button);
                    button.Dispose();
                }
            }
        }

        private void EnsureGroupTab(string groupName, int count)
        {
            Button button;
            if (!groupTabs.TryGetValue(groupName, out button))
            {
                string captured = groupName;
                button = CreateGroupTab("", delegate { SelectGroup(captured); });
                button.Tag = groupName;
                groupTabs[groupName] = button;
                groupTabsPanel.Controls.Add(button);
            }
            button.Text = groupName + "(" + count + ")";
            ApplyGroupTabState(button, string.Equals(groupName, activeGroup, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyGroupTabState(Button button, bool active)
        {
            if (button == null) return;
            button.BackColor = active ? Color.FromArgb(226, 242, 255) : Color.White;
            button.FlatAppearance.BorderColor = active ? Color.FromArgb(88, 160, 220) : Color.FromArgb(210, 210, 210);
        }
    }
}
