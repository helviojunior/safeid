using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Data;
using System.Data.SqlClient;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{
    [Serializable()]
    public class PluginConfigMapping: IDisposable, ICloneable
    {
        public Int64 field_id;
        public String field_name;
        public String data_name;
        public String data_type;
        public Boolean is_id;
        public Boolean is_password;
        public Boolean is_property;
        public Boolean is_unique_property;
        public Boolean is_login;
        public Boolean is_name;

        public PluginConfigMapping(Int64 field_id, String field_name, String data_name, String data_type, Boolean is_id, Boolean is_password, Boolean is_property, Boolean is_unique_property, Boolean is_login, Boolean is_name)
        {
            this.field_id = field_id;
            this.field_name = field_name;
            this.data_name = data_name;
            this.data_type = data_type;
            this.is_id = is_id;
            this.is_password = is_password;
            this.is_property = is_property;
            this.is_unique_property = is_unique_property;
            this.is_login = is_login;
            this.is_name = is_name;
        }

        public void Dispose()
        {
            this.data_name = null;
            this.data_type = null;
        }

        public Object Clone()
        {
            return new PluginConfigMapping(
                this.field_id,
                this.field_name,
                this.data_name,
                this.data_type,
                this.is_id,
                this.is_password,
                this.is_property,
                this.is_unique_property,
                this.is_login,
                this.is_name);
        }
    }


    [Serializable()]
    public class PluginConfig : IAMDatabase, IDisposable
    {
        public String uri;
        public String assembly;
        public Int64 resource;
        public Int64 resource_plugin;
        public Int64 plugin_id;
        public Boolean enable_import;
        public Boolean enable_deploy;
        public Boolean permit_add_entity;
        public Boolean import_groups;
        public Boolean import_containers;
        public String mail_domain;
        public Boolean build_login;
        public Boolean build_mail;
        public Int64 name_field_id;
        public Int64 mail_field_id;
        public Int64 login_field_id;
        public Int32 order;
        public String parameters;
        public List<PluginConfigMapping> mapping;
        public String schedule;

        public Dictionary<String, String> mappingDataTypeDic
        {
            get
            {
                Dictionary<String, String> tmp = new Dictionary<string, string>();
                foreach (PluginConfigMapping p in mapping)
                    tmp.Add(p.data_name, p.data_type);
                return tmp;
            }
        }

        public PluginConfig(SqlConnection conn, String scheme, Int64 pluginId, Int64 resourcePluginId) :
            this(null, conn, scheme, pluginId, resourcePluginId) { }


        public PluginConfig(OpenSSL.X509.X509Certificate cert, SqlConnection conn, String scheme, Int64 pluginId, Int64 resourcePluginId)
        {
            this.Connection = conn;

            switch (scheme.ToLower())
            {

                case "connector":

                    DataTable dt = ExecuteDataTable("select p.id plugin_id, p.uri, p.[assembly], rp.*, rp.id resource_plugin_id from plugin p with(nolock) inner join resource_plugin rp with(nolock) on rp.plugin_id = p.id inner join [resource] r with(nolock) on r.id = rp.resource_id where r.enabled = 1 and rp.enabled = 1 and rp.id = " + resourcePluginId);
                    if ((dt != null) && (dt.Rows.Count > 0))
                    {
                        DataRow dr = dt.Rows[0];

                        DataTable dt2 = ExecuteDataTable("select top 1 schedule from resource_plugin_schedule with(nolock) where resource_plugin_id = " + dr["resource_plugin_id"].ToString());
                        if ((dt2 != null) && (dt2.Rows.Count > 0))
                            this.schedule = dt2.Rows[0]["schedule"].ToString();

                        this.mapping = new List<PluginConfigMapping>();

                        //Adiciona os mapeamentos padrões (login, e-mail e nome), se estiver mapeado
                        DataTable dt3 = ExecuteDataTable("select rp.id resource_plugin_id, f.id field_id, f.name field_name, 'login' data_name, f.data_type, cast(0 as bit) is_password, cast(0 as bit) is_property, cast(0 as bit) is_id, is_unique_property = case when f.id = rp.login_field_id then cast(1 as bit) else cast(0 as bit) end from resource_plugin rp with(nolock) inner join field f with(nolock) on rp.login_field_id = f.id where rp.id = " + dr["resource_plugin_id"].ToString());
                        if ((dt3 != null) && (dt3.Rows.Count > 0))
                            foreach (DataRow dr3 in dt3.Rows)
                                this.mapping.Add(new PluginConfigMapping(
                                    (Int64)dr3["field_id"],
                                    dr3["field_name"].ToString(), 
                                    dr3["data_name"].ToString(), 
                                    dr3["data_type"].ToString(), 
                                    (Boolean)dr3["is_id"], 
                                    (Boolean)dr3["is_password"], 
                                    (Boolean)dr3["is_property"], 
                                    (Boolean)dr3["is_unique_property"],
                                    ((Int64)dr["login_field_id"] == (Int64)dr3["field_id"]),
                                    ((Int64)dr["name_field_id"] == (Int64)dr3["field_id"])
                                    ));


                        //Adiciona os mapeamentos
                        DataTable dt4 = ExecuteDataTable("select m.*, f.data_type, f.name field_name from resource_plugin_mapping m with(nolock) inner join resource_plugin rp with(nolock) on rp.id = m.resource_plugin_id inner join field f with(nolock) on m.field_id = f.id where rp.id = " + dr["resource_plugin_id"].ToString());
                        if ((dt4 != null) && (dt4.Rows.Count > 0))
                            foreach (DataRow dr4 in dt4.Rows)
                                this.mapping.Add(new PluginConfigMapping(
                                    (Int64)dr4["field_id"],
                                    dr4["field_name"].ToString(), 
                                    dr4["data_name"].ToString(), 
                                    dr4["data_type"].ToString(), 
                                    (Boolean)dr4["is_id"], 
                                    (Boolean)dr4["is_password"], 
                                    (Boolean)dr4["is_property"], 
                                    (Boolean)dr4["is_unique_property"],
                                    ((Int64)dr["login_field_id"] == (Int64)dr4["field_id"]),
                                    ((Int64)dr["name_field_id"] == (Int64)dr4["field_id"])
                                    ));
                        
                        //Adiciona o campo de login caso não exista
                        DataTable dt5 = ExecuteDataTable("select rp.id resource_plugin_id, f.id field_id, f.name field_name, 'login' data_name, f.data_type, cast(0 as bit), cast(0 as bit), cast(0 as bit) is_id, is_unique_property = case when f.id = rp.login_field_id then cast(1 as bit) else cast(0 as bit) end from resource_plugin rp with(nolock) inner join field f with(nolock) on rp.login_field_id = f.id where rp.id = " + dr["resource_plugin_id"].ToString());
                        if ((dt5 != null) && (dt5.Rows.Count > 0))
                            foreach (DataRow dr5 in dt5.Rows)
                                if (!this.mapping.Exists(m => (m.is_login)))
                                    this.mapping.Add(new PluginConfigMapping(
                                        (Int64)dr5["field_id"],
                                        dr5["field_name"].ToString(),
                                        dr5["data_name"].ToString(),
                                        dr5["data_type"].ToString(),
                                        (Boolean)dr5["is_id"],
                                        (Boolean)dr5["is_password"],
                                        (Boolean)dr5["is_property"],
                                        (Boolean)dr5["is_unique_property"],
                                        ((Int64)dr["login_field_id"] == (Int64)dr5["field_id"]),
                                        ((Int64)dr["name_field_id"] == (Int64)dr5["field_id"])
                                        ));


                        this.uri = dr["uri"].ToString();
                        this.assembly = dr["assembly"].ToString();
                        this.resource = (Int64)dr["resource_id"];
                        this.resource_plugin = (Int64)dr["id"];
                        this.name_field_id = (Int64)dr["name_field_id"];
                        this.mail_field_id = (Int64)dr["mail_field_id"];
                        this.login_field_id = (Int64)dr["login_field_id"];
                        this.enable_import = (Boolean)dr["enable_import"];
                        this.enable_deploy = (Boolean)dr["enable_deploy"];
                        this.import_groups = (Boolean)dr["import_groups"];
                        this.import_containers = (Boolean)dr["import_containers"];
                        this.permit_add_entity = (Boolean)dr["permit_add_entity"];
                        this.mail_domain = dr["mail_domain"].ToString();
                        this.build_login = (Boolean)dr["build_login"];
                        this.build_mail = (Boolean)dr["build_mail"];
                        this.order = (Int32)dr["order"];
                        this.plugin_id = (Int64)dr["plugin_id"];

                        if (cert != null)
                        {
                            JsonGeneric cfg = new JsonGeneric();
                            cfg.fields = new String[] { "key", "value" };

                            DataTable dt1 = ExecuteDataTable("select [key], [value] from resource_plugin_par with(nolock) where resource_plugin_id = " + dr["resource_plugin_id"].ToString());
                            if ((dt1 != null) && (dt1.Rows.Count > 0))
                                foreach (DataRow dr1 in dt1.Rows)
                                    cfg.data.Add(new String[] { dr1["key"].ToString(), dr1["value"].ToString() });

                            using (CryptApi cApi = new CryptApi(cert, Encoding.UTF8.GetBytes(cfg.ToJsonString())))
                                parameters = Convert.ToBase64String(cApi.ToBytes());

                        }
                    }
                    break;

                case "agent":
                    DataTable dtA = ExecuteDataTable("select p.id plugin_id, p.uri, p.[assembly], pp.id proxy_plugin_id from plugin p with(nolock) inner join proxy_plugin pp with(nolock) on pp.plugin_id = p.id where pp.enabled = 1 and p.id = " + pluginId);
                    if ((dtA != null) && (dtA.Rows.Count > 0))
                    {
                        DataRow dr = dtA.Rows[0];

                        this.uri = dr["uri"].ToString();
                        this.assembly = dr["assembly"].ToString();
                        this.plugin_id = (Int64)dr["plugin_id"];

                        if (cert != null)
                        {
                            JsonGeneric cfg = new JsonGeneric();
                            cfg.fields = new String[] { "key", "value" };

                            DataTable dt1 = ExecuteDataTable("select [key], [value] from proxy_plugin_par with(nolock) where proxy_plugin_id = " + dr["proxy_plugin_id"].ToString());
                            if ((dt1 != null) && (dt1.Rows.Count > 0))
                                foreach (DataRow dr1 in dt1.Rows)
                                    cfg.data.Add(new String[] { dr1["key"].ToString(), dr1["value"].ToString() });

                            using (CryptApi cApi = new CryptApi(cert, Encoding.UTF8.GetBytes(cfg.ToJsonString())))
                                parameters = Convert.ToBase64String(cApi.ToBytes());

                        }
                    }
                    break;
            }
        }

        public override string ToString()
        {
            String data = "";

            data += "General: " + Environment.NewLine;
            data += "\tURI: " + this.uri.ToString() + Environment.NewLine;
            data += "\tAssembly: " + this.assembly.ToString() + Environment.NewLine;
            data += "\tResource: " + this.resource.ToString() + Environment.NewLine;
            data += "\tResource Plugin: " + this.resource_plugin.ToString() + Environment.NewLine;
            data += "\tPlugin id: " + this.plugin_id.ToString() + Environment.NewLine;
            data += "\tmail_domain: " + this.mail_domain.ToString() + Environment.NewLine;

            data += "Import: " + Environment.NewLine;
            data += "\tEnable import: " + this.enable_import.ToString() + Environment.NewLine;
            data += "\tPermit add entity: " + this.permit_add_entity.ToString() + Environment.NewLine;
            data += "\tBuild login: " + this.build_login.ToString() + Environment.NewLine;
            data += "\tBuild mail: " + this.build_mail.ToString() + Environment.NewLine;
            data += "\tImport groups: " + this.import_groups.ToString() + Environment.NewLine;
            data += "\tImport containers: " + this.import_containers.ToString() + Environment.NewLine;


            data += "Deploy: " + Environment.NewLine;
            data += "\tEnable Deploy: " + this.enable_deploy.ToString() + Environment.NewLine;

            data += "Fields: " + Environment.NewLine;
            data += "\tName field id: " + this.name_field_id.ToString() + Environment.NewLine;
            data += "\tMail field id: " + this.mail_field_id.ToString() + Environment.NewLine;
            data += "\tLogin field id: " + this.login_field_id.ToString() + Environment.NewLine;

            return data;
        }



        public void Dispose()
        {
            this.uri = null;
            this.assembly = null;
            this.schedule = null;
            this.mail_domain = null;
            this.parameters = null;

            if (this.mapping != null)
            {
                foreach (PluginConfigMapping m in this.mapping)
                    if (m != null) m.Dispose();

                this.mapping.Clear();
            }
            this.mapping = null;
            
        }

    }
}
