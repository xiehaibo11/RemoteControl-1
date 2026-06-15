using System;
using System.IO;
using System.Text;

namespace RemoteControl.Client.Handlers
{
    partial class RequestDownloadHandler
    {
        private partial class ZipStoreWriter
        {
            private static void WriteCentralDirectoryHeader(BinaryWriter writer, ZipEntryInfo entry)
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(entry.Name);
                writer.Write(CentralDirectoryHeaderSignature);
                writer.Write((ushort)VersionNeeded);
                writer.Write(VersionNeeded);
                writer.Write(Utf8Flag);
                writer.Write((ushort)0);
                writer.Write(entry.DosTime);
                writer.Write(entry.DosDate);
                writer.Write(entry.Crc);
                writer.Write(entry.Size);
                writer.Write(entry.Size);
                writer.Write((ushort)nameBytes.Length);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((uint)0);
                writer.Write((uint)entry.Offset);
                writer.Write(nameBytes);
            }

            private static void WriteFileData(BinaryWriter writer, string filePath)
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[8192];
                    int read;
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, read);
                    }
                }
            }

            private static uint ComputeFileCrc(string filePath)
            {
                uint crc = 0xffffffff;
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[8192];
                    int read;
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                            crc = CrcTable[(crc ^ buffer[i]) & 0xff] ^ (crc >> 8);
                    }
                }
                return crc ^ 0xffffffff;
            }

            private static uint[] BuildCrcTable()
            {
                uint[] table = new uint[256];
                for (uint i = 0; i < table.Length; i++)
                {
                    uint crc = i;
                    for (int j = 0; j < 8; j++)
                        crc = (crc & 1) == 1 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
                    table[i] = crc;
                }
                return table;
            }

            private static string NormalizeEntryName(string name)
            {
                return name.Replace('\\', '/').TrimStart('/');
            }

            private static void ToDosTimeDate(DateTime value, out ushort dosTime, out ushort dosDate)
            {
                if (value.Year < 1980)
                    value = new DateTime(1980, 1, 1, 0, 0, 0);

                dosTime = (ushort)((value.Hour << 11) | (value.Minute << 5) | (value.Second / 2));
                dosDate = (ushort)(((value.Year - 1980) << 9) | (value.Month << 5) | value.Day);
            }
        }
    }
}
