using System;
using Microsoft.Win32;

namespace RemoteControl.Client
{
    partial class Program
    {
        static void EnsureAutoStart()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                {
                    string runCmd = "\"" + exePath + "\" /r";
                    object val = key.GetValue("SystemService");
                    if (val == null || val.ToString() != runCmd)
                    {
                        key.SetValue("SystemService", runCmd, RegistryValueKind.String);
                        DoOutput("开机自启动已写入注册表。");
                    }
                }
            }
            catch (Exception ex)
            {
                DoOutput("写入自启动失败: " + ex.Message);
            }
        }
    }
}
