using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteControl.Relay
{
    public partial class RelayServer
    {
        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (IsRunning && !token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(30000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                var deadClients = new List<string>();
                foreach (var kv in _clients)
                {
                    if (!kv.Value.IsConnected)
                        deadClients.Add(kv.Key);
                }

                foreach (string id in deadClients)
                {
                    ClientSession session;
                    if (_clients.TryRemove(id, out session))
                    {
                        session.Close();
                        await NotifyControllersClientOfflineAsync(id);
                    }
                }

                var deadControllers = new List<string>();
                foreach (var kv in _controllers)
                {
                    if (!kv.Value.IsConnected)
                        deadControllers.Add(kv.Key);
                }

                foreach (string id in deadControllers)
                {
                    ClientSession session;
                    if (_controllers.TryRemove(id, out session))
                        session.Close();
                }

                Console.WriteLine("[status] clients=" + _clients.Count + " controllers=" + _controllers.Count);
            }
        }
    }
}
