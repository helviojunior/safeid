using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linux
{
    internal class UserData
    {
        public String UserId { get; set; }
        public String Username { get; set; }
        public String DefaultGroup { get; set; }
        public String Information { get; set; }
        public String HomePath { get; set; }
        public String Bash { get; set; }

        public UserData()
        {

        }

        public UserData(String UserId, String Username, String DefaultGroup, String Information, String HomePath, String Bash)
        {
            this.UserId = UserId;
            this.Username = Username;
            this.DefaultGroup = DefaultGroup;
            this.Information = Information;
            this.HomePath = HomePath;
            this.Bash = Bash;
        }
            /*
         ret.fields.Add("username", new List<string>());
                        ret.fields.Add("user_id", new List<string>());
                        ret.fields.Add("default_group", new List<string>());
                        ret.fields.Add("information", new List<string>());
                        ret.fields.Add("home_path", new List<string>());
                        ret.fields.Add("bash", new List<string>());*/

    }
}
