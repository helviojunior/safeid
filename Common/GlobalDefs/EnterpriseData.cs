using System;
using System.Collections.Generic;
using System.Text;

namespace IAM.GlobalDefs
{
    [Serializable()]
    public class EnterpriseData : GlobalJson
    {
        public String Host { get; set; }
        public String Name { get; set; }
        public String Language { get; set; }
        public Int64 Id { get; set; }
        public String AuthPlugin { get; set; }

        public override string ToString()
        {
            return Serialize<EnterpriseData>(this);
        }
    }

}