using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
