using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLibrary.Models
{
    [Serializable]
    public class ChatMessage
    {
        public string UserName;
        public string Message;
        public DateTime MessageSendDate;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[");
            sb.Append(MessageSendDate);
            sb.Append("] ");
            sb.Append(UserName);
            sb.Append(": ");
            sb.AppendLine(Message);
            return sb.ToString();
        }
    }
}
