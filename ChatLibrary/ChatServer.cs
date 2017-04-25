using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Collections.Concurrent;
using System.Net.Sockets;
using ChatLibrary.Models;
using System.Net;
using ChatLibrary.Interfaces;

namespace ChatLibrary
{
    public class ChatServer : IDisposable
    {
        private ConcurrentBag<Task> serverTasks;

        private IChatServer chatPipeServer;

        public void Dispose()
        {
            foreach(var task in serverTasks)
            {
                task.Dispose();
            }
        }

        public ChatServer(Action<ChatMessage> messageRecievedEvent)
        {

            chatPipeServer = new ChatPipeServer();
            chatPipeServer.MessageRecievedEvent += messageRecievedEvent;

            ChatHistory.Instance.ChatMessages.Add(new ChatMessage()
            {
                UserName = "Sarah Kerrigan",
                Message = "Amon will be dead.",
                MessageSendDate = DateTime.Now
            });
            ChatHistory.Instance.ChatMessages.Add(new ChatMessage()
            {
                UserName = "James Raynor",
                Message = "Yeeeah, absolutely!",
                MessageSendDate = DateTime.Now
            });

            Task.Factory.StartNew(() =>
            {
                chatPipeServer.ListenNewClient();
            });

        }
    }
}
