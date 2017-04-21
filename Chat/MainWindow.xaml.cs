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

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatConnectionClient chatClient;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            chatClient.SendMessage(textBoxNewMessage.Text);
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            chatClient = new ChatConnectionClient(textBoxClientName.Text, AddMessage);
            ShowChatHistory();
        }

        private void ShowChatHistory()
        {
            for(int i = 0; i < chatClient.ChatHistory.Count(); i++)
            {
                textBoxChatMessages.Text += chatClient.ChatHistory[i].ToString();
            }
        }

		private void textBoxClientName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(textBoxClientName.Text == string.Empty)
			{
				textBoxClientName.Text = "DefaultName";
			}
		}

		public void AddMessage(ChatMessage message)
		{
			var del = new Action<string>(AddMessageCrossThread);
            if (message != null)
            {
                Dispatcher.Invoke(del, message.ToString());
            }

		}

		public void AddMessageCrossThread(string text)
		{
			textBoxChatMessages.Text += text;
		}

        public void window_Closed(object sender, EventArgs e)
        {
            chatClient.Dispose();
        }

    }
}
