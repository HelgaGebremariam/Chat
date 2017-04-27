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
    public class ChatTcpServer : IChatServer
    {
        public ConcurrentBag<ChatClient> ChatClients { get; set; }
        public event Action<ChatMessage> MessageRecievedEvent;

        private readonly TcpListener _tcpListener;
		private volatile bool _isTimeToFinish = false;
		private static IPAddress IpAddress => Dns.GetHostEntry("localhost").AddressList[1];

        private static int ServerSocketPort => Convert.ToInt32(ConfigurationManager.AppSettings["serverSocketPort"]);
        private static IPEndPoint ServerEndpoint => new IPEndPoint(IpAddress, ServerSocketPort);

        public ChatTcpServer()
        {
            ChatClients = new ConcurrentBag<ChatClient>();
			_tcpListener = new TcpListener(ServerEndpoint);
		}

        private static ChatMessage GetNewUserJoinedMessage(string username)
        {
            return new ChatMessage() { UserName = username, Message = "Joined", MessageSendDate = DateTime.Now };
        }

        public void Start()
        {
            _tcpListener.Start();
            Task.Factory.StartNew(ListenNewClient);
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
            try
            {
                var chatMessageStream = new ChatMessageClientServerStream(client.ClientTcp.GetStream(), client.ClientId, client.ClientName);
                while (!_isTimeToFinish)
                {

                    var message = chatMessageStream.GetNextMessage();

                    GlobalChatHistory.Instance.ChatMessages.Add(message);
                    MessageRecievedEvent?.Invoke(message);
                }
            }
            catch (System.IO.IOException)
            {
                // ignored
            }
        }

        private static void ExchangeInitializationInformationWithNewClient(TcpClient handler, out string newClientName, out string newClientId)
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
            while (!_isTimeToFinish)
            {
                try
                {
                    var handler = _tcpListener.AcceptTcpClient();
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
                    Task.Factory.StartNew(() => { ListenNewChatMessages(chatClient); });
                }
                catch (SocketException)
                {
                    return;
                }

            }
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
        }
    }
}
