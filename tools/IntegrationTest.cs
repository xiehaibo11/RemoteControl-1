using System;
using System.Diagnostics;
using System.IO;

namespace IntegrationTest
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string root = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
            string script = Path.Combine(root, "tools", "Test-LocalClientSafeE2E.ps1");
            if (!File.Exists(script))
            {
                Console.Error.WriteLine("Safe E2E script not found: " + script);
                return 2;
            }

            string arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"";
            if (args != null && args.Length > 0)
                arguments += " " + string.Join(" ", QuoteArgs(args));

            Console.WriteLine("Running safe relay E2E wrapper.");
            Console.WriteLine("Script: " + script);
            Console.WriteLine("Destructive and privacy-sensitive actions are intentionally skipped.");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ResolvePowerShell();
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = root;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            using (Process process = Process.Start(startInfo))
            {
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null) Console.WriteLine(e.Data);
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null) Console.Error.WriteLine(e.Data);
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private static string FindRepositoryRoot(string start)
        {
            DirectoryInfo current = new DirectoryInfo(start);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                    File.Exists(Path.Combine(current.FullName, "RemoteControl.sln")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            return Directory.GetCurrentDirectory();
        }

        private static string ResolvePowerShell()
        {
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string x86 = Path.Combine(windir, "SysWOW64", "WindowsPowerShell", "v1.0", "powershell.exe");
            if (File.Exists(x86))
                return x86;
            return "powershell.exe";
        }

        private static string[] QuoteArgs(string[] args)
        {
            string[] quoted = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                quoted[i] = "\"" + args[i].Replace("\"", "\\\"") + "\"";
            return quoted;
        }
    }
}
