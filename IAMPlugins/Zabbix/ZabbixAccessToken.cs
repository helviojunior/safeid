using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using SafeTrend.Json;
using System.IO;

namespace Zabbix
{
    

    [Serializable()]
    public class ZabbixAccessToken
    {
        [OptionalField()]
        public String access_token;
        
        [OptionalField()]
        public String error;

        [OptionalField()]
        public Version api_ver;

        [OptionalField()]
        public Int64 create_time;
        
        [OptionalField()]
        public Int64 expires_in;

        public ZabbixAccessToken()
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.UtcNow;
            var expireTime = DateTime.UtcNow.AddMinutes(20);

            create_time = (int)issueTime.Subtract(utc0).TotalSeconds;
            expires_in = (int)expireTime.Subtract(utc0).TotalSeconds;
        }

        //Authorization: Bearer ya29.AHES6ZTCwB_tZDfoBGQQKYZDrSr9_qNrY-CYjM0Uu1aa3g
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
                DateTime issueTime = DateTime.UtcNow;

                if ((Int64)issueTime.Subtract(utc0).TotalSeconds < (create_time + expires_in - 60)) //Com 1 minutos a menos
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

                String jData = JSON.Serialize<ZabbixAccessToken>(this);
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
                String tokenFile = Path.GetFullPath(asm.Location) + "-" + cacheId + ".zToken";
                File.WriteAllText(tokenFile, jData, Encoding.UTF8);
            }
            catch { }
        }

        public void LoadFromFile(String cacheId)
        {
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
                String tokenFile = Path.GetFullPath(asm.Location) + "-" + cacheId + ".zToken";

                if (!File.Exists(tokenFile))
                    return;
                
                String jData = File.ReadAllText(tokenFile, Encoding.UTF8);
                ZabbixAccessToken item = JSON.Deserialize<ZabbixAccessToken>(jData);

                this.access_token = item.access_token;
                this.create_time = item.create_time;
                this.error = item.error;
                this.expires_in = item.expires_in;
            }
            catch {
                return;
            }

        }

    }
}
