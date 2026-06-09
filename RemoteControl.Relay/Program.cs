using System;
using System.Net;

namespace RemoteControl.Relay
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 10010;
            if (args.Length > 0 && int.TryParse(args[0], out int p))
            {
                port = p;
            }

            Console.WriteLine("=== RemoteControl Relay Server ===");
            Console.WriteLine($"监听端口: {port}");

            var server = new RelayServer(port);
            server.Start();

            Console.WriteLine("服务已启动，按 Ctrl+C 退出...");
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                server.Stop();
            };

            // 保持运行
            while (server.IsRunning)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
