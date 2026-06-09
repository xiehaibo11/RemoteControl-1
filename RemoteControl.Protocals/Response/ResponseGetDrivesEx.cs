using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class DriveInfoEx
    {
        public string Name;
        public string DriveType;
        public long TotalSize;
        public long FreeSpace;
        public string VolumeLabel;
    }

    public class ResponseGetDrivesEx : ResponseBase
    {
        public List<DriveInfoEx> Drives;
    }
}
