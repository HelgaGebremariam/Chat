using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLibrary.Models;

namespace ChatLibrary.Interfaces
{
    public interface IChatServer : IDisposable
    {
        ConcurrentBag<ChatClient> ChatClients { get; set; }
        event Action<ChatMessage> MessageRecievedEvent;
        void Connect();
        void ListenNewClient();
        void SendMessageToClients(ChatMessage message);

    }
}
