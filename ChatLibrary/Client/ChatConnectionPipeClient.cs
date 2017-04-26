using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using System.Threading.Tasks;
using ChatLibrary.Common;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;

namespace ChatLibrary.Client
{
    public class ChatConnectionPipeClient : IChatConnectionClient
    {
		public List<ChatMessage> ChatHistory { get; set; }
        private StreamObjectReader _chatMessageStreamClient;
        private StreamObjectReader _chatMessageStreamServer;
        private string _clientName;
        private NamedPipeClientStream _pipeClient;
        private NamedPipeClientStream _pipeServer;
		private event Action<ChatMessage> MessageRecievedEvent;
        private Task _chatListenerTask;
        private const int ConnectionTimeout = 1000;
        private string ClientId { get; set; }

        private string ClientPipeName => ConfigurationManager.AppSettings["clientPipeName"] + ClientId;

        private static string GreetingPipeName => ConfigurationManager.AppSettings["greetingPipeName"];

		private string ServerPipeName => ConfigurationManager.AppSettings["serverPipeName"] + ClientId;

        private static string ServerName => ConfigurationManager.AppSettings["serverName"];

		private void ChatListener()
		{
			while (_pipeServer.IsConnected)
			{
			    var newMessage = _chatMessageStreamServer.ReadMessage<ChatMessage>();
			    MessageRecievedEvent?.Invoke(newMessage);
			}
		}

		private bool ExchangeInitializationInformationWithServer()
		{
			var greetingPipe = new NamedPipeClientStream(ServerName, GreetingPipeName, PipeDirection.InOut);
            try
            {
                greetingPipe.Connect(ConnectionTimeout);
            }
            catch(TimeoutException)
            {
                return false;
            }
            if (!greetingPipe.IsConnected)
                return false;
			var firstMessage = new ChatMessage() { UserName = _clientName };

			var greetingStream = new StreamObjectReader(greetingPipe);
			greetingStream.WriteMessage(firstMessage);
			greetingPipe.WaitForPipeDrain();
            ClientId = greetingStream.ReadMessage<string>();
			ChatHistory = new List<ChatMessage>();
            while (true)
            {
                var message = greetingStream.ReadMessage<ChatMessage>();
                if (message.MessageSendDate == DateTime.MinValue)
                    break;
                ChatHistory.Add(message);
            }
			greetingPipe.Close();
            return true;
		}

		public ChatConnectionPipeClient(Action<ChatMessage> messageRecievedEvent)
		{
			MessageRecievedEvent += messageRecievedEvent;
		}

        public bool SendMessage(string message)
        {
            if (!_pipeServer.IsConnected) return false;
            _chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = _clientName, Message = message, MessageSendDate = DateTime.Now });
            return true;
        }

        public void Dispose()
        {
            _pipeClient.Close();
            _pipeServer.Close();
            _pipeClient.Dispose();
            _pipeServer.Dispose();
            _chatListenerTask.Wait();
            _chatListenerTask.Dispose();
        }

        public bool Connect(string clientName)
        {
            _clientName = clientName;

            if (!ExchangeInitializationInformationWithServer())
                return false;

            _pipeServer = new NamedPipeClientStream(ServerName, ServerPipeName, PipeDirection.In);
            _pipeServer.Connect(ConnectionTimeout);

            if (!_pipeServer.IsConnected)
                return false;

            _chatMessageStreamServer = new StreamObjectReader(_pipeServer);

            _pipeClient = new NamedPipeClientStream(ServerName, ClientPipeName, PipeDirection.Out);
            _pipeClient.Connect(ConnectionTimeout);

            if (!_pipeClient.IsConnected)
                return false;

            _chatMessageStreamClient = new StreamObjectReader(_pipeClient);

            _chatListenerTask = Task.Factory.StartNew(ChatListener);

            return true;
        }
    }
}
