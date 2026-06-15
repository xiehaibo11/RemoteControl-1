using System;
using System.IO;
using System.Text;

namespace RemoteControl.Client
{
    partial class Program
    {
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
