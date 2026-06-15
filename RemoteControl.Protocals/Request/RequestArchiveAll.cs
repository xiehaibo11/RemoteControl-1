using System;

namespace RemoteControl.Protocals.Request
{
    public class RequestArchiveAll
    {
        /// <summary>
        /// 归档模式: 0=全部(TG+密码+键盘记录), 1=仅TG, 2=仅密码, 3=仅键盘记录
        /// </summary>
        public int Mode { get; set; }
    }
}
