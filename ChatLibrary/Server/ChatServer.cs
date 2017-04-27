using System;
using System.Collections.Concurrent;
using ChatLibrary.Common;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;

namespace ChatLibrary.Server
{
    public class ChatServer : IDisposable
    {

        private readonly ConcurrentBag<IChatServer> _chatServers;

        public void Dispose()
        {
            if (_chatServers == null) return;
            foreach (var server in _chatServers)
            {
                server.SendMessageToClients(new ChatMessage() {MessageSendDate = DateTime.MinValue});
                server.Dispose();
            }
        }

        public void SendMessageToClients(ChatMessage message)
        {
            foreach (var server in _chatServers)
            {
                server.SendMessageToClients(message);
            }
        }

        public ChatServer(Action<ChatMessage> messageRecievedEvent)
        {
            _chatServers = new ConcurrentBag<IChatServer>();

            var chatPipeServer = new ChatPipeServer();
            chatPipeServer.MessageRecievedEvent += messageRecievedEvent;
            chatPipeServer.MessageRecievedEvent += SendMessageToClients;

            _chatServers.Add(chatPipeServer);

            var chatSocketServer = new ChatTcpServer();
            chatSocketServer.MessageRecievedEvent += messageRecievedEvent;
            chatSocketServer.MessageRecievedEvent += SendMessageToClients;
            _chatServers.Add(chatSocketServer);

            GlobalChatHistory.Instance.ChatMessages.Add(new ChatMessage
            {
                UserName = "Sarah Kerrigan",
                Message = "Amon will be dead.",
                MessageSendDate = DateTime.Now
            });
            GlobalChatHistory.Instance.ChatMessages.Add(new ChatMessage
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
