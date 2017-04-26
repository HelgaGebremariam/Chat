using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using ChatLibrary.Common;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;

namespace ChatLibrary.Client
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
                using (var greetingSocket = new TcpClient())
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
                    var greetingStream = new StreamObjectReader(greetingSocket.GetStream());
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
                return true;
            }
            catch(SocketException se)
            {
                return false;
            }
            
        }

        private void ChatListener()
        {
            while(!_isTimeToFinish)
            {
                if (!_messageReceived.WaitOne(100)) continue;
                var serverSocket = new TcpClient();
                serverSocket.Connect(ServerName, _socketSettings.ServerSocketPort);
                if (!serverSocket.Connected)
                    throw new ServerException();
                var chatMessageStreamServer = new StreamObjectReader(serverSocket.GetStream());
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
                var clientSocket = new TcpClient();
                clientSocket.Connect(ServerName, _socketSettings.ClientSocketPort);
                if (!clientSocket.Connected)
                    return false;
                var chatMessageStreamClient = new StreamObjectReader(clientSocket.GetStream());
                chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = _clientName, Message = message, MessageSendDate = DateTime.Now });
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
