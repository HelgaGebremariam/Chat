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
	public class ChatMessageClientServerStream
	{
		private ChatMessageExchanger chatMessageExchanger;
		private string clientName;
		NamedPipeServerStream clientPipe;

		private string clientPipeName
		{
			get
			{
				return ConfigurationManager.AppSettings["clientPipeName"] + clientName;
			}
		}

		public ChatMessageClientServerStream(string clientName)
		{
			this.clientName = clientName;
			clientPipe = new NamedPipeServerStream(clientPipeName, PipeDirection.In, 1);
			clientPipe.WaitForConnection();
			chatMessageExchanger = new ChatMessageExchanger(clientPipe);
		}

		public ChatMessage GetNextMessage()
		{
			ChatMessage message = chatMessageExchanger.ReadMessage();
			message.UserName = clientName;
			message.MessageSendDate = DateTime.Now;
			return message;
		}
	}
}
