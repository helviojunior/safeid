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
using System.Data;
using System.Data.SqlClient;
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
    internal class Resource : IAMAPIBase
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
                    return newresource(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "list":
                case "search":
                    return list(database, parameters);
                    break;

                case "change":
                    return change(database, parameters);
                    break;

                case "delete":
                    return delete(database, parameters);
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
        private Dictionary<String, Object> newresource(IAMDatabase database, Dictionary<String, Object> parameters)
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


            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }

            Int64 contextid = 0;
            try
            {
                contextid = Int64.Parse((String)parameters["contextid"]);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                return null;
            }

            if (!parameters.ContainsKey("proxyid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not defined.", "", null);
                return null;
            }

            Int64 proxyid = 0;
            try
            {
                proxyid = Int64.Parse((String)parameters["proxyid"]);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter proxyid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_name", typeof(String)).Value = name;
            par.Add("@proxy_id", typeof(Int64)).Value = proxyid;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable( "sp_new_resource", CommandType.StoredProcedure, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource not found.", "", null);
                return null;
            }

            DataRow dr1 = dtUsers.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("role_id", dr1["id"]);
            newItem.Add("proxy_id", dr1["proxy_id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("resource_plugin_qty", dr1["resource_plugin_qty"]);
            newItem.Add("enabled", dr1["enabled"]);
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

            if (!parameters.ContainsKey("resourceid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return null;
            }


            String resource = parameters["resourceid"].ToString();
            if (String.IsNullOrWhiteSpace(resource))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return null;
            }

            Int64 resourceid = 0;
            try
            {
                resourceid = Int64.Parse(resource);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_id", typeof(Int64)).Value = resourceid;

            DataTable dtResource = database.ExecuteDataTable( "select r.*, c.enterprise_id, c.name as context_name, p.name as proxy_name, resource_plugin_qty = (select COUNT(distinct rp.id) from resource_plugin rp with(nolock) where rp.resource_id = r.id) from resource r with(nolock) inner join context c with(nolock) on c.id = r.context_id inner join proxy p on p.id = r.proxy_id where c.enterprise_id = @enterprise_id and r.id = @resource_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource not found.", "", null);
                return null;
            }

            DataRow dr1 = dtResource.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("resource_id", dr1["id"]);
            newItem.Add("proxy_id", dr1["proxy_id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("proxy_name", dr1["proxy_name"]);
            newItem.Add("context_name", dr1["context_name"]);
            newItem.Add("resource_plugin_qty", dr1["resource_plugin_qty"]);
            newItem.Add("enabled", dr1["enabled"]);
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

            if (!parameters.ContainsKey("resourceid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return false;
            }


            String resource = parameters["resourceid"].ToString();
            if (String.IsNullOrWhiteSpace(resource))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return false;
            }

            Int64 resourceid = 0;
            try
            {
                resourceid = Int64.Parse(resource);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_id", typeof(Int64)).Value = resourceid;

            DataTable dtResource = database.ExecuteDataTable( "select r.*, c.enterprise_id, c.name as context_name, p.name as proxy_name, resource_plugin_qty = (select COUNT(distinct rp.id) from resource_plugin rp with(nolock) where rp.resource_id = r.id) from resource r with(nolock) inner join context c with(nolock) on c.id = r.context_id inner join proxy p on p.id = r.proxy_id where c.enterprise_id = @enterprise_id and r.id = @resource_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtResource.Rows[0]["resource_plugin_qty"] > 0)
            {
                Error(ErrorType.SystemError, "Resource is being used and can not be deleted.", "", null);
                return false;
            }


            database.ExecuteNonQuery( "delete from resource where id = @resource_id", CommandType.Text, par);
            
                database.AddUserLog(LogKey.Resource_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource " + dtResource.Rows[0]["name"] + " deleted", "");

            return true;
        }
        
        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> change(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("resourceid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return null;
            }


            String resource = parameters["resourceid"].ToString();
            if (String.IsNullOrWhiteSpace(resource))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return null;
            }

            Int64 resourceid = 0;
            try
            {
                resourceid = Int64.Parse(resource);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_id", typeof(Int64)).Value = resourceid;

            DataTable dtResource = database.ExecuteDataTable( "select r.*, c.enterprise_id, c.name as context_name, p.name as proxy_name, resource_plugin_qty = (select COUNT(distinct rp.id) from resource_plugin rp with(nolock) where rp.resource_id = r.id) from resource r with(nolock) inner join context c with(nolock) on c.id = r.context_id inner join proxy p on p.id = r.proxy_id where c.enterprise_id = @enterprise_id and r.id = @resource_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource not found.", "", null);
                return null;
            }

            List<String> log = new List<String>();

            String updateSQL = "";
            Boolean update = false;
            foreach (String key in parameters.Keys)
                switch (key)
                {
                    case "name":
                        String name = parameters["name"].ToString();
                        if ((!String.IsNullOrWhiteSpace(name)) && (name != dtResource.Rows[0]["name"]))
                        {
                            par.Add("@name", typeof(String)).Value = name;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " name = @name";
                            update = true;

                            log.Add("Name changed from '" + dtResource.Rows[0]["name"] + "' to '" + name + "'");
                        }
                        break;

                    case "enabled":
                        Boolean enabled = true;
                        try
                        {
                            enabled = Boolean.Parse(parameters["enabled"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter enabled is not a boolean.", "", null);
                            return null;
                        }

                        if (enabled != (Boolean)dtResource.Rows[0]["enabled"])
                        {
                            par.Add("@enabled", typeof(Boolean)).Value = enabled;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " [enabled] = @enabled";
                            update = true;
                            log.Add((enabled ? "Enabled" : "Disabled"));
                        }
                        break;

                    case "contextid":
                        String context = parameters["contextid"].ToString();
                        if (!String.IsNullOrWhiteSpace(context))
                        {
                            try
                            {
                                Int64 contextid = Int64.Parse(context);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                                return null;
                            }

                            if (context != dtResource.Rows[0]["context_id"].ToString())
                            {
                                
                                //Verifica se o contexto pertence a mesma empresa
                                DataTable dtCtx = database.ExecuteDataTable( "select * from context where enterprise_id = @enterprise_id and id = " + context, CommandType.Text, par, null);
                                if ((dtCtx == null) || (dtCtx.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New context not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                //Verifica se há usuários vinculados a reste recurso
                                DataTable dtIdent = database.ExecuteDataTable( "select COUNT(distinct i.id) qty from resource r with(nolock) inner join context c with(nolock) on r.context_id = c.id inner join resource_plugin rp with(nolock) on rp.resource_id = r.id inner join [identity] i with(nolock) on rp.id = i.resource_plugin_id where r.id = " + resourceid, CommandType.Text, par, null);
                                if ((dtIdent != null) && (dtIdent.Rows.Count > 0) && ((Int32)dtIdent.Rows[0]["qty"] > 0))
                                {
                                    Error(ErrorType.SystemError, "The context of this resource has identy(ies) and can not be changed.", "", null);
                                    return null;
                                }

                                par.Add("@context_id", typeof(Int64)).Value = Int64.Parse(context);
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " context_id = @context_id";
                                update = true;

                                log.Add("Context changed from '" + dtResource.Rows[0]["context_name"] + "' to '" + dtCtx.Rows[0]["name"] + "'");
                            }

                        }
                        break;


                    case "proxyid":
                        String proxy = parameters["proxyid"].ToString();
                        if (!String.IsNullOrWhiteSpace(proxy))
                        {
                            try
                            {
                                Int64 proxyid = Int64.Parse(proxy);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter proxyid is not a long integer.", "", null);
                                return null;
                            }

                            if (proxy != dtResource.Rows[0]["proxy_id"].ToString())
                            {

                                //Verifica se o contexto pertence a mesma empresa
                                DataTable dtProxy = database.ExecuteDataTable( "select * from proxy where enterprise_id = @enterprise_id and id = " + proxy, CommandType.Text, par, null);
                                if ((dtProxy == null) || (dtProxy.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New proxy not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                par.Add("@proxy_id", typeof(Int64)).Value = Int64.Parse(proxy);
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " proxy_id = @proxy_id";
                                update = true;

                                log.Add("Proxy changed from '" + dtResource.Rows[0]["proxy_name"] + "' to '" + dtProxy.Rows[0]["name"] + "'");
                            }
                        }
                        break;
                }

            if (update)
            {
                updateSQL = "update resource set " + updateSQL + " where id = @resource_id";
                database.ExecuteNonQuery( updateSQL, CommandType.Text, par);
                database.AddUserLog( LogKey.Role_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource changed", String.Join("\r\n", log));
            }

            //Atualiza a busca com os dados atualizados
            dtResource = database.ExecuteDataTable( "select r.*, c.enterprise_id, c.name as context_name, p.name as proxy_name, resource_plugin_qty = (select COUNT(distinct rp.id) from resource_plugin rp with(nolock) where rp.resource_id = r.id) from resource r with(nolock) inner join context c with(nolock) on c.id = r.context_id inner join proxy p on p.id = r.proxy_id where c.enterprise_id = @enterprise_id and r.id = @resource_id", CommandType.Text, par, null);

            DataRow dr1 = dtResource.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("resource_id", dr1["id"]);
            newItem.Add("proxy_id", dr1["proxy_id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("proxy_name", dr1["proxy_name"]);
            newItem.Add("context_name", dr1["context_name"]);
            newItem.Add("resource_plugin_qty", dr1["resource_plugin_qty"]);
            newItem.Add("enabled", dr1["enabled"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            return result;

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
            sql += "    ROW_NUMBER() OVER (ORDER BY r.name) AS [row_number], r.*, c.enterprise_id, c.name as context_name, p.name as proxy_name, resource_plugin_qty = (select COUNT(distinct rp.id) from resource_plugin rp with(nolock) where rp.resource_id = r.id) ";
            sql += "     from resource r with(nolock) inner join context c with(nolock) on c.id = r.context_id  inner join proxy p on p.id = r.proxy_id";
            sql += "     where c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and r.name like '%'+@text+'%'");

            if ((parameters.ContainsKey("filter")) && (parameters["filter"] is Dictionary<String, Object>))
            {
                Dictionary<String, Object> filter = (Dictionary<String, Object>)parameters["filter"];
                foreach(String k in filter.Keys)
                    switch (k.ToLower())
                    {
                        case "contextid":
                            try{
                                sql += " and c.id = " + Int64.Parse(filter[k].ToString()).ToString();
                            }catch{}
                            break;

                        case "proxyid":
                            try
                            {
                                sql += " and r.proxy_id = " + Int64.Parse(filter[k].ToString()).ToString();
                            }
                            catch { }
                            break;
                    }
            }

            sql += ") SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtRoles = database.ExecuteDataTable( sql, CommandType.Text, par, null);
            if ((dtRoles != null) && (dtRoles.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtRoles.Rows)
                {
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("context_id", dr1["context_id"]);
                    newItem.Add("resource_id", dr1["id"]);
                    newItem.Add("proxy_id", dr1["proxy_id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("proxy_name", dr1["proxy_name"]);
                    newItem.Add("context_name", dr1["context_name"]);
                    newItem.Add("resource_plugin_qty", dr1["resource_plugin_qty"]);
                    newItem.Add("enabled", dr1["enabled"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    result.Add(newItem);
                }

            }

            return result;
        }


    }
}
