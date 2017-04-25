using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLibrary.Models;

namespace ChatLibrary
{
    public class ChatClients
    {
        private static ChatClients _instance = null;
        private static readonly object Padlock = new object();
        public ConcurrentBag<ChatClient> ChatClientsBag = new ConcurrentBag<ChatClient>();
        public static ChatClients Instance
        {
            get
            {
                lock (Padlock)
                {
                    return _instance ?? (_instance = new ChatClients());
                }
            }
        }
    }
}
