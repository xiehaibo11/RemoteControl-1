using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestGetNetworkConnectionsHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            RunTaskThread(() =>
            {
                var resp = new ResponseGetNetworkConnections();
                resp.Connections = new List<NetworkConnectionInfo>();
                resp.CollectedAt = DateTime.Now.ToString("HH:mm:ss");

                try
                {
                    var req = reqObj as RequestGetNetworkConnections;
                    string filter = req == null ? null : req.Filter;

                    // 使用 netstat 获取TCP连接
                    var tcpConnList = GetActiveTcpConnections(filter);
                    resp.Connections.AddRange(tcpConnList);
                    resp.TcpCount = tcpConnList.Count;

                    if (req != null && req.IncludeUDP)
                    {
                        var udpConnList = GetActiveUdpListeners(filter);
                        resp.Connections.AddRange(udpConnList);
                        resp.UdpCount = udpConnList.Count;
                    }

                    resp.Result = true;
                }
                catch (Exception ex)
                {
                    resp.Result = false;
                    resp.Message = ex.Message;
                }

                session.Send(ePacketType.PACKET_GET_NETWORK_CONNECTIONS_RESPONSE, resp);
            });
        }

        private List<NetworkConnectionInfo> GetActiveTcpConnections(string filter)
        {
            var result = new List<NetworkConnectionInfo>();

            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                proc.StandardInput.WriteLine("netstat -ano -p tcp");
                proc.StandardInput.Close();

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("TCP")) continue;

                    var cols = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cols.Length < 4) continue;

                    var info = ParseConnectionLine(cols, "TCP");
                    if (info != null)
                    {
                        if (string.IsNullOrEmpty(filter) ||
                            info.LocalAddress.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            info.RemoteAddress.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            info.ProcessName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            result.Add(info);
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        private List<NetworkConnectionInfo> GetActiveUdpListeners(string filter)
        {
            var result = new List<NetworkConnectionInfo>();

            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                proc.StandardInput.WriteLine("netstat -ano -p udp");
                proc.StandardInput.Close();

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("UDP")) continue;

                    var cols = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cols.Length < 3) continue;

                    var info = ParseConnectionLine(cols, "UDP");
                    if (info != null)
                    {
                        info.Status = "";
                        if (string.IsNullOrEmpty(filter) ||
                            info.LocalAddress.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            info.ProcessName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            result.Add(info);
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        private NetworkConnectionInfo ParseConnectionLine(string[] cols, string protocol)
        {
            try
            {
                var info = new NetworkConnectionInfo();
                info.Protocol = protocol;

                ParseAddressPort(cols[1], out info.LocalAddress, out info.LocalPort);

                if (cols.Length > 2 && protocol == "TCP")
                {
                    ParseAddressPort(cols[2], out info.RemoteAddress, out info.RemotePort);
                }

                if (protocol == "TCP" && cols.Length > 3)
                {
                    info.Status = cols[3];
                    if (cols.Length > 4)
                        int.TryParse(cols[4], out info.ProcessId);
                }
                else if (protocol == "UDP")
                {
                    if (cols.Length > 2)
                        int.TryParse(cols[2], out info.ProcessId);
                }

                if (info.ProcessId > 0)
                {
                    try
                    {
                        using (var p = Process.GetProcessById(info.ProcessId))
                        {
                            info.ProcessName = p.ProcessName + ".exe";
                        }
                    }
                    catch { }
                }

                return info;
            }
            catch { return null; }
        }

        private void ParseAddressPort(string addrPort, out string address, out int port)
        {
            address = "";
            port = 0;
            int lastColon = addrPort.LastIndexOf(':');
            if (lastColon < 0) return;

            address = addrPort.Substring(0, lastColon);
            int p;
            if (int.TryParse(addrPort.Substring(lastColon + 1), out p))
                port = p;
        }
    }
}
