using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineChat
{
    public class friend
    {
        public string name;
        public string ip;

        public friend()
        {
            ip = null;
            name = null;
        }
        public friend(string IP , string na)
        {
            name = na;
            ip = IP;
        }
    }
}
