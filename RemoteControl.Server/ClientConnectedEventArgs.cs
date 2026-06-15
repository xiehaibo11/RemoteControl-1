using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    class ClientConnectedEventArgs : EventArgs
    {
        public ClientConnectedEventArgs(SocketSession client)
        {
            this.Client = client;
        }

        public SocketSession Client { get; private set; }
    }

    class ClientListChangedEventArgs : EventArgs
    {
        public ClientListChangedEventArgs(List<SocketSession> addedClients, List<SocketSession> removedClients)
            : this(addedClients, removedClients, null)
        {
        }

        public ClientListChangedEventArgs(List<SocketSession> addedClients, List<SocketSession> removedClients, List<SocketSession> updatedClients)
        {
            this.AddedClients = addedClients ?? new List<SocketSession>();
            this.RemovedClients = removedClients ?? new List<SocketSession>();
            this.UpdatedClients = updatedClients ?? new List<SocketSession>();
        }

        public List<SocketSession> AddedClients { get; private set; }
        public List<SocketSession> RemovedClients { get; private set; }
        public List<SocketSession> UpdatedClients { get; private set; }
    }
}
