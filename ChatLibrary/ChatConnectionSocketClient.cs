using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Remoting;

namespace ChatLibrary
{
    public class ChatConnectionSocketClient : IChatConnectionClient
    {
        private string _clientName;
        private event Action<ChatMessage> MessageRecievedEvent;

        private EventWaitHandle _messageReceived;
        private Task _chatListenerTask;
        private volatile bool _isTimeToFinish = false;

        public List<ChatMessage> ChatHistory { get; set; }


        private SocketSettings _socketSettings;

        private static string ServerName => ConfigurationManager.AppSettings["serverName"];

        private static IEnumerable<int> GreetingSocketPorts => ConfigurationManager.AppSettings["greetingSocketPorts"].Split(',').Select(s=>Convert.ToInt32(s)).ToList();

        private bool ExchangeInitializationInformationWithServer()
        {
            try
            {
                using (var greetingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    foreach (var greetingPort in GreetingSocketPorts)
                    {
                        greetingSocket.Connect(ServerName, greetingPort);
                        if (greetingSocket.Connected)
                            break;
                    }

                    if (!greetingSocket.Connected)
                        return false;

                    var firstMessage = new ChatMessage() { UserName = _clientName };
                    using (var greetingSocketStream = new NetworkStream(greetingSocket))
                    {
                        var greetingStream = new StreamObjectReader(greetingSocketStream);
                        greetingStream.WriteMessage(firstMessage);
                        greetingStream.ReadMessage<string>();
                        _socketSettings = greetingStream.ReadMessage<SocketSettings>();
                        ChatHistory = new List<ChatMessage>();
                        while (true)
                        {
                            var message = greetingStream.ReadMessage<ChatMessage>();
                            if (message.MessageSendDate == DateTime.MinValue)
                                break;
                            ChatHistory.Add(message);
                        }
                        greetingSocket.Close();
                    }
                }
                return true;
            }
            catch(SocketException)
            {
                return false;
            }
            
        }

        private void ChatListener()
        {
            while(!_isTimeToFinish)
            {
                if (!_messageReceived.WaitOne(100)) continue;
                var serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Connect(ServerName, _socketSettings.ServerSocketPort);
                if (!serverSocket.Connected)
                    throw new ServerException();
                var chatMessageStreamServer = new StreamObjectReader(new NetworkStream(serverSocket));
                var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
                MessageRecievedEvent?.Invoke(newMessage);
            }
        }

        public ChatConnectionSocketClient(Action<ChatMessage> messageRecievedEvent)
        {
            this.MessageRecievedEvent += messageRecievedEvent;
        }

        public void Dispose()
        {
            _messageReceived.Dispose();
            _isTimeToFinish = true;
            _chatListenerTask.Wait();
            _chatListenerTask.Dispose();
        }

        public bool SendMessage(string message)
        {
            try
            {
                var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(ServerName, _socketSettings.ClientSocketPort);
                if (!clientSocket.Connected)
                    return false;
                var chatMessageStreamClient = new StreamObjectReader(new NetworkStream(clientSocket));
                chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = _clientName, Message = message, MessageSendDate = DateTime.Now });
                clientSocket.Dispose();
                return true;
            }
            catch(SocketException)
            {
                return false;
            }

        }

        public bool Connect(string clientName)
        {
            this._clientName = clientName;
            if (!ExchangeInitializationInformationWithServer())
                return false;
            _messageReceived = EventWaitHandle.OpenExisting(_socketSettings.EventWaitHandleEventName);
            _chatListenerTask = Task.Factory.StartNew(ChatListener);
            return true;
        }
    }
}
