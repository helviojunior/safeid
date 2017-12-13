using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using SafeTrend.Json;
using System.IO;

namespace GoogleAdmin
{
    [Serializable()]
    public class GoogleAccessToken
    {
        [OptionalField()]
        public String token_type;
        [OptionalField()]
        public String access_token;
        [OptionalField()]
        public String expires_in;
        [OptionalField()]
        public String error;

        [OptionalField()]
        public String customer_id;

        [OptionalField()]
        public Int64 create_time;

        public GoogleAccessToken()
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.UtcNow;

            create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
        }

        //Authorization: Bearer ya29.AHES6ZTCwB_tZDfoBGQQKYZDrSr9_qNrY-CYjM0Uu1aa3g
        public String Authorization
        {
            get { return token_type + " " + access_token; }
        }

        public Boolean IsValid
        {
            get
            {
                if ((access_token == null) || (access_token.Trim() == "") || (token_type == null) || (token_type.Trim() == "") || (expires_in == null) || (expires_in.Trim() == ""))
                    return false;

                Int64 exp = 0;
                try
                {
                    exp = Int64.Parse(expires_in);
                }
                catch { return false; }

                DateTime utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                DateTime issueTime = DateTime.UtcNow;

                if ((Int64)issueTime.Subtract(utc0).TotalSeconds < (create_time + exp - 600)) //Com 10 minutos a menos
                    return true;

                return false;

            }
        }

        public void SaveToFile(String cacheId)
        {
            try
            {
                if (create_time == 0)
                {
                    var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    var issueTime = DateTime.UtcNow;

                    create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
                }

                String jData = JSON.Serialize<GoogleAccessToken>(this);
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
                String tokenFile = Path.GetFullPath(asm.Location) + "-" + cacheId + ".gToken";
                File.WriteAllText(tokenFile, jData, Encoding.UTF8);
            }
            catch { }
        }

        public void LoadFromFile(String cacheId)
        {
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
                String tokenFile = Path.GetFullPath(asm.Location) + "-" + cacheId + ".gToken";

                if (!File.Exists(tokenFile))
                    return;
                
                String jData = File.ReadAllText(tokenFile, Encoding.UTF8);
                GoogleAccessToken item = JSON.Deserialize<GoogleAccessToken>(jData);

                this.access_token = item.access_token;
                this.token_type = item.token_type;
                this.create_time = item.create_time;
                this.error = item.error;
                this.expires_in = item.expires_in;
                this.customer_id = item.customer_id;
            }
            catch {
                return;
            }

        }

    }
}
