using System.Drawing;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client
{
    class MouseOpeUtil
    {
        public static void MouseMove(int x, int y)
        {
            Win32API.SetCursorPos(x, y);
        }

        public static void MouseMove(Point p)
        {
            MouseMove(p.X, p.Y);
        }

        public static void MouseDown(eMouseButtons button, Point p)
        {
            int mouseButton = GetMouseDownFlag(button);
            if (mouseButton == 0)
                return;

            MouseMove(p);
            Win32API.mouse_event(Win32API.MOUSEEVENTF_ABSOLUTE | mouseButton, p.X, p.Y, 0, 0);
        }

        public static void MouseUp(eMouseButtons button, Point p)
        {
            int mouseButton = GetMouseUpFlag(button);
            if (mouseButton == 0)
                return;

            MouseMove(p);
            Win32API.mouse_event(Win32API.MOUSEEVENTF_ABSOLUTE | mouseButton, p.X, p.Y, 0, 0);
        }

        public static void MousePress(eMouseButtons button, Point p)
        {
            MouseDown(button, p);
            MouseUp(button, p);
        }

        public static void MouseDoubleClick(eMouseButtons button, Point p)
        {
            MousePress(button, p);
            MousePress(button, p);
        }

        private static int GetMouseDownFlag(eMouseButtons button)
        {
            if (button == eMouseButtons.Left)
                return Win32API.MOUSEEVENTF_LEFTDOWN;
            if (button == eMouseButtons.Middle)
                return Win32API.MOUSEEVENTF_MIDDLEDOWN;
            if (button == eMouseButtons.Right)
                return Win32API.MOUSEEVENTF_RIGHTDOWN;
            return 0;
        }

        private static int GetMouseUpFlag(eMouseButtons button)
        {
            if (button == eMouseButtons.Left)
                return Win32API.MOUSEEVENTF_LEFTUP;
            if (button == eMouseButtons.Middle)
                return Win32API.MOUSEEVENTF_MIDDLEUP;
            if (button == eMouseButtons.Right)
                return Win32API.MOUSEEVENTF_RIGHTUP;
            return 0;
        }
    }
}
