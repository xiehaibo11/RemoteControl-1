using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class ResponseArchiveAll
    {
        public bool Result { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public byte[] ArchiveData { get; set; }
    }
}
