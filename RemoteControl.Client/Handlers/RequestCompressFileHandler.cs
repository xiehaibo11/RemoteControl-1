using System;
using System.IO;
using System.IO.Compression;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestCompressFileHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                try
                {
                    var req = reqObj as RequestCompressFile;
                    if (req == null) return;

                    string destPath = req.DestPath;
                    if (string.IsNullOrEmpty(destPath))
                        destPath = req.SourcePath + ".gz";

                    using (FileStream sourceStream = new FileStream(req.SourcePath, FileMode.Open, FileAccess.Read))
                    using (FileStream destStream = new FileStream(destPath, FileMode.Create))
                    using (GZipStream gzip = new GZipStream(destStream, CompressionMode.Compress))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            gzip.Write(buffer, 0, bytesRead);
                        }
                    }

                    var resp = new ResponseCompressFile();
                    resp.Result = true;
                    resp.Message = "压缩完成: " + destPath;
                    session.Send(ePacketType.PACKET_COMPRESS_FILE_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    var resp = new ResponseCompressFile();
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_COMPRESS_FILE_RESPONSE, resp);
                }
            });
        }
    }
}
