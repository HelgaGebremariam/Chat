﻿using System;
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
    public class ChatConnectionClient : IDisposable
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

        private string clientPipeName
        {
            get
            {
                return ConfigurationManager.AppSettings["clientPipeName"] + clientId;
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
                return ConfigurationManager.AppSettings["serverPipeName"] + clientId;
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
			while (pipeServer.IsConnected)
			{
				var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
				messageRecievedEvent(newMessage);
			}
		}

		public bool Greet()
		{
			NamedPipeClientStream greetingPipe = new NamedPipeClientStream(serverName, greetingPipeName, PipeDirection.InOut);
            greetingPipe.Connect(connectionTimeout);
            if (!greetingPipe.IsConnected)
                return false;
			ChatMessage firstMessage = new ChatMessage() { UserName = clientName };

			StreamObjectReader greetingStream = new StreamObjectReader(greetingPipe);
			greetingStream.WriteMessage(firstMessage);
			greetingPipe.WaitForPipeDrain();
            clientId = greetingStream.ReadMessage<string>();
			ChatHistory = new List<ChatMessage>();
            while (ChatHistory.Count() < 100)
            {
                var message = greetingStream.ReadMessage<ChatMessage>();
                if (message.MessageSendDate == DateTime.MinValue)
                    break;
                ChatHistory.Add(message);
            }
			greetingPipe.Close();
            return true;
		}

		public ChatConnectionClient(string clientName, Action<ChatMessage> messageRecievedEvent)
		{
			this.messageRecievedEvent += messageRecievedEvent;
			this.clientName = clientName;
			if(!Greet())
                throw new Exception();
            pipeServer = new NamedPipeClientStream(serverName, serverPipeName, PipeDirection.In);
            pipeServer.Connect(connectionTimeout);
            if (!pipeServer.IsConnected)
                throw new Exception();
            chatMessageStreamServer = new StreamObjectReader(pipeServer);

            pipeClient = new NamedPipeClientStream(serverName, clientPipeName, PipeDirection.Out);
            pipeClient.Connect(connectionTimeout);
            if (!pipeClient.IsConnected)
                throw new Exception();
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
    }
}
