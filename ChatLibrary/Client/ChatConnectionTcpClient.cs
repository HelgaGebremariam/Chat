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
    public class ChatConnectionTcpClient : IChatConnectionClient
    {
        private string _clientName;
        private event Action<ChatMessage> MessageRecievedEvent;
        private volatile bool _isConnected = false;
        public List<ChatMessage> ChatHistory { get; set; }

		private TcpClient _tcpClient;

        private static string ServerName => ConfigurationManager.AppSettings["serverName"];

        private static IEnumerable<int> ServerSocketPorts => ConfigurationManager.AppSettings["serverSocketPorts"].Split(',').Select(s=>Convert.ToInt32(s)).ToList();

        private bool ExchangeInitializationInformationWithServer()
        {
            try
            {
                var firstMessage = new ChatMessage() { UserName = _clientName };
                var greetingStream = new StreamObjectReader(_tcpClient.GetStream());
                greetingStream.WriteMessage(firstMessage);
                greetingStream.ReadMessage<string>();
                ChatHistory = new List<ChatMessage>();
                while (true)
                {
                    var message = greetingStream.ReadMessage<ChatMessage>();
                    if (message.MessageSendDate == DateTime.MinValue)
                        break;
                    ChatHistory.Add(message);
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
            var chatMessageStreamServer = new StreamObjectReader(_tcpClient.GetStream());
            while (_isConnected)
            {
                var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
                if (newMessage.MessageSendDate == DateTime.MinValue)
                {
                    return;
                }
                MessageRecievedEvent?.Invoke(newMessage);
            }
        }

        public ChatConnectionTcpClient(Action<ChatMessage> messageRecievedEvent)
        {
            this.MessageRecievedEvent += messageRecievedEvent;
        }

        public void Dispose()
        {
            _isConnected = false;
        }

        public bool SendMessage(string message)
        {
            try
            {
                if (!_tcpClient.Connected)
                    return false;
                var chatMessageStreamClient = new StreamObjectReader(_tcpClient.GetStream());
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
            _isConnected = false;
            this._clientName = clientName;
			_tcpClient = new TcpClient();

			foreach (var serverPort in ServerSocketPorts)
			{
				_tcpClient.Connect(ServerName, serverPort);
				if (_tcpClient.Connected)
					break;
			}

			if (!_tcpClient.Connected)
                return _isConnected;

            if (!ExchangeInitializationInformationWithServer())
                return _isConnected;

            _isConnected = true;
            return _isConnected;
        }

        public Task StartListening()
        {
            if (!_isConnected)
                throw new Exception("Not connected");
            return Task.Factory.StartNew(ChatListener);
        }
    }
}
