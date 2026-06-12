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
    class RequestCaptureScreenHandler : AbstractRequestHandler
    {
        private const int MinFps = 1;
        private const int MaxFps = 10;
        private bool _isRunning = false;
        private RequestStartGetScreen _request = null;
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
            int sleepValue = 1000;
            int fpsValue = 1;
            while (_isRunning)
            {
                fpsValue = NormalizeFps(_request == null ? MinFps : _request.fps);
                sleepValue = 1000 / fpsValue;
                ResponseStartGetScreen resp = new ResponseStartGetScreen();
                try
                {
                    using (var image = ScreenUtil.CaptureScreenOptimized())
                    {
                        resp.SetImage(image, ImageFormat.Jpeg);
                    }
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                    resp.Detail = ex.StackTrace;
                }

                session.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE, resp);
                Thread.Sleep(sleepValue);
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
