using System;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Threading;
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
        public EventWaitHandle ClientEventWaitHandle;
        public void Dispose()
        {
            IsActive = false;
            ClientPipe?.Dispose();
            ClientSocket?.Dispose();
            ClientEventWaitHandle?.Dispose();
        }
    }
}
