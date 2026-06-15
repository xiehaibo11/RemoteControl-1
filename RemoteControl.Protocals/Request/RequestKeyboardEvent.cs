using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteControl.Protocals
{
    public class RequestKeyboardEvent
    {
        // KeyValue==(int)KeyCode
        // KeyData contains alt ctrl shift
        public eKeyboardOpe KeyOperation;
        public eKeyboardKeys KeyCode;
        public eKeyboardKeys KeyData;
        public int KeyValue;
    }
}
