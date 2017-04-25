using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;
using ChatLibrary.Models;
using ChatLibrary.Interfaces;
using System.Runtime.Remoting;

namespace ChatLibrary
{
    public class ChatConnectionPipeClient : IChatConnectionClient
    {
		public List<ChatMessage> ChatHistory { get; set; }
        private StreamObjectReader chatMessageStreamClient;
        private StreamObjectReader chatMessageStreamServer;
        private string clientName;
        NamedPipeClientStream pipeClient;
        NamedPipeClientStream pipeServer;
		private event Action<ChatMessage> messageRecievedEvent;
        private Task chatListenerTask;
        private int connectionTimeout = 1000;
        private string clientId { get; set; }

        private string clientPipeName => ConfigurationManager.AppSettings["clientPipeName"] + clientId;

        private string greetingPipeName => ConfigurationManager.AppSettings["greetingPipeName"];

		private string serverPipeName => ConfigurationManager.AppSettings["serverPipeName"] + clientId;

        private string serverName => ConfigurationManager.AppSettings["serverName"];

		private void ChatListener()
		{
			while (pipeServer.IsConnected)
			{
				var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
				messageRecievedEvent(newMessage);
			}
		}

		private bool Greet()
		{
			NamedPipeClientStream greetingPipe = new NamedPipeClientStream(serverName, greetingPipeName, PipeDirection.InOut);
            try
            {
                greetingPipe.Connect(connectionTimeout);
            }
            catch(TimeoutException)
            {
                return false;
            }
            if (!greetingPipe.IsConnected)
                return false;
			ChatMessage firstMessage = new ChatMessage() { UserName = clientName };

			StreamObjectReader greetingStream = new StreamObjectReader(greetingPipe);
			greetingStream.WriteMessage(firstMessage);
			greetingPipe.WaitForPipeDrain();
            clientId = greetingStream.ReadMessage<string>();
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

		public ChatConnectionPipeClient(string clientName, Action<ChatMessage> messageRecievedEvent)
		{
			this.messageRecievedEvent += messageRecievedEvent;
			this.clientName = clientName;
            if (!Greet())
                throw new ServerException();
            pipeServer = new NamedPipeClientStream(serverName, serverPipeName, PipeDirection.In);
            pipeServer.Connect(connectionTimeout);
            if (!pipeServer.IsConnected)
                throw new ServerException();
            chatMessageStreamServer = new StreamObjectReader(pipeServer);

            pipeClient = new NamedPipeClientStream(serverName, clientPipeName, PipeDirection.Out);
            pipeClient.Connect(connectionTimeout);
            if (!pipeClient.IsConnected)
                throw new ServerException();
            chatMessageStreamClient = new StreamObjectReader(pipeClient);

            chatListenerTask = Task.Factory.StartNew(() => { ChatListener(); });
		}

        public bool SendMessage(string message)
        {
            if (pipeServer.IsConnected)
            {
                chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = clientName, Message = message, MessageSendDate = DateTime.Now });
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            pipeClient.Close();
            pipeServer.Close();
            pipeClient.Dispose();
            pipeServer.Dispose();
            chatListenerTask.Wait();
        }

        public bool Connect()
        {
            throw new NotImplementedException();
        }
    }
}
