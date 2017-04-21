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

namespace ChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ChatConnectionServer chatConnectionServer;
        public MainWindow()
        {
            InitializeComponent();
			chatConnectionServer = new ChatConnectionServer(AddMessage);
            ShowChatHistory();
        }

        public void ShowChatHistory()
        {
            for(int i = 0; i < chatConnectionServer.ChatHistory.Count(); i++)
            {
				AddMessage(chatConnectionServer.ChatHistory.ElementAt(i));
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
