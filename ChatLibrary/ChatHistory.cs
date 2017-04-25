using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLibrary.Models;

namespace ChatLibrary
{
    public class ChatHistory
    {
        private static ChatHistory instance = null;
        private static readonly object padlock = new object();
        public ConcurrentBag<ChatMessage> ChatMessages = new ConcurrentBag<ChatMessage>();
        public static ChatHistory Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ChatHistory();
                    }
                    return instance;
                }
            }
        }
    }
}
