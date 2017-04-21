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
            var hostName = "localhost";
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPEndPoint localEP = new IPEndPoint(ipHostInfo.AddressList[1], greetingSocketPort);

            Socket greetingListener = new Socket(localEP.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            greetingListener.Bind(localEP);
            greetingListener.Listen(1);

            greetingListener.BeginAccept((IAsyncResult result)=> {
                var listener = result.AsyncState as Socket;
                Socket handler = listener.EndAccept(result);
                var greetingStream = new StreamObjectReader(new NetworkStream(handler));
                var firstMessage = greetingStream.ReadMessage<ChatMessage>();
                var clientId = clientUniqueIdPostfix;
                greetingStream.WriteMessage<string>(clientId);
                SendChatHistory(greetingStream);
                handler.Close();
                messageRecievedEvent(new ChatMessage() { UserName = firstMessage.UserName, Message = "Joined", MessageSendDate = DateTime.Now });

                localEP = new IPEndPoint(ipHostInfo.AddressList[1], serverSocketPort);
                Socket serverSocket = new Socket(localEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(localEP);
                serverSocket.Listen(1);

                serverSocket.BeginAccept((IAsyncResult asyncResult) => {
                    Socket serverHandler = serverSocket.EndAccept(asyncResult);

                    var chatClient = new ChatClient()
                    {
                        ClientId = clientId,
                        ClientName = firstMessage.UserName,
                        ClientSocket = serverHandler,
                        IsActive = true
                    };
                    chatClients.Add(chatClient);
                    chatClient.ListenerTask = Task.Factory.StartNew(() =>
                    {
                        ChatListenerSocket(chatClient);
                    });

                }, serverSocket);
            },
                greetingListener);



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

        private void ChatListenerSocket(ChatClient chatClient)
        {
            //using (var chatMessageStream = new ChatMessageClientServerStream(chatClient.ClientName, chatClient.ClientId))
            //{
            //    while (chatClient.IsActive && chatClient.ClientPipe.IsConnected)
            //    {
            //        var message = chatMessageStream.GetNextMessage();
            //        if (message == null)
            //        {
            //            chatClient.Dispose();
            //            return;
            //        }
            //        ChatHistory.Add(message);
            //        messageRecievedEvent(message);
            //    }
            //}
        }


        public void SendMessageToClients(ChatMessage message)
        {
            foreach(var client in chatClients)
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

			this.messageRecievedEvent += messageRecievedEvent;
            this.messageRecievedEvent += SendMessageToClients;
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


        }
    }
}
