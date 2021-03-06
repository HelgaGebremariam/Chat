﻿using System;
using System.Collections.Concurrent;
using ChatLibrary.Models;

namespace ChatLibrary.Interfaces
{
    public interface IChatServer : IDisposable
    {
        ConcurrentBag<ChatClient> ChatClients { get; set; }
        event Action<ChatMessage> MessageRecievedEvent;
        void Start();
        void ListenNewClient();
        void SendMessageToClients(ChatMessage message);

    }
}
