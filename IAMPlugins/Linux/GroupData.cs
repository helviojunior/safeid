using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linux
{
    internal class GroupData
    {
        public String GroupId { get; set; }
        public String Groupname { get; set; }
        public List<String> Users { get; set; }

        public GroupData()
        {
            this.Users = new List<string>();
        }

    }
}
