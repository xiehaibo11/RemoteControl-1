using System;
using System.Collections.Generic;
using System.Management;
using System.Net.NetworkInformation;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    partial class RequestGetHostInfoHandler
    {
        private void CollectDiskInfo(List<HostDiskInfo> disks)
        {
            try
            {
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    var disk = new HostDiskInfo();
                    disk.Mount = drive.Name;
                    disk.FileSystem = drive.DriveFormat;
                    disk.TotalGB = drive.TotalSize / (1024 * 1024 * 1024);
                    disk.AvailableGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                    disk.UsagePercent = Math.Round((1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100, 1);
                    disks.Add(disk);
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
                    {
                        na.IPAddresses.Add(addr.Address.ToString());
                    }
                    list.Add(na);
                }
            }
            catch { }
        }

        private void CollectInstalledSoftware(List<HostSoftwareInfo> list)
        {
            try
            {
                using (var reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (reg != null)
                    {
                        foreach (string subKeyName in reg.GetSubKeyNames())
                        {
                            using (var subKey = reg.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;
                                string name = subKey.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(name)) continue;
                                list.Add(new HostSoftwareInfo
                                {
                                    Name = name,
                                    Version = subKey.GetValue("DisplayVersion") as string ?? "",
                                    InstallDate = subKey.GetValue("InstallDate") as string ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void CollectSecuritySoftware(List<HostSecuritySoftwareInfo> list)
        {
            try
            {
                using (var wmi = new ManagementObjectSearcher("SELECT * FROM AntiVirusProduct"))
                {
                    foreach (ManagementObject obj in wmi.Get())
                    {
                        list.Add(new HostSecuritySoftwareInfo
                        {
                            Name = obj["displayName"].ToString(),
                            IsEnabled = true
                        });
                    }
                }
            }
            catch { }
        }
    }
}
