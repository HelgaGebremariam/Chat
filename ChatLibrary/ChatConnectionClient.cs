using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;

namespace ChatLibrary
{
    public class ChatConnectionClient : IChatConnectionClient
    {
		public List<string> ChatHistory { get; set; }

		private int maxClientsNumber
		{
			get
			{
				return Convert.ToInt32(ConfigurationManager.AppSettings["maxClientsNumber"]);
			}
		}

		private string pipeName
		{
			get
			{
				return ConfigurationManager.AppSettings["pipeName"];
			}
		}

		private string serverName
		{
			get
			{
				return ConfigurationManager.AppSettings["serverName"];
			}
		}

		public void UploadChatHistory(StreamString streamString)
		{
			ChatHistory = new List<string>();
			ChatHistory.Add(streamString.ReadString());
		}

		public ChatConnectionClient()
		{
			NamedPipeClientStream pipeClient = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut);
			pipeClient.Connect();
			StreamString streamString = new StreamString(pipeClient);

			UploadChatHistory(streamString);
		}

        public void Connect()
        {

        }

        public void SendMessage()
        {

        }

        public void Syncronize()
        {

        }
    }
}
