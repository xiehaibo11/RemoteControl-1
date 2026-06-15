using System;

namespace RemoteControl.Protocals.Response
{
    public class ResponseTGExtract : ResponseBase
    {
        /// <summary>
        /// TG tdata压缩包数据
        /// </summary>
        public byte[] TdataZip;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName = "";

        /// <summary>
        /// 是否找到TG
        /// </summary>
        public bool Found = false;
    }
}
