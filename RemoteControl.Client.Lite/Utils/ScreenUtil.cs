using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RemoteControl.Client
{
    partial class ScreenUtil
    {
        private static int _preferredCaptureMode = -1;

        private enum CaptureMode
        {
            FastBitBlt = 0,
            HwndBitBlt = 1,
            CopyFromScreen = 2
        }

        /// <summary>
        /// 捕获屏幕。不支持未登录或锁屏安全桌面的场景。
        /// </summary>
        public static Image CaptureScreen1()
        {
            return CaptureScreenCopyFromScreen(GetCaptureBounds());
        }

        /// <summary>
        /// 捕获屏幕。GDI 兼容路径。
        /// </summary>
        public static Image CaptureScreen2()
        {
            return CaptureScreenHwndBitBlt(GetCaptureBounds());
        }

        public static Image CaptureScreenOptimized()
        {
            Rectangle bounds;
            try
            {
                bounds = GetCaptureBounds();
            }
            catch (Exception ex)
            {
                return CreateDiagnosticImage(new Rectangle(0, 0, 640, 360), ex);
            }

            List<CaptureMode> modes = BuildCaptureModeOrder();
            Exception lastException = null;
            Bitmap lastBlackFrame = null;

            for (int i = 0; i < modes.Count; i++)
            {
                CaptureMode mode = modes[i];
                try
                {
                    Bitmap image = CaptureByMode(mode, bounds);
                    if (!IsMostlyBlackFrame(image))
                    {
                        _preferredCaptureMode = (int)mode;
                        if (lastBlackFrame != null)
                        {
                            lastBlackFrame.Dispose();
                        }
                        return image;
                    }

                    if (lastBlackFrame != null)
                    {
                        lastBlackFrame.Dispose();
                    }
                    lastBlackFrame = image;
                    lastException = new InvalidOperationException(mode + " returned a black frame.");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (_preferredCaptureMode == (int)mode)
                    {
                        _preferredCaptureMode = -1;
                    }
                }
            }

            if (lastBlackFrame != null)
            {
                lastBlackFrame.Dispose();
            }

            return CreateDiagnosticImage(bounds, lastException);
        }

        private static List<CaptureMode> BuildCaptureModeOrder()
        {
            List<CaptureMode> modes = new List<CaptureMode>();
            if (Enum.IsDefined(typeof(CaptureMode), _preferredCaptureMode))
            {
                modes.Add((CaptureMode)_preferredCaptureMode);
            }

            AddModeIfMissing(modes, CaptureMode.FastBitBlt);
            AddModeIfMissing(modes, CaptureMode.HwndBitBlt);
            AddModeIfMissing(modes, CaptureMode.CopyFromScreen);
            return modes;
        }

        private static void AddModeIfMissing(List<CaptureMode> modes, CaptureMode mode)
        {
            if (!modes.Contains(mode))
            {
                modes.Add(mode);
            }
        }

        private static Bitmap CaptureByMode(CaptureMode mode, Rectangle bounds)
        {
            switch (mode)
            {
                case CaptureMode.FastBitBlt:
                    return CaptureScreenFastBitBlt(bounds);
                case CaptureMode.HwndBitBlt:
                    return CaptureScreenHwndBitBlt(bounds);
                case CaptureMode.CopyFromScreen:
                    return CaptureScreenCopyFromScreen(bounds);
                default:
                    throw new NotSupportedException(mode.ToString());
            }
        }

    }
}
