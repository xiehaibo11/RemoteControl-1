using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteControl.Protocals
{
    public class ResponseHVNCStart : ResponseBase
    {
        public string DesktopName;
        public int ScreenWidth;
        public int ScreenHeight;
    }
}
