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
    public class ChatTCPServer : IChatServer
    {
        public ConcurrentBag<ChatClient> ChatClients { get; set; }
        public event Action<ChatMessage> MessageRecievedEvent;
        private readonly ConcurrentBag<Task> _serverTasks;

        private readonly TcpListener _tcpListener;
		private volatile bool _isTimeToFinish = false;
		private static IPAddress IpAddress => Dns.GetHostEntry("localhost").AddressList[1];

        private static int ServerSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["serverSocketPort"]);
        private static IPEndPoint ServerEndpoint => new IPEndPoint(IpAddress, ServerSocketPort);

        private int _clientsCounter = 0;

        public ChatTCPServer()
        {
            ChatClients = new ConcurrentBag<ChatClient>();
            _serverTasks = new ConcurrentBag<Task>();
			_tcpListener = new TcpListener(ServerEndpoint);
		}

        private static ChatMessage GetNewUserJoinedMessage(string username)
        {
            return new ChatMessage() { UserName = username, Message = "Joined", MessageSendDate = DateTime.Now };
        }

        public void Start()
        {
			_serverTasks.Add(Task.Factory.StartNew(ListenNewClient));
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

        private void ListenNewChatMessages(ChatClient client)
        {
			var chatMessageStream = new ChatMessageClientServerStream(client.ClientTcp.GetStream(), client.ClientId, client.ClientName);
			while (!_isTimeToFinish)
			{
				var message = chatMessageStream.GetNextMessage();

				GlobalChatHistory.Instance.ChatMessages.Add(message);
				MessageRecievedEvent?.Invoke(message);
			}
        }

        private void ExchangeInitializationInformationWithNewClient(TcpClient handler, out string newClientName, out string newClientId)
        {
            newClientId = Guid.NewGuid().ToString();
			var greetingStream = new StreamObjectReader(handler.GetStream());
            var firstMessage = greetingStream.ReadMessage<ChatMessage>();
            greetingStream.WriteMessage(newClientId);

            SendChatHistory(greetingStream);
            newClientName = firstMessage.UserName;

        }

        public void ListenNewClient()
        {
			_tcpListener.Start();
			_tcpListener.BeginAcceptTcpClient(result => {

				try
				{
					var handler = _tcpListener.EndAcceptTcpClient(result);
					string newClientName;
					string newClientId;
					ExchangeInitializationInformationWithNewClient(handler, out newClientName, out newClientId);

					MessageRecievedEvent?.Invoke(GetNewUserJoinedMessage(newClientName));

					var chatClient = new ChatClient()
					{
						ClientId = newClientId,
						ClientName = newClientName,
						IsActive = true,
						ClientTcp = handler
					};
					ChatClients.Add(chatClient);
					_serverTasks.Add(Task.Factory.StartNew(() => { ListenNewChatMessages(chatClient); }));
					if (!_isTimeToFinish)
						ListenNewClient();
				}
				catch(ObjectDisposedException)
				{
					return;
				}
            },
            new object());
        }

		public void SendMessageToClients(ChatMessage message)
		{
			foreach (var client in ChatClients)
			{
				if (!client.IsActive) continue;
                if (client.ClientTcp.Connected)
                {
                    var messageStream = new StreamObjectReader(client.ClientTcp.GetStream());
                    messageStream.WriteMessage(message);
                }
                else
                {
                    client.Dispose();
                }
            }
        }

        public void Dispose()
        {
			_isTimeToFinish = true;
			_tcpListener.Stop();
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
