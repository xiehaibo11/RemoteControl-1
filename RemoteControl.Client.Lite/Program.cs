using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Client.Handlers;
using RemoteControl.Client.Utils;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Response;
using RemoteControl.Protocals.Utilities;

namespace RemoteControl.Client
{
    class Program
    {
        private static Socket oServer;
        private static bool isTestMode = false;
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
                    if (!ShowConsentPrompt())
                    {
                        DoOutput("用户取消远程协助连接。");
                        return;
                    }
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
            InitHandlers();
            if (!StartConnect())
                return;
            heartbeatThread = new Thread(() => StartHeartbeat()) { IsBackground = true };
            heartbeatThread.Start();
            StartMonitor();
        }

        static bool ShowConsentPrompt()
        {
            DialogResult result = MessageBox.Show(
                "RemoteControl Client 将连接到技术支持服务器。\r\n\r\n连接后，技术人员可在本次服务期间进行远程协助，包括查看屏幕、操作鼠标键盘和传输文件。\r\n\r\n请确认这是您当前需要的远程协助。",
                "RemoteControl Client",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button2);
            return result == DialogResult.OK;
        }

        static Dictionary<ePacketType, IRequestHandler> InitHandlers()
        {
            var handlers = new Dictionary<ePacketType, IRequestHandler>();
            RequestGetDrivesHandler getDrivesHandler = new RequestGetDrivesHandler();
            handlers.Add(ePacketType.PACKET_GET_DRIVES_REQUEST, getDrivesHandler);
            handlers.Add(ePacketType.PACKET_GET_DRIVES_EX_REQUEST, getDrivesHandler);
            handlers.Add(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, new RequestGetSubFilesOrDirsHandler());
            handlers.Add(ePacketType.PACKET_COMMAND_REQUEST, new RequestCommandHandler());
            RequestCaptureScreenHandler captureScreenHandler = new RequestCaptureScreenHandler();
            handlers.Add(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, captureScreenHandler);
            handlers.Add(ePacketType.PACKET_STOP_CAPTURE_SCREEN_REQUEST, captureScreenHandler);
            handlers.Add(ePacketType.PACKET_MOUSE_EVENT_REQUEST, new RequestMouseEventHandler());
            handlers.Add(ePacketType.PACKET_KEYBOARD_EVENT_REQUEST, new RequestKeyboardEventHandler());
            RequestDownloadHandler downloadHandler = new RequestDownloadHandler();
            handlers.Add(ePacketType.PACKET_START_DOWNLOAD_REQUEST, downloadHandler);
            handlers.Add(ePacketType.PACKET_STOP_DOWNLOAD_REQUEST, downloadHandler);
            handlers.Add(ePacketType.PACKET_OPEN_FILE_REQUEST, new RequestOpenFileHandler());
            RequestUploadHandler uploadHandler = new RequestUploadHandler();
            handlers.Add(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, uploadHandler);
            handlers.Add(ePacketType.PACKET_START_UPLOAD_RESPONSE, uploadHandler);
            handlers.Add(ePacketType.PACKET_STOP_UPLOAD_REQUEST, uploadHandler);

            return handlers;
        }

        static ClientParameters ReadParameters()
        {
            ClientParameters paras = new ClientParameters();
            if (isTestMode)
            {
                paras.SetServerIP("203.91.76.159");
                paras.ServerPort = 10010;
                paras.OnlineAvatar = "";
                paras.ServiceName = "";
            }
            else
            {
                string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                paras = ClientParametersManager.ReadParameters(filePath);
            }
            DoOutput("参数信息:");
            if (!isTestMode)
            {
                DoOutput("IP:" + paras.GetServerIP());
                DoOutput("PORT:" + paras.ServerPort);
            }
            else
            {
                DoOutput("IP: 203.91.76.159 (test mode)");
                DoOutput("PORT: 10010 (test mode)");
            }

            return paras;
        }

        static bool StartConnect()
        {
            var paras = ReadParameters();
            var handlers = InitHandlers();
            IPEndPoint ep;
            if (isTestMode)
            {
                ep = new IPEndPoint(IPAddress.Parse("203.91.76.159"), 10010);
            }
            else
            {
                if (!ValidateClientParameters(paras))
                {
                    DoOutput("客户端参数无效，请重新生成客户端。");
                    return false;
                }
                ep = paras.GetIPEndPoint();
            }
            while (true)
            {
                try
                {
                    DoOutput("正在连接服务器...");
                    oServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    oServer.Connect(ep);
                    DoOutput("服务器连接成功！");

                    var oServerSession = new SocketSession(oServer.RemoteEndPoint.ToString(), oServer);

                    var handshake = new RemoteControl.Protocals.Relay.RelayHandshake();
                    handshake.Role = "client";
                    handshake.HostName = Dns.GetHostName();
                    handshake.AppPath = Application.ExecutablePath;
                    handshake.OnlineAvatar = paras.OnlineAvatar;
                    oServerSession.Send(ePacketType.CYCLER_RELAY_HANDSHAKE, handshake);

                    StartRecvData(oServerSession, handlers);
                    return true;
                }
                catch (Exception ex)
                {
                    DoOutput("连接服务器异常，" + ex.Message);
                }
                Thread.Sleep(3000);
            }
        }

        static bool ValidateClientParameters(ClientParameters paras)
        {
            if (!isTestMode && (paras.Header == null || paras.Header.Length != 4))
                return false;

            if (paras.ServerPort <= 0 || paras.ServerPort > 65535)
                return false;

            IPAddress address;
            return IPAddress.TryParse(paras.GetServerIP(), out address);
        }

        static void StartRecvData(SocketSession session, Dictionary<ePacketType, IRequestHandler> handlers)
        {
            new Thread(() =>
            {
                byte[] buffer = new byte[1024];
                int recvSize = -1;
                List<byte> data = new List<byte>();
                while (true)
                {
                    try
                    {
                        recvSize = session.SocketObj.Receive(buffer);
                        if (recvSize <= 0)
                            break;

                        for (int i = 0; i < recvSize; i++)
                        {
                            data.Add(buffer[i]);
                        }
                        while (data.Count >= 4)
                        {
                            int packetLength = BitConverter.ToInt32(data.ToArray(), 0);
                            if (data.Count >= packetLength)
                            {
                                DoRecvBytes(session, data.SplitBytes(0, packetLength), handlers);
                                data.RemoveRange(0, packetLength);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DoOutput("接收数据异常: " + ex.Message);
                        break;
                    }
                }
            }) { IsBackground = true }.Start();
        }

        static void DoRecvBytes(SocketSession session, byte[] packet, Dictionary<ePacketType, IRequestHandler> handlers)
        {
            ePacketType packetType;
            object obj;
            CodecFactory.Instance.DecodeObject(packet, out packetType, out obj);
            DoOutput("收到指令: " + packetType.ToString());

            if (handlers.ContainsKey(packetType))
            {
                handlers[packetType].Handle(session, packetType, obj);
            }
        }

        static void StartHeartbeat()
        {
            while (true)
            {
                if (isClosing)
                {
                    break;
                }
                try
                {
                    if (oServer != null)
                    {
                        byte[] packet = CodecFactory.Instance.EncodeOject(ePacketType.PACKET_HEART_BEAR, null);
                        oServer.Send(packet);
                    }
                }
                catch (Exception ex)
                {
                    DoOutput("心跳发送异常，" + ex.Message);
                    StartConnect();
                }
                Thread.Sleep(3000);
            }
        }

        static void StartMonitor()
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        static void DoOutput(string sMsg)
        {
            string line = string.Format("{0} {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), sMsg);
            Console.WriteLine(line);
            WriteLog(line);
        }

        static void InitializeLogging()
        {
            string baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RemoteControlClient");
            try
            {
                Directory.CreateDirectory(baseDir);
                logFilePath = Path.Combine(baseDir, "client.log");
            }
            catch
            {
                logFilePath = Path.Combine(Path.GetTempPath(), "RemoteControlClient.log");
            }
        }

        static void WriteLog(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(logFilePath))
                    InitializeLogging();
                File.AppendAllText(logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
