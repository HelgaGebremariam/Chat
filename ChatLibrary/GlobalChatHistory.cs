using System.Collections.Concurrent;
using ChatLibrary.Models;

namespace ChatLibrary
{
    public class GlobalChatHistory
    {
        private static GlobalChatHistory _instance = null;
        private static readonly object Padlock = new object();
        public ConcurrentBag<ChatMessage> ChatMessages = new ConcurrentBag<ChatMessage>();
        public static GlobalChatHistory Instance
        {
            get
            {
                lock (Padlock)
                {
                    return _instance ?? (_instance = new GlobalChatHistory());
                }
            }
        }
    }
}
