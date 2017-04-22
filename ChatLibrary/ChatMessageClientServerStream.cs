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
using ChatLibrary.Models;

namespace ChatLibrary
{
	public class ChatMessageClientServerStream : IDisposable
	{
		private StreamObjectReader chatMessageExchanger;
		private string clientName;
        private string clientId;
		Stream clientStream;

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
            var stream = new NamedPipeServerStream(clientPipeName, PipeDirection.In, 1);
            stream.WaitForConnection();
            clientStream = stream;
            chatMessageExchanger = new StreamObjectReader(clientStream);
		}

        public ChatMessageClientServerStream(Stream stream, string clientId, string clientName)
        {
            this.clientName = clientName;
            this.clientId = clientId;
            clientStream = stream;
            chatMessageExchanger = new StreamObjectReader(clientStream);
        }

        public ChatMessage GetNextMessage()
		{
			ChatMessage message = chatMessageExchanger.ReadMessage<ChatMessage>();
            if (message == null)
                return null;
            if(clientName != string.Empty)
			    message.UserName = clientName;
			message.MessageSendDate = DateTime.Now;
			return message;
		}

        public void Dispose()
        {
            clientStream.Dispose();
        }
    }
}
