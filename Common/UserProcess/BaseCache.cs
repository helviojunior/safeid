using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.UserProcess
{
    public class BaseCache
    {
        protected List<ValueCacheItem> Items { get; set; }

        public delegate Boolean CheckHasOther(Int64 context_id, Int64 entity_id, String value);

        public BaseCache()
        {
            this.Items = new List<ValueCacheItem>();
        }
        
        public void AddItem(Int64 context_id, Int64 entity_id, String value)
        {

            if (String.IsNullOrWhiteSpace(value))
                return;

            if (!Items.Exists(c => (c.context_id == context_id && c.entity_id == entity_id && c.value.ToLower() == value.ToLower())))
                Items.Add(new ValueCacheItem(context_id, entity_id, value));
        }


        public Boolean Exists(Int64 context_id, Int64 entity_id, String value)
        {
            if (Items == null)
                Items = new List<ValueCacheItem>();

            return Items.Exists(c => (c.context_id == context_id && c.entity_id == entity_id && c.value.ToLower() == value.ToLower()));
        }

        public Boolean HasOther(Int64 context_id, Int64 entity_id, String value)
        {
            if (Items == null)
                Items = new List<ValueCacheItem>();

            return Items.Exists(c => (c.context_id == context_id && c.entity_id != entity_id && c.value.ToLower() == value.ToLower()));
        }

        public void Clear()
        {
            if (Items == null)
                return;

            Items.Clear();

        }
    }

}
