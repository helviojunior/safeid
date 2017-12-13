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
using IAM.Filters;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{
    
    public class RoleRules :  SqlBase, IDisposable
    {
        Dictionary<String, List<RoleRuleItem>> rules;

        public RoleRules()
        {
            rules = new Dictionary<String, List<RoleRuleItem>>();
        }

        public void GetDBConfig(SqlConnection conn)
        {
            base.Connection = conn;

            DataTable dt = ExecuteDataTable("select rp.resource_id, p.uri, f.id filter_id, f.name filter_name, r.name role_name, r.id role_id from resource_plugin_role_filter rpf with(nolock) inner join role r with(nolock) on rpf.role_id = r.id inner join filters f with(nolock) on f.id = rpf.filter_id inner join resource_plugin rp with(nolock) on rpf.resource_plugin_id = rp.id inner join plugin p with(nolock) on rp.plugin_id = p.id");

            if ((dt == null) || (dt.Rows.Count == 0))
                return;

            foreach (DataRow dr in dt.Rows)
            {
                if (!rules.ContainsKey(dr["resource_id"] + "-" + dr["uri"].ToString().ToLower()))
                    rules.Add(dr["resource_id"] + "-" + dr["uri"].ToString().ToLower(), new List<RoleRuleItem>());


                //Lista as condições
                FilterRule f = new FilterRule(dr["filter_name"].ToString());

                DataTable dt2 = ExecuteDataTable("select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = " + dr["filter_id"]);

                if ((dt2 != null) || (dt2.Rows.Count > 0))
                {

                    foreach (DataRow dr2 in dt2.Rows)
                        f.AddCondition(dr2["group_id"].ToString(), dr2["group_selector"].ToString(), (Int64)dr2["field_id"], dr2["field_name"].ToString(), dr2["data_type"].ToString(), dr2["text"].ToString(), dr2["condition"].ToString(), dr2["selector"].ToString());

                    RoleRuleItem fi = rules[dr["resource_id"] + "-" + dr["uri"].ToString().ToLower()].Find(ri => (ri.RoleId == (Int64)dr["role_id"]));

                    if (fi == null)
                    {
                        rules[dr["resource_id"] + "-" + dr["uri"].ToString().ToLower()].Add(new RoleRuleItem((Int64)dr["role_id"], dr["role_name"].ToString(), new FilterRuleCollection()));
                        fi = rules[dr["resource_id"] + "-" + dr["uri"].ToString().ToLower()].Find(ri => (ri.RoleId == (Int64)dr["role_id"]));
                    }

                    ((FilterRuleCollection)fi.FilterRuleCollection).AddFilterRule(f);

                }
            }

        }

        public List<RoleRuleItem> GetItem(Int64 ResourceId, String PluginUri)
        {
            if (!rules.ContainsKey(ResourceId + "-" + PluginUri))
                return null;

            return rules[ResourceId + "-" + PluginUri];
        }

        public void Dispose()
        {
            foreach (List<RoleRuleItem> li in rules.Values){
                foreach(RoleRuleItem i in li)
                    i.Dispose();
                
                li.Clear();
            }

            rules.Clear();
        }
    }
}
