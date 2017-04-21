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
        private ConcurrentBag<NamedPipeServerStream> clientPipes;


        private event Action<ChatMessage> messageRecievedEvent;

        private int maxClientsNumber
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["maxClientsNumber"]);
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

        public void GreetNewClient()
        {
            NamedPipeServerStream pipeGreeting = new NamedPipeServerStream(greetingPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeGreeting.BeginWaitForConnection((IAsyncResult asyncResult) =>
            {
                pipeGreeting.EndWaitForConnection(asyncResult);
                var greetingStream = new ChatMessageExchanger(pipeGreeting);
                var firstMessage = greetingStream.ReadMessage();
                SendChatHistory(greetingStream);
                pipeGreeting.WaitForPipeDrain();
                pipeGreeting.Close();

                NamedPipeServerStream pipeServer = new NamedPipeServerStream(serverPipeName + firstMessage.UserName, PipeDirection.Out, maxClientsNumber, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                pipeServer.BeginWaitForConnection((IAsyncResult ar) =>
                {
                    pipeServer.EndWaitForConnection(ar);
                    clientPipes.Add(pipeServer);
                    ChatListener(firstMessage.UserName);
                }, new object());

                GreetNewClient();

            }, new object());
        }

        private void ChatListener(string userName)
        {
            var chatMessageStream = new ChatMessageClientServerStream(userName);
            while (true)
            {
                var message = chatMessageStream.GetNextMessage();
                ChatHistory.Add(message);
                messageRecievedEvent(message);
            }
        }


        public void SendMessageToClients(ChatMessage message)
        {
            foreach(var pipe in clientPipes)
            {
                var messageStream = new ChatMessageExchanger(pipe);
                messageStream.WriteMessage(message);
            }
        }

		public ChatConnectionServer(Action<ChatMessage> messageRecievedEvent)
        {

			this.messageRecievedEvent += messageRecievedEvent;
            this.messageRecievedEvent += SendMessageToClients;
            clientPipes = new ConcurrentBag<NamedPipeServerStream>();
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
            Task.Factory.StartNew(() =>
            {
                GreetNewClient();
            });
            

        }
    }
}
