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
    public class ChatConnectionServer : IChatConnectionServer
    {
		private Dictionary<string, string> chatHistory;

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

		private void ServerThread(object data)
        {
            NamedPipeServerStream pipeServer =
                new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxClientsNumber);

            int threadId = Thread.CurrentThread.ManagedThreadId;

            pipeServer.WaitForConnection();

            try
            {
                StreamString streamString = new StreamString(pipeServer);

				if(chatHistory != null)
				{
					foreach(var message in chatHistory)
					{
						StringBuilder messageToSend = new StringBuilder(message.Key);
						messageToSend.Append(": ");
						messageToSend.Append(message.Value);
						streamString.WriteString(messageToSend.ToString());
					}
				}
            }

            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            pipeServer.Close();
        }

		public ChatConnectionServer()
        {
			chatHistory = new Dictionary<string, string>();
			chatHistory.Add("Sarah Kerrigan", "Amun will be dead");
			Thread server = new Thread(ServerThread);
			server.Start();

		}
    }
}
