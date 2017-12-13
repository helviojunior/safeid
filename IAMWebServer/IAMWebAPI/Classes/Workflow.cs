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
using System.IO;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using IAM.GlobalDefs.Messages;
using SafeTrend.Data;
using IAM.PluginManager;
using IAM.PluginInterface;
using SafeTrend.WebAPI;
using IAM.Workflow;
using System.Net.Mail;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Workflow : IAMAPIBase
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
                    return newplugin(database, parameters);
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

                case "getrequest":
                    return getaccessrequest(database, parameters);
                    break;

                case "accessrequestlist":
                case "accessrequestsearch":
                    return accessrequestlist(database, parameters);
                    break;

                case "accessrequestallow":
                    return accessrequestallow(database, parameters);
                    break;

                case "accessrequestdeny":
                    return accessrequestdeny(database, parameters);
                    break;

                case "accessrequestrevoke":
                    return accessrequestrevoke(database, parameters);
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
        private Dictionary<String, Object> newplugin(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            throw new NotImplementedException();

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


            Int64 parentid = 0;
            if (parameters.ContainsKey("parentid"))
            {
                try
                {
                    parentid = Int64.Parse(parameters["parentid"].ToString());
                }
                catch
                {
                    Error(ErrorType.InvalidRequest, "Parameter parentid is not a long integer.", "", null);
                    return null;
                }

            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_name", typeof(String)).Value = name;
            par.Add("@parent_id", typeof(Int64)).Value = parentid;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable( "sp_new_role", CommandType.StoredProcedure, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Role not found.", "", null);
                return null;
            }

            DataRow dr1 = dtUsers.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("role_id", dr1["id"]);
            newItem.Add("parent_id", dr1["parent_id"]);
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("entity_qty", dr1["entity_qty"]);
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

            if (!parameters.ContainsKey("workflowid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return null;
            }


            String plugin = parameters["workflowid"].ToString();
            if (String.IsNullOrWhiteSpace(plugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return null;
            }

            Int64 workflowid = 0;
            try
            {
                workflowid = Int64.Parse(plugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@workflow_id", typeof(Int64)).Value = workflowid;

            DataTable dtPlugin = database.ExecuteDataTable("select w.id, request_qty = (select COUNT(*) from st_workflow_request wr with(nolock) where wr.workflow_id = w.id) from st_workflow w with(nolock) inner join context c with(nolock) on c.id = w.context_id where c.enterprise_id = @enterprise_id and w.id = @workflow_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Workflow not found.", "", null);
                return null;
            }


            DataRow dr1 = dtPlugin.Rows[0];


            using (WorkflowConfig wk = new WorkflowConfig())
            {
                wk.GetDatabaseData(database, (Int64)dr1["id"]);

                Dictionary<string, object> newItem = wk.ToJsonObject();

                newItem.Add("request_qty", dr1["request_qty"]);

                result.Add("info", newItem);
            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean delete(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("workflowid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return false;
            }


            String plugin = parameters["workflowid"].ToString();
            if (String.IsNullOrWhiteSpace(plugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return false;
            }

            Int64 workflowid = 0;
            try
            {
                workflowid = Int64.Parse(plugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@plugin_id", typeof(Int64)).Value = workflowid;

            DataTable dtPlugin = database.ExecuteDataTable( "select p.*, resource_plugin_qty = (select COUNT(distinct rp1.plugin_id) from resource_plugin rp1 where rp1.plugin_id = p.id) from plugin p with(nolock) where p.enterprise_id = @enterprise_id and p.id = @plugin_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Plugin not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtPlugin.Rows[0]["resource_plugin_qty"] > 0)
            {
                Error(ErrorType.SystemError, "Plugin is being used and can not be deleted.", "", null);
                return false;
            }

            //Localiza o arquivo físico
            FileInfo assemblyFile = null;
            try
            {
                DirectoryInfo pluginsDir = null;

                pluginsDir = new DirectoryInfo(database.GetDBConfig( "pluginFolder"));

                if (pluginsDir.Exists)
                    assemblyFile = new FileInfo(Path.Combine(pluginsDir.FullName, dtPlugin.Rows[0]["assembly"].ToString()));
            }
            catch
            {
                assemblyFile = null;
            }

            if ((assemblyFile == null) || (!assemblyFile.Exists))
            {
                Error(ErrorType.SystemError, "Plugin physical file not found.", "", null);
                return false;
            }

            SqlTransaction trans = (SqlTransaction)database.BeginTransaction();
            try
            {
                database.ExecuteNonQuery( "delete from plugin where id = @plugin_id", CommandType.Text,par,  trans);
                database.AddUserLog( LogKey.Plugin_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Plugin " + dtPlugin.Rows[0]["name"] + " deleted", "", trans);

                assemblyFile.Delete();

                trans.Commit();
            }
            catch {
                trans.Rollback();
                Error(ErrorType.SystemError, "Fail on delete physical file", "", null);
                return false;
            }
            
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
            sql += "    ROW_NUMBER() OVER (ORDER BY w.name) AS [row_number], w.id, request_qty = (select COUNT(*) from st_workflow_request wr with(nolock) where wr.workflow_id = w.id) ";
            sql += "     from st_workflow w with(nolock) inner join context c with(nolock) on c.id = w.context_id ";
            sql += "     where ((c.enterprise_id = @enterprise_id) " + (String.IsNullOrWhiteSpace(text) ? "" : " and w.name like '%'+@text+'%'") + ")";
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

                    using (WorkflowConfig wk = new WorkflowConfig())
                    {
                        wk.GetDatabaseData(database, (Int64)dr1["id"]);

                        Dictionary<string, object> newItem = wk.ToJsonObject();

                        newItem.Add("request_qty", dr1["request_qty"]);

                        result.Add(newItem);
                    }
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

            if (!parameters.ContainsKey("workflowid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return null;
            }


            String plugin = parameters["workflowid"].ToString();
            if (String.IsNullOrWhiteSpace(plugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return null;
            }

            Int64 workflowid = 0;
            try
            {
                workflowid = Int64.Parse(plugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@workflow_id", typeof(Int64)).Value = workflowid;

            DataTable dtPlugin = database.ExecuteDataTable("select w.id, request_qty = (select COUNT(*) from st_workflow_request wr with(nolock) where wr.workflow_id = w.id) from st_workflow w with(nolock) inner join context c with(nolock) on c.id = w.context_id where c.enterprise_id = @enterprise_id and w.id = @workflow_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Workflow not found.", "", null);
                return null;
            }


            String updateSQL = "update st_workflow set ";
            String updateFields = "";
            Boolean update = false;
            Boolean disableTrigger = true;

            foreach (String key in parameters.Keys)
            {
                switch (key.ToLower())
                {
                    case "name":
                        String name = parameters["name"].ToString();
                        if (!String.IsNullOrWhiteSpace(name))
                        {
                            par.Add("@name", typeof(String)).Value = name;
                            if (updateFields != "") updateFields += ", ";
                            updateFields += "name = @name";
                            update = true;
                        }
                        else
                        {
                            Error(ErrorType.InvalidRequest, "Parameter name is empty.", "", null);
                            return null;
                        }
                        break;

                }
            }

            if (update)
            {
                updateSQL += updateFields + " where id = @workflow_id";

                Object trans = database.BeginTransaction();
                try
                {
                    //Desabilita a trigger para evitar a criação de um novo workflow
                    //Os campos alterados não interferem no funcionamento
                    if (disableTrigger)
                        database.ExecuteNonQuery("DISABLE TRIGGER st_WorkflowUpdate ON st_workflow", CommandType.Text, null, trans);

                    database.ExecuteNonQuery(updateSQL, CommandType.Text, par, trans);

                    if (disableTrigger)
                        database.ExecuteNonQuery("ENABLE TRIGGER st_WorkflowUpdate ON st_workflow", CommandType.Text, null, trans);

                    database.Commit();
                }
                catch (Exception ex)
                {
                    database.Rollback();

                    Error(ErrorType.InternalError, "Error updating workflow", ex.Message, null);
                    return null;
                }
            }

            //Atualiza a busca com os dados atualizados
            return get(database, parameters);

        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Object> accessrequestlist(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Object> result = new List<Object>();

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;

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

            /*
            select * from st_workflow_request r with(nolock) 
             * inner join entity e  with(nolock) on e.id = r.entity_id 
             * inner join context c  with(nolock) on c.id = e.context_id
             * */
            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT ";
            sql += "    ROW_NUMBER() OVER (ORDER BY r.create_date) AS [row_number], r.*, e.context_id, c.enterprise_id, e.full_name, e.login";
            sql += "     from st_workflow_request r with(nolock)  ";
            sql += "     inner join entity e  with(nolock) on e.id = r.entity_id   ";
            sql += "     inner join context c  with(nolock) on c.id = e.context_id  ";
            sql += "     where (c.enterprise_id = @enterprise_id ";

            if ((parameters.ContainsKey("filter")) && (parameters["filter"] is Dictionary<String, Object>))
            {
                Dictionary<String, Object> filter = (Dictionary<String, Object>)parameters["filter"];
                foreach (String k in filter.Keys)
                    switch (k.ToLower())
                    {
                        case "text":
                            if (!String.IsNullOrWhiteSpace(filter["text"].ToString()))
                            {
                                par.Add("@text", typeof(String)).Value = filter["text"].ToString();
                                sql += " and (e.full_name like '%'+@text+'%' or e.login like '%'+@text+'%' or r.description like '%'+@text+'%')";
                            }
                            break;

                        case "contextid":
                            if (!String.IsNullOrWhiteSpace(filter["contextid"].ToString()))
                            {
                                try
                                {
                                    Int64 tmp = Int64.Parse(filter["contextid"].ToString());
                                    par.Add("@context_id", typeof(Int64)).Value = tmp;
                                    sql += " and c.id = @context_id";
                                }
                                catch { }
                            }
                            break;

                        case "workflowid":
                            if (!String.IsNullOrWhiteSpace(filter["workflowid"].ToString()))
                            {
                                try
                                {
                                    Int64 tmp = Int64.Parse(filter["workflowid"].ToString());
                                    par.Add("@workflow_id", typeof(Int64)).Value = tmp;
                                    sql += " and r.workflow_id = @workflow_id";
                                }
                                catch { }
                            }
                            break;

                        case "status":
                            if (!String.IsNullOrWhiteSpace(filter["status"].ToString()))
                            {
                                try
                                {
                                    WorkflowRequestStatus tmp = (WorkflowRequestStatus)Int32.Parse(filter["status"].ToString());
                                    par.Add("@status", typeof(Int32)).Value = (Int32)tmp;
                                    sql += " and r.status = @status";
                                }
                                catch { }
                            }
                            break;
                    }
            }
            
            sql += "     )";
            sql += ") SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtRequest = database.ExecuteDataTable(sql, CommandType.Text, par, null);
            if ((dtRequest != null) && (dtRequest.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtRequest.Rows)
                {
                    using(IAMRBAC rbac = new IAMRBAC())
                    if (!rbac.UserAdmin(database, Acl.EntityId, this._enterpriseId))
                        using (WorkflowRequest request = new WorkflowRequest((Int64)dr1["id"]))
                        {
                            WorkflowRequestProccess proc = request.GetInicialData(database);
                            if (!proc.Success)
                            {
                                Error(ErrorType.InternalError, proc.Message, proc.Debug, null);
                                return null;
                            }

                            if (!database.ExecuteScalar<Boolean>("select case when COUNT(*) > 0 then CAST(1 as bit) else CAST(0 as bit) end from entity e with(nolock) where e.id = "+ Acl.EntityId +" and (e.id in (" + request.Workflow.Owner + "," + request.Activity.ManualApproval.EntityApprover + ") or e.id in (select i.entity_id from identity_role ir with(nolock) inner join [identity] i with(nolock) on i.id = ir.identity_id where ir.role_id = " + request.Activity.ManualApproval.RoleApprover + "))", CommandType.Text, null))
                                continue;
                        }

                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("access_request_id", dr1["id"]);
                    newItem.Add("userid", dr1["entity_id"]);
                    newItem.Add("context_id", dr1["context_id"]);
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("workflow_id", dr1["workflow_id"]);
                    newItem.Add("status", dr1["status"]);
                    newItem.Add("description", dr1["description"]);
                    newItem.Add("entity_full_name", dr1["full_name"]);
                    newItem.Add("entity_login", dr1["login"]);
                    newItem.Add("deployed", dr1["deployed"]);
                    newItem.Add("start_date", (dr1["start_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["start_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                    newItem.Add("end_date", (dr1["end_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["end_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    WorkflowConfig wk = new WorkflowConfig();
                    wk.GetDatabaseData(database, (Int64)dr1["workflow_id"]);

                    newItem.Add("workflow", wk.ToJsonObject());

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
        private Dictionary<String, Object> getaccessrequest(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("requestid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not defined.", "", null);
                return null;
            }

            Int64 requestid = 0;
            try
            {
                requestid = Int64.Parse(parameters["requestid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@request_id", typeof(Int64)).Value = requestid;

            DataTable dtPlugins = database.ExecuteDataTable("select r.*, e.context_id, c.enterprise_id, e.full_name, e.login from st_workflow_request r with(nolock) inner join entity e  with(nolock) on e.id = r.entity_id inner join context c  with(nolock) on c.id = e.context_id where r.id = @request_id and c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if ((dtPlugins == null) || (dtPlugins.Rows.Count == 0))
            {
                Error(ErrorType.InvalidRequest, "Access request not found.", "", null);
                return null;
            }

            DataRow dr1 = dtPlugins.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("access_request_id", dr1["id"]);
            newItem.Add("userid", dr1["entity_id"]);
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("workflow_id", dr1["workflow_id"]);
            newItem.Add("status", dr1["status"]);
            newItem.Add("description", dr1["description"]);
            newItem.Add("entity_full_name", dr1["full_name"]);
            newItem.Add("entity_login", dr1["login"]);
            newItem.Add("deployed", dr1["deployed"]);
            newItem.Add("start_date", (dr1["start_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["start_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("end_date", (dr1["end_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["end_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            WorkflowConfig wk = new WorkflowConfig();
            wk.GetDatabaseData(database, (Int64)dr1["workflow_id"]);

            newItem.Add("workflow", wk.ToJsonObject());

            result.Add("info", newItem);

            return result;
        }

        //

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean accessrequestallow(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("requestid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not defined.", "", null);
                return false;
            }

            Int64 requestid = 0;
            try
            {
                requestid = Int64.Parse(parameters["requestid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not a long integer.", "", null);
                return false;
            }


            WorkflowRequest req = new WorkflowRequest(requestid);
            WorkflowRequestProccess proc = req.SetStatus(database, WorkflowRequestStatus.Approved, Acl.EntityId);
            if (!proc.Success)
            {
                Error(ErrorType.InvalidRequest, proc.Message, proc.Debug, null);
                return false;
            }
            else
            {
                return true;
            }

        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean accessrequestrevoke(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("requestid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not defined.", "", null);
                return false;
            }

            Int64 requestid = 0;
            try
            {
                requestid = Int64.Parse(parameters["requestid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not a long integer.", "", null);
                return false;
            }



            WorkflowRequest req = new WorkflowRequest(requestid);
            WorkflowRequestProccess proc = req.SetStatus(database, WorkflowRequestStatus.Revoked, Acl.EntityId);
            if (!proc.Success)
            {
                Error(ErrorType.InvalidRequest, proc.Message, proc.Debug, null);
                return false;
            }
            else
            {
                return true;
            }

        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean accessrequestdeny(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("requestid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not defined.", "", null);
                return false;
            }

            Int64 requestid = 0;
            try
            {
                requestid = Int64.Parse(parameters["requestid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter requestid is not a long integer.", "", null);
                return false;
            }

            WorkflowRequest req = new WorkflowRequest(requestid);
            WorkflowRequestProccess proc = req.SetStatus(database, WorkflowRequestStatus.Deny, Acl.EntityId);
            if (!proc.Success)
            {
                Error(ErrorType.InvalidRequest, proc.Message, proc.Debug, null);
                return false;
            }
            else
            {
                return true;
            }

        }

        //

    }
}
