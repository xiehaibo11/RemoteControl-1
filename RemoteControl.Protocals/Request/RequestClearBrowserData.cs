using System;

namespace RemoteControl.Protocals.Request
{
    public enum eBrowserType
    {
        IE = 0,
        Chrome,
        Firefox,
        Skype,
        Browser360,
        QQ,
        Sogou
    }

    public class RequestClearBrowserData
    {
        public eBrowserType BrowserType;
    }
}
