using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Collections.Concurrent;

namespace ChatLibrary
{
    public class ChatConnectionServer
    {
		public ConcurrentBag<ChatMessage> ChatHistory;
		NamedPipeServerStream pipeServer;
		ChatMessageExchanger chatMessageSender;
		NamedPipeServerStream pipeGreeting;
		private event Action<ChatMessage> messageRecievedEvent;
		private int maxClientsNumber
		{
			get
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings["maxClientsNumber"]);
			}
		}

        private string clientPipeName
        {
            get
            {
                return ConfigurationManager.AppSettings["clientPipeName"];
            }
        }

        private string serverPipeName
        {
            get
            {
                return ConfigurationManager.AppSettings["serverPipeName"];
            }
        }

		private string greetingPipeName
		{
			get
			{
				return ConfigurationManager.AppSettings["greetingPipeName"];
			}
		}


		private void SendChatHistory(ChatMessageExchanger chatMessageStream)
        {
            if (ChatHistory != null)
            {
                foreach (var message in ChatHistory)
                {
                    chatMessageStream.WriteMessage(message);
                }
            }
            var defaultMessage = new ChatMessage() { MessageSendDate = DateTime.MinValue };
            chatMessageStream.WriteMessage(defaultMessage);
        }

        private void ChatListener(ChatMessageClientServerStream chatMessageStream)
        {
            while(true)
            {
				var message = chatMessageStream.GetNextMessage();
                ChatHistory.Add(message);
				messageRecievedEvent(message);
				chatMessageSender.WriteMessage(message);
			}
        }

		private void ServerThread(object data)
        {
			try
			{
				pipeGreeting.WaitForConnection();
				var greetingStream = new ChatMessageExchanger(pipeGreeting);
				var firstMessage = greetingStream.ReadMessage();
				SendChatHistory(greetingStream);

				pipeGreeting.WaitForPipeDrain();
				pipeGreeting.Close();
				var chatMessageClientServerStream = new ChatMessageClientServerStream(firstMessage.UserName);
				pipeServer.WaitForConnection();
				ChatListener(chatMessageClientServerStream);
			}

            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
			pipeGreeting.Close();
        }

		public ChatConnectionServer(Action<ChatMessage> messageRecievedEvent)
        {
			pipeServer = new NamedPipeServerStream(serverPipeName, PipeDirection.Out, maxClientsNumber);
			
			chatMessageSender = new ChatMessageExchanger(pipeServer);

			this.messageRecievedEvent += messageRecievedEvent;

			ChatHistory = new ConcurrentBag<ChatMessage>();
            ChatMessage message = new ChatMessage()
            {
                UserName = "Sarah Kerrigan",
                Message = "Amon will be dead.",
                MessageSendDate = DateTime.Now
            };
            ChatHistory.Add(message);
            message = new ChatMessage()
            {
                UserName = "James Raynor",
                Message = "Yeeeah, absolutely!",
                MessageSendDate = DateTime.Now
            };
			pipeGreeting = new NamedPipeServerStream(greetingPipeName, PipeDirection.InOut, 1);
            ChatHistory.Add(message);
			Thread server = new Thread(ServerThread);
			server.Start();

		}
    }
}
