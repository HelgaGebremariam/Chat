using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Collections.Concurrent;
using System.Net.Sockets;
using ChatLibrary.Models;
using System.Net;

namespace ChatLibrary
{
    public class ChatConnectionServer : IDisposable
    {
        public ConcurrentBag<ChatMessage> ChatHistory;
        private ConcurrentBag<ChatClient> chatClients;

        private event Action<ChatMessage> messageRecievedEvent;

        private int clientsCounter = 0;
        private EventWaitHandle eventMessageReceived;
        private Socket greetingSocket;
        private Socket clientSocket;
        private Socket serverSocket;

        private string clientUniqueIdPostfix
        {
            get
            {
                string uniqueName = clientsCounter.ToString();
                clientsCounter++;
                return uniqueName;
            }
        }

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

        private int clientSocketPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["clientSocketPort"]);
            }
        }

        private int serverSocketPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["serverSocketPort"]);
            }
        }

        private int greetingSocketPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["greetingSocketPort"]);
            }
        }


        private void SendChatHistory(StreamObjectReader chatMessageStream)
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

        public void GreetNewPipeClient()
        {
            var pipeGreeting = new NamedPipeServerStream(greetingPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                pipeGreeting.BeginWaitForConnection((IAsyncResult asyncResult) =>
                {
                    pipeGreeting.EndWaitForConnection(asyncResult);
                    var greetingStream = new StreamObjectReader(pipeGreeting);
                    var firstMessage = greetingStream.ReadMessage<ChatMessage>();
                    var clientId = clientUniqueIdPostfix;
                    greetingStream.WriteMessage<string>(clientId);
                    SendChatHistory(greetingStream);
                    pipeGreeting.WaitForPipeDrain();
                    pipeGreeting.Close();
                    messageRecievedEvent(new ChatMessage() { UserName = firstMessage.UserName, Message = "Joined", MessageSendDate = DateTime.Now });
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream(serverPipeName + clientId, PipeDirection.Out, maxClientsNumber, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    pipeServer.BeginWaitForConnection((IAsyncResult ar) =>
                    {
                        pipeServer.EndWaitForConnection(ar);
                        var chatClient = new ChatClient()
                        {
                            ClientId = clientId,
                            ClientName = firstMessage.UserName,
                            ClientPipe = pipeServer,
                            IsActive = true
                        };
                        chatClients.Add(chatClient);
                        chatClient.ListenerTask = Task.Factory.StartNew(() =>
                        {
                            ChatListener(chatClient);
                        });
                        

                    }, new object());

                    GreetNewPipeClient();

                }, new object());
        }

        public void GreetNewSocketClient()
        {

            greetingSocket.Listen(1);

            greetingSocket.BeginAccept((IAsyncResult result)=> {
                Socket handler = greetingSocket.EndAccept(result);
                string clientId = clientUniqueIdPostfix;
                var greetingStream = new StreamObjectReader(new NetworkStream(handler));
                var firstMessage = greetingStream.ReadMessage<ChatMessage>();
                greetingStream.WriteMessage<string>(clientId);
                SocketSettings socketSettings = new SocketSettings()
                {
                    ClientSocketPort = clientSocketPort,
                    ServerSocketPort = serverSocketPort
                };
                
                greetingStream.WriteMessage<SocketSettings>(socketSettings);
                SendChatHistory(greetingStream);
                handler.Close();
                messageRecievedEvent(new ChatMessage() { UserName = firstMessage.UserName, Message = "Joined", MessageSendDate = DateTime.Now });

                var chatClient = new ChatClient()
                {
                    ClientId = clientId,
                    ClientName = firstMessage.UserName,
                    IsActive = true
                };
                chatClients.Add(chatClient);
                GreetNewSocketClient();

            },
                new object());

            

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
                    ChatHistory.Add(message);
                    messageRecievedEvent(message);
                }
            }
        }

        private void ChatListenerSocket()
        {

                clientSocket.Listen(1);
                clientSocket.BeginAccept((IAsyncResult result) =>
                {
                    var handler = clientSocket.EndAccept(result);

                    using (var chatMessageStream = new ChatMessageClientServerStream(new NetworkStream(handler), string.Empty, string.Empty))
                    {

                        var message = chatMessageStream.GetNextMessage();

                        ChatHistory.Add(message);
                        messageRecievedEvent(message);

                    }
                    ChatListenerSocket();
                }, new object());
        }


        public void SendMessageToClients(ChatMessage message)
        {
            eventMessageReceived.Set();

            serverSocket.Listen(maxClientsNumber);

            serverSocket.BeginAccept((IAsyncResult asyncResult) => {
                Socket serverHandler = serverSocket.EndAccept(asyncResult);
                var messageStream = new StreamObjectReader(new NetworkStream(serverHandler));
                messageStream.WriteMessage(message);
            }, serverSocket);

            foreach (var client in chatClients)
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

        public void Dispose()
        {
            foreach (var client in chatClients)
            {
                client.Dispose();
            }
        }

        public ChatConnectionServer(Action<ChatMessage> messageRecievedEvent)
        {
            eventMessageReceived = new EventWaitHandle(false, EventResetMode.AutoReset, "eventMessageReceived");
            this.messageRecievedEvent += messageRecievedEvent;
            this.messageRecievedEvent += SendMessageToClients;

            var hostName = "localhost";
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPEndPoint localEP = new IPEndPoint(ipHostInfo.AddressList[1], greetingSocketPort);

            greetingSocket = new Socket(localEP.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            greetingSocket.Bind(localEP);

            localEP = new IPEndPoint(ipHostInfo.AddressList[1], serverSocketPort);
            serverSocket = new Socket(localEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(localEP);
            serverSocket.Listen(1);

            localEP = new IPEndPoint(ipHostInfo.AddressList[1], clientSocketPort);
            clientSocket = new Socket(localEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Bind(localEP);

            chatClients = new ConcurrentBag<ChatClient>();
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
                GreetNewPipeClient();
            });
            Task.Factory.StartNew(() =>
            {
                GreetNewSocketClient();
            });
            Task.Factory.StartNew(() =>
            {
                ChatListenerSocket();
            });


        }
    }
}
