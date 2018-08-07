using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p2pServer
{
    public class ChatMember
    {
        private string IP;
        private string name;
        

        public ChatMember(string ip , string na)
        {
            IP = ip;
            name = na;
        }
    }
}
