using System;
using System.Configuration;
using System.IO;
using System.IO.Pipes;
using ChatLibrary.Common;
using ChatLibrary.Models;

namespace ChatLibrary.Client
{
	public class ChatMessageClientServerStream : IDisposable
	{
		private readonly StreamObjectReader _chatMessageExchanger;
		private readonly string _clientName;
        private readonly string _clientId;
	    private readonly Stream _clientStream;

		private string ClientPipeName => ConfigurationManager.AppSettings["clientPipeName"] + _clientId;

		public ChatMessageClientServerStream(string clientName, string clientId)
		{
			this._clientName = clientName;
            this._clientId = clientId;
            var stream = new NamedPipeServerStream(ClientPipeName, PipeDirection.In, 1);
            stream.WaitForConnection();
            _clientStream = stream;
            _chatMessageExchanger = new StreamObjectReader(_clientStream);
		}

        public ChatMessageClientServerStream(Stream stream, string clientId, string clientName)
        {
            this._clientName = clientName;
            this._clientId = clientId;
            _clientStream = stream;
            _chatMessageExchanger = new StreamObjectReader(_clientStream);
        }

        public ChatMessage GetNextMessage()
		{
			var message = _chatMessageExchanger.ReadMessage<ChatMessage>();
            if (message == null)
                return null;
            if(_clientName != string.Empty)
			    message.UserName = _clientName;
			message.MessageSendDate = DateTime.Now;
			return message;
		}

        public void Dispose()
        {
            _clientStream.Dispose();
        }
    }
}
