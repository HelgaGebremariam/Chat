using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ChatLibrary.Models
{
    public class ChatClient : IDisposable
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public NamedPipeServerStream ClientPipe { get; set; }
        public Socket ClientSocket { get; set; }
        public Task ListenerTask { get; set; }
        public bool IsActive { get; set; }
        public EventWaitHandle clientEventWaitHandle;
        public void Dispose()
        {
            IsActive = false;
            if (ClientPipe != null)
                ClientPipe.Dispose();
            if(ClientSocket != null)
                ClientSocket.Dispose();
            if (clientEventWaitHandle != null)
                clientEventWaitHandle.Dispose();
        }
    }
}
