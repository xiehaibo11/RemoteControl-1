using System;
using System.Collections.Generic;
using System.Text;

namespace RemoteControl.Relay
{
    public static partial class PacketCodec
    {
        private static readonly byte[] RelayFrameMagic = Encoding.ASCII.GetBytes("RCF1");

        private static bool TryDecodeRelayDataFrameBinary(byte[] fullPacket, out RelayDataFrameData frame)
        {
            frame = null;
            if (fullPacket == null || fullPacket.Length < 5 + RelayFrameMagic.Length)
                return false;

            int offset = 5;
            for (int i = 0; i < RelayFrameMagic.Length; i++)
            {
                if (fullPacket[offset + i] != RelayFrameMagic[i])
                    return false;
            }

            offset += RelayFrameMagic.Length;
            frame = new RelayDataFrameData();
            frame.StreamId = ReadInt(fullPacket, ref offset);
            frame.InnerPacketType = ReadInt(fullPacket, ref offset);
            int clientIdLength = ReadInt(fullPacket, ref offset);
            int sessionIdLength = ReadInt(fullPacket, ref offset);
            int requestIdLength = ReadInt(fullPacket, ref offset);
            int payloadLength = ReadInt(fullPacket, ref offset);
            frame.ClientId = ReadString(fullPacket, ref offset, clientIdLength);
            frame.SessionId = ReadString(fullPacket, ref offset, sessionIdLength);
            frame.RequestId = ReadString(fullPacket, ref offset, requestIdLength);
            frame.Payload = ReadBytes(fullPacket, ref offset, payloadLength);
            return true;
        }

        private static byte[] BuildRelayDataFramePacket(byte packetType, RelayDataFrameData frame)
        {
            byte[] clientId = Encoding.UTF8.GetBytes(frame.ClientId ?? string.Empty);
            byte[] sessionId = Encoding.UTF8.GetBytes(frame.SessionId ?? string.Empty);
            byte[] requestId = Encoding.UTF8.GetBytes(frame.RequestId ?? string.Empty);
            byte[] payload = frame.Payload ?? new byte[0];

            List<byte> body = new List<byte>(RelayFrameMagic.Length + 24 +
                clientId.Length + sessionId.Length + requestId.Length + payload.Length);
            body.AddRange(RelayFrameMagic);
            AddInt(body, frame.StreamId);
            AddInt(body, frame.InnerPacketType);
            AddInt(body, clientId.Length);
            AddInt(body, sessionId.Length);
            AddInt(body, requestId.Length);
            AddInt(body, payload.Length);
            body.AddRange(clientId);
            body.AddRange(sessionId);
            body.AddRange(requestId);
            body.AddRange(payload);

            int packetLength = 4 + 1 + body.Count;
            byte[] packet = new byte[packetLength];
            Buffer.BlockCopy(BitConverter.GetBytes(packetLength), 0, packet, 0, 4);
            packet[4] = packetType;
            body.CopyTo(packet, 5);
            return packet;
        }

        private static void AddInt(List<byte> bytes, int value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private static int ReadInt(byte[] bytes, ref int offset)
        {
            int value = BitConverter.ToInt32(bytes, offset);
            offset += 4;
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
