using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;

namespace ChatLibrary
{
    public class ChatConnectionClient
    {
		public List<ChatMessage> ChatHistory { get; set; }
        private ChatMessageExchanger chatMessageStreamClient;
		private ChatMessageExchanger chatMessageStreamServer;
		private string clientName;
        NamedPipeClientStream pipeClient;
        NamedPipeClientStream pipeServer;
		private event Action<ChatMessage> messageRecievedEvent;

		private string clientPipeName
        {
            get
            {
                return ConfigurationManager.AppSettings["clientPipeName"] + clientName;
            }
        }

		private string greetingPipeName
		{
			get
			{
				return ConfigurationManager.AppSettings["greetingPipeName"];
			}
		}

		private string serverPipeName
        {
            get
            {
                return ConfigurationManager.AppSettings["serverPipeName"] + clientName;
            }
        }

        private string serverName
		{
			get
			{
				return ConfigurationManager.AppSettings["serverName"];
			}
		}

		public void ChatListener()
		{
			while(true)
			{
				var newMessage = chatMessageStreamServer.ReadMessage();
				messageRecievedEvent(newMessage);
			}
		}

		public void Greet()
		{
			NamedPipeClientStream greetingPipe = new NamedPipeClientStream(serverName, greetingPipeName, PipeDirection.InOut);
			greetingPipe.Connect();
			ChatMessage firstMessage = new ChatMessage() { UserName = clientName };

			ChatMessageExchanger greetingStream = new ChatMessageExchanger(greetingPipe);
			greetingStream.WriteMessage(firstMessage);
			greetingPipe.WaitForPipeDrain();
			ChatHistory = new List<ChatMessage>();
            while (ChatHistory.Count() < 100)
            {
                var message = greetingStream.ReadMessage();
                if (message.MessageSendDate == DateTime.MinValue)
                    break;
                ChatHistory.Add(message);
            }
			greetingPipe.Close();
		}

		public ChatConnectionClient(string clientName, Action<ChatMessage> messageRecievedEvent)
		{
			this.messageRecievedEvent += messageRecievedEvent;
			this.clientName = clientName;
			Greet();



			pipeServer = new NamedPipeClientStream(serverName, serverPipeName, PipeDirection.In);
			pipeServer.Connect();
			chatMessageStreamServer = new ChatMessageExchanger(pipeServer);


            pipeClient = new NamedPipeClientStream(serverName, clientPipeName, PipeDirection.Out);
            pipeClient.Connect();
            chatMessageStreamClient = new ChatMessageExchanger(pipeClient);

            Thread server = new Thread(ChatListener);
			server.Start();

		}

        public void SendMessage(string message)
        {
            chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = clientName, Message = message, MessageSendDate = DateTime.Now });
        }

    }
}
