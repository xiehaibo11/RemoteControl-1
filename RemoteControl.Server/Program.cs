using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using RemoteControl.Protocals;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RemoteControl.Server
{
    static class Program
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            StartupTrace.Write("Program.Main enter");
            //Win32API.keybd_event(0x11, 0, 0, 0);
            //Win32API.keybd_event(18, 0, 0, 0);
            //Win32API.keybd_event(0x2E, 0, 0, 0);
            //Win32API.keybd_event(0x11, 0, 2, 0);
            //Win32API.keybd_event(18, 0, 2, 0);
            //Win32API.keybd_event(0x2E, 0, 2, 0);

            EnsureLogDirectory();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ApplicationExit += Application_ApplicationExit;
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            try
            {
                StartupTrace.Write("Application.EnableVisualStyles");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                StartupTrace.Write("Application.Run FrmMain");
                Application.Run(new FrmMain());
                StartupTrace.Write("Application.Run returned");
            }
            catch (Exception ex)
            {
                StartupTrace.Write("Application startup failed: " + ex);
                LogFatal("Application startup failed", ex);
                MessageBox.Show("总控制端启动失败，详情请查看安装目录 Log\\log.txt。\r\n\r\n" + ex.Message,
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void EnsureLogDirectory()
        {
            try
            {
                string logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);
            }
            catch
            {
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            StartupTrace.Write("Application.ThreadException: " + e.Exception);
            LogFatal("UI thread exception", e.Exception);
            MessageBox.Show("总控制端运行异常，详情请查看安装目录 Log\\log.txt。\r\n\r\n" + e.Exception.Message,
                "运行异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            StartupTrace.Write("Application.ApplicationExit");
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            StartupTrace.Write("AppDomain.ProcessExit");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            StartupTrace.Write("UnhandledException: " + (ex == null ? e.ExceptionObject.ToString() : ex.ToString()));
            LogFatal("Unhandled exception", ex);
        }

        private static void LogFatal(string message, Exception ex)
        {
            try
            {
                if (ex != null)
                    Logger.Fatal(message, ex);
                else
                    Logger.Fatal(message);
            }
            catch
            {
            }
        }
    }
}
