using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ChatLibrary.Client;
using ChatLibrary.Common;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;

namespace ChatLibrary.Server
{
    public class ChatSocketServer : IChatServer
    {
        public ConcurrentBag<ChatClient> ChatClients { get; set; }
        public event Action<ChatMessage> MessageRecievedEvent;
        private readonly ConcurrentBag<Task> _serverTasks;

        private readonly TcpListener _greetingListener;
        private readonly TcpListener _clientListener;
        private readonly TcpListener _serverListener;

        private static IPAddress IpAddress => Dns.GetHostEntry("localhost").AddressList[1];

        private static int ClientSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["clientSocketPort"]);
        private static int ServerSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["serverSocketPort"]);
        private static int GreetingSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["greetingSocketPort"]);
        private static IPEndPoint GreetingEndpoint => new IPEndPoint(IpAddress, GreetingSocketPort);
        private static IPEndPoint ClientEndpoint => new IPEndPoint(IpAddress, ClientSocketPort);
        private static IPEndPoint ServerEndpoint => new IPEndPoint(IpAddress, ServerSocketPort);
        private static string EventWaitHandleName => ConfigurationManager.AppSettings["eventWaitHandleName"];

        private int _clientsCounter = 0;

        public ChatSocketServer()
        {
            ChatClients = new ConcurrentBag<ChatClient>();
            _serverTasks = new ConcurrentBag<Task>();

            _greetingListener = new TcpListener(GreetingEndpoint);
            _serverListener = new TcpListener(ServerEndpoint);
            _clientListener = new TcpListener(ClientEndpoint);
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
            _serverListener.Start();
            _serverTasks.Add(Task.Factory.StartNew(ListenNewClient));
            _serverTasks.Add(Task.Factory.StartNew(ListenNewChatMessages));
        }

        private static void SendChatHistory(StreamObjectReader chatMessageStream)
        {
            foreach (var message in GlobalChatHistory.Instance.ChatMessages)
            {
                chatMessageStream.WriteMessage(message);
            }
            var defaultMessage = new ChatMessage() { MessageSendDate = DateTime.MinValue };
            chatMessageStream.WriteMessage(defaultMessage);
        }

        private void ListenNewChatMessages()
        {
            _clientListener.Start();
            _clientListener.BeginAcceptTcpClient(result =>
            {
                var handler = _clientListener.EndAcceptTcpClient(result);

                using (var chatMessageStream = new ChatMessageClientServerStream(handler.GetStream(), string.Empty, string.Empty))
                {

                    var message = chatMessageStream.GetNextMessage();

                    GlobalChatHistory.Instance.ChatMessages.Add(message);
                    MessageRecievedEvent?.Invoke(message);
                }
                ListenNewChatMessages();
            }, new object());
        }

        private void ExchangeInitializationInformationWithNewClient(TcpClient handler, out string newClientName, out string newClientId, out EventWaitHandle newClientEvent)
        {
            newClientId = ClientUniqueIdPostfix;
            var clientEventName = EventWaitHandleName + newClientId;
            newClientEvent = new EventWaitHandle(false, EventResetMode.AutoReset, clientEventName);
            var greetingStream = new StreamObjectReader(handler.GetStream());
            var firstMessage = greetingStream.ReadMessage<ChatMessage>();
            greetingStream.WriteMessage(newClientId);

            var socketSettings = new SocketSettings()
            {
                ClientSocketPort = ClientSocketPort,
                ServerSocketPort = ServerSocketPort,
                EventWaitHandleEventName = clientEventName
            };

            greetingStream.WriteMessage(socketSettings);
            SendChatHistory(greetingStream);
            newClientName = firstMessage.UserName;

        }

        public void ListenNewClient()
        {
            _greetingListener.Start();
            _greetingListener.BeginAcceptTcpClient(result => {
                    var handler = _greetingListener.EndAcceptTcpClient(result);
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
                        ClientEventWaitHandle = newClientEvent
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
                if (client.ClientEventWaitHandle != null)
                {
                    _serverListener.Start();
                    client.ClientEventWaitHandle.Set();
                    _serverListener.BeginAcceptTcpClient(asyncResult => {
                        var serverHandler = _serverListener.EndAcceptTcpClient(asyncResult);
                        var messageStream = new StreamObjectReader(serverHandler.GetStream());
                        messageStream.WriteMessage(message);
                    }, _serverListener);
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
