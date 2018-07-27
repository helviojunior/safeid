using System;
using System.Collections.Generic;
using System.Text;

namespace IAM.GlobalDefs
{

    [Serializable()]
    public class LoginData : GlobalJson
    {
        public Int64 Id { get; set; }
        public Int64 EnterpriseId { get; set; }
        public String Alias { get; set; }
        public String Login { get; set; }
        public Byte SecurityToken { get; set; }
        public String FullName { get; set; }
        public String CASGrantTicket { get; set; }
        public String CASLongTicket { get; set; }

        public override string ToString()
        {
            return Serialize<LoginData>(this);
        }
    }

}
