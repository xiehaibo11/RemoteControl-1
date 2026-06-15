using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private Panel dashboardPanel;
        private ListView hostListView;
        private ImageList hostListImages;
        private FlowLayoutPanel dashboardTabsPanel;
        private Button dashboardDefaultTab;
        private Button dashboardPluginTab;
        private readonly Dictionary<string, Button> dashboardGroupTabs = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> dashboardSessionGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string activeDashboardGroup = DefaultDashboardGroupName;
        private const string DefaultDashboardGroupName = "默认";

        private void InitializeHostDashboardLayout()
        {
            this.menuStrip1.Visible = false;
            this.toolStrip1.Visible = false;
            this.toolStrip2.Visible = false;
            if (topNavigationPanel != null)
            {
                topNavigationPanel.Visible = false;
            }

            this.Text = APP_TITLE;
            this.splitContainer2.Panel2Collapsed = true;
            this.tabControl1.Visible = false;

            BuildHostDashboardPanel();
            BuildDashboardTabsPanel();
            RefreshHostDashboard();
        }

        private void BuildHostDashboardPanel()
        {
            if (dashboardPanel != null)
                return;

            dashboardPanel = new Panel();
            dashboardPanel.Dock = DockStyle.Fill;
            dashboardPanel.BackColor = Color.White;

            hostListImages = new ImageList();
            hostListImages.ColorDepth = ColorDepth.Depth32Bit;
            hostListImages.ImageSize = new Size(16, 16);
            if (this.treeView1.ImageList != null && this.treeView1.ImageList.Images.ContainsKey("computer"))
            {
                hostListImages.Images.Add("computer", this.treeView1.ImageList.Images["computer"]);
            }

            hostListView = new ListView();
            hostListView.Dock = DockStyle.Fill;
            hostListView.BorderStyle = BorderStyle.FixedSingle;
            hostListView.View = View.Details;
            hostListView.FullRowSelect = true;
            hostListView.GridLines = true;
            hostListView.HideSelection = false;
            hostListView.MultiSelect = true;
            hostListView.SmallImageList = hostListImages;
            hostListView.ContextMenuStrip = contextMenuStripClient;
            hostListView.BackColor = Color.White;
            hostListView.Font = new Font("微软雅黑", 9F);
            EnableListViewDoubleBuffer(hostListView);
            hostListView.Columns.Add("主机名", 110);
            hostListView.Columns.Add("用户", 150);
            hostListView.Columns.Add("外网IP", 120);
            hostListView.Columns.Add("内网IP", 130);
            hostListView.Columns.Add("位置", 170);
            hostListView.Columns.Add("系统", 210);
            hostListView.Columns.Add("权限", 90);
            hostListView.Columns.Add("摄像头", 80);
            hostListView.Columns.Add("杀软", 130);
            hostListView.Columns.Add("QQ", 70);
            hostListView.Columns.Add("TG", 70);
            hostListView.Columns.Add("WX", 70);
            hostListView.Columns.Add("状态", 70);
            hostListView.Columns.Add("地区", 90);
            hostListView.Columns.Add("ISP", 120);
            hostListView.Columns.Add("备注", 110);
            hostListView.Columns.Add("最后活动", 110);
            hostListView.MouseUp += hostListView_MouseUp;
            hostListView.DoubleClick += hostListView_DoubleClick;

            dashboardPanel.Controls.Add(hostListView);
            this.splitContainer2.Panel1.Controls.Add(dashboardPanel);
            dashboardPanel.BringToFront();
        }

        private static void EnableListViewDoubleBuffer(ListView listView)
        {
            if (listView == null)
                return;

            typeof(ListView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, listView, new object[] { true });
        }

        private void BuildDashboardTabsPanel()
        {
            if (dashboardTabsPanel != null)
                return;

            dashboardTabsPanel = new FlowLayoutPanel();
            dashboardTabsPanel.Dock = DockStyle.Bottom;
            dashboardTabsPanel.Height = 27;
            dashboardTabsPanel.Padding = new Padding(4, 2, 4, 2);
            dashboardTabsPanel.BackColor = Color.FromArgb(245, 245, 245);
            dashboardTabsPanel.WrapContents = false;

            dashboardDefaultTab = CreateDashboardTab("默认(0)", delegate { SelectDashboardGroup(DefaultDashboardGroupName); });
            dashboardDefaultTab.Tag = DefaultDashboardGroupName;
            dashboardTabsPanel.Controls.Add(dashboardDefaultTab);
            dashboardTabsPanel.Controls.Add(CreateDashboardTab("设置", delegate { toolStripButtonSettings_Click(this, EventArgs.Empty); }));
            dashboardPluginTab = CreateDashboardTab("插件", delegate { ShowPluginDashboardNotice(); });
            dashboardTabsPanel.Controls.Add(dashboardPluginTab);

            this.Controls.Add(dashboardTabsPanel);
            this.Controls.SetChildIndex(dashboardTabsPanel, this.Controls.GetChildIndex(this.statusStrip1));
        }

        private Button CreateDashboardTab(string text, EventHandler click)
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
            button.Font = new Font("微软雅黑", 9F);
            button.Click += click;
            return button;
        }

        private void ShowDashboardHome()
        {
            if (dashboardPanel == null)
                return;

            this.tabControl1.Visible = false;
            dashboardPanel.Visible = true;
            dashboardPanel.BringToFront();
            RefreshHostDashboard();
        }

        private void ShowWorkspacePage(int pageIndex)
        {
            ShowWorkspacePopup(pageIndex);
        }
    }
}
