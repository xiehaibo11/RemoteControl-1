using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public class FrmOnlineLogViewer : FrmBase
    {
        private DateTimePicker _datePicker;
        private TextBox _filterBox;
        private DataGridView _gridView;
        private Label _statusLabel;
        private string _logDirectory;

        public FrmOnlineLogViewer(string logDirectory)
        {
            _logDirectory = logDirectory;
            this.Text = "上线日志查看器 — 魔法师";
            this.Size = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeControls();
            LoadLogs();
        }

        private void InitializeControls()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 40;
            topPanel.Padding = new Padding(8, 6, 8, 4);

            var dateLabel = new Label();
            dateLabel.Text = "日期:";
            dateLabel.AutoSize = true;
            dateLabel.Location = new Point(8, 10);

            _datePicker = new DateTimePicker();
            _datePicker.Location = new Point(50, 7);
            _datePicker.Size = new Size(140, 22);
            _datePicker.Format = DateTimePickerFormat.Short;
            _datePicker.ValueChanged += (s, e) => LoadLogs();

            var filterLabel = new Label();
            filterLabel.Text = "过滤:";
            filterLabel.AutoSize = true;
            filterLabel.Location = new Point(200, 10);

            _filterBox = new TextBox();
            _filterBox.Location = new Point(240, 7);
            _filterBox.Size = new Size(320, 22);
            SetPlaceholder(_filterBox, "外网IP / 主机名 / 分组 / 用户 ...");
            _filterBox.TextChanged += (s, e) => ApplyFilter();

            var refreshBtn = new Button();
            refreshBtn.Text = "刷新(R)";
            refreshBtn.Location = new Point(580, 5);
            refreshBtn.Size = new Size(75, 26);
            refreshBtn.Click += (s, e) => LoadLogs();

            var openDirBtn = new Button();
            openDirBtn.Text = "打开目录(O)";
            openDirBtn.Location = new Point(670, 5);
            openDirBtn.Size = new Size(90, 26);
            openDirBtn.Click += (s, e) =>
            {
                if (Directory.Exists(_logDirectory))
                    System.Diagnostics.Process.Start(_logDirectory);
            };

            topPanel.Controls.AddRange(new Control[] {
                dateLabel, _datePicker, filterLabel, _filterBox, refreshBtn, openDirBtn
            });

            _gridView = new DataGridView();
            _gridView.Dock = DockStyle.Fill;
            _gridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridView.AllowUserToAddRows = false;
            _gridView.AllowUserToDeleteRows = false;
            _gridView.ReadOnly = true;
            _gridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridView.Font = new Font("微软雅黑", 9F);
            _gridView.Columns.Add("Time", "时间");
            _gridView.Columns.Add("Event", "事件");
            _gridView.Columns.Add("ExternalIP", "外网IP");
            _gridView.Columns.Add("HostName", "主机名");
            _gridView.Columns.Add("Details", "详情");
            _gridView.Columns[0].FillWeight = 160;
            _gridView.Columns[1].FillWeight = 50;
            _gridView.Columns[2].FillWeight = 120;
            _gridView.Columns[3].FillWeight = 150;
            _gridView.Columns[4].FillWeight = 400;

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 24;
            _statusLabel.Font = new Font("微软雅黑", 9F);

            this.Controls.Add(_gridView);
            this.Controls.Add(_statusLabel);
            this.Controls.Add(topPanel);
        }

        private DataTable _dataTable = new DataTable();

        private void LoadLogs()
        {
            if (string.IsNullOrEmpty(_logDirectory) || !Directory.Exists(_logDirectory))
            {
                _statusLabel.Text = "日志目录不存在";
                return;
            }

            string dateStr = _datePicker.Value.ToString("yyyy-MM-dd");
            string logFile = Path.Combine(_logDirectory, "online_" + dateStr + ".log");
            if (!File.Exists(logFile))
            {
                _gridView.Rows.Clear();
                _statusLabel.Text = logFile + " — 0 条记录";
                return;
            }

            _dataTable.Clear();
            _dataTable.Columns.Clear();
            _dataTable.Columns.Add("Time", typeof(string));
            _dataTable.Columns.Add("Event", typeof(string));
            _dataTable.Columns.Add("ExternalIP", typeof(string));
            _dataTable.Columns.Add("HostName", typeof(string));
            _dataTable.Columns.Add("Details", typeof(string));

            try
            {
                var lines = File.ReadAllLines(logFile);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;
                    string time = parts[0] + " " + (parts.Length > 1 ? parts[1] : "");
                    string evt = parts.Length > 2 ? parts[2] : "";
                    string detail = parts.Length > 3 ? parts[3] : "";
                    string ip = "";
                    string host = "";
                    // 尝试从detail中提取ip和host
                    if (detail.Contains("ip="))
                    {
                        int idx = detail.IndexOf("ip=");
                        int end = detail.IndexOf(' ', idx);
                        ip = end > 0 ? detail.Substring(idx, end - idx) : detail.Substring(idx);
                    }
                    if (detail.Contains("host="))
                    {
                        int idx = detail.IndexOf("host=");
                        int end = detail.IndexOf(' ', idx);
                        host = end > 0 ? detail.Substring(idx, end - idx) : detail.Substring(idx);
                    }
                    _dataTable.Rows.Add(time, evt, ip, host, detail);
                }
            }
            catch { }

            _gridView.DataSource = _dataTable;
            _statusLabel.Text = logFile + " — " + _dataTable.Rows.Count + " 条记录 — " +
                (File.Exists(logFile) ? new FileInfo(logFile).Length : 0) + " 字节";
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
            string placeholder = "外网IP / 主机名 / 分组 / 用户 ...";
            return (txt == placeholder) ? "" : txt;
        }

        private void ApplyFilter()
        {
            string filter = GetFilterText();
            if (string.IsNullOrEmpty(filter))
            {
                _gridView.DataSource = _dataTable;
            }
            else
            {
                var filtered = new DataView(_dataTable);
                filtered.RowFilter = string.Format(
                    "ExternalIP LIKE '%{0}%' OR HostName LIKE '%{0}%' OR Details LIKE '%{0}%'",
                    filter.Replace("'", "''"));
                _gridView.DataSource = filtered;
            }
        }
    }
}
