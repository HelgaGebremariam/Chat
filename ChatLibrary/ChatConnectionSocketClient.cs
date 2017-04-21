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

namespace ChatLibrary
{
    public class ChatConnectionSocketClient : IChatConnectionClient
    {
        private Socket clientSocket;
        private Socket serverSocket;
        private string clientName;
        StreamObjectReader chatMessageStreamClient;
        StreamObjectReader chatMessageStreamServer;
        private string clientId;
        private event Action<ChatMessage> messageRecievedEvent;
        private Stream clientSocketStream;
        private Stream serverSocketStream;
        private Task chatListenerTask;

        public List<ChatMessage> ChatHistory { get; set; }


        private int clientSocketPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["clientSocketPort"]);
            }
        }

        private string serverName
        {
            get
            {
                return ConfigurationManager.AppSettings["serverName"];
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

        private bool Greet()
        {
            
            using (Socket greetingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                greetingSocket.Connect(serverName, greetingSocketPort);
                if (!greetingSocket.Connected)
                    return false;

                ChatMessage firstMessage = new ChatMessage() { UserName = clientName };
                using (var greetingSocketStream = new NetworkStream(greetingSocket))
                {
                    StreamObjectReader greetingStream = new StreamObjectReader(greetingSocketStream);
                    greetingStream.WriteMessage(firstMessage);
                    clientId = greetingStream.ReadMessage<string>();
                    ChatHistory = new List<ChatMessage>();
                    while (ChatHistory.Count() < 100)
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

        private void ChatListener()
        {
            try
            {
                while (serverSocket.Connected)
                {
                    var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
                    messageRecievedEvent(newMessage);
                }
            }
            catch(IOException ex)
            {

            }
        }

        public ChatConnectionSocketClient(string clientName, Action<ChatMessage> messageRecievedEvent)
        {

            this.messageRecievedEvent += messageRecievedEvent;
            this.clientName = clientName;
            if (!Greet())
                throw new Exception();

            serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Connect(serverName, serverSocketPort);
            if (!serverSocket.Connected)
                throw new Exception();

            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverName, clientSocketPort);
            if (!clientSocket.Connected)
                throw new Exception();

            clientSocketStream = new NetworkStream(clientSocket);
            serverSocketStream = new NetworkStream(serverSocket);
            chatMessageStreamClient = new StreamObjectReader(clientSocketStream);
            chatMessageStreamServer = new StreamObjectReader(serverSocketStream);

            chatListenerTask = Task.Factory.StartNew(() => { ChatListener(); });
        }

        public void Dispose()
        {
            clientSocketStream.Dispose();
            serverSocketStream.Dispose();
            clientSocket.Dispose();
            serverSocket.Dispose();
        }

        public bool SendMessage(string message)
        {
            if (serverSocket.Connected)
            {
                chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = clientName, Message = message, MessageSendDate = DateTime.Now });
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
