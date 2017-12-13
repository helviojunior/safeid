using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{
    public class IdentifyRuleField: IDisposable, ICloneable
    {
        public String FieldKey { get; internal set; }
        public Int64 FieldId { get; internal set; }
        public Boolean IsId { get; internal set; }
        public Boolean IsUniqueItem { get; internal set; }

        public IdentifyRuleField(String FieldKey, Int64 FieldId, Boolean IsId, Boolean IsUniqueItem)
        {
            this.FieldKey = FieldKey;
            this.FieldId = FieldId;
            this.IsId = IsId;
            this.IsUniqueItem = IsUniqueItem;
        }

        public void Dispose()
        {
            this.FieldKey = null;
        }

        public Object Clone()
        {
            return new IdentifyRuleField(this.FieldKey, this.FieldId, this.IsId, this.IsUniqueItem);
        }
    }

    public class IdentifyRuleItem: IDisposable, ICloneable
    {
        public Int64 ContextId { get; internal set; }
        public String PluginUri { get; internal set; }
        //public Boolean IsId { get; internal set; }
        //public Boolean IsUniqueItem { get; internal set; }

        private List<IdentifyRuleField> rules;

        public IdentifyRuleItem(Int64 ContextId, String PluginUri)
        {
            this.ContextId = ContextId;
            this.PluginUri = PluginUri;
            //this.IsId = IsId;
            //this.IsUniqueItem = IsUniqueItem;
            this.rules = new List<IdentifyRuleField>();
        }

        public void AddField(String FieldKey, Int64 FieldId, Boolean IsId, Boolean IsUniqueItem)
        {
            rules.Add(new IdentifyRuleField(FieldKey, FieldId, IsId, IsUniqueItem));
        }

        public Boolean HasUniqueItem()
        {
            return rules.Exists(f => (f.IsUniqueItem));
        }

        public Boolean HasID()
        {
            return rules.Exists(f => (f.IsId));
        }

        public IdentifyRuleField GetField(Int64 id)
        {
            return rules.Find(f => (f.FieldId == id));
        }

        public IdentifyRuleField GetField(String key)
        {
            return rules.Find(f => (f.FieldKey == key));
        }

        public Dictionary<Int64, String> GetFieldsId()
        {
            Dictionary<Int64, String> ret = new Dictionary<Int64, String>();
            foreach (IdentifyRuleField r in rules)
                ret.Add(r.FieldId, r.FieldKey);

            return ret;
        }

        public Dictionary<String, String> GetFieldsNameValue()
        {
            Dictionary<String, String> ret = new Dictionary<String, String>();
            foreach (IdentifyRuleField r in rules)
                ret.Add(r.FieldKey, null);

            return ret;
        }

        public Dictionary<Int64, String> GetFieldsIdValue()
        {
            Dictionary<Int64, String> ret = new Dictionary<Int64, String>();
            foreach (IdentifyRuleField r in rules)
                ret.Add(r.FieldId, null);

            return ret;
        }


        public void Dispose()
        {
            rules.Clear();
            PluginUri = null;
        }

        public Object Clone()
        {
            IdentifyRuleItem newItem = new IdentifyRuleItem(this.ContextId, this.PluginUri);
            foreach (IdentifyRuleField r in rules)
                newItem.AddField(r.FieldKey, r.FieldId, r.IsId, r.IsUniqueItem);

            return newItem;
        }
    }

    public class IdentifyRules :  SqlBase, IDisposable
    {
        Dictionary<String, IdentifyRuleItem> rules;

        public IdentifyRules()
        {
            rules = new Dictionary<String, IdentifyRuleItem>();
        }

        public void GetDBConfig(SqlConnection conn)
        {
            this.Connection = conn;

            DataTable dt = ExecuteDataTable("select rp.resource_id, m.field_id, m.data_name, p.uri, m.is_id, m.is_unique_property from resource_plugin rp with(nolock) inner join resource_plugin_mapping m with(nolock) on m.resource_plugin_id = rp.id inner join plugin p with(nolock) on rp.plugin_id = p.id where m.is_id = 1 or m.is_unique_property = 1");

            if ((dt == null) || (dt.Rows.Count == 0))
                return;

            foreach (DataRow dr in dt.Rows)
            {
                if (!rules.ContainsKey(dr["resource_id"] + "-" + dr["uri"].ToString().ToLower()))
                    rules.Add(dr["resource_id"] + "-" + dr["uri"].ToString().ToLower(), new IdentifyRuleItem((Int64)dr["resource_id"], dr["uri"].ToString()));

                rules[dr["resource_id"] + "-" + dr["uri"].ToString().ToLower()].AddField(dr["data_name"].ToString(), (Int64)dr["field_id"], (Boolean)dr["is_id"], (Boolean)dr["is_unique_property"]);
            }
        }

        public IdentifyRuleItem GetItem(Int64 ResourceId, String PluginUri)
        {
            if (!rules.ContainsKey(ResourceId + "-" + PluginUri))
                return null;

            return (IdentifyRuleItem)(rules[ResourceId + "-" + PluginUri].Clone());
        }


        public void Dispose()
        {
            foreach (IdentifyRuleItem li in rules.Values)
                li.Dispose();

            rules.Clear();
        }
    }
}
