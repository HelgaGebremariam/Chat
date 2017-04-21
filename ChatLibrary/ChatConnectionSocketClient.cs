﻿using System;
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

        private Task chatListenerTask;
        private EventWaitHandle eventMessageReceived;

        public List<ChatMessage> ChatHistory { get; set; }


        private SocketSettings socketSettings;

        private string serverName
        {
            get
            {
                return ConfigurationManager.AppSettings["serverName"];
            }
        }

        

        private List<int> greetingSocketPorts
        {
            get
            {
                return ConfigurationManager.AppSettings["greetingSocketPorts"].Split(',').Select(s=>Convert.ToInt32(s)).ToList();
            }
        }

        private bool Greet()
        {
            
            using (Socket greetingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                foreach(var greetingPort in greetingSocketPorts)
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
                while (eventMessageReceived.WaitOne())
                {
                    serverSocket.Connect(serverName, socketSettings.ServerSocketPort);
                    if (!serverSocket.Connected)
                        throw new Exception();
                    chatMessageStreamServer = new StreamObjectReader(new NetworkStream(serverSocket));
                    var newMessage = chatMessageStreamServer.ReadMessage<ChatMessage>();
                    messageRecievedEvent(newMessage);
                    serverSocket.Disconnect(true);
                }
            }
            catch(IOException)
            {

            }
        }

        public ChatConnectionSocketClient(string clientName, Action<ChatMessage> messageRecievedEvent)
        {
            eventMessageReceived = new EventWaitHandle(false, EventResetMode.AutoReset, serverName + "eventMessageReceived");
            this.messageRecievedEvent += messageRecievedEvent;
            this.clientName = clientName;
            if (!Greet())
                throw new Exception();

            serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            chatListenerTask = Task.Factory.StartNew(() => { ChatListener(); });
        }

        public void Dispose()
        {
        }

        public bool SendMessage(string message)
        {
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverName, socketSettings.ClientSocketPort);
            if (!clientSocket.Connected)
                throw new Exception();
            chatMessageStreamClient = new StreamObjectReader(new NetworkStream(clientSocket));
            chatMessageStreamClient.WriteMessage(new ChatMessage() { UserName = clientName, Message = message, MessageSendDate = DateTime.Now });
            clientSocket.Dispose();
            return true;

        }
    }
}
