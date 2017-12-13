using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.UserProcess
{
    public class ValueCacheItem : IDisposable
    {
        public Int64 entity_id { get; protected set; }
        public Int64 context_id { get; protected set; }
        public String value { get; protected set; }

        public ValueCacheItem(Int64 context_id, Int64 entity_id, String value)
        {
            this.entity_id = entity_id;
            this.context_id = context_id;
            this.value = value;
        }

        public void Dispose()
        {
            this.value = null;
        }
    }
    
}
