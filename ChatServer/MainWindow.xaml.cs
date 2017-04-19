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
			chatConnectionServer = new ChatConnectionServer();
            ShowChatHistory();
        }

        public void ShowChatHistory()
        {
            for(int i = 0; i < chatConnectionServer.ChatHistory.Count(); i++)
            {
                textBlockChatHistory.Text += chatConnectionServer.ChatHistory.ElementAt(i).ToString();
            }
        }

    }
}
