using System;

namespace RemoteControl.Protocals.Response
{
    public class ResponseChangeResolution : ResponseBase
    {
        public int PreviousWidth;
        public int PreviousHeight;
        public int CurrentWidth;
        public int CurrentHeight;
    }
}
