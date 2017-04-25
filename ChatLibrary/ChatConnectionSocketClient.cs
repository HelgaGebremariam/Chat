using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLibrary.Interfaces;
using ChatLibrary.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Remoting;

namespace ChatLibrary
{
    public class ChatConnectionSocketClient : IChatConnectionClient
    {
        private string clientName;
        private string clientId;
        private event Action<ChatMessage> messageRecievedEvent;

        private Task chatListenerTask;
        private EventWaitHandle messageReceived;

        public List<ChatMessage> ChatHistory { get; set; }


        private SocketSettings socketSettings;

        private string serverName => ConfigurationManager.AppSettings["serverName"];

        private List<int> greetingSocketPorts => ConfigurationManager.AppSettings["greetingSocketPorts"].Split(',').Select(s=>Convert.ToInt32(s)).ToList();

        private bool Greet()
        {
            try
            {
                using (Socket greetingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    foreach (var greetingPort in greetingSocketPorts)
                    {
                        greetingSocket.Connect(serverName, greetingPort);
                        if (greetingSocket.Connected)
                            break;
                    }

                    if (!greetingSocket.Connected)
                        return false;

                    ChatMessage firstMessage = new ChatMessage() { UserName = clientName };
                    using (var greetingSocketStream = new NetworkStream(greetingSocket))
                    {
                        StreamObjectReader greetingStream = new StreamObjectReader(greetingSocketStream);
                        greetingStream.WriteMessage(firstMessage);
                        clientId = greetingStream.ReadMessage<string>();
                        socketSettings = greetingStream.ReadMessage<SocketSettings>();
                        ChatHistory = new List<ChatMessage>();
                        while (true)
                        {
                            var message = greetingStream.ReadMessage<ChatMessage>();
                            if (message.MessageSendDate == DateTime.MinValue)
                                break;
                            ChatHistory.Add(message);
                        }
                        greetingSocket.Close();
                    }
                }
                return true;
            }
            catch(SocketException)
            {
                return false;
            }
            
        }

        private void ChatListener()
        {
            while (true)
            {
                messageReceived.WaitOne();
                var serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Connect(serverName, socketSettings.ServerSocketPort);
                if (!serverSocket.Connected)
                    throw new ServerException();
                var chatMessageStreamServer = new StreamObjectReader(new NetworkStream(serverSocket));
                var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
                messageRecievedEvent(newMessage);
            }
        }

        public ChatConnectionSocketClient(string clientName, Action<ChatMessage> messageRecievedEvent)
        {
            
            this.messageRecievedEvent += messageRecievedEvent;
            this.clientName = clientName;
            if (!Greet())
                throw new ServerException();
            messageReceived = EventWaitHandle.OpenExisting(socketSettings.EventWaitHandleEventName);
            chatListenerTask = Task.Factory.StartNew(() => { ChatListener(); });
        }

        public void Dispose()
        {
            messageReceived.Dispose();
        }

        public bool SendMessage(string message)
        {
            try
            {
                var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(serverName, socketSettings.ClientSocketPort);
                if (!clientSocket.Connected)
                    return false;
                var chatMessageStreamClient = new StreamObjectReader(new NetworkStream(clientSocket));
                chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = clientName, Message = message, MessageSendDate = DateTime.Now });
                clientSocket.Dispose();
                return true;
            }
            catch(SocketException)
            {
                return false;
            }

        }

        public bool Connect()
        {
            throw new NotImplementedException();
        }
    }
}
