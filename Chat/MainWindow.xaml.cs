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

        private void InitialState()
        {
            buttonConnect.IsEnabled = true;
            textBoxClientName.IsEnabled = true;
            buttonSendMessage.IsEnabled = false;
        }

        private void ConnectedState()
        {
            buttonConnect.IsEnabled = false;
            textBoxClientName.IsEnabled = false;
            buttonSendMessage.IsEnabled = true;
            labelServerError.Visibility = Visibility.Hidden;
        }

        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if(!chatClient.SendMessage(textBoxNewMessage.Text))
            {
                labelServerError.Visibility = Visibility.Visible;
                InitialState();
            }
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            if(textBoxClientName.Text == string.Empty)
            {
                textBoxClientName.Text = "Unknown";
            }
            
            try
            {
                chatClient = new ChatConnectionClient(textBoxClientName.Text, AddMessage);
                ShowChatHistory();
                ConnectedState();
            }
            catch(Exception ex)
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
            chatClient.Dispose();
        }

    }
}
