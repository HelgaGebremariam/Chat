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
using ChatLibrary.Client;

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IChatConnectionClient _chatClient;
        public MainWindow()
        {
            InitializeComponent();
            InitialState();

        }

        private void InitialState()
        {
            buttonConnect.IsEnabled = true;
            textBoxClientName.IsEnabled = true;
            buttonSendMessage.IsEnabled = false;
            textBoxNewMessage.IsEnabled = false;
            radioButtonPipe.IsEnabled = true;
            radioButtonSocket.IsEnabled = true;
        }

        private void ConnectedState()
        {
            buttonConnect.IsEnabled = false;
            textBoxClientName.IsEnabled = false;
            buttonSendMessage.IsEnabled = true;
            textBoxNewMessage.IsEnabled = true;
            labelServerError.Visibility = Visibility.Hidden;
            radioButtonPipe.IsEnabled = false;
            radioButtonSocket.IsEnabled = false;
        }

        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if(!_chatClient.SendMessage(textBoxNewMessage.Text))
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
                if (radioButtonPipe.IsChecked == true)
                {
                    _chatClient = new ChatConnectionPipeClient(AddMessage);
                }
                else
                {
                    _chatClient = new ChatConnectionSocketClient(AddMessage);
                }
                _chatClient.Connect(textBoxClientName.Text);
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
            for(var i = 0; i < _chatClient.ChatHistory.Count(); i++)
            {
                textBoxChatMessages.Text += _chatClient.ChatHistory[i].ToString();
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
            _chatClient?.Dispose();
        }

        private static void ResizeControl(FrameworkElement control, double xChange, double yChange)
        {
            control.Height = control.ActualHeight * yChange;
            control.Width = control.ActualWidth * xChange;

            Canvas.SetTop(control, Canvas.GetTop(control) * yChange);
            Canvas.SetLeft(control, Canvas.GetLeft(control) * xChange);
        }
        
        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Width = e.NewSize.Width;
            this.Height = e.NewSize.Height;

            double xChange = 1, yChange = 1;
            if (Math.Abs(e.PreviousSize.Width) > 0)
                xChange = (e.NewSize.Width / e.PreviousSize.Width);

            if (Math.Abs(e.PreviousSize.Height) > 0)
                yChange = (e.NewSize.Height / e.PreviousSize.Height);

            ResizeControl(textBoxClientName, xChange, yChange);
            ResizeControl(textBoxNewMessage, xChange, yChange);
            ResizeControl(scrollViewerChatMessages, xChange, yChange);
            ResizeControl(buttonConnect, xChange, yChange);
            ResizeControl(buttonSendMessage, xChange, yChange);
        }
    }
}
