/// 
/// @file apiinfo.cs
/// <summary>
/// Implementações da classe APIInfo. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 19/10/2013
/// $Id: apiinfo.cs, v1.0 2013/10/19 Helvio Junior $

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.WebAPI;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Proxy : IAMAPIBase
    {
        public override event Error Error;
        public override event ExternalAccessControl ExternalAccessControl;

        /// <summary>
        /// Método de processamentoda requisição
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="enterpriseId">ID da empresa</param>
        /// <param name="method">String com o método que deverá ser processado</param>
        /// <param name="auth">String com a chave de autenticação.</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        public override Object iProcess(IAMDatabase database, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters)
        {

            this._enterpriseId = enterpriseId;
            //base.Connection = sqlConnection;

            method = method.ToLower();
            String[] mp = method.Split(".".ToCharArray(), 2);

            if (mp.Length != 2)
                return null;

            if (this.GetType().Name.ToLower() != mp[0])
                return null;

            Acl = ValidateCtrl(database, method, auth, parameters, ExternalAccessControl);
            if (!Acl.Result)
            {
                Error(ErrorType.InvalidParameters, "Not authorized", "", null);
                return null;
            }

            switch (mp[1])
            {
                case "new":
                    return newproxy(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "list":
                case "search":
                    return list(database, parameters);
                    break;

                case "delete":
                    return delete(database, parameters);
                    break;

                case "restart":
                    return restart(database, parameters);
                    break;

                case "change":
                    return change(database, parameters);
                    break;

                default:
                    Error(ErrorType.InvalidRequest, "JSON-rpc method is unknow.", "", null);
                    return null;
                    break;
            }

            return null;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> newproxy(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("name"))
            {
                Error(ErrorType.InvalidRequest, "Parameter name is not defined.", "", null);
                return null;
            }

            String name = parameters["name"].ToString();
            if (String.IsNullOrWhiteSpace(name))
            {
                Error(ErrorType.InvalidRequest, "Parameter name is not defined.", "", null);
                return null;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_-]+$"))
            {
                Error(ErrorType.InvalidRequest, "Invalid name. The name can contain letters ('a' to 'z' and 'A' to 'Z'), numbers (0-9), underscore ('_') and hyphens ('-')", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@proxy_name", typeof(String)).Value = name;

            DataTable dtProxy = database.ExecuteDataTable( "sp_new_proxy", CommandType.StoredProcedure, par, null);
            if (dtProxy == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtProxy.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Proxy not found.", "", null);
                return null;
            }

            DataRow dr1 = dtProxy.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("proxy_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("last_sync", (dr1["last_sync"] != DBNull.Value ? (Int32)((((DateTime)dr1["last_sync"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("last_sync_address", dr1["address"]);
            newItem.Add("last_sync_version", dr1["version"]);
            newItem.Add("resource_qty", dr1["resource_qty"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);


            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> get(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("proxyid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return null;
            }


            String proxy = parameters["proxyid"].ToString();
            if (String.IsNullOrWhiteSpace(proxy))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return null;
            }

            Int64 proxyid = 0;
            try
            {
                proxyid = Int64.Parse(proxy);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyid;


            DataTable dtPlugin = database.ExecuteDataTable( "select p.*, resource_qty = (select COUNT(distinct r1.proxy_id) from resource r1 with(nolock) where r1.proxy_id = p.id) from proxy p with(nolock) where p.enterprise_id = @enterprise_id and p.id = @proxy_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Proxy not found.", "", null);
                return null;
            }


            DataRow dr1 = dtPlugin.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("proxy_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("last_sync", (dr1["last_sync"] != DBNull.Value ? (Int32)((((DateTime)dr1["last_sync"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("last_sync_address", dr1["address"]);
            newItem.Add("last_sync_version", dr1["version"]);
            newItem.Add("last_sync_pid", dr1["pid"]);
            newItem.Add("resource_qty", dr1["resource_qty"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);


            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean delete(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("proxyid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return false;
            }


            String proxy = parameters["proxyid"].ToString();
            if (String.IsNullOrWhiteSpace(proxy))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return false;
            }

            Int64 proxyid = 0;
            try
            {
                proxyid = Int64.Parse(proxy);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyid;

            DataTable dtProxy = database.ExecuteDataTable( "select p.*, resource_qty = (select COUNT(distinct r1.proxy_id) from resource r1 with(nolock) where r1.proxy_id = p.id) from proxy p where (p.enterprise_id = @enterprise_id or p.enterprise_id = 0) and p.id = @proxy_id", CommandType.Text, par, null);
            if (dtProxy == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtProxy.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Proxy not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtProxy.Rows[0]["resource_qty"] > 0)
            {
                Error(ErrorType.SystemError, "Plugin is being used and can not be deleted.", "", null);
                return false;
            }

            database.ExecuteNonQuery( "delete from proxy where id = @proxy_id", CommandType.Text, par);
            database.AddUserLog(LogKey.Proxy_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Proxy " + dtProxy.Rows[0]["name"] + " deleted", "");
            
            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean restart(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("proxyid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return false;
            }


            String proxy = parameters["proxyid"].ToString();
            if (String.IsNullOrWhiteSpace(proxy))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return false;
            }

            Int64 proxyid = 0;
            try
            {
                proxyid = Int64.Parse(proxy);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyid;

            DataTable dtProxy = database.ExecuteDataTable("select * from proxy p where (p.enterprise_id = @enterprise_id or p.enterprise_id = 0) and p.id = @proxy_id", CommandType.Text, par, null);
            if (dtProxy == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtProxy.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Proxy not found.", "", null);
                return false;
            }

            database.ExecuteNonQuery("update proxy set restart = 1 where id = @proxy_id", CommandType.Text, par);
            database.AddUserLog(LogKey.Proxy_ResetRequest, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Proxy " + dtProxy.Rows[0]["name"] + " reset requested", "");

            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Object> list(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Object> result = new List<Object>();

            String text = "";

            if (parameters.ContainsKey("text"))
                text = (String)parameters["text"];

            if (String.IsNullOrWhiteSpace(text))
                text = "";

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@text", typeof(String)).Value = text;

            Int32 page = 1;
            Int32 pageSize = 10;

            if (parameters.ContainsKey("page"))
                Int32.TryParse(parameters["page"].ToString(), out page);

            if (parameters.ContainsKey("page_size"))
                Int32.TryParse(parameters["page_size"].ToString(), out pageSize);

            if (pageSize < 1)
                pageSize = 1;

            if (page < 1)
                page = 1;

            Int32 rStart = ((page - 1) * pageSize) + 1;
            Int32 rEnd = rStart + (pageSize - 1);


            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT ";
            sql += "    ROW_NUMBER() OVER (ORDER BY p.name) AS [row_number], p.*, resource_qty = (select COUNT(distinct r1.proxy_id) from resource r1 with(nolock) where r1.proxy_id = p.id) ";
            sql += "     from proxy p ";
            sql += "     where ((p.enterprise_id = 0 or p.enterprise_id = @enterprise_id) " + (String.IsNullOrWhiteSpace(text) ? "" : " and p.name like '%'+@text+'%'") + ")";
            sql += ") SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtPlugins = database.ExecuteDataTable( sql, CommandType.Text, par, null);
            if ((dtPlugins != null) && (dtPlugins.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtPlugins.Rows)
                {
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("proxy_id", dr1["id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("last_sync", (dr1["last_sync"] != DBNull.Value ? (Int32)((((DateTime)dr1["last_sync"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                    newItem.Add("last_sync_address", dr1["address"]);
                    newItem.Add("last_sync_version", dr1["version"]);
                    newItem.Add("last_sync_pid", dr1["pid"]);
                    newItem.Add("resource_qty", dr1["resource_qty"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    result.Add(newItem);
                }

            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> change(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("proxyid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return null;
            }


            String proxy = parameters["proxyid"].ToString();
            if (String.IsNullOrWhiteSpace(proxy))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return null;
            }

            Int64 proxyid = 0;
            try
            {
                proxyid = Int64.Parse(proxy);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyid;


            DataTable dtProxy = database.ExecuteDataTable( "select p.*, resource_qty = (select COUNT(distinct r1.proxy_id) from resource r1 with(nolock) where r1.proxy_id = p.id) from proxy p where (p.enterprise_id = @enterprise_id or p.enterprise_id = 0) and p.id = @proxy_id", CommandType.Text, par, null);
            if (dtProxy == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtProxy.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Proxy not found.", "", null);
                return null;
            }


            List<String> log = new List<String>();

            String updateSQL = "update proxy set ";
            Boolean update = false;
            if (parameters["name"] != null)
            {
                String name = parameters["name"].ToString();
                if (!String.IsNullOrWhiteSpace(name))
                {

                    if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_-]+$"))
                    {
                        Error(ErrorType.InvalidRequest, "Invalid name. The name can contain letters ('a' to 'z' and 'A' to 'Z'), numbers (0-9), underscore ('_') and hyphens ('-')", "", null);
                        return null;
                    }

                    par.Add("@name", typeof(String)).Value = name;
                    updateSQL += "name = @name";
                    update = true;

                    log.Add("Name changed from '" + dtProxy.Rows[0]["name"] + "' to '" + name + "'");
                }

            }

            if (update)
            {
                updateSQL += " where id = @proxy_id";
                database.ExecuteNonQuery(updateSQL, CommandType.Text, par);
                database.AddUserLog( LogKey.Proxy_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Proxy changed", String.Join("\r\n", log));
            }

            //Atualiza a busca com os dados atualizados
            dtProxy = database.ExecuteDataTable( "select p.*, resource_qty = (select COUNT(distinct r1.proxy_id) from resource r1 with(nolock) where r1.proxy_id = p.id) from proxy p where (p.enterprise_id = @enterprise_id or p.enterprise_id = 0) and p.id = @proxy_id", CommandType.Text, par, null);

            DataRow dr1 = dtProxy.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("proxy_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("last_sync", (dr1["last_sync"] != DBNull.Value ? (Int32)((((DateTime)dr1["last_sync"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("last_sync_address", dr1["address"]);
            newItem.Add("last_sync_version", dr1["version"]);
            newItem.Add("resource_qty", dr1["resource_qty"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            return result;

        }


    }
}
