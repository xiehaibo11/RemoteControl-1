using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class HostBasicInfo
    {
        public string HostName;
        public string UserName;
        public string DomainOrWorkgroup;
        public string TimeZone;
        public string Region;
        public string UpTime;
    }

    public class HostSystemInfo
    {
        public string OSName;
        public string BuildNumber;
        public string Architecture;
        public string ScreenResolution;
    }

    public class HostHardwareInfo
    {
        public string CpuName;
        public int PhysicalCoreCount;
        public int LogicalCoreCount;
        public long TotalMemoryMB;
        public long AvailableMemoryMB;
    }

    public class HostDiskInfo
    {
        public string Mount;
        public string FileSystem;
        public long TotalGB;
        public long AvailableGB;
        public double UsagePercent;
    }

    public class HostSoftwareInfo
    {
        public string Name;
        public string Version;
        public string InstallDate;
    }

    public class HostSecuritySoftwareInfo
    {
        public string Name;
        public bool IsEnabled;
    }

    public class HostNetworkAdapterInfo
    {
        public string Name;
        public string MacAddress;
        public List<string> IPAddresses;
        public string Status;
    }

    public class ResponseGetHostInfo : ResponseBase
    {
        public HostBasicInfo Basic;
        public HostSystemInfo System;
        public HostHardwareInfo Hardware;
        public List<HostDiskInfo> Disks;
        public List<HostNetworkAdapterInfo> NetworkAdapters;
        public List<HostSoftwareInfo> InstalledSoftware;
        public List<HostSecuritySoftwareInfo> SecuritySoftware;
        public string CollectedAt;
    }
}
