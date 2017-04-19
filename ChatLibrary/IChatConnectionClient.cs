using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLibrary
{
    public interface IChatConnectionClient
    {
        void Connect();
        void SendMessage();
        void Syncronize();
    }
}
