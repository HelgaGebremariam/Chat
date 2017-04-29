using System;
using System.Collections.Generic;
using ChatLibrary.Models;
using System.Threading.Tasks;

namespace ChatLibrary.Interfaces
{
    public interface IChatConnectionClient : IDisposable
    {
        bool Connect(string clientName);
        Task StartListening();
        bool SendMessage(string message);
        List<ChatMessage> ChatHistory { get; set; }
    }
}
