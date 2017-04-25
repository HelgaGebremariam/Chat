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
using System.Runtime.Remoting;

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IChatConnectionClient chatClient;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitialState()
        {
            buttonConnect.IsEnabled = true;
            textBoxClientName.IsEnabled = true;
            buttonSendMessage.IsEnabled = false;
            radioButtonPipe.IsEnabled = true;
            radioButtonSocket.IsEnabled = true;
        }

        private void ConnectedState()
        {
            buttonConnect.IsEnabled = false;
            textBoxClientName.IsEnabled = false;
            buttonSendMessage.IsEnabled = true;
            labelServerError.Visibility = Visibility.Hidden;
            radioButtonPipe.IsEnabled = false;
            radioButtonSocket.IsEnabled = false;
        }

        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if(!chatClient.SendMessage(textBoxNewMessage.Text))
            {
                labelServerError.Visibility = Visibility.Visible;
                InitialState();
            }
            textBoxNewMessage.Text = string.Empty;
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            if(textBoxClientName.Text == string.Empty)
            {
                textBoxClientName.Text = "Unknown";
            }
            
            try
            {
                if(radioButtonPipe.IsChecked == true)
                    chatClient = new ChatConnectionPipeClient(textBoxClientName.Text, AddMessage);
                else
                    chatClient = new ChatConnectionSocketClient(textBoxClientName.Text, AddMessage);
                ShowChatHistory();
                ConnectedState();
            }
            catch(ServerException)
            {
                labelServerError.Visibility = Visibility.Visible;
            }
        }

        private void ShowChatHistory()
        {
            for(int i = 0; i < chatClient.ChatHistory.Count(); i++)
            {
                textBoxChatMessages.Text += chatClient.ChatHistory[i].ToString();
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
            if(chatClient != null)
                chatClient.Dispose();
        }

    }
}
