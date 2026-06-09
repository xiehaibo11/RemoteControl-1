using System;
using System.Runtime.InteropServices;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestChangeResolutionHandler : AbstractRequestHandler
    {
        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int CDS_UPDATEREGISTRY = 0x00000001;
        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const int DM_BITSPERPEL = 0x00040000;
        private const int DM_PELSWIDTH = 0x00080000;
        private const int DM_PELSHEIGHT = 0x00100000;
        private const int DM_DISPLAYFREQUENCY = 0x00400000;

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseChangeResolution();
                try
                {
                    var req = reqObj as RequestChangeResolution;
                    if (req == null || req.Width <= 0 || req.Height <= 0)
                    {
                        throw new ArgumentException("分辨率参数不正确");
                    }

                    DEVMODE devMode = CreateDevMode();
                    if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
                    {
                        throw new InvalidOperationException("读取当前分辨率失败");
                    }

                    resp.PreviousWidth = devMode.dmPelsWidth;
                    resp.PreviousHeight = devMode.dmPelsHeight;

                    devMode.dmPelsWidth = req.Width;
                    devMode.dmPelsHeight = req.Height;
                    devMode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
                    if (req.BitsPerPel > 0)
                    {
                        devMode.dmBitsPerPel = req.BitsPerPel;
                        devMode.dmFields |= DM_BITSPERPEL;
                    }
                    if (req.DisplayFrequency > 0)
                    {
                        devMode.dmDisplayFrequency = req.DisplayFrequency;
                        devMode.dmFields |= DM_DISPLAYFREQUENCY;
                    }

                    int result = ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY);
                    if (result != DISP_CHANGE_SUCCESSFUL)
                    {
                        result = ChangeDisplaySettings(ref devMode, 0);
                    }
                    if (result != DISP_CHANGE_SUCCESSFUL)
                    {
                        throw new InvalidOperationException("修改分辨率失败，错误码: " + result);
                    }

                    DEVMODE current = CreateDevMode();
                    if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref current))
                    {
                        resp.CurrentWidth = current.dmPelsWidth;
                        resp.CurrentHeight = current.dmPelsHeight;
                    }
                    else
                    {
                        resp.CurrentWidth = req.Width;
                        resp.CurrentHeight = req.Height;
                    }
                    resp.Result = true;
                    resp.Message = "分辨率修改成功";
                    session.Send(ePacketType.PACKET_CHANGE_RESOLUTION_RESPONSE, resp);
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_CHANGE_RESOLUTION_RESPONSE, resp);
                }
            });
        }

        private static DEVMODE CreateDevMode()
        {
            DEVMODE devMode = new DEVMODE();
            devMode.dmDeviceName = new string(new char[32]);
            devMode.dmFormName = new string(new char[32]);
            devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            return devMode;
        }
    }
}
