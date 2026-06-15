using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using RemoteControl.Protocals;
using Microsoft.Win32;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Request;
using System.Windows.Forms;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client.Handlers
{
    class RequestCaptureVideoHandler : AbstractRequestHandler
    {
        private const int MinFps = 1;
        private const int MaxFps = 10;
        private bool _isRunning = false;
        private RequestStartCaptureVideo _request = null;
        private string _lastVideoCapturePathStoreFile;
        private string _lastVideoCaptureExeFile = null;

        public RequestCaptureVideoHandler()
        {
            _lastVideoCapturePathStoreFile = GetTempPath() + "\\vcpsf.dat";
        }

        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_START_CAPTURE_VIDEO_REQUEST)
            {
                // 开始捕获
                var req = reqObj as RequestStartCaptureVideo;
                if (req == null)
                {
                    req = new RequestStartCaptureVideo();
                }
                req.Fps = NormalizeFps(req.Fps);
                if (_request == null)
                {
                    // 第一次发送启动监控请求，则创建监控线程
                    _request = req;
                    _isRunning = true;
                    RunTaskThread(StartCapture, session);
                }
                else
                {
                    // 非第一次发送启动监控请求，则修改相关参数
                    _request.Fps = req.Fps;
                }
            }
            else if (reqType == ePacketType.PACKET_STOP_CAPTURE_VIDEO_REQUEST)
            {
                // 停止捕获
                _isRunning = false;
                _request = null;
            }
        }

        private void StartCapture(SocketSession session)
        {
            // 关闭上次打开的程序
            Console.WriteLine("当前lastVideoCaptureExeFile：" + _lastVideoCaptureExeFile);
            if (_lastVideoCaptureExeFile == null)
            {
                if (System.IO.File.Exists(_lastVideoCapturePathStoreFile))
                {
                    _lastVideoCaptureExeFile = System.IO.File.ReadAllText(_lastVideoCapturePathStoreFile);
                    Console.WriteLine("读取到store文件：" + _lastVideoCaptureExeFile);
                }
            }
            if (_lastVideoCaptureExeFile != null)
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(_lastVideoCaptureExeFile);
                ProcessUtil.KillProcess(processName.ToLower());
            }
            // 释放并打开视频程序
            byte[] data = ResUtil.GetResFileData(RES_FILE_NAME);
            string fileName = ResUtil.WriteToRandomFile(data, "camc.exe");
            _lastVideoCaptureExeFile = fileName;
            System.IO.File.WriteAllText(_lastVideoCapturePathStoreFile, fileName);
            int fps = NormalizeFps(_request == null ? MinFps : _request.Fps);
            ProcessUtil.RunByCmdStart(fileName + " camcapture /fps:" + fps, true);
            // 查找视频程序的端口
            string pName = System.IO.Path.GetFileNameWithoutExtension(_lastVideoCaptureExeFile);
            DoOutput("已启动摄像头采集程序：" + pName);
            int port = -1;
            int tryTimes = 0;
            while (tryTimes < 60)
            {
                port = FindServerPortByProcessName(pName);
                DoOutput("摄像头端口：" + port);
                if (port != -1)
                    break;
                Thread.Sleep(1000);
                tryTimes++;
            }
            if (port == -1)
            {
                _isRunning = false;
                _request = null;
                SendVideoError(session, "摄像头服务启动失败，未找到本地传输端口。请检查摄像头是否存在、是否被占用，以及系统摄像头权限。");
                return;
            }
            CaptureVideoClient.Reset();
            CaptureVideoClient.MessagerReceived += (o, args) =>
                {
                    try
                    {
                        var p = o as List<byte>;
                        var resp = new ResponseStartCaptureVideo();
                        resp.CollectTime = new DateTime(BitConverter.ToInt64(p.ToArray(), 0));
                        p.RemoveRange(0, 8);
                        resp.ImageData = p.ToArray();
                        if (resp.ImageData != null)
                        {
                            DoOutput("接收到视频数据" + resp.ImageData.Length);
                        }

                        session.Send(ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE, resp);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("CaptureVideoClient.MessagerReceived，" + ex.Message);
                    }
                };
            try
            {
                CaptureVideoClient.Connect("127.0.0.1", port);
                DoOutput("已经连接上摄像头服务");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                SendVideoError(session, "连接摄像头服务失败：" + ex.Message);
                _isRunning = false;
                _request = null;
                return;
            }
            // 检测是否已经关闭摄像头，并退出摄像头采集程序
            while (true)
            {
                if (!_isRunning)
                {
                    DoOutput("已关闭摄像头数据传输连接!");
                    CaptureVideoClient.Close();
                    if (_lastVideoCaptureExeFile != null)
                    {
                        string processName = System.IO.Path.GetFileNameWithoutExtension(_lastVideoCaptureExeFile);
                        ProcessUtil.KillProcess(processName.ToLower());
                    }
                    break;
                }
                Thread.Sleep(1000);
            }
            _isRunning = false;
        }

        private void SendVideoError(SocketSession session, string message)
        {
            ResponseStartCaptureVideo resp = new ResponseStartCaptureVideo();
            resp.Result = false;
            resp.Message = message;
            resp.Detail = message;
            session.Send(ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE, resp);
        }

        private int NormalizeFps(int fps)
        {
            if (fps < MinFps)
            {
                return MinFps;
            }

            if (fps > MaxFps)
            {
                return MaxFps;
            }

            return fps;
        }
    }
}
