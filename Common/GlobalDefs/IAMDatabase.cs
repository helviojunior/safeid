using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Configuration;
using System.Data;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;
using System.Web.UI;
using System.Web.Hosting;

namespace IAM.GlobalDefs
{
    [Serializable()]
    public class IAMDatabase: SqlBase, IDisposable
    {
        public IAMDatabase()
            : base() { }
        
        /*
        public IAMDatabase(ConnectionStringSettingsCollection connections)
            : base(connections["IAMDatabase"].ConnectionString) { }*/

        public IAMDatabase(String server, String dbName, String username, String password)
            : base(server, dbName, username, password) { }

        public IAMDatabase(SqlConnection connection)
            : base(connection) { }

        public IAMDatabase(String connectionString)
            : base(connectionString) { }
        
        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
        }

        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
        }

        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, Int64 executedByEntityId)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", executedByEntityId, null);
        }

        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, null);
        }

        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
        {
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, 0, transaction);
        }

        public void AddUserLog(LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId, SqlTransaction transaction)
        {
            DbParameterCollection par = new DbParameterCollection();
            par.Add("@date", typeof(DateTime)).Value = (date.HasValue ? date.Value : DateTime.Now);
            par.Add("@source", typeof(String), source.Length).Value = source;
            par.Add("@key", typeof(Int32)).Value = (Int32)key;
            par.Add("@level", typeof(Int32)).Value = (Int32)level;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyId;
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextId;
            par.Add("@resource_id", typeof(Int64)).Value = resourceId;
            par.Add("@plugin_id", typeof(Int64)).Value = pluginId;
            par.Add("@entity_id", typeof(Int64)).Value = entityId;
            par.Add("@identity_id", typeof(Int64)).Value = identityId;
            par.Add("@text", typeof(String), text.Length).Value = text;
            par.Add("@additional_data", typeof(String), additionalData.Length).Value = additionalData;
            par.Add("@executed_by_entity_id", typeof(Int64)).Value = executedByEntityId;

            ExecuteNonQuery("insert into logs ([date] ,[source] ,[key] ,[level] ,[proxy_id] ,[enterprise_id] ,[context_id] ,[resource_id] ,[plugin_id] ,[entity_id] ,[identity_id] ,[text] ,[additional_data], [executed_by_entity_id]) values (@date ,@source ,@key ,@level ,@proxy_id ,@enterprise_id ,@context_id ,@resource_id, @plugin_id ,@entity_id ,@identity_id ,@text ,@additional_data, @executed_by_entity_id)", System.Data.CommandType.Text, par, transaction);

        }


        public void AddLinkCount(Page page)
        {
            try
            {

                Int64 id = 0;

                if (!String.IsNullOrWhiteSpace((String)page.RouteData.Values["id"]))
                    Int64.TryParse((String)page.RouteData.Values["id"], out id);

                //Não selecinou nenhum item, ou seja menu principal
                if (id == 0)
                {
                    //Opções para gravação dos links na base
                    Int64 enterpriseId = 0;
                    Int64 userId = 0;
                    String submodule = "dashboard";

                    if ((page.Session["enterprise_data"]) != null && (page.Session["enterprise_data"] is EnterpriseData))
                        enterpriseId = ((EnterpriseData)page.Session["enterprise_data"]).Id;

                    if ((page.Session["login"]) != null && (page.Session["login"] is LoginData))
                        userId = ((LoginData)page.Session["login"]).Id;

                    if (userId > 0 && enterpriseId > 0)
                    {

                        String path = page.Request.ServerVariables["PATH_INFO"].ToLower();
                        String ApplicationVirtualPath = HostingEnvironment.ApplicationVirtualPath;
                        path = path.Substring(ApplicationVirtualPath.Length); //Corta o virtual path

                        if (path.IndexOf("_admin") == 0)
                            return;

                        if (!String.IsNullOrWhiteSpace((String)page.RouteData.Values["module"]))
                            submodule = (String)page.RouteData.Values["module"];

                        DbParameterCollection par = new DbParameterCollection();
                        par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                        par.Add("@entity_id", typeof(Int64)).Value = userId;
                        par.Add("@module", typeof(String)).Value = "admin";
                        par.Add("@submodule", typeof(String)).Value = submodule;
                        par.Add("@path", typeof(String)).Value = path;

                        ExecuteNonQuery("sp_insert_link_count", System.Data.CommandType.StoredProcedure, par);
                    }
                }
            }
            catch { }
        }


        public void ServiceStatus(String serviceName, String additionsData, SqlTransaction transaction)
        {
            DbParameterCollection par = new DbParameterCollection();
            par.Add("@name", typeof(String)).Value = serviceName;
            par.Add("@data", typeof(String)).Value = additionsData;

            ExecuteNonQuery("sp_service_status", System.Data.CommandType.StoredProcedure, par, transaction);
        }

        public String GetDBConfig(String key)
        {
            DataTable dt = Select("select * from server_config with(nolock) where data_name = '" + key + "'");
            if ((dt == null) || (dt.Rows.Count == 0))
                return "";

            return dt.Rows[0]["data_value"].ToString();
        }

        
        public void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text)
        {
            base.Connection = conn;
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", null);
        }

        public void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData)
        {
            base.Connection = conn;
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, null);
        }

        public void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, Int64 executedByEntityId)
        {
            base.Connection = conn;
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, "", executedByEntityId, null);
        }

        public void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId)
        {
            base.Connection = conn;
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, null);
        }

        public void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, SqlTransaction transaction)
        {
            base.Connection = conn;
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, 0, transaction);
        }

        public void AddUserLog(SqlConnection conn, LogKey key, DateTime? date, String source, UserLogLevel level, Int64 proxyId, Int64 enterpriseId, Int64 contextId, Int64 resourceId, Int64 pluginId, Int64 entityId, Int64 identityId, String text, String additionalData, Int64 executedByEntityId, SqlTransaction transaction)
        {
            base.Connection = conn;
            AddUserLog(key, date, source, level, proxyId, enterpriseId, contextId, resourceId, pluginId, entityId, identityId, text, additionalData, executedByEntityId, transaction);
        }


        static public SqlConnection GetWebConnection()
        {

            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["IAMDatabase"].ConnectionString);
            conn.Open();

            return conn;
        }

        static public String GetWebConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["IAMDatabase"].ConnectionString;
        }

        static public ConnectionStringSettings GetWebConnectionStringSettings()
        {
            return ConfigurationManager.ConnectionStrings["IAMDatabase"];
        }


        public void Dispose()
        {
            base.Dispose();
        }
    }
}
