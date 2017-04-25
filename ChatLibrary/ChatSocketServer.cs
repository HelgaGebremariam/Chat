using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatLibrary.Models;
using ChatLibrary.Interfaces;

namespace ChatLibrary
{
    public class ChatSocketServer : IChatServer
    {
        public ConcurrentBag<ChatClient> ChatClients { get; set; }
        public event Action<ChatMessage> MessageRecievedEvent;
        private readonly ConcurrentBag<Task> _serverTasks;

        private readonly Socket _greetingSocket;
        private readonly Socket _clientSocket;
        private readonly Socket _serverSocket;

        private IPAddress IpAddress => Dns.GetHostEntry("localhost").AddressList.FirstOrDefault();

        private static int ClientSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["clientSocketPort"]);
        private static int ServerSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["serverSocketPort"]);
        private static int GreetingSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["greetingSocketPort"]);
        private IPEndPoint GreetingEndpoint => new IPEndPoint(IpAddress, GreetingSocketPort);
        private IPEndPoint ClientEndpoint => new IPEndPoint(IpAddress, ClientSocketPort);
        private IPEndPoint ServerEndpoint => new IPEndPoint(IpAddress, ServerSocketPort);
        private static string EventWaitHandleName => ConfigurationManager.AppSettings["eventWaitHandleName"];

        private int _clientsCounter = 0;

        public ChatSocketServer()
        {
            ChatClients = new ConcurrentBag<ChatClient>();
            _serverTasks = new ConcurrentBag<Task>();

            _greetingSocket = new Socket(GreetingEndpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket = new Socket(ServerEndpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket = new Socket(ClientEndpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
        }

        private string ClientUniqueIdPostfix
        {
            get
            {
                var uniqueName = _clientsCounter.ToString();
                _clientsCounter++;
                return uniqueName;
            }
        }

        private static ChatMessage GetNewUserJoinedMessage(string username)
        {
            return new ChatMessage() { UserName = username, Message = "Joined", MessageSendDate = DateTime.Now };
        }

        public void Start()
        {
            _greetingSocket.Bind(GreetingEndpoint);
            _serverSocket.Bind(ServerEndpoint);
            _serverSocket.Listen(1);
            _clientSocket.Bind(ClientEndpoint);
            _serverTasks.Add(Task.Factory.StartNew(ListenNewClient));
            _serverTasks.Add(Task.Factory.StartNew(ListenNewChatMessages));

        }

        private static void SendChatHistory(StreamObjectReader chatMessageStream)
        {
            foreach (var message in ChatHistory.Instance.ChatMessages)
            {
                chatMessageStream.WriteMessage(message);
            }
            var defaultMessage = new ChatMessage() { MessageSendDate = DateTime.MinValue };
            chatMessageStream.WriteMessage(defaultMessage);
        }

        private void ListenNewChatMessages()
        {

            _clientSocket.Listen(1);
            _clientSocket.BeginAccept(result =>
            {
                var handler = _clientSocket.EndAccept(result);

                using (var chatMessageStream = new ChatMessageClientServerStream(new NetworkStream(handler), string.Empty, string.Empty))
                {

                    var message = chatMessageStream.GetNextMessage();

                    ChatHistory.Instance.ChatMessages.Add(message);
                    MessageRecievedEvent?.Invoke(message);
                }
                ListenNewChatMessages();
            }, new object());
        }

        private void ExchangeInitializationInformationWithNewClient(Socket handler, out string newClientName, out string newClientId, out EventWaitHandle newClientEvent)
        {
            newClientId = ClientUniqueIdPostfix;
            var clientEventName = EventWaitHandleName + newClientId;
            newClientEvent = new EventWaitHandle(false, EventResetMode.AutoReset, clientEventName);
            var greetingStream = new StreamObjectReader(new NetworkStream(handler));
            var firstMessage = greetingStream.ReadMessage<ChatMessage>();
            greetingStream.WriteMessage<string>(newClientId);

            var socketSettings = new SocketSettings()
            {
                ClientSocketPort = ClientSocketPort,
                ServerSocketPort = ServerSocketPort,
                EventWaitHandleEventName = clientEventName
            };

            greetingStream.WriteMessage<SocketSettings>(socketSettings);
            SendChatHistory(greetingStream);
            newClientName = firstMessage.UserName;

        }

        public void ListenNewClient()
        {
            _greetingSocket.Listen(1);
            _greetingSocket.BeginAccept(result => {
                    var handler = _greetingSocket.EndAccept(result);
                    string newClientName;
                    string newClientId;
                    EventWaitHandle newClientEvent;
                    ExchangeInitializationInformationWithNewClient(handler, out newClientName, out newClientId, out newClientEvent);
                    handler.Close();
                    MessageRecievedEvent?.Invoke(GetNewUserJoinedMessage(newClientName));

                    var chatClient = new ChatClient()
                    {
                        ClientId = newClientId,
                        ClientName = newClientName,
                        IsActive = true,
                        clientEventWaitHandle = newClientEvent
                    };
                    ChatClients.Add(chatClient);
                    ListenNewClient();

                },
                new object());
        }

        public void SendMessageToClients(ChatMessage message)
        {
            foreach (var client in ChatClients)
            {
                if (!client.IsActive) continue;
                if (client.clientEventWaitHandle != null)
                {
                    _serverSocket.Listen(1);
                    client.clientEventWaitHandle.Set();
                    _serverSocket.BeginAccept(asyncResult => {
                        var serverHandler = _serverSocket.EndAccept(asyncResult);
                        var messageStream = new StreamObjectReader(new NetworkStream(serverHandler));
                        messageStream.WriteMessage(message);
                    }, _serverSocket);
                }
                else
                {
                    client.Dispose();
                }
            }
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
