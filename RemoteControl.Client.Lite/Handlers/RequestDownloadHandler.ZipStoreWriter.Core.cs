using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RemoteControl.Client.Handlers
{
    partial class RequestDownloadHandler
    {
        private partial class ZipStoreWriter
        {
            private const uint LocalFileHeaderSignature = 0x04034b50;
            private const uint CentralDirectoryHeaderSignature = 0x02014b50;
            private const uint EndOfCentralDirectorySignature = 0x06054b50;
            private const ushort VersionNeeded = 20;
            private const ushort Utf8Flag = 0x0800;
            private static readonly uint[] CrcTable = BuildCrcTable();

            private class ZipEntryInfo
            {
                public string Name;
                public uint Crc;
                public uint Size;
                public long Offset;
                public ushort DosTime;
                public ushort DosDate;
            }

            public static void CreateFromDirectory(string sourceDir, string zipPath)
            {
                string rootName = Path.GetFileName(sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(rootName))
                    rootName = "download";

                List<ZipEntryInfo> entries = new List<ZipEntryInfo>();
                using (FileStream zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter writer = new BinaryWriter(zipStream, Encoding.UTF8))
                {
                    AddDirectory(writer, entries, sourceDir, rootName);

                    long centralDirectoryOffset = zipStream.Position;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        WriteCentralDirectoryHeader(writer, entries[i]);
                    }
                    long centralDirectorySize = zipStream.Position - centralDirectoryOffset;

                    writer.Write(EndOfCentralDirectorySignature);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)entries.Count);
                    writer.Write((ushort)entries.Count);
                    writer.Write((uint)centralDirectorySize);
                    writer.Write((uint)centralDirectoryOffset);
                    writer.Write((ushort)0);
                }
            }

            private static void AddDirectory(BinaryWriter writer, List<ZipEntryInfo> entries, string dir, string entryName)
            {
                string normalizedDirName = NormalizeEntryName(entryName) + "/";
                WriteEntry(writer, entries, normalizedDirName, null, DateTime.Now);

                string[] files = SafeGetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = NormalizeEntryName(entryName + "/" + Path.GetFileName(files[i]));
                    WriteEntry(writer, entries, fileName, files[i], File.GetLastWriteTime(files[i]));
                }

                string[] dirs = SafeGetDirectories(dir);
                for (int i = 0; i < dirs.Length; i++)
                {
                    AddDirectory(writer, entries, dirs[i], entryName + "/" + Path.GetFileName(dirs[i]));
                }
            }

            private static string[] SafeGetFiles(string dir)
            {
                try { return Directory.GetFiles(dir); }
                catch { return new string[0]; }
            }

            private static string[] SafeGetDirectories(string dir)
            {
                try { return Directory.GetDirectories(dir); }
                catch { return new string[0]; }
            }

            private static void WriteEntry(BinaryWriter writer, List<ZipEntryInfo> entries, string entryName, string filePath, DateTime lastWriteTime)
            {
                long size = 0;
                uint crc = 0;
                if (!string.IsNullOrEmpty(filePath))
                {
                    FileInfo info = new FileInfo(filePath);
                    if (info.Length > uint.MaxValue)
                        return;
                    size = info.Length;
                    crc = ComputeFileCrc(filePath);
                }

                ZipEntryInfo entry = new ZipEntryInfo();
                entry.Name = entryName;
                entry.Crc = crc;
                entry.Size = (uint)size;
                entry.Offset = writer.BaseStream.Position;
                ToDosTimeDate(lastWriteTime, out entry.DosTime, out entry.DosDate);

                WriteLocalFileHeader(writer, entry);
                if (!string.IsNullOrEmpty(filePath))
                    WriteFileData(writer, filePath);

                entries.Add(entry);
            }

            private static void WriteLocalFileHeader(BinaryWriter writer, ZipEntryInfo entry)
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(entry.Name);
                writer.Write(LocalFileHeaderSignature);
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
                writer.Write(nameBytes);
            }
        }
    }
}
