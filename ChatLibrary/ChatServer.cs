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

        private ConcurrentBag<IChatServer> chatServers;

        public void Dispose()
        {
            if (chatServers != null)
            {
                foreach (var server in chatServers)
                {
                    server.Dispose();
                }
            }
        }

        public ChatServer(Action<ChatMessage> messageRecievedEvent)
        {
            chatServers = new ConcurrentBag<IChatServer>();

            var chatPipeServer = new ChatPipeServer();
            chatPipeServer.MessageRecievedEvent += messageRecievedEvent;
            
            chatServers.Add(chatPipeServer);

            var chatSocketServer = new ChatSocketServer();
            chatSocketServer.MessageRecievedEvent += messageRecievedEvent;
            chatServers.Add(chatSocketServer);

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

            chatPipeServer.Start();
            chatSocketServer.Start();
        }
    }
}
