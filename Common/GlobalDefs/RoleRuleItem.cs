using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.GlobalDefs
{
    public class RoleRuleItem : IDisposable, ICloneable
    {
        public Int64 RoleId { get; internal set; }
        public String Name { get; internal set; }
        public Object FilterRuleCollection { get; internal set; }

        public RoleRuleItem(Int64 roleId, String name, Object filterRuleCollection)
        {
            this.RoleId = roleId;
            this.FilterRuleCollection = filterRuleCollection;
            this.Name = name;
        }

        public void Dispose()
        {
            Name = null;
            FilterRuleCollection = null;
        }

        public Object Clone()
        {
            return new RoleRuleItem(this.RoleId, this.Name, this.FilterRuleCollection);
        }
    }

}
