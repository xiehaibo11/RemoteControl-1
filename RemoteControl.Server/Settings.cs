using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace RemoteControl.Server
{
    class Settings
    {
        const string SettingFileName = "config.json";
        public static Settings CurrentSettings = new Settings(); 
        public ClientParas ClientPara = new ClientParas();
        public int ServerPort;
        public string SkinPath;
        public bool EnableLegacySkin = false;
        public string RelayServerIP = "";
        public int RelayServerPort = 10010;

        // 服务器配置
        public string ListenAddress = "0.0.0.0";

        // 性能配置
        public int WorkerLoops = 0;         // 0=自动(CPU核心数)
        public int HeavyWorkers = 4;

        // 屏幕传输
        public int ScreenCompressionMode = 0;  // 0=JPEG+ZSTD, 1=PNG, 2=RAW
        public int ScreenFps = 15;

        // 终端字体
        public string PreferredFont = "Sarasa Mono SC";

        // 文件传输
        public bool FileAutoSave = false;
        public string FileDownloadDir = "";
        public bool FileSkipLocked = true;
        public bool FileAutoDecompress = true;
        public int LargeFileThresholdMB = 1024;

        // 审计保留
        public int AuditRetentionDays = 30;

        // 显示选项
        public bool ShowProtocolVersion = false;
        public bool ShowClientVersion = false;

        private static string BaseDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private static string SettingFilePath
        {
            get { return System.IO.Path.Combine(BaseDir, SettingFileName); }
        }

        static Settings()
        {
            try
            {
                string json = System.IO.File.ReadAllText(SettingFilePath);
                Settings.CurrentSettings = JsonConvert.DeserializeObject<Settings>(json);
                EnsureDefaults();
                ResolveRelativePaths();
            }
            catch (Exception)
            {
                EnsureDefaults();
            }
        }

        private static void EnsureDefaults()
        {
            if (CurrentSettings == null)
                CurrentSettings = new Settings();
            if (CurrentSettings.ClientPara == null)
                CurrentSettings.ClientPara = new ClientParas();
            if (CurrentSettings.RelayServerPort <= 0)
                CurrentSettings.RelayServerPort = 10010;
        }

        private static void ResolveRelativePaths()
        {
            if (CurrentSettings == null)
                return;
            CurrentSettings.SkinPath = ResolveSkinPath(CurrentSettings.SkinPath);
            if (CurrentSettings.ClientPara != null)
            {
                CurrentSettings.ClientPara.ClientIconPath =
                    ResolvePath(CurrentSettings.ClientPara.ClientIconPath);
            }
        }

        private static string ResolveSkinPath(string path)
        {
            string resolved = ResolvePath(path);
            if (!string.IsNullOrEmpty(resolved) && System.IO.File.Exists(resolved))
                return resolved;

            string skinFile = ResolveSkinFile(path);
            if (!string.IsNullOrEmpty(skinFile))
                return skinFile;

            return resolved;
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            // 如果路径存在则直接返回
            if (System.IO.File.Exists(path))
                return path;
            // 如果是相对路径，解析为相对于应用目录
            if (!System.IO.Path.IsPathRooted(path))
            {
                string resolved = System.IO.Path.Combine(BaseDir, path);
                if (System.IO.File.Exists(resolved))
                    return resolved;
            }
            // 绝对路径不存在时，尝试取文件名在应用目录查找
            string fileName = System.IO.Path.GetFileName(path);
            string fallback = System.IO.Path.Combine(BaseDir, fileName);
            if (System.IO.File.Exists(fallback))
                return fallback;
            return path;
        }

        private static string ResolveSkinFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            string skinRoot = System.IO.Path.Combine(BaseDir, "Skins");
            if (!System.IO.Directory.Exists(skinRoot))
                return null;

            string fileName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
                return null;

            string familyName = null;
            try
            {
                string parent = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent))
                    familyName = System.IO.Path.GetFileName(parent);
            }
            catch
            {
                familyName = null;
            }

            if (!string.IsNullOrEmpty(familyName))
            {
                string familyCandidate = System.IO.Path.Combine(skinRoot, familyName, fileName);
                if (System.IO.File.Exists(familyCandidate))
                    return familyCandidate;
            }

            try
            {
                string[] matches = System.IO.Directory.GetFiles(
                    skinRoot,
                    fileName,
                    System.IO.SearchOption.AllDirectories);
                if (matches.Length > 0)
                    return matches[0];
            }
            catch
            {
                return null;
            }

            return null;
        }

        public static void SaveSettings()
        {
            if (Settings.CurrentSettings == null)
                return;
            string json = JsonConvert.SerializeObject(Settings.CurrentSettings);
            string dir = System.IO.Path.GetDirectoryName(SettingFilePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(SettingFilePath, json);
        }
    }

    class ClientParas
    {
        public string ServerIP;
        public int ServerPort;
        public string ServiceName;
        public string OnlineAvatar;
        public bool IsHide;
        public string ClientIconPath;
    }
}
