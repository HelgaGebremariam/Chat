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
    public class ChatConnectionServer : IChatConnectionServer
    {
		public ConcurrentBag<ChatMessage> ChatHistory;

		private int maxClientsNumber
		{
			get
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings["maxClientsNumber"]);
			}
		}

		private string pipeName
		{
			get
			{
				return ConfigurationManager.AppSettings["pipeName"];
			}
		}

        private void SendChatHistory(ChatMessageStream chatMessageStream)
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

        private void ChatListener(ChatMessageStream chatMessageStream)
        {
            while(true)
            {

            }
        }

		private void ServerThread(object data)
        {
            NamedPipeServerStream pipeServer =
                new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxClientsNumber);

            int threadId = Thread.CurrentThread.ManagedThreadId;

            pipeServer.WaitForConnection();

            try
            {
                var chatMessageStream = new ChatMessageStream(pipeServer);

                SendChatHistory(chatMessageStream);
            }

            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            pipeServer.Close();
        }

		public ChatConnectionServer()
        {
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

            ChatHistory.Add(message);
			Thread server = new Thread(ServerThread);
			server.Start();

		}
    }
}
