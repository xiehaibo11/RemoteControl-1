using System;
using System.IO;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        // ==================== 设置保存逻辑 ====================

        private void SaveServerConfig()
        {
            Settings.CurrentSettings.ListenAddress = txtListenAddress.Text.Trim();
            Settings.CurrentSettings.ServerPort = (int)nudServerPort.Value;
            Settings.SaveSettings();
            doOutput("服务器配置已保存");
        }

        private void SavePerformanceConfig()
        {
            Settings.CurrentSettings.WorkerLoops = (int)nudWorkerLoops.Value;
            Settings.CurrentSettings.HeavyWorkers = (int)nudHeavyWorkers.Value;
            Settings.SaveSettings();
            doOutput("性能配置已保存");
        }

        private void SaveScreenTransferConfig()
        {
            Settings.CurrentSettings.ScreenCompressionMode = cboCompression.SelectedIndex;
            Settings.CurrentSettings.ScreenFps = (int)nudScreenFps.Value;
            Settings.SaveSettings();
            doOutput("屏幕传输配置已保存");
        }

        private void SaveFileTransferConfig()
        {
            Settings.CurrentSettings.FileAutoSave = chkFileAutoSave.Checked;
            Settings.CurrentSettings.FileDownloadDir = txtDownloadDir.Text.Trim();
            Settings.CurrentSettings.FileSkipLocked = chkSkipLocked.Checked;
            Settings.CurrentSettings.FileAutoDecompress = chkAutoDecompress.Checked;
            Settings.CurrentSettings.LargeFileThresholdMB = (int)nudLargeFileThreshold.Value;
            Settings.SaveSettings();
            doOutput("文件传输配置已保存");
        }

        private void SaveAuditRetentionConfig()
        {
            Settings.CurrentSettings.AuditRetentionDays = (int)nudRetentionDays.Value;
            Settings.SaveSettings();
            doOutput("审计保留策略已保存");
        }

        private void SaveDisplayOptions()
        {
            Settings.CurrentSettings.ShowProtocolVersion = chkShowProtocolVersion.Checked;
            Settings.CurrentSettings.ShowClientVersion = chkShowClientVersion.Checked;
            Settings.SaveSettings();
        }

        // ==================== 设置功能按钮 ====================

        private void BrowseDownloadDir()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择文件下载目录";
                if (!string.IsNullOrEmpty(txtDownloadDir.Text) && Directory.Exists(txtDownloadDir.Text))
                    dlg.SelectedPath = txtDownloadDir.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtDownloadDir.Text = dlg.SelectedPath;
            }
        }

        private void OpenFileOperationLog()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (Directory.Exists(logDir))
                System.Diagnostics.Process.Start("explorer.exe", logDir);
            else
                doOutput("日志目录不存在: " + logDir);
        }

        private void CleanAuditLogs()
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir))
                {
                    doOutput("日志目录不存在，无需清理");
                    return;
                }
                int days = (int)nudRetentionDays.Value;
                DateTime cutoff = DateTime.Now.AddDays(-days);
                int cleaned = 0;
                foreach (string file in Directory.GetFiles(logDir, "*.log"))
                {
                    if (File.GetLastWriteTime(file) < cutoff)
                    {
                        File.Delete(file);
                        cleaned++;
                    }
                }
                doOutput("已清理 " + cleaned + " 个过期日志文件");
            }
            catch (Exception ex)
            {
                doOutput("清理失败: " + ex.Message);
            }
        }

        private void ViewOnlineLogs()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string[] logFiles = Directory.Exists(logDir)
                ? Directory.GetFiles(logDir, "*.log")
                : new string[0];
            if (logFiles.Length == 0)
            {
                doOutput("暂无上线日志");
                return;
            }
            string latest = logFiles[0];
            DateTime latestTime = File.GetLastWriteTime(latest);
            foreach (string f in logFiles)
            {
                DateTime t = File.GetLastWriteTime(f);
                if (t > latestTime)
                {
                    latest = f;
                    latestTime = t;
                }
            }
            System.Diagnostics.Process.Start("notepad.exe", latest);
        }

        private void UpdateLogButtonText()
        {
            if (btnViewLogs == null) return;
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            int count = 0;
            if (Directory.Exists(logDir))
                count = Directory.GetFiles(logDir, "*.log").Length;
            btnViewLogs.Text = "查看上线日志(" + count + ")";
        }
    }
}
