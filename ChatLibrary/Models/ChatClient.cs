using System;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Net.Sockets;

namespace ChatLibrary.Models
{
    public class ChatClient : IDisposable
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public NamedPipeServerStream ClientPipe { get; set; }
        public Task ListenerTask { get; set; }
        public bool IsActive { get; set; }

		public TcpClient ClientTcp;

        public void Dispose()
        {
            IsActive = false;
            ClientPipe?.Dispose();
		}
    }
}
