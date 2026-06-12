using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Management;
using System.Windows.Forms;

namespace RemoteControl.Client
{
    class ScreenUtil
    {
        private const int SRCCOPY = 0x00CC0020;
        private const int CAPTUREBLT = 0x40000000;
        private static bool? _hasHardwareDisplayAdapter;

        /// <summary>
        /// 捕获屏幕
        /// <para>不支持未登陆时截图</para>
        /// </summary>
        /// <returns></returns>
        public static Image CaptureScreen1()
        {
            Image myImage = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (Graphics g = Graphics.FromImage(myImage))
            {
                g.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
            }

            return myImage;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        public static Image CaptureScreenOptimized()
        {
            if (HasHardwareDisplayAdapter())
            {
                try
                {
                    return CaptureScreenFastBitBlt();
                }
                catch
                {
                    // 显卡优化路径失败时保持兼容，避免黑屏或中断远程协助。
                }
            }

            return CaptureScreen2();
        }

        /// <summary>
        /// 捕获屏幕
        /// <para>支持未登陆时截图</para>
        /// </summary>
        /// <returns></returns>
        public static Image CaptureScreen2()
        {
            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;
            Bitmap screenCopy = new Bitmap(width, height);
            using (Graphics gDest = Graphics.FromImage(screenCopy))
            using (Graphics gSrc = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr hSrcDC = IntPtr.Zero;
                IntPtr hDC = IntPtr.Zero;
                try
                {
                    hSrcDC = gSrc.GetHdc();
                    hDC = gDest.GetHdc();
                    BitBlt(hDC, 0, 0, width, height, hSrcDC, 0, 0, SRCCOPY | CAPTUREBLT);
                }
                finally
                {
                    if (hDC != IntPtr.Zero)
                    {
                        gDest.ReleaseHdc(hDC);
                    }

                    if (hSrcDC != IntPtr.Zero)
                    {
                        gSrc.ReleaseHdc(hSrcDC);
                    }
                }
            }

            return screenCopy;
        }

        private static Image CaptureScreenFastBitBlt()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            IntPtr screenDC = IntPtr.Zero;
            IntPtr memoryDC = IntPtr.Zero;
            IntPtr bitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                screenDC = GetDC(IntPtr.Zero);
                if (screenDC == IntPtr.Zero)
                {
                    throw new InvalidOperationException("GetDC failed.");
                }

                memoryDC = CreateCompatibleDC(screenDC);
                if (memoryDC == IntPtr.Zero)
                {
                    throw new InvalidOperationException("CreateCompatibleDC failed.");
                }

                bitmap = CreateCompatibleBitmap(screenDC, bounds.Width, bounds.Height);
                if (bitmap == IntPtr.Zero)
                {
                    throw new InvalidOperationException("CreateCompatibleBitmap failed.");
                }

                oldBitmap = SelectObject(memoryDC, bitmap);
                if (!BitBlt(memoryDC, 0, 0, bounds.Width, bounds.Height, screenDC, bounds.X, bounds.Y, SRCCOPY | CAPTUREBLT))
                {
                    throw new InvalidOperationException("BitBlt failed.");
                }

                using (Bitmap captured = Image.FromHbitmap(bitmap))
                {
                    return new Bitmap(captured);
                }
            }
            finally
            {
                if (memoryDC != IntPtr.Zero && oldBitmap != IntPtr.Zero)
                {
                    SelectObject(memoryDC, oldBitmap);
                }

                if (bitmap != IntPtr.Zero)
                {
                    DeleteObject(bitmap);
                }

                if (memoryDC != IntPtr.Zero)
                {
                    DeleteDC(memoryDC);
                }

                if (screenDC != IntPtr.Zero)
                {
                    ReleaseDC(IntPtr.Zero, screenDC);
                }
            }
        }

        private static bool HasHardwareDisplayAdapter()
        {
            if (_hasHardwareDisplayAdapter.HasValue)
            {
                return _hasHardwareDisplayAdapter.Value;
            }

            bool result = false;
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name,AdapterCompatibility,PNPDeviceID FROM Win32_VideoController"))
                {
                    foreach (ManagementObject adapter in searcher.Get())
                    {
                        string name = Convert.ToString(adapter["Name"]);
                        string vendor = Convert.ToString(adapter["AdapterCompatibility"]);
                        string pnpId = Convert.ToString(adapter["PNPDeviceID"]);
                        string text = (name + " " + vendor + " " + pnpId).ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        if (!IsSoftwareOrVirtualDisplayAdapter(text))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
                result = false;
            }

            _hasHardwareDisplayAdapter = result;
            return result;
        }

        private static bool IsSoftwareOrVirtualDisplayAdapter(string text)
        {
            string[] tokens = new string[]
            {
                "basic display",
                "microsoft remote",
                "remote display",
                "rdp",
                "mirror",
                "mirage",
                "virtual",
                "vmware",
                "vbox",
                "citrix",
                "parsec",
                "gameviewer",
                "mumu",
                "root\\display"
            };

            foreach (string token in tokens)
            {
                if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
