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
        private readonly ConcurrentBag<Task> _serverTasks;
        private int _clientsCounter = 0;

        public event Action<ChatMessage> MessageRecievedEvent;

        private string ClientUniqueIdPostfix
        {
            get
            {
                var uniqueName = _clientsCounter.ToString();
                _clientsCounter++;
                return uniqueName;
            }
        }
        
        private static int MaxClientsNumber => Convert.ToInt32(ConfigurationManager.AppSettings["maxClientsNumber"]);
        private static string ServerPipeName => ConfigurationManager.AppSettings["serverPipeName"];
        private static string GreetingPipeName => ConfigurationManager.AppSettings["greetingPipeName"];

        private static void SendChatHistory(StreamObjectReader chatMessageStream)
        {
            foreach (var message in ChatHistory.Instance.ChatMessages)
            {
                chatMessageStream.WriteMessage(message);
            }
            var defaultMessage = new ChatMessage() { MessageSendDate = DateTime.MinValue };
            chatMessageStream.WriteMessage(defaultMessage);
        }

        private static ChatMessage GetNewUserJoinedMessage(string username)
        {
            return new ChatMessage() { UserName = username, Message = "Joined", MessageSendDate = DateTime.Now };
        }

        public void SendMessageToClients(ChatMessage message)
        {
            foreach (var client in ChatClients)
            {
                if (!client.IsActive) continue;
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

        public ChatPipeServer()
        {
            ChatClients = new ConcurrentBag<ChatClient>();
            _serverTasks = new ConcurrentBag<Task>();
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

        private void ExchangeInitializationInformationWithNewClient(PipeStream pipe, out string newClientName, out string newClientId)
        {
            var greetingStream = new StreamObjectReader(pipe);
            var firstMessage = greetingStream.ReadMessage<ChatMessage>();
            newClientId = ClientUniqueIdPostfix;
            greetingStream.WriteMessage<string>(newClientId);
            SendChatHistory(greetingStream);
            pipe.WaitForPipeDrain();
            newClientName = firstMessage.UserName;
        }

        private void OpenPipeForNewClient(ChatClient chatClient)
        {
            var pipeServer = new NamedPipeServerStream(ServerPipeName + chatClient.ClientId, PipeDirection.Out, MaxClientsNumber, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServer.BeginWaitForConnection((ar) =>
            {
                pipeServer.EndWaitForConnection(ar);
                chatClient.ClientPipe = pipeServer;
                chatClient.ListenerTask = Task.Factory.StartNew(() =>
                {
                    ChatListener(chatClient);
                });
            }, new object());
        }

        public void ListenNewClient()
        {
            var pipeGreeting = new NamedPipeServerStream(GreetingPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeGreeting.BeginWaitForConnection(asyncResult =>
            {
                pipeGreeting.EndWaitForConnection(asyncResult);
                string newClientName;
                string newClientId;
                ExchangeInitializationInformationWithNewClient(pipeGreeting, out newClientName, out newClientId);
                pipeGreeting.Close();

                MessageRecievedEvent?.Invoke(GetNewUserJoinedMessage(newClientName));
                var chatClient = new ChatClient()
                {
                    ClientId = newClientId,
                    ClientName = newClientName,
                    IsActive = true
                };
                ChatClients.Add(chatClient);
                OpenPipeForNewClient(chatClient);

                ListenNewClient();
            }, new object());
        }

        public void Start()
        {
            _serverTasks.Add(Task.Factory.StartNew(ListenNewClient));
        }


        public void Dispose()
        {
            foreach (var client in ChatClients)
            {
                client.Dispose();
            }
            foreach (var task in _serverTasks)
            {
                task.Dispose();
            }
        }
    }
}
