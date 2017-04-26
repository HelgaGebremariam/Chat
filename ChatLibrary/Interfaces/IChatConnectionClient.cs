using System;
using System.Collections.Generic;
using ChatLibrary.Models;

namespace ChatLibrary.Interfaces
{
    public interface IChatConnectionClient : IDisposable
    {
        bool Connect(string clientName);
        bool SendMessage(string message);
        List<ChatMessage> ChatHistory { get; set; }
    }
}
