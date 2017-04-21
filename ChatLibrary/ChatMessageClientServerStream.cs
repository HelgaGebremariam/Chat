using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;

namespace ChatLibrary
{
	public class ChatMessageClientServerStream : IDisposable
	{
		private StreamObjectReader chatMessageExchanger;
		private string clientName;
        private string clientId;
		NamedPipeServerStream clientPipe;

		private string clientPipeName
		{
			get
			{
				return ConfigurationManager.AppSettings["clientPipeName"] + clientId;
			}
		}

		public ChatMessageClientServerStream(string clientName, string clientId)
		{
			this.clientName = clientName;
            this.clientId = clientId;
			clientPipe = new NamedPipeServerStream(clientPipeName, PipeDirection.In, 1);
			clientPipe.WaitForConnection();
			chatMessageExchanger = new StreamObjectReader(clientPipe);
		}

		public ChatMessage GetNextMessage()
		{
			ChatMessage message = chatMessageExchanger.ReadMessage<ChatMessage>();
			message.UserName = clientName;
			message.MessageSendDate = DateTime.Now;
			return message;
		}

        public void Dispose()
        {
            clientPipe.Dispose();
        }
    }
}
