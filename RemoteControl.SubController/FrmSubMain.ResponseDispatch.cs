using System;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Response;

namespace RemoteControl.SubController
{
    partial class FrmSubMain
    {
        private void OnPacketReceived(object sender, PacketEventArgs e)
        {
            SafeInvoke(() => DispatchPacket(e));
        }

        private void DispatchPacket(PacketEventArgs e)
        {
            switch (e.PacketType)
            {
                case ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE:
                    DispatchScreenResponse(e);
                    break;
                case ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE:
                    DispatchVideoResponse(e);
                    break;
                case ePacketType.PACKET_GET_DRIVES_RESPONSE:
                case ePacketType.PACKET_GET_DRIVES_EX_RESPONSE:
                case ePacketType.PACKET_GET_SUBFILES_OR_DIRS_RESPONSE:
                case ePacketType.PACKET_START_DOWNLOAD_HEADER_RESPONSE:
                case ePacketType.PACKET_START_DOWNLOAD_RESPONSE:
                case ePacketType.PACKET_START_UPLOAD_RESPONSE:
                case ePacketType.PACKET_COPY_FILE_OR_DIR_RESPONSE:
                case ePacketType.PACKET_MOVE_FILE_OR_DIR_RESPONSE:
                case ePacketType.PACKET_CREATE_FILE_OR_DIR_RESPONSE:
                case ePacketType.PACKET_DELETE_FILE_OR_DIR_RESPONSE:
                    DispatchFileResponse(e);
                    break;
                case ePacketType.PACKET_SERVICE_MANAGER_RESPONSE:
                    DispatchServiceResponse(e);
                    break;
                case ePacketType.PACKET_GET_HOST_NAME_RESPONSE:
                    DispatchHostNameResponse(e);
                    break;
            }
        }

        private void DispatchScreenResponse(PacketEventArgs e)
        {
            if (e.Session == null) return;
            string sid = e.Session.SocketId;
            FrmScreenViewer frm;
            if (_screenForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                var resp = e.Obj as ResponseStartGetScreen;
                if (resp != null) frm.HandleScreen(resp);
            }
        }

        private void DispatchVideoResponse(PacketEventArgs e)
        {
            if (e.Session == null) return;
            string sid = e.Session.SocketId;
            FrmVideoViewer frm;
            if (_videoForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                var resp = e.Obj as ResponseStartCaptureVideo;
                if (resp != null) frm.HandleVideo(resp);
            }
        }

        private void DispatchFileResponse(PacketEventArgs e)
        {
            if (e.Session == null) return;
            string sid = e.Session.SocketId;
            FrmFileManager frm;
            if (_fileForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                frm.HandleResponse(e.PacketType, e.Obj);
            }
        }

        private void DispatchServiceResponse(PacketEventArgs e)
        {
            if (e.Session == null) return;
            string sid = e.Session.SocketId;
            FrmServiceManager frm;
            if (_serviceForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
            {
                var resp = e.Obj as RemoteControl.Protocals.Response.ResponseServiceManager;
                if (resp != null) frm.HandleResponse(resp);
            }
        }

        private void DispatchHostNameResponse(PacketEventArgs e)
        {
            if (e.Session != null)
            {
                UpsertDashboardClient(e.Session);
            }
        }
    }
}
