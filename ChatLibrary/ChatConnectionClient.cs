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
    public class ChatConnectionClient : IChatConnectionClient
    {
		public List<ChatMessage> ChatHistory { get; set; }

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

		private string serverName
		{
			get
			{
				return ConfigurationManager.AppSettings["serverName"];
			}
		}

		public void UploadChatHistory(ChatMessageStream streamString)
		{
			ChatHistory = new List<ChatMessage>();
            while (ChatHistory.Count() < 100)
            {
                var message = streamString.ReadMessage();
                if (message.MessageSendDate == DateTime.MinValue)
                    break;
                ChatHistory.Add(message);
            }
		}

		public ChatConnectionClient()
		{
			NamedPipeClientStream pipeClient = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut);
			pipeClient.Connect();
			ChatMessageStream streamString = new ChatMessageStream(pipeClient);

			UploadChatHistory(streamString);
		}

        public void Connect()
        {

        }

        public void SendMessage()
        {

        }

        public void Syncronize()
        {

        }
    }
}
