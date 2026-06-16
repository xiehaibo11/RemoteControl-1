using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    class RequestCaptureScreenHandler : AbstractRequestHandler
    {
        private const int MinFps = 1;
        private const int MaxFps = 10;
        private const int MinJpegQuality = 35;
        private const int MaxJpegQuality = 75;
        private bool _isRunning = false;
        private RequestStartGetScreen _request = null;
        private int _adaptiveFps = 1;
        private int _jpegQuality = 70;
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST)
            {
                // 开始捕获
                RequestStartGetScreen req = reqObj as RequestStartGetScreen;
                if (req == null)
                {
                    return;
                }

                req.fps = NormalizeFps(req.fps);
                _request = req;

                if (!_isRunning)
                {
                    // 第一次或停止后重新发送启动监控请求，则创建监控线程
                    _isRunning = true;
                    RunTaskThread(StartCaptureScreen, session);
                }
            }
            else if (reqType == ePacketType.PACKET_STOP_CAPTURE_SCREEN_REQUEST)
            {
                // 停止捕获
                _isRunning = false;
            }
        }

        private void StartCaptureScreen(SocketSession session)
        {
            while (_isRunning)
            {
                Stopwatch frameWatch = Stopwatch.StartNew();
                int requestedFps = NormalizeFps(_request == null ? MinFps : _request.fps);
                if (_adaptiveFps < MinFps || _adaptiveFps > requestedFps)
                    _adaptiveFps = requestedFps;
                int sleepValue = 1000 / _adaptiveFps;

                ResponseStartGetScreen resp = new ResponseStartGetScreen();
                try
                {
                    using (var image = ScreenUtil.CaptureScreenOptimized())
                    {
                        resp.SetImageJpegQuality(image, _jpegQuality);
                    }
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                    resp.Detail = ex.StackTrace;
                }

                Stopwatch sendWatch = Stopwatch.StartNew();
                session.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE, resp);
                sendWatch.Stop();
                AdjustRealtimeBudget(sendWatch.ElapsedMilliseconds, requestedFps);

                int remain = sleepValue - (int)frameWatch.ElapsedMilliseconds;
                if (remain > 0)
                    Thread.Sleep(remain);
                else
                    Thread.Sleep(1);
            }
        }

        private void AdjustRealtimeBudget(long sendMilliseconds, int requestedFps)
        {
            int frameBudget = 1000 / Math.Max(MinFps, _adaptiveFps);
            if (sendMilliseconds > 250 || sendMilliseconds > frameBudget)
            {
                if (_adaptiveFps > MinFps)
                    _adaptiveFps--;
                if (_jpegQuality > MinJpegQuality)
                    _jpegQuality -= 5;
            }
            else if (sendMilliseconds < 50)
            {
                if (_adaptiveFps < requestedFps)
                    _adaptiveFps++;
                if (_jpegQuality < MaxJpegQuality)
                    _jpegQuality += 2;
            }
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
