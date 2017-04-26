using System;

namespace ChatLibrary.Models
{
    [Serializable]
    public class SocketSettings
    {
        public int ServerSocketPort { get; set; }
        public int ClientSocketPort { get; set; }
        public string EventWaitHandleEventName { get; set; }
    }
}
