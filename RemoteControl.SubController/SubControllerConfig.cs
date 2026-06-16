using System;
using System.IO;
using Newtonsoft.Json;

namespace RemoteControl.SubController
{
    internal class SubControllerConfig
    {
        private const string ConfigFileName = "subcontroller-config.json";

        public string RelayServerIP = "";
        public int RelayServerPort = 10010;

        private static SubControllerConfig _current;

        public static SubControllerConfig Current
        {
            get
            {
                if (_current == null)
                    Load();
                return _current;
            }
        }

        private static string ConfigFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName); }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _current = JsonConvert.DeserializeObject<SubControllerConfig>(json);
                }
            }
            catch { }

            if (_current == null)
                _current = new SubControllerConfig();
        }

        public static void Save()
        {
            try
            {
                if (_current == null)
                    _current = new SubControllerConfig();
                string json = JsonConvert.SerializeObject(_current);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch { }
        }
    }
}
