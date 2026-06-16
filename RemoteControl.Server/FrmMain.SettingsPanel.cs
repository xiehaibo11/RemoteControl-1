using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private Panel settingsPanel;

        // 控件引用（用于读写值）
        private TextBox txtListenAddress;
        private NumericUpDown nudServerPort;
        private NumericUpDown nudWorkerLoops;
        private NumericUpDown nudHeavyWorkers;
        private ComboBox cboCompression;
        private NumericUpDown nudScreenFps;
        private ComboBox cboFont;
        private CheckBox chkFileAutoSave;
        private TextBox txtDownloadDir;
        private CheckBox chkSkipLocked;
        private CheckBox chkAutoDecompress;
        private NumericUpDown nudLargeFileThreshold;
        private NumericUpDown nudRetentionDays;
        private Button btnViewLogs;
        private CheckBox chkShowProtocolVersion;
        private CheckBox chkShowClientVersion;

        private void BuildSettingsPanel()
        {
            if (settingsPanel != null)
                return;

            settingsPanel = new Panel();
            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.AutoScroll = true;
            settingsPanel.BackColor = Color.FromArgb(30, 35, 45);
            settingsPanel.Visible = false;
            settingsPanel.Padding = new Padding(20, 10, 20, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.Dock = DockStyle.Top;
            layout.Padding = new Padding(0);

            // 左列
            layout.Controls.Add(BuildServerConfigGroup(), 0, 0);
            layout.Controls.Add(BuildPerformanceGroup(), 0, 1);
            layout.Controls.Add(BuildScreenTransferGroup(), 0, 2);
            layout.Controls.Add(BuildTerminalFontGroup(), 0, 3);

            // 右列
            layout.Controls.Add(BuildFileTransferGroup(), 1, 0);
            layout.Controls.Add(BuildAuditRetentionGroup(), 1, 1);
            layout.Controls.Add(BuildLogViewGroup(), 1, 2);
            layout.Controls.Add(BuildDisplayOptionsGroup(), 1, 3);

            settingsPanel.Controls.Add(layout);
            this.Controls.Add(settingsPanel);
            this.Controls.SetChildIndex(settingsPanel, 0);
        }

        private void ShowSettingsPanel()
        {
            BuildSettingsPanel();
            LoadSettingsToPanel();
            dashboardPanel.Visible = false;
            settingsPanel.Visible = true;
            settingsPanel.BringToFront();
        }

        private void HideSettingsPanel()
        {
            if (settingsPanel != null)
                settingsPanel.Visible = false;
            if (dashboardPanel != null)
                dashboardPanel.Visible = true;
        }

        private void LoadSettingsToPanel()
        {
            Settings s = Settings.CurrentSettings;
            if (s == null) return;

            txtListenAddress.Text = s.ListenAddress ?? "0.0.0.0";
            nudServerPort.Value = Math.Max(nudServerPort.Minimum, Math.Min(nudServerPort.Maximum, s.ServerPort > 0 ? s.ServerPort : 10010));
            nudWorkerLoops.Value = Math.Max(nudWorkerLoops.Minimum, Math.Min(nudWorkerLoops.Maximum, s.WorkerLoops));
            nudHeavyWorkers.Value = Math.Max(nudHeavyWorkers.Minimum, Math.Min(nudHeavyWorkers.Maximum, s.HeavyWorkers));
            cboCompression.SelectedIndex = Math.Max(0, Math.Min(cboCompression.Items.Count - 1, s.ScreenCompressionMode));
            nudScreenFps.Value = Math.Max(nudScreenFps.Minimum, Math.Min(nudScreenFps.Maximum, s.ScreenFps));

            SelectFontItem(s.PreferredFont);

            chkFileAutoSave.Checked = s.FileAutoSave;
            txtDownloadDir.Text = s.FileDownloadDir ?? "";
            chkSkipLocked.Checked = s.FileSkipLocked;
            chkAutoDecompress.Checked = s.FileAutoDecompress;
            nudLargeFileThreshold.Value = Math.Max(nudLargeFileThreshold.Minimum, Math.Min(nudLargeFileThreshold.Maximum, s.LargeFileThresholdMB));
            nudRetentionDays.Value = Math.Max(nudRetentionDays.Minimum, Math.Min(nudRetentionDays.Maximum, s.AuditRetentionDays));

            chkShowProtocolVersion.Checked = s.ShowProtocolVersion;
            chkShowClientVersion.Checked = s.ShowClientVersion;

            UpdateLogButtonText();
        }

        private void SelectFontItem(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                cboFont.SelectedIndex = 0;
                return;
            }
            for (int i = 0; i < cboFont.Items.Count; i++)
            {
                if (cboFont.Items[i].ToString().IndexOf(fontName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cboFont.SelectedIndex = i;
                    return;
                }
            }
            cboFont.SelectedIndex = 0;
        }

        // ==================== 区块构建方法 ====================

        private GroupBox BuildServerConfigGroup()
        {
            GroupBox grp = CreateSettingsGroup("服务器配置");

            Label lblAddr = CreateSettingsLabel("监听地址:");
            lblAddr.Location = new Point(14, 28);
            txtListenAddress = new TextBox();
            txtListenAddress.Location = new Point(100, 25);
            txtListenAddress.Size = new Size(200, 23);
            txtListenAddress.Font = SettingsFont();

            Label lblPort = CreateSettingsLabel("端口号:");
            lblPort.Location = new Point(14, 60);
            nudServerPort = new NumericUpDown();
            nudServerPort.Location = new Point(100, 57);
            nudServerPort.Size = new Size(200, 23);
            nudServerPort.Minimum = 1; nudServerPort.Maximum = 65535;
            nudServerPort.Font = SettingsFont();

            Button btnSave = CreateSaveButton();
            btnSave.Location = new Point(220, 92);
            btnSave.Click += delegate { SaveServerConfig(); };

            grp.Controls.AddRange(new Control[] { lblAddr, txtListenAddress, lblPort, nudServerPort, btnSave });
            grp.Height = 125;
            return grp;
        }

        private GroupBox BuildPerformanceGroup()
        {
            GroupBox grp = CreateSettingsGroup("性能配置");

            Label lblWL = CreateSettingsLabel("Worker Loops:");
            lblWL.Location = new Point(14, 28);
            nudWorkerLoops = new NumericUpDown();
            nudWorkerLoops.Location = new Point(130, 25);
            nudWorkerLoops.Size = new Size(170, 23);
            nudWorkerLoops.Minimum = 0; nudWorkerLoops.Maximum = 128;
            nudWorkerLoops.Font = SettingsFont();

            Label lblHW = CreateSettingsLabel("Heavy Workers:");
            lblHW.Location = new Point(14, 60);
            nudHeavyWorkers = new NumericUpDown();
            nudHeavyWorkers.Location = new Point(130, 57);
            nudHeavyWorkers.Size = new Size(170, 23);
            nudHeavyWorkers.Minimum = 1; nudHeavyWorkers.Maximum = 64;
            nudHeavyWorkers.Font = SettingsFont();

            Button btnSave = CreateSaveButton();
            btnSave.Location = new Point(220, 92);
            btnSave.Click += delegate { SavePerformanceConfig(); };

            grp.Controls.AddRange(new Control[] { lblWL, nudWorkerLoops, lblHW, nudHeavyWorkers, btnSave });
            grp.Height = 125;
            return grp;
        }

        private GroupBox BuildScreenTransferGroup()
        {
            GroupBox grp = CreateSettingsGroup("屏幕传输");

            Label lblAlg = CreateSettingsLabel("压缩算法:");
            lblAlg.Location = new Point(14, 28);
            cboCompression = new ComboBox();
            cboCompression.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCompression.Location = new Point(100, 25);
            cboCompression.Size = new Size(200, 23);
            cboCompression.Font = SettingsFont();
            cboCompression.Items.AddRange(new object[] {
                "JPEG + ZSTD (有损, 省带宽)",
                "PNG (无损)",
                "RAW (无压缩)"
            });

            Label lblFps = CreateSettingsLabel("帧率:");
            lblFps.Location = new Point(14, 60);
            nudScreenFps = new NumericUpDown();
            nudScreenFps.Location = new Point(100, 57);
            nudScreenFps.Size = new Size(170, 23);
            nudScreenFps.Minimum = 1; nudScreenFps.Maximum = 60;
            nudScreenFps.Font = SettingsFont();

            Label lblFpsUnit = CreateSettingsLabel("fps");
            lblFpsUnit.Location = new Point(275, 60);

            Button btnSave = CreateSaveButton();
            btnSave.Location = new Point(220, 92);
            btnSave.Click += delegate { SaveScreenTransferConfig(); };

            grp.Controls.AddRange(new Control[] { lblAlg, cboCompression, lblFps, nudScreenFps, lblFpsUnit, btnSave });
            grp.Height = 125;
            return grp;
        }

        private GroupBox BuildTerminalFontGroup()
        {
            GroupBox grp = CreateSettingsGroup("终端字体");

            Label lblFont = CreateSettingsLabel("首选字体:");
            lblFont.Location = new Point(14, 28);
            cboFont = new ComboBox();
            cboFont.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFont.Location = new Point(100, 25);
            cboFont.Size = new Size(200, 23);
            cboFont.Font = SettingsFont();
            cboFont.Items.AddRange(new object[] {
                "Sarasa Mono SC  (内置 · 中文推荐)",
                "Consolas",
                "Courier New",
                "微软雅黑"
            });

            grp.Controls.AddRange(new Control[] { lblFont, cboFont });
            grp.Height = 65;
            return grp;
        }

        private GroupBox BuildFileTransferGroup()
        {
            GroupBox grp = CreateSettingsGroup("文件传输");

            int y = 25;
            Label lblPolicy = CreateSettingsLabel("保存策略:");
            lblPolicy.Location = new Point(14, y);
            chkFileAutoSave = new CheckBox();
            chkFileAutoSave.Text = "自动保存到目录";
            chkFileAutoSave.Location = new Point(100, y - 2);
            chkFileAutoSave.AutoSize = true;
            chkFileAutoSave.Font = SettingsFont();
            chkFileAutoSave.ForeColor = Color.White;

            y += 28;
            Label lblDir = CreateSettingsLabel("下载目录:");
            lblDir.Location = new Point(14, y);
            txtDownloadDir = new TextBox();
            txtDownloadDir.Location = new Point(100, y - 2);
            txtDownloadDir.Size = new Size(170, 23);
            txtDownloadDir.Font = SettingsFont();
            Button btnBrowse = new Button();
            btnBrowse.Text = "浏览...";
            btnBrowse.Location = new Point(275, y - 3);
            btnBrowse.Size = new Size(55, 23);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.Font = SettingsFont();
            btnBrowse.Click += delegate { BrowseDownloadDir(); };

            y += 28;
            Label lblPack = CreateSettingsLabel("打包策略:");
            lblPack.Location = new Point(14, y);
            chkSkipLocked = new CheckBox();
            chkSkipLocked.Text = "打包目录时跳过被占用的文件";
            chkSkipLocked.Location = new Point(100, y - 2);
            chkSkipLocked.AutoSize = true;
            chkSkipLocked.Font = SettingsFont();
            chkSkipLocked.ForeColor = Color.White;

            y += 24;
            Label lblDecomp = CreateSettingsLabel("解压策略:");
            lblDecomp.Location = new Point(14, y);
            chkAutoDecompress = new CheckBox();
            chkAutoDecompress.Text = "自动解压打包文件";
            chkAutoDecompress.Location = new Point(100, y - 2);
            chkAutoDecompress.AutoSize = true;
            chkAutoDecompress.Font = SettingsFont();
            chkAutoDecompress.ForeColor = Color.White;

            y += 28;
            Label lblThreshold = CreateSettingsLabel("大文件直传阈值:");
            lblThreshold.Location = new Point(14, y);
            lblThreshold.AutoSize = true;
            nudLargeFileThreshold = new NumericUpDown();
            nudLargeFileThreshold.Location = new Point(130, y - 2);
            nudLargeFileThreshold.Size = new Size(140, 23);
            nudLargeFileThreshold.Minimum = 1; nudLargeFileThreshold.Maximum = 10240;
            nudLargeFileThreshold.Font = SettingsFont();
            Label lblMB = CreateSettingsLabel("MB");
            lblMB.Location = new Point(275, y);

            y += 32;
            Button btnFileLog = new Button();
            btnFileLog.Text = "文件操作记录(L)";
            btnFileLog.Location = new Point(130, y);
            btnFileLog.Size = new Size(110, 25);
            btnFileLog.FlatStyle = FlatStyle.Flat;
            btnFileLog.Font = SettingsFont();
            btnFileLog.Click += delegate { OpenFileOperationLog(); };

            Button btnSave = CreateSaveButton();
            btnSave.Location = new Point(255, y);
            btnSave.Click += delegate { SaveFileTransferConfig(); };

            grp.Controls.AddRange(new Control[] {
                lblPolicy, chkFileAutoSave, lblDir, txtDownloadDir, btnBrowse,
                lblPack, chkSkipLocked, lblDecomp, chkAutoDecompress,
                lblThreshold, nudLargeFileThreshold, lblMB, btnFileLog, btnSave
            });
            grp.Height = y + 35;
            return grp;
        }

        private GroupBox BuildAuditRetentionGroup()
        {
            GroupBox grp = CreateSettingsGroup("审计保留策略");

            Label lblDays = CreateSettingsLabel("保留天数:");
            lblDays.Location = new Point(14, 28);
            nudRetentionDays = new NumericUpDown();
            nudRetentionDays.Location = new Point(100, 25);
            nudRetentionDays.Size = new Size(170, 23);
            nudRetentionDays.Minimum = 1; nudRetentionDays.Maximum = 365;
            nudRetentionDays.Font = SettingsFont();
            Label lblUnit = CreateSettingsLabel("天");
            lblUnit.Location = new Point(275, 28);

            Button btnClean = new Button();
            btnClean.Text = "立即清理(C)";
            btnClean.Location = new Point(130, 60);
            btnClean.Size = new Size(90, 25);
            btnClean.FlatStyle = FlatStyle.Flat;
            btnClean.Font = SettingsFont();
            btnClean.Click += delegate { CleanAuditLogs(); };

            Button btnSave = CreateSaveButton();
            btnSave.Location = new Point(230, 60);
            btnSave.Click += delegate { SaveAuditRetentionConfig(); };

            grp.Controls.AddRange(new Control[] { lblDays, nudRetentionDays, lblUnit, btnClean, btnSave });
            grp.Height = 100;
            return grp;
        }

        private GroupBox BuildLogViewGroup()
        {
            GroupBox grp = CreateSettingsGroup("日志查看");

            btnViewLogs = new Button();
            btnViewLogs.Text = "查看上线日志(0)";
            btnViewLogs.Location = new Point(14, 25);
            btnViewLogs.Size = new Size(130, 28);
            btnViewLogs.FlatStyle = FlatStyle.Flat;
            btnViewLogs.Font = SettingsFont();
            btnViewLogs.Click += delegate { ViewOnlineLogs(); };

            grp.Controls.Add(btnViewLogs);
            grp.Height = 70;
            return grp;
        }

        private GroupBox BuildDisplayOptionsGroup()
        {
            GroupBox grp = CreateSettingsGroup("显示选项");

            chkShowProtocolVersion = new CheckBox();
            chkShowProtocolVersion.Text = "显示协议版本";
            chkShowProtocolVersion.Location = new Point(14, 25);
            chkShowProtocolVersion.AutoSize = true;
            chkShowProtocolVersion.Font = SettingsFont();
            chkShowProtocolVersion.ForeColor = Color.White;
            chkShowProtocolVersion.CheckedChanged += delegate { SaveDisplayOptions(); };

            chkShowClientVersion = new CheckBox();
            chkShowClientVersion.Text = "显示客户端版本";
            chkShowClientVersion.Location = new Point(14, 50);
            chkShowClientVersion.AutoSize = true;
            chkShowClientVersion.Font = SettingsFont();
            chkShowClientVersion.ForeColor = Color.White;
            chkShowClientVersion.CheckedChanged += delegate { SaveDisplayOptions(); };

            grp.Controls.AddRange(new Control[] { chkShowProtocolVersion, chkShowClientVersion });
            grp.Height = 82;
            return grp;
        }

        // ==================== 辅助方法 ====================

        private static Font SettingsFont()
        {
            return new Font("Microsoft YaHei", 9F);
        }

        private GroupBox CreateSettingsGroup(string title)
        {
            GroupBox grp = new GroupBox();
            grp.Text = title;
            grp.ForeColor = Color.White;
            grp.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
            grp.Dock = DockStyle.Top;
            grp.Margin = new Padding(6);
            grp.Padding = new Padding(10, 16, 10, 6);
            return grp;
        }

        private Label CreateSettingsLabel(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.AutoSize = true;
            lbl.Font = SettingsFont();
            lbl.ForeColor = Color.FromArgb(220, 220, 220);
            return lbl;
        }

        private Button CreateSaveButton()
        {
            Button btn = new Button();
            btn.Text = "保存(S)";
            btn.Size = new Size(75, 25);
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = SettingsFont();
            btn.BackColor = Color.FromArgb(52, 91, 138);
            btn.ForeColor = Color.White;
            return btn;
        }
    }
}
