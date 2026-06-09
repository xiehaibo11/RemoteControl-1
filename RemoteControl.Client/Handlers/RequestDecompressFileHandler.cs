using System;
using System.IO;
using System.IO.Compression;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestDecompressFileHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestDecompressFile;
                    if (req == null) return;

                    string destPath = req.DestPath;
                    if (string.IsNullOrEmpty(destPath))
                    {
                        destPath = req.SourcePath.EndsWith(".gz")
                            ? req.SourcePath.Substring(0, req.SourcePath.Length - 3)
                            : req.SourcePath + ".decomp";
                    }

                    using (FileStream sourceStream = new FileStream(req.SourcePath, FileMode.Open, FileAccess.Read))
                    using (GZipStream gzip = new GZipStream(sourceStream, CompressionMode.Decompress))
                    using (FileStream destStream = new FileStream(destPath, FileMode.Create))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = gzip.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destStream.Write(buffer, 0, bytesRead);
                        }
                    }

                    var resp = new ResponseDecompressFile();
                    resp.Result = true;
                    resp.Message = "解压完成: " + destPath;
                    session.Send(ePacketType.PACKET_DECOMPRESS_FILE_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseDecompressFile();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_DECOMPRESS_FILE_RESPONSE, resp);
                }
            });
        }
    }
}
