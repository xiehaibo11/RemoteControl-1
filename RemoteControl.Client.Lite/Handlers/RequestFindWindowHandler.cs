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
    class RequestFindWindowHandler : AbstractRequestHandler
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

        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseFindWindow();
                resp.Windows = new List<WindowInfo>();

                try
                {
                    var req = reqObj as RequestFindWindow;
                    string keyword = req == null ? string.Empty : (req.Keyword ?? string.Empty).Trim();

                    EnumWindows((hWnd, lParam) =>
                    {
                        if (!IsWindowVisible(hWnd))
                            return true;

                        int length = GetWindowTextLength(hWnd);
                        if (length <= 0)
                            return true;

                        StringBuilder titleBuilder = new StringBuilder(length + 1);
                        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                        string title = titleBuilder.ToString();
                        if (title.Length == 0)
                            return true;

                        if (keyword.Length > 0 && title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                            return true;

                        uint processIdValue;
                        GetWindowThreadProcessId(hWnd, out processIdValue);

                        string processName = string.Empty;
                        try
                        {
                            using (Process process = Process.GetProcessById((int)processIdValue))
                            {
                                processName = process.ProcessName;
                            }
                        }
                        catch { }

                        resp.Windows.Add(new WindowInfo
                        {
                            Title = title,
                            ProcessId = (int)processIdValue,
                            ProcessName = processName,
                            Handle = hWnd.ToInt64().ToString("X")
                        });

                        return true;
                    }, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }

                session.Send(ePacketType.PACKET_FIND_WINDOW_RESPONSE, resp);
            });
        }
    }
}
