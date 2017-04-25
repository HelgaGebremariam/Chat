using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChatLibrary;
using ChatLibrary.Models;
using ChatLibrary.Interfaces;

namespace ChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ChatLibrary.ChatServer chatConnectionServer;
        public MainWindow()
        {
            InitializeComponent();
			chatConnectionServer = new ChatLibrary.ChatServer(AddMessage);
            ShowChatHistory();
        }

        public void ShowChatHistory()
        {
            for(int i = 0; i < ChatHistory.Instance.ChatMessages.Count(); i++)
            {
				AddMessage(ChatHistory.Instance.ChatMessages.ElementAt(i));
            }
        }

		public void AddMessage(ChatMessage message)
		{
            if (message == null)
                return;
            var del = new Action<string>(AddMessageCrossThread);
			Dispatcher.Invoke(del, message.ToString());
			
		}

		public void AddMessageCrossThread(string text)
		{
			textBlockChatHistory.Text += text;
		}

        public void window_Closed(object sender, EventArgs e)
        {
            if(chatConnectionServer != null)
                chatConnectionServer.Dispose();
        }
    }
}
