using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RemoteControl.Client
{
    partial class ScreenUtil
    {
        private const int SRCCOPY = 0x00CC0020;
        private const int CAPTUREBLT = 0x40000000;

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

        private static Rectangle GetCaptureBounds()
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                bounds = Screen.PrimaryScreen.Bounds;
            }

            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new InvalidOperationException("No active screen was found.");
            }

            return bounds;
        }

        private static Bitmap CaptureScreenCopyFromScreen(Rectangle bounds)
        {
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    bounds.Left,
                    bounds.Top,
                    0,
                    0,
                    bounds.Size,
                    (CopyPixelOperation)(SRCCOPY | CAPTUREBLT));
            }

            return bitmap;
        }

        private static Bitmap CaptureScreenHwndBitBlt(Rectangle bounds)
        {
            Bitmap screenCopy = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics gDest = Graphics.FromImage(screenCopy))
            using (Graphics gSrc = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr hSrcDC = IntPtr.Zero;
                IntPtr hDC = IntPtr.Zero;
                try
                {
                    hSrcDC = gSrc.GetHdc();
                    hDC = gDest.GetHdc();
                    if (!BitBlt(hDC, 0, 0, bounds.Width, bounds.Height, hSrcDC, bounds.Left, bounds.Top, SRCCOPY | CAPTUREBLT))
                    {
                        throw new InvalidOperationException("BitBlt failed.");
                    }
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

        private static Bitmap CaptureScreenFastBitBlt(Rectangle bounds)
        {
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
                if (!BitBlt(memoryDC, 0, 0, bounds.Width, bounds.Height, screenDC, bounds.Left, bounds.Top, SRCCOPY | CAPTUREBLT))
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
    }
}
