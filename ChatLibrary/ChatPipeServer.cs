using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;

namespace ChatLibrary
{
    public class ChatPipeServer : IChatServer
    {
        public ConcurrentBag<ChatClient> ChatClients { get; set; }

        private int _clientsCounter = 0;

        public event Action<ChatMessage> MessageRecievedEvent;

        private IPAddress IpAddress => Dns.GetHostEntry("localhost").AddressList.FirstOrDefault();

        private IPEndPoint GreetingEndpoint => new IPEndPoint(IpAddress, GreetingSocketPort);
        private IPEndPoint ClientEndpoint => new IPEndPoint(IpAddress, ClientSocketPort);
        private IPEndPoint ServerEndpoint => new IPEndPoint(IpAddress, ServerSocketPort);

        private string ClientUniqueIdPostfix
        {
            get
            {
                var uniqueName = _clientsCounter.ToString();
                _clientsCounter++;
                return uniqueName;
            }
        }

        
        private int MaxClientsNumber => Convert.ToInt32(ConfigurationManager.AppSettings["maxClientsNumber"]);
        private string ServerPipeName => ConfigurationManager.AppSettings["serverPipeName"];
        private string GreetingPipeName => ConfigurationManager.AppSettings["greetingPipeName"];
        private string EventWaitHandleName => ConfigurationManager.AppSettings["eventWaitHandleName"];
        private int ClientSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["clientSocketPort"]);
        private int ServerSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["serverSocketPort"]);
        private int GreetingSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["greetingSocketPort"]);

        private void SendChatHistory(StreamObjectReader chatMessageStream)
        {
                foreach (var message in ChatHistory.Instance.ChatMessages)
                {
                    chatMessageStream.WriteMessage(message);
                }
            var defaultMessage = new ChatMessage() { MessageSendDate = DateTime.MinValue };
            chatMessageStream.WriteMessage(defaultMessage);
        }

        private ChatMessage GetNewUserJoinedMessage(string username)
        {
            return new ChatMessage() { UserName = username, Message = "Joined", MessageSendDate = DateTime.Now };
        }

        public void SendMessageToClients(ChatMessage message)
        {

            foreach (var client in ChatClients)
            {
                if (client.IsActive)
                {
                    if (client.ClientPipe != null && client.ClientPipe.IsConnected)

                    {
                        var messageStream = new StreamObjectReader(client.ClientPipe);
                        messageStream.WriteMessage(message);
                    }
                    else
                    {
                        client.Dispose();
                    }
                }
            }
        }

        public ChatPipeServer()
        {
            ChatClients = new ConcurrentBag<ChatClient>();
        }

        private void ChatListener(ChatClient chatClient)
        {
            using (var chatMessageStream = new ChatMessageClientServerStream(chatClient.ClientName, chatClient.ClientId))
            {
                while (chatClient.IsActive && chatClient.ClientPipe.IsConnected)
                {
                    var message = chatMessageStream.GetNextMessage();
                    if (message == null)
                    {
                        chatClient.Dispose();
                        return;
                    }
                    ChatHistory.Instance.ChatMessages.Add(message);
                    MessageRecievedEvent?.Invoke(message);
                }
            }
        }

        public void ListenNewClient()
        {
            var pipeGreeting = new NamedPipeServerStream(GreetingPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeGreeting.BeginWaitForConnection(asyncResult =>
            {
                pipeGreeting.EndWaitForConnection(asyncResult);

                var greetingStream = new StreamObjectReader(pipeGreeting);
                var firstMessage = greetingStream.ReadMessage<ChatMessage>();
                var clientId = ClientUniqueIdPostfix;
                greetingStream.WriteMessage<string>(clientId);
                SendChatHistory(greetingStream);
                pipeGreeting.WaitForPipeDrain();
                pipeGreeting.Close();

                MessageRecievedEvent?.Invoke(GetNewUserJoinedMessage(firstMessage.UserName));

                NamedPipeServerStream pipeServer = new NamedPipeServerStream(ServerPipeName + clientId, PipeDirection.Out, MaxClientsNumber, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                pipeServer.BeginWaitForConnection((ar) =>
                {
                    pipeServer.EndWaitForConnection(ar);
                    var chatClient = new ChatClient()
                    {
                        ClientId = clientId,
                        ClientName = firstMessage.UserName,
                        ClientPipe = pipeServer,
                        IsActive = true
                    };
                    ChatClients.Add(chatClient);
                    chatClient.ListenerTask = Task.Factory.StartNew(() =>
                    {
                        ChatListener(chatClient);
                    });
                }, new object());
                ListenNewClient();
            }, new object());
        }

        public void Connect()
        {
        }


        public void Dispose()
        {
            foreach (var client in ChatClients)
            {
                client.Dispose();
            }
        }
    }
}
