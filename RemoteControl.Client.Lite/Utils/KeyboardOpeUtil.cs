using RemoteControl.Protocals;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client
{
    class KeyboardOpeUtil
    {
        public static void KeyOpe(eKeyboardKeys key, eKeyboardOpe operation)
        {
            Win32API.keybd_event((byte)key, 0, operation == eKeyboardOpe.KeyDown ? 0 : 2, 0);
        }
    }
}
