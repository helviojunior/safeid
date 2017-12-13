using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CASPluginIAM
{
    [Serializable()]
    public class APIAccessToken : CAS.PluginInterface.CASJsonBase
    {
        public String access_token;
        public Int64 expires_in;
        public String error;
        public Int64 create_time;
        public Int64 userid;

        public APIAccessToken()
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.Now;

            create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
        }

        public String Authorization
        {
            get { return access_token; }
        }

        public Boolean IsValid
        {
            get
            {
                if ((access_token == null) || (access_token.Trim() == "") || (expires_in <= 0))
                    return false;

                DateTime utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                DateTime issueTime = DateTime.Now;

                //if ((Int64)issueTime.Subtract(utc0).TotalSeconds < (create_time + expires_in - 600)) //Com 10 minutos a menos
                if ((Int64)issueTime.Subtract(utc0).TotalSeconds < (create_time + expires_in)) //Com 10 minutos a menos
                    return true;

                return false;

            }
        }

        public void SaveToFile(String sufix)
        {
            if (create_time == 0)
            {
                var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var issueTime = DateTime.Now;

                create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
            }

            String jData = Serialize<APIAccessToken>(this);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            String tokenFile = Path.GetFullPath(asm.Location) + sufix + ".apiToken";
            File.WriteAllText(tokenFile, jData, Encoding.UTF8);
        }

        public void LoadFromFile(String sufix)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            String tokenFile = Path.GetFullPath(asm.Location) + sufix + ".apiToken";

            if (!File.Exists(tokenFile))
                return;

            String jData = File.ReadAllText(tokenFile, Encoding.UTF8);
            APIAccessToken item = Deserialize<APIAccessToken>(jData);

            this.access_token = item.access_token;
            this.create_time = item.create_time;
            this.expires_in = item.expires_in;
            this.userid = item.userid;
        }

    }
}
