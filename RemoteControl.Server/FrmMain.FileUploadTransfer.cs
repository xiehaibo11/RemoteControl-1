using System.Collections.Generic;
using System.IO;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private Dictionary<string, FileStream> uploadDic = new Dictionary<string, FileStream>();
        private Dictionary<string, FrmDownload> uploadFrmDic = new Dictionary<string, FrmDownload>();

        private void DoUploadFileInternal(SocketSession session, string fileId)
        {
            if (uploadDic.ContainsKey(fileId))
            {
                FileStream fs = uploadDic[fileId];
                FrmDownload frm = uploadFrmDic[fileId];
                if (fs != null)
                {
                    byte[] buffer = new byte[2048];
                    int totalSize = 0;
                    while (true)
                    {
                        int size = fs.Read(buffer, 0, buffer.Length);
                        if (size < 1)
                            break;

                        if (!uploadDic.ContainsKey(fileId))
                        {
                            break;
                        }
                        byte[] data = new byte[size];
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = buffer[i];
                        }
                        ResponseStartUpload resp = new ResponseStartUpload();
                        resp.Id = fileId;
                        resp.Data = data;

                        session.Send(ePacketType.PACKET_START_UPLOAD_RESPONSE, resp);

                        totalSize += size;
                        frm.UpdateProgress(totalSize);
                    }

                    RequestStopUpload reqStop = new RequestStopUpload();
                    reqStop.Id = fileId;
                    session.Send(ePacketType.PACKET_STOP_UPLOAD_REQUEST, reqStop);
                    uploadDic.Remove(fileId);
                    uploadFrmDic.Remove(fileId);

                    fs.Close();
                    fs.Dispose();
                    fs = null;

                    MsgBox.Info("上传完成!");
                    RefreshCurrentFileView();
                }
                if (frm != null)
                {
                    frm.Close();
                    frm.Dispose();
                    frm = null;
                }
            }
        }
    }
}
