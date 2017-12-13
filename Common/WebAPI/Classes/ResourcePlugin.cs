/// 
/// @file apiinfo.cs
/// <summary>
/// Implementações da classe APIInfo. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 19/10/2013
/// $Id: apiinfo.cs, v1.0 2013/10/19 Helvio Junior $

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.Script.Serialization;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using IAM.PluginManager;
using IAM.PluginInterface;
using IAM.Filters;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class ResourcePlugin : APIBase
    {
        public override event Error Error;
        public override event ExternalAccessControl ExternalAccessControl;

        private Int64 _enterpriseId;

        /// <summary>
        /// Método de processamentoda requisição
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="enterpriseId">ID da empresa</param>
        /// <param name="method">String com o método que deverá ser processado</param>
        /// <param name="auth">String com a chave de autenticação.</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        public override Object Process(SqlConnection sqlConnection, Int64 enterpriseId, String method, String auth, Dictionary<String, Object> parameters)
        {

            this._enterpriseId = enterpriseId;
            base.Connection = sqlConnection;

            method = method.ToLower();
            String[] mp = method.Split(".".ToCharArray(), 2);

            if (mp.Length != 2)
                return null;

            if (this.GetType().Name.ToLower() != mp[0])
                return null;

            AccessControl ac = ValidateCtrl(sqlConnection, method, auth, parameters, ExternalAccessControl); 
            if (!ac.Result)
            {
                Error(ErrorType.InvalidParameters, "Not authorized", "", null);
                return null;
            }

            switch (mp[1])
            {
                case "new":
                    return newresourceplugin(sqlConnection, parameters);
                    break;

                case "get":
                    return get(sqlConnection, parameters, false);
                    break;

                case "clone":
                    return clone(sqlConnection, parameters, false);
                    break;

                case "parameters":
                    return getparameters(sqlConnection, parameters);
                    break;

                case "mapping":
                    return getmapping(sqlConnection, parameters);
                    break;

                case "lockrules":
                    return getlockrules(sqlConnection, parameters);
                    break;

                case "ignore":
                    return getignore(sqlConnection, parameters);
                    break;

                case "roles":
                    return getroles(sqlConnection, parameters);
                    break;

                case "schedules":
                    return getschedules(sqlConnection, parameters);
                    break;

                case "fieldsfetch":
                    return fieldsfetch(sqlConnection, parameters);
                    break;

                case "list":
                case "search":
                    return list(sqlConnection, parameters);
                    break;

                case "change":
                    return change(sqlConnection, parameters);
                    break;

                case "changeparameters":
                    return changeparameters(sqlConnection, parameters);
                    break;

                case "changemapping":
                    return changemapping(sqlConnection, parameters);
                    break;

                case "changerole":
                    return changerole(sqlConnection, parameters);
                    break;

                case "changelockrules":
                    return changelockrules(sqlConnection, parameters);
                    break;

                case "changeignore":
                    return changeignore(sqlConnection, parameters);
                    break;

                case "changeschedules":
                    return changeschedules(sqlConnection, parameters);
                    break;

                case "delete":
                    return delete(sqlConnection, parameters);
                    break;

                case "enable":
                    return enable(sqlConnection, parameters);
                    break;

                case "deploy":
                    return deploy(sqlConnection, parameters);
                    break;

                case "disable":
                    return disable(sqlConnection, parameters);
                    break;

                case "newfetch":
                    return newfetch(sqlConnection, parameters);
                    break;

                case "deletefetch":
                    return deletefetch(sqlConnection, parameters);
                    break;

                case "identity":
                    return identity(sqlConnection, parameters);
                    break;

                case "addidentity":
                    return addidentity(sqlConnection, parameters);
                    break;

                case "deleteidentity":
                    return deleteidentity(sqlConnection, parameters);
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
        private Dictionary<String, Object> newresourceplugin(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("resourceid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not defined.", "", null);
                return null;
            }

            Int64 resourceid = 0;
            try
            {
                resourceid = Int64.Parse((String)parameters["resourceid"]);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourceid is not a long integer.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("pluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not defined.", "", null);
                return null;
            }

            Int64 pluginid = 0;
            try
            {
                pluginid = Int64.Parse((String)parameters["pluginid"]);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not a long integer.", "", null);
                return null;
            }

            if (!parameters.ContainsKey("mail_domain"))
            {
                Error(ErrorType.InvalidRequest, "Parameter mail_domain is not defined.", "", null);
                return null;
            }

            String mail_domain = parameters["mail_domain"].ToString();
            if (String.IsNullOrWhiteSpace(mail_domain))
            {
                Error(ErrorType.InvalidRequest, "Parameter mail_domain is not defined.", "", null);
                return null;
            }

            try
            {
                System.Net.Mail.MailAddress tst = new System.Net.Mail.MailAddress("teste@" + (String)parameters["mail_domain"]);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter mail_domain is not a valid mail domain.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_id", typeof(Int64)).Value = resourceid;
            par.Add("@plugin_id", typeof(Int64)).Value = pluginid;
            par.Add("@mail_domain", typeof(String), mail_domain.Length).Value = mail_domain;

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "sp_new_resource_plugin", CommandType.StoredProcedure, par, null);
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
            newItem.Add("name", dr1["name"]);
            newItem.Add("resource_name", dr1["resource_name"]);
            newItem.Add("plugin_id", dr1["plugin_id"]);
            newItem.Add("plugin_name", dr1["plugin_name"]);
            newItem.Add("resource_plugin_id", dr1["id"]);
            newItem.Add("resource_id", dr1["resource_id"]);
            newItem.Add("permit_add_entity", dr1["permit_add_entity"]);
            newItem.Add("enabled", dr1["enabled"]);
            newItem.Add("mail_domain", dr1["mail_domain"]);
            newItem.Add("build_login", dr1["build_login"]);
            newItem.Add("build_mail", dr1["build_mail"]);
            newItem.Add("enable_import", dr1["enable_import"]);
            newItem.Add("enable_deploy", dr1["enable_deploy"]);
            newItem.Add("order", dr1["order"]);
            newItem.Add("name_field_id", dr1["name_field_id"]);
            newItem.Add("mail_field_id", dr1["mail_field_id"]);
            newItem.Add("login_field_id", dr1["login_field_id"]);
            newItem.Add("deploy_after_login", dr1["deploy_after_login"]);
            newItem.Add("password_after_login", dr1["password_after_login"]);
            newItem.Add("deploy_process", dr1["deploy_process"]);
            newItem.Add("deploy_all", dr1["deploy_all"]);
            newItem.Add("deploy_password_hash", (dr1["deploy_password_hash"] != DBNull.Value ? dr1["deploy_password_hash"].ToString().ToLower() : "none"));
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getroles(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginRole = ExecuteDataTable(sqlConnection, "select r.* from resource_plugin_role rpr with(nolock) inner join role r with(nolock) on rpr.role_id = r.id WHERE rpr.resource_plugin_id = @resource_plugin_id order by r.name", CommandType.Text, par, null);
            if (dtResourcePluginRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginRole.Rows)
            {
                Dictionary<string, object> newItem = new Dictionary<string, object>();

                newItem.Add("role_id", dr1["id"]);
                newItem.Add("role_name", dr1["name"]);

                DataTable dtResourcePluginRoleAction = ExecuteDataTable(sqlConnection, "select * from resource_plugin_role_action rpa with(nolock) WHERE rpa.resource_plugin_id = @resource_plugin_id and rpa.role_id = " + dr1["id"], CommandType.Text, par, null);
                if (dtResourcePluginRoleAction != null)
                {
                    List<Dictionary<string, object>> act = new List<Dictionary<string, object>>();
                    foreach (DataRow dr2 in dtResourcePluginRoleAction.Rows)
                    {
                        Dictionary<string, object> act1 = new Dictionary<string, object>();

                        act1.Add("action_key", dr2["action_key"]);
                        act1.Add("action_add_value", dr2["action_add_value"]);
                        act1.Add("action_del_value", dr2["action_del_value"]);
                        act1.Add("additional_data", dr2["additional_data"]);

                        act.Add(act1);
                    }
                    newItem.Add("actions", act);
                }

                DataTable dtResourcePluginRoleExpression = ExecuteDataTable(sqlConnection, "select rpf.*, f.name filter_name from resource_plugin_role_filter rpf with(nolock) inner join filters f with(nolock) on rpf.filter_id = f.id WHERE rpf.resource_plugin_id = @resource_plugin_id and rpf.role_id = " + dr1["id"], CommandType.Text, par, null);
                if (dtResourcePluginRoleExpression != null)
                {

                    List<Dictionary<string, object>> filters = new List<Dictionary<string, object>>();
                    foreach (DataRow dr2 in dtResourcePluginRoleExpression.Rows)
                    {
                        Dictionary<string, object> filter1 = new Dictionary<string, object>();
                        
                        filter1.Add("filter_id", dr2["filter_id"].ToString());
                        filter1.Add("filter_name", dr2["filter_name"].ToString());

                        //Lista as condições
                        List<Dictionary<String, Object>> conditions = new List<Dictionary<string, object>>();

                        FilterRule f = new FilterRule(dr2["filter_name"].ToString());
                        DataTable dt3 = ExecuteDataTable(sqlConnection, "select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = " + dr2["filter_id"]);
                        if ((dt3 != null) || (dt3.Rows.Count > 0))
                            foreach (DataRow dr3 in dt3.Rows)
                                f.AddCondition(dr3["group_id"].ToString(), dr3["group_selector"].ToString(), (Int64)dr3["field_id"], dr3["field_name"].ToString(), dr3["data_type"].ToString(), dr3["text"].ToString(), dr3["condition"].ToString(), dr3["selector"].ToString());

                        filter1.Add("conditions_description", f.ToString());
                        
                        filters.Add(filter1);
                    }

                    newItem.Add("filters", filters);
                }


                DataTable dtResourcePluginRoleAcl = ExecuteDataTable(sqlConnection, "select * from resource_plugin_role_time_acl rpacl with(nolock) WHERE rpacl.resource_plugin_id = @resource_plugin_id and rpacl.role_id = " + dr1["id"], CommandType.Text, par, null);
                if (dtResourcePluginRoleAcl != null)
                {
                    List<Dictionary<string, object>> acl = new List<Dictionary<string, object>>();
                    foreach (DataRow dr2 in dtResourcePluginRoleAcl.Rows)
                    {
                        TimeACL.TimeAccess tAcl = new TimeACL.TimeAccess();
                        tAcl.FromJsonString(dr2["time_acl"].ToString());

                        acl.Add(tAcl.ToJsonObject());

                    }
                    
                    newItem.Add("time_acl", acl);
                }


                result.Add(newItem);
            }

            return result;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getmapping(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginMapping = ExecuteDataTable(sqlConnection, "select rpm.*, f.name field_name, f.data_type field_data_type from resource_plugin_mapping rpm with(nolock) inner join field f on rpm.field_id = f.id WHERE rpm.resource_plugin_id = @resource_plugin_id order by f.name", CommandType.Text, par, null);
            if (dtResourcePluginMapping == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginMapping.Rows)
            {
                Dictionary<string, object> newItem = new Dictionary<string, object>();

                newItem.Add("data_name", dr1["data_name"]);
                newItem.Add("field_id", dr1["field_id"]);
                newItem.Add("field_name", dr1["field_name"]);
                newItem.Add("field_data_type", dr1["field_data_type"]);
                newItem.Add("is_id", dr1["is_id"]);
                newItem.Add("is_password", dr1["is_password"]);
                newItem.Add("is_property", dr1["is_property"]);
                newItem.Add("is_unique_property", dr1["is_unique_property"]);

                result.Add(newItem);
            }


            return result;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getlockrules(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginLock = ExecuteDataTable(sqlConnection, "select rplf.*, f.name filter_name from resource_plugin_lock_filter rplf with(nolock) inner join filters f with(nolock) on rplf.filter_id = f.id WHERE rplf.resource_plugin_id = @resource_plugin_id order by f.name", CommandType.Text, par, null);
            if (dtResourcePluginLock == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginLock.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();

                newItem.Add("filter_id", dr1["filter_id"].ToString());
                newItem.Add("filter_name", dr1["filter_name"].ToString());

                //Lista as condições
                List<Dictionary<String, Object>> conditions = new List<Dictionary<string, object>>();

                FilterRule f = new FilterRule(dr1["filter_name"].ToString());
                DataTable dt2 = ExecuteDataTable(sqlConnection, "select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = " + dr1["filter_id"]);
                if ((dt2 != null) || (dt2.Rows.Count > 0))
                    foreach (DataRow dr2 in dt2.Rows)
                        f.AddCondition(dr2["group_id"].ToString(), dr2["group_selector"].ToString(), (Int64)dr2["field_id"], dr2["field_name"].ToString(), dr2["data_type"].ToString(), dr2["text"].ToString(), dr2["condition"].ToString(), dr2["selector"].ToString());

                newItem.Add("conditions_description", f.ToString());


                result.Add(newItem);
            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getignore(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginIgnore = ExecuteDataTable(sqlConnection, "select rpif.*, f.name filter_name from resource_plugin_ignore_filter rpif with(nolock) inner join filters f with(nolock) on rpif.filter_id = f.id WHERE rpif.resource_plugin_id = @resource_plugin_id order by f.name", CommandType.Text, par, null);
            if (dtResourcePluginIgnore == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginIgnore.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();

                newItem.Add("filter_id", dr1["filter_id"].ToString());
                newItem.Add("filter_name", dr1["filter_name"].ToString());

                //Lista as condições
                List<Dictionary<String, Object>> conditions = new List<Dictionary<string, object>>();

                FilterRule f = new FilterRule(dr1["filter_name"].ToString());
                DataTable dt2 = ExecuteDataTable(sqlConnection, "select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = " + dr1["filter_id"]);
                if ((dt2 != null) || (dt2.Rows.Count > 0))
                    foreach (DataRow dr2 in dt2.Rows)
                        f.AddCondition(dr2["group_id"].ToString(), dr2["group_selector"].ToString(), (Int64)dr2["field_id"], dr2["field_name"].ToString(), dr2["data_type"].ToString(), dr2["text"].ToString(), dr2["condition"].ToString(), dr2["selector"].ToString());

                newItem.Add("conditions_description", f.ToString());


                result.Add(newItem);
            }

            return result;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getschedules(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginSchedule = ExecuteDataTable(sqlConnection, "select * from resource_plugin_schedule rple WHERE rple.resource_plugin_id = @resource_plugin_id order by rple.id", CommandType.Text, par, null);
            if (dtResourcePluginSchedule == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginSchedule.Rows)
            {
                Scheduler.Schedule schedule = new Scheduler.Schedule();
                schedule.FromJsonString(dr1["schedule"].ToString());

                result.Add(schedule.ToJsonObject());
            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> fieldsfetch(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginFetch = ExecuteDataTable(sqlConnection, "select * from resource_plugin_fetch rpff WHERE rpff.resource_plugin_id = @resource_plugin_id order by rpff.request_date desc", CommandType.Text, par, null);
            if (dtResourcePluginFetch == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginFetch.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();

                newItem.Add("fetch_id", dr1["id"]);
                newItem.Add("request_date", (dr1["request_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["request_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                newItem.Add("response_date", (dr1["response_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["response_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                newItem.Add("success", (dr1["success"] == DBNull.Value ? false : dr1["success"]));

                try
                {
                    if (dr1["response_date"] != DBNull.Value)
                    {
                        JavaScriptSerializer _ser = new JavaScriptSerializer();
                        Dictionary<String, Object> tmp = _ser.Deserialize<Dictionary<String, Object>>(dr1["json_data"].ToString());

                        if (tmp.ContainsKey("logs"))
                            newItem.Add("logs", tmp["logs"]);

                        if (tmp.ContainsKey("result_data"))
                        {
                            String rDataJson = Encoding.UTF8.GetString(Convert.FromBase64String(tmp["result_data"].ToString()));

                            PluginConnectorBaseFetchResult r = JsonBase.JSON.Deserialize<PluginConnectorBaseFetchResult>(rDataJson);
                            List<Dictionary<String, Object>> fetch_fields = new List<Dictionary<String, Object>> ();
                            foreach(String key in r.fields.Keys){

                                Dictionary<String, Object> nff = new Dictionary<string, object>();
                                List<String> sample_data = new List<String>();

                                foreach (String sd in r.fields[key])
                                    if (!sample_data.Contains(sd))
                                        sample_data.Add(sd);

                                nff.Add("key", key);
                                nff.Add("sample_data", sample_data);
                                fetch_fields.Add(nff);
                            }

                            r.Dispose();
                            r = null;
                            rDataJson = null;

                            newItem.Add("fetch_fields", fetch_fields);
                        }
                        
                            
                        /*
                        && (tmp["result_data"] is Dictionary<String, Object>) && ((Dictionary<String, Object>)tmp["result_data"]).ContainsKey("fields") && (((Dictionary<String, Object>)tmp["result_data"])["fields"]  is Dictionary<string, List<string>>))
                        newItem.Add("fetch_fields", ((Dictionary<String, Object>)tmp["result_data"])["fields"]);*/
                    }

                }
                catch { }

                result.Add(newItem);
            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getparameters(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id  WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataTable dtResourcePluginPar = ExecuteDataTable(sqlConnection, "select * from resource_plugin_par rpp WHERE rpp.resource_plugin_id = @resource_plugin_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr1 in dtResourcePluginPar.Rows)
            {
                Dictionary<string, object> newItem = new Dictionary<string, object>();

                newItem.Add("key", dr1["key"]);
                newItem.Add("value", dr1["value"]);

                result.Add(newItem);
            }


            return result;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> get(SqlConnection sqlConnection, Dictionary<String, Object> parameters, Boolean ignoreCheckConfigErrors)
        {

            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p.scheme plugin_scheme, p.uri plugin_uri, c.id context_id, c.name context_name, p1.last_sync proxy_last_sync, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name, identity_qty = (select COUNT(distinct i.id) from [identity] i with(nolock) where i.resource_plugin_id = rp.id) FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataRow dr1 = dtResource.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("name", dr1["name"]);
            newItem.Add("plugin_id", dr1["plugin_id"]);
            newItem.Add("resource_plugin_id", dr1["id"]);
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("context_name", dr1["context_name"]);
            newItem.Add("resource_id", dr1["resource_id"]);
            newItem.Add("permit_add_entity", dr1["permit_add_entity"]);
            newItem.Add("enabled", dr1["enabled"]);
            newItem.Add("resource_enabled", dr1["resource_enabled"]);
            newItem.Add("mail_domain", dr1["mail_domain"]);
            newItem.Add("build_login", dr1["build_login"]);
            newItem.Add("build_mail", dr1["build_mail"]);
            newItem.Add("enable_import", dr1["enable_import"]);
            newItem.Add("enable_deploy", dr1["enable_deploy"]);
            newItem.Add("order", dr1["order"]);
            newItem.Add("name_field_id", dr1["name_field_id"]);
            newItem.Add("mail_field_id", dr1["mail_field_id"]);
            newItem.Add("login_field_id", dr1["login_field_id"]);
            newItem.Add("deploy_after_login", dr1["deploy_after_login"]);
            newItem.Add("password_after_login", dr1["password_after_login"]);
            newItem.Add("deploy_process", dr1["deploy_process"]);
            newItem.Add("deploy_all", dr1["deploy_all"]);
            newItem.Add("deploy_password_hash", (dr1["deploy_password_hash"] != DBNull.Value ? dr1["deploy_password_hash"].ToString().ToLower() : "none"));
            newItem.Add("proxy_last_sync", (dr1["proxy_last_sync"] != DBNull.Value ? (Int32)((((DateTime)dr1["proxy_last_sync"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("identity_qty", dr1["identity_qty"]);
            newItem.Add("plugin_uri", dr1["plugin_uri"]);
            newItem.Add("plugin_scheme", dr1["plugin_scheme"]);

            result.Add("info", newItem);

            Dictionary<string, object> newItem2 = new Dictionary<string, object>();
            newItem2.Add("resource_name", dr1["resource_name"]);
            newItem2.Add("plugin_name", dr1["plugin_name"]);
            newItem2.Add("name_field_name", dr1["name_field_name"]);
            newItem2.Add("mail_field_name", dr1["mail_field_name"]);
            newItem2.Add("login_field_name", dr1["login_field_name"]);

            result.Add("related_names", newItem2);

            //Checa as dependências de configuração configurações 
            if (parameters.ContainsKey("checkconfig") && (parameters["checkconfig"] is Boolean) && ((Boolean)parameters["checkconfig"]))
            {
                Dictionary<string, object> cc = CheckConfig(sqlConnection, resourcepluginid, ignoreCheckConfigErrors);
                if (cc == null)
                {
                    if (!ignoreCheckConfigErrors)
                        return null;
                }
                else
                {
                    result.Add("check_config", cc);
                }
            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> clone(SqlConnection sqlConnection, Dictionary<String, Object> parameters, Boolean ignoreCheckConfigErrors)
        {

            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p.scheme plugin_scheme, p.uri plugin_uri, c.id context_id, p1.last_sync proxy_last_sync, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name, identity_qty = (select COUNT(distinct i.id) from [identity] i with(nolock) where i.resource_plugin_id = rp.id) FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            SqlTransaction trans = sqlConnection.BeginTransaction();
            try
            {

                DataTable dtNew = ExecuteDataTable(sqlConnection, "sp_clone_resourceplugin", CommandType.StoredProcedure, par, trans);

                //Retorna os dados do novo recurso x plugin
                Dictionary<String, Object> parameters2 = new Dictionary<string, object>();
                parameters2.Add("resourcepluginid", dtNew.Rows[0]["id"]);

                trans.Commit();
                trans = null;

                return get(sqlConnection, parameters2, true);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Error cloning resource x plugin.", "", null);
                return null;
            }
            finally
            {
                //Saída inexperada, é erro
                if (trans != null)
                    trans.Rollback();
            }

        }


        private Dictionary<string, object> CheckConfig(SqlConnection sqlConnection, Int64 resourcePluginId)
        {
            return CheckConfig(sqlConnection, resourcePluginId, false);
        }

        private Dictionary<string, object> CheckConfig(SqlConnection sqlConnection, Int64 resourcePluginId, Boolean ignoreCheckConfigErrors)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            List<String> errMessages = new List<string>();

            FileInfo assemblyFile = null;

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcePluginId;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "select p.*, rp.mail_domain, rp.name_field_id, rp.mail_field_id, rp.login_field_id from resource_plugin rp with(nolock) inner join plugin p with(nolock) on p.id = rp.plugin_id WHERE rp.id = @resource_plugin_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                if (!ignoreCheckConfigErrors) Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                if (!ignoreCheckConfigErrors) Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }


            DataRow dr1 = dtResourcePlugin.Rows[0];



            /* Checa configurações gerais
            ==================================================*/
            if (String.IsNullOrEmpty(dr1["mail_domain"].ToString()) || ((Int64)dr1["name_field_id"] == 0) || ((Int64)dr1["mail_field_id"] == 0) || ((Int64)dr1["login_field_id"] == 0))
                result.Add("general", false);
            else
                result.Add("general", true);


            /* Checa parâmetros de configuração do plugin
            ==================================================*/

            try
            {
                assemblyFile = new FileInfo(Path.Combine(GetDBConfig(sqlConnection, "pluginFolder"), dr1["assembly"].ToString()));
            }
            catch
            {
                assemblyFile = null;
            }

            if ((assemblyFile == null) || (!assemblyFile.Exists))
            {
                if (!ignoreCheckConfigErrors) Error(ErrorType.InternalError, "Plugin assembly file not found", "", null);
                return null;
            }

            try
            {
                PluginConnectorBase selectedPlugin = null;

                Byte[] rawAssembly = File.ReadAllBytes(assemblyFile.FullName);
                List<PluginConnectorBase> p1 = Plugins.GetPlugins<PluginConnectorBase>(rawAssembly);

                Array.Clear(rawAssembly, 0, rawAssembly.Length);
                rawAssembly = null;

                foreach (PluginConnectorBase p in p1)
                    if (p.GetPluginId().AbsoluteUri.ToLower() == dr1["uri"].ToString().ToLower())
                        selectedPlugin = p;

                if (selectedPlugin == null)
                {
                    if (!ignoreCheckConfigErrors) Error(ErrorType.InternalError, "Plugin uri '" + dr1["uri"] + "' not found in assembly '" + dr1["assembly"] + "'", "", null);
                    return null;
                }


                DataTable dtResourcePluginPar = ExecuteDataTable(sqlConnection, "select * from resource_plugin_par rpp WHERE rpp.resource_plugin_id = @resource_plugin_id", CommandType.Text, par, null);
                if (dtResourcePlugin == null)
                {
                    if (!ignoreCheckConfigErrors) Error(ErrorType.InternalError, "", "", null);
                    return null;
                }
                
                Boolean pgConfig = true;
                //Verifica todas as dependências
                Dictionary<String, Object> fields = new Dictionary<string, object>();
                foreach (DataRow drPar in dtResourcePluginPar.Rows)
                    PluginConnectorBase.FillConfig(selectedPlugin, ref fields, drPar["key"].ToString(), drPar["value"]);


                LogEvent iLog = new LogEvent(delegate(Object sender, PluginLogType type, string text)
                {
                    if (type == PluginLogType.Error)
                        errMessages.Add(text);
                });

                pgConfig = selectedPlugin.ValidateConfigFields(fields, false, iLog, true, false);

                iLog = null;

                /*
                 * Método antigo, não será mais utilizado, a validação está interna em cada plugin
                foreach (PluginConfigFields f in selectedPlugin.GetConfigFields())
                {
                    //Verifica somente os marcados como requerido
                    if (f.DeployRequired || f.ImportRequired)
                    {
                        Boolean ok = false;
                        foreach (DataRow drPar in dtResourcePluginPar.Rows)
                        {
                            if (drPar["key"].ToString() == f.Key)
                            {
                                ok = true;
                                break;
                            }
                        }

                        if (!ok)
                        {
                            pgConfig = false;
                            break;
                        }
                    }
                }*/

                result.Add("plugin_par", pgConfig);

            }
            catch (Exception ex)
            {
                if (!ignoreCheckConfigErrors) Error(ErrorType.InternalError, "Erro on load assembly '" + dr1["assembly"] + "'", ex.Message, null);
                return null;
            }


            /* Checa mapeamento de campos
            ==================================================*/
            //Busca se há pelo menus um campo como ID ou Unique
            DataTable dtResourcePluginMapping = ExecuteDataTable(sqlConnection, "select rpm.*, f.name field_name, f.data_type field_data_type from resource_plugin_mapping rpm with(nolock) inner join field f on rpm.field_id = f.id WHERE (rpm.is_id = 1 or rpm.is_unique_property = 1) and rpm.resource_plugin_id = @resource_plugin_id order by f.name", CommandType.Text, par, null);
            if (dtResourcePluginMapping == null)
            {
                if (!ignoreCheckConfigErrors) Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            result.Add("mapping", (dtResourcePluginMapping.Rows.Count > 0));

            result.Add("error_messages", errMessages);

            return result;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean delete(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "select *, qty = (select COUNT(distinct i.id) from [identity] i with(nolock) where i.resource_plugin_id = rp.id) from resource_plugin rp with(nolock) inner join resource r with(nolock) on r.id = rp.resource_id inner join context c with(nolock) on c.id = r.context_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtResource.Rows[0]["qty"] > 0)
            {
                Error(ErrorType.SystemError, "Resource x plugin is being used and can not be deleted.", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "delete from resource_plugin where id = @resource_plugin_id", CommandType.Text, par);
            AddUserLog(sqlConnection, LogKey.Resource_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource " + dtResource.Rows[0]["name"] + " deleted", "");

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean enable(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            //Verifica as configurações se estão tudo OK
            Dictionary<String, Object> cc = CheckConfig(sqlConnection, resourcepluginid);
            foreach (String key in cc.Keys)
                if ((cc[key] is Boolean) && (!(Boolean)cc[key]))
                {
                    Error(ErrorType.InvalidRequest, "Resource x Plugin configuration not completed.", "", null);
                    return false;
                }

            ExecuteNonQuery(sqlConnection, "update resource_plugin set enabled = 1 where id = @resource_plugin_id", CommandType.Text, par);
            AddUserLog(sqlConnection, LogKey.ResourcePlugin_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource " + dtResource.Rows[0]["name"] + " enabled", "");

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deploy(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.*, p1.id proxy_id, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            //Verifica as configurações se estão tudo OK
            Dictionary<String, Object> cc = CheckConfig(sqlConnection, resourcepluginid);
            foreach (String key in cc.Keys)
                if ((cc[key] is Boolean) && (!(Boolean)cc[key]))
                {
                    Error(ErrorType.InvalidRequest, "Resource x Plugin configuration not completed.", "", null);
                    return false;
                }

            DataTable dtResourceSc = ExecuteDataTable(sqlConnection, "select * from resource_plugin_schedule rps WHERE rps.resource_plugin_id = @resource_plugin_id", CommandType.Text, par, null);
            if ((dtResourceSc == null) || (dtResourceSc.Rows.Count == 0))
            {
                Error(ErrorType.InvalidRequest, "Has no schedules to mark deploy now", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "update resource_plugin_schedule set [next] = '1970-01-01 00:00:00' where resource_plugin_id = @resource_plugin_id", CommandType.Text, par);
            ExecuteNonQuery(sqlConnection, "update proxy set config = 1 where id = " + dtResource.Rows[0]["proxy_id"], CommandType.Text, par);
            AddUserLog(sqlConnection, LogKey.ResourcePluginDeploy, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource " + dtResource.Rows[0]["name"] + " marked for deploy now", "");

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean disable(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "update resource_plugin set enabled = 0 where id = @resource_plugin_id", CommandType.Text, par);
            AddUserLog(sqlConnection, LogKey.ResourcePlugin_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource " + dtResource.Rows[0]["name"] + " disabled", "");

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean newfetch(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.id, openfetch = (select COUNT(distinct f.id) from resource_plugin_fetch f where f.resource_plugin_id = rp.id and response_date is null) FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            if ((Int32)dtResource.Rows[0]["openfetch"] > 0)
            {
                Error(ErrorType.InvalidRequest, "There is a fetch in progress wait for the end.", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "insert into resource_plugin_fetch (resource_plugin_id) values(@resource_plugin_id)", CommandType.Text, par);

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deletefetch(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            String fetch = parameters["fetchid"].ToString();
            if (String.IsNullOrWhiteSpace(fetch))
            {
                Error(ErrorType.InvalidRequest, "Parameter fetchid is not defined.", "", null);
                return false;
            }


            Int64 fetchid = 0;
            try
            {
                fetchid = Int64.Parse(fetch);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter fetchid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "SELECT rp.id, openfetch = (select COUNT(distinct f.id) from resource_plugin_fetch f where f.resource_plugin_id = rp.id) FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }


            ExecuteNonQuery(sqlConnection, "delete from resource_plugin_fetch where resource_plugin_id = @resource_plugin_id and id = " + fetchid, CommandType.Text, par);

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> changemapping(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("mapping"))
            {
                Error(ErrorType.InvalidRequest, "Parameter mapping is not defined.", "", null);
                return null;
            }

            if (!(parameters["mapping"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter mapping is not valid.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, p1.id proxy_id, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataRow dr1 = dtResourcePlugin.Rows[0];


            List<String> log = new List<String>();

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {
                List<String> fieldList = new List<String>();
                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["mapping"]).ToArray());


                for (Int32 i = 0; i < lst.Count; i++)
                //foreach (Dictionary<String, Object> field in mapping)
                {
                    if (!(lst[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Mapping " + i + " is not valid", "", null);
                        return null;
                    }

                    Dictionary<String, Object> field = (Dictionary<String, Object>)lst[i];

                    if (!field.ContainsKey("field_id"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is not defined in mapping " + i, "", null);
                        return null;
                    }

                    Int64 fieldId = 0;
                    if (!String.IsNullOrWhiteSpace((String)field["field_id"]))
                    {
                        try
                        {
                            fieldId = Int64.Parse(field["field_id"].ToString());
                        }
                        catch
                        {
                            Error(ErrorType.InvalidRequest, "Parameter field_id is not a long integer on mapping " + i, "", null);
                            return null;
                        }
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is empty on mapping " + i, "", null);
                        return null;
                    }

                    if (!field.ContainsKey("data_name"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter data_name is not defined in mapping " + i, "", null);
                        return null;
                    }

                    if (String.IsNullOrWhiteSpace((String)field["data_name"]))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter data_name is empty on mapping " + i, "", null);
                        return null;
                    }

                    DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field where enterprise_id = @enterprise_id and id = " + fieldId, CommandType.Text, par, trans);
                    if ((dtField == null) || (dtField.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Field on mapping " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return null;
                    }

                    String dataName = field["data_name"].ToString();
                    Boolean isId = (field.ContainsKey("is_id") && field["is_id"] is Boolean && (Boolean)field["is_id"]);
                    Boolean isPassword = (field.ContainsKey("is_password") && field["is_password"] is Boolean && (Boolean)field["is_password"]);
                    Boolean isUnique = (!isId && field.ContainsKey("is_unique_property") && field["is_unique_property"] is Boolean && (Boolean)field["is_unique_property"]);
                    Boolean isProperty = (!isId && !isUnique);


                    fieldList.Add(fieldId.ToString());
                    log.Add(dr1["name"] + " mapped to field " + dtField.Rows[0]["name"] + " with data name '" + dataName + "' (" + (isId ? "'is ID' " : "") + (isPassword ? "'is Password' " : "") + (isUnique ? "'is unique property' " : "") + (isProperty ? "'is simple property' " : "") + ")");
                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_mapping where resource_plugin_id = @resource_plugin_id and [field_id] = " + fieldId, CommandType.Text,par,  trans);
                    ExecuteNonQuery(sqlConnection, "insert into resource_plugin_mapping (resource_plugin_id, [field_id], [data_name], [is_id], [is_password], [is_property], [is_unique_property]) values (@resource_plugin_id, " + fieldId + ", '"  + dataName + "'," + (isId ? 1 : 0) + ", " + (isPassword ? 1 : 0) + ", " + (isProperty ? 1 : 0) + ", " + (isUnique ? 1 : 0) + ") ",  CommandType.Text,par, trans);

                }

                //Remove todos os campos que não foram listados
                if (fieldList.Count > 0)
                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_mapping where resource_plugin_id = @resource_plugin_id and [field_id] not in (" + String.Join(",", fieldList) + ")",  CommandType.Text,par, trans);

                AddUserLog(sqlConnection, LogKey.ResourcePluginMapping_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin fields maping changed", String.Join("\r\n", log), trans);

                //Se o recurso x plugin estiver habilitado marca para que o proxy realize o download das configurações
                if ((Boolean)dr1["enabled"] && (Boolean)dr1["resource_enabled"])
                    ExecuteNonQuery(sqlConnection, "update proxy set config = 1 where id = " + dr1["proxy_id"], CommandType.Text,par,  trans);

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update fields mapping", "", null);
                return null;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }

            //Retorna os dados
            return get(sqlConnection, parameters, false);
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean changelockrules(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            if (!parameters.ContainsKey("lock_filters"))
            {
                Error(ErrorType.InvalidRequest, "Parameter lock_filters is not defined.", "", null);
                return false;
            }

            if (!(parameters["lock_filters"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter lock_filters is not valid.", "", null);
                return false;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, p1.id proxy_id, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            DataRow dr1 = dtResourcePlugin.Rows[0];

            List<String> log = new List<String>();

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {
                List<String> filterList = new List<String>();
                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["lock_filters"]).ToArray());

                for (Int32 i = 0; i < lst.Count; i++)
                {

                    Int64 lfId = 0;
                    try
                    {
                        lfId = Int64.Parse(lst[i].ToString());
                    }
                    catch
                    {
                        Error(ErrorType.InvalidRequest, "Lock filter id " + lst[i].ToString() + " is not valid", "", null);
                        return false;
                    }

                    //Checa se o filtro existe

                    DataTable dtRole = ExecuteDataTable(sqlConnection, "select f.* from filters f with(nolock) where f.enterprise_id = @enterprise_id and f.id = " + lfId, CommandType.Text, par, trans);
                    if ((dtRole == null) || (dtRole.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Filter on lock filter " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return false;
                    }

                    //Adiciona a expressão
                    log.Add("Lock filter " + dtRole.Rows[0]["name"] + " added in resource x plugin " + dr1["name"]);

                    DbParameterCollection parFilter = new DbParameterCollection();
                    parFilter.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                    parFilter.Add("@filter_id", typeof(Int64)).Value = lfId;

                    ExecuteNonQuery(sqlConnection, "insert into resource_plugin_lock_filter (resource_plugin_id, filter_id) select @resource_plugin_id, @filter_id WHERE not exists (select 1 from resource_plugin_lock_filter with(nolock) where resource_plugin_id = @resource_plugin_id and filter_id = @filter_id)", CommandType.Text,parFilter,  trans);

                    parFilter.Clear();
                    parFilter = null;

                    filterList.Add(lfId.ToString());

                }

                //Remove todos os campos que não foram listados
                DbParameterCollection parExp = new DbParameterCollection();
                parExp.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

                if (filterList.Count > 0)
                {
                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_lock_filter where resource_plugin_id = @resource_plugin_id and filter_id not in (" + String.Join(",", filterList) + ")", CommandType.Text,parExp,  trans);
                }
                else
                {
                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_lock_filter where resource_plugin_id = @resource_plugin_id", CommandType.Text,parExp,  trans);
                }

                AddUserLog(sqlConnection, LogKey.ResourcePluginLockExpression_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin lock rules changed", String.Join("\r\n", log), trans);

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update lock rules", ex.Message, null);
                return false;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean changeignore(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            if (!parameters.ContainsKey("ignore_filters"))
            {
                Error(ErrorType.InvalidRequest, "Parameter ignore_filters is not defined.", "", null);
                return false;
            }

            if (!(parameters["ignore_filters"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter ignore_filters is not valid.", "", null);
                return false;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, p1.id proxy_id, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            DataRow dr1 = dtResourcePlugin.Rows[0];

            List<String> log = new List<String>();

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {
                List<String> filterList = new List<String>();
                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["ignore_filters"]).ToArray());

                for (Int32 i = 0; i < lst.Count; i++)
                {

                    Int64 lfId = 0;
                    try
                    {
                        lfId = Int64.Parse(lst[i].ToString());
                    }
                    catch
                    {
                        Error(ErrorType.InvalidRequest, "Ignore filter id " + lst[i].ToString() + " is not valid", "", null);
                        return false;
                    }

                    //Checa se o filtro existe

                    DataTable dtRole = ExecuteDataTable(sqlConnection, "select f.* from filters f with(nolock) where f.enterprise_id = @enterprise_id and f.id = " + lfId, CommandType.Text, par, trans);
                    if ((dtRole == null) || (dtRole.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Filter on ignore filter " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return false;
                    }

                    //Adiciona a expressão
                    log.Add("Ignore filter " + dtRole.Rows[0]["name"] + " added in resource x plugin " + dr1["name"]);

                    DbParameterCollection parFilter = new DbParameterCollection();
                    parFilter.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                    parFilter.Add("@filter_id", typeof(Int64)).Value = lfId;

                    ExecuteNonQuery(sqlConnection, "insert into resource_plugin_ignore_filter (resource_plugin_id, filter_id) select @resource_plugin_id, @filter_id WHERE not exists (select 1 from resource_plugin_ignore_filter with(nolock) where resource_plugin_id = @resource_plugin_id and filter_id = @filter_id)", CommandType.Text,parFilter,  trans);

                    parFilter.Clear();
                    parFilter = null;

                    filterList.Add(lfId.ToString());

                }

                //Remove todos os campos que não foram listados
                DbParameterCollection parExp = new DbParameterCollection();
                parExp.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

                if (filterList.Count > 0)
                {
                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_ignore_filter where resource_plugin_id = @resource_plugin_id and filter_id not in (" + String.Join(",", filterList) + ")", CommandType.Text,parExp,  trans);
                }
                else
                {
                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_ignore_filter where resource_plugin_id = @resource_plugin_id", CommandType.Text,parExp,  trans);
                }

                AddUserLog(sqlConnection, LogKey.ResourcePluginLockExpression_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin lock rules changed", String.Join("\r\n", log), trans);

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update ignore rules", ex.Message, null);
                return false;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean changeschedules(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            if (!parameters.ContainsKey("schedules"))
            {
                Error(ErrorType.InvalidRequest, "Parameter schedules is not defined.", "", null);
                return false;
            }

            if (!(parameters["schedules"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter schedules is not valid.", "", null);
                return false;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.name resource_name, r.enabled resource_enabled, p.name plugin_name, p1.last_sync proxy_last_sync, p1.id proxy_id, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            DataRow dr1 = dtResourcePlugin.Rows[0];

            List<String> log = new List<String>();

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {
                List<String> sheduleList = new List<String>();
                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["schedules"]).ToArray());

                for (Int32 i = 0; i < lst.Count; i++)
                {

                    if (!(lst[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Scherule " + i + " is not valid", "", null);
                        return false;
                    }

                    Dictionary<String, Object> schedule = (Dictionary<String, Object>)lst[i];
                    IAM.Scheduler.Schedule newItem = new IAM.Scheduler.Schedule();

                    if (!schedule.ContainsKey("trigger"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter trigger of schedule "+ i +" is not defined.", "", null);
                        return false;
                    }

                    switch(schedule["trigger"].ToString().ToLower())
                    {
                        case "annually":
                            newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Annually;
                            break;

                        case "monthly":
                            newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Monthly;
                            break;

                        case "weekly":
                            newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Weekly;
                            break;

                        case "dialy":
                            newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Dialy;
                            break;

                        default:
                            Error(ErrorType.InvalidRequest, "Parameter trigger of schedule " + i + " is not valid.", "", null);
                            return false;
                            break;
                    }


                    if (!schedule.ContainsKey("startdate"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter startdate of schedule " + i + " is not defined.", "", null);
                        return false;
                    }

                    try
                    {
                        newItem.StartDate = DateTime.Parse(schedule["startdate"].ToString());
                    }
                    catch
                    {
                        Error(ErrorType.InvalidRequest, "Parameter startdate of schedule " + i + " is not valid.", "", null);
                        return false;
                    }

                    if (!schedule.ContainsKey("triggertime"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter triggertime of schedule " + i + " is not defined.", "", null);
                        return false;
                    }

                    try
                    {
                        newItem.TriggerTime = DateTime.Parse(schedule["triggertime"].ToString());
                    }
                    catch
                    {
                        Error(ErrorType.InvalidRequest, "Parameter triggertime of schedule " + i + " is not valid.", "", null);
                        return false;
                    }


                    try
                    {
                        newItem.Repeat = Int32.Parse(schedule["repeat"].ToString());
                    }
                    catch { }


                    //Adiciona a expressão
                    log.Add("Schedule " + newItem.ToString() + " added in resource x plugin " + dr1["name"]);

                    DbParameterCollection parSchedule = new DbParameterCollection();
                    parSchedule.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                    parSchedule.Add("@schedule", typeof(String)).Value = newItem.ToJsonString();

                    ExecuteNonQuery(sqlConnection, "insert into resource_plugin_schedule (resource_plugin_id, [schedule], next) select @resource_plugin_id, @schedule, getdate() WHERE not exists (select 1 from resource_plugin_schedule where resource_plugin_id = @resource_plugin_id and schedule = @schedule)", CommandType.Text,parSchedule,  trans);

                    parSchedule.Clear();
                    parSchedule = null;

                    sheduleList.Add(newItem.ToJsonString());

                }

                //Remove todos os campos que não foram listados
                if (sheduleList.Count > 0)
                {
                    List<String> k = new List<string>();
                    DbParameterCollection parSchedule = new DbParameterCollection();
                    parSchedule.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                    for (Int32 i = 0; i < sheduleList.Count; i++)
                    {
                        parSchedule.Add("@e" + i, typeof(String)).Value = sheduleList[i];
                        k.Add("@e" + i);
                    }

                    ExecuteNonQuery(sqlConnection, "delete from resource_plugin_schedule where resource_plugin_id = @resource_plugin_id and [schedule] not in (" + String.Join(",", k) + ")", CommandType.Text,parSchedule,  trans);
                }

                AddUserLog(sqlConnection, LogKey.ResourcePluginLockSchedule_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin schedule changed", String.Join("\r\n", log), trans);

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update schedules", ex.Message, null);
                return false;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> changerole(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("roles"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roles is not defined.", "", null);
                return null;
            }

            if (!(parameters["roles"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter roles is not valid.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "select p.*, r.proxy_id, rp.enabled resource_plugin_enabled, r.enabled resource_enabled, rp.mail_domain, rp.name_field_id, rp.mail_field_id, rp.login_field_id from resource_plugin rp with(nolock) inner join plugin p with(nolock) on p.id = rp.plugin_id inner join resource r with(nolock) on r.id = rp.resource_id WHERE rp.id = @resource_plugin_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataRow dr1 = dtResourcePlugin.Rows[0];


            FileInfo assemblyFile = null;
            try
            {
                assemblyFile = new FileInfo(Path.Combine(GetDBConfig(sqlConnection, "pluginFolder"), dr1["assembly"].ToString()));
            }
            catch
            {
                assemblyFile = null;
            }

            if ((assemblyFile == null) || (!assemblyFile.Exists))
            {
                Error(ErrorType.InternalError, "Plugin assembly file not found", "", null);
                return null;
            }

            PluginConnectorBase selectedPlugin = null;
            try
            {

                Byte[] rawAssembly = File.ReadAllBytes(assemblyFile.FullName);
                List<PluginConnectorBase> p1 = Plugins.GetPlugins<PluginConnectorBase>(rawAssembly);

                Array.Clear(rawAssembly, 0, rawAssembly.Length);
                rawAssembly = null;

                foreach (PluginConnectorBase p in p1)
                    if (p.GetPluginId().AbsoluteUri.ToLower() == dr1["uri"].ToString().ToLower())
                        selectedPlugin = p;

                if (selectedPlugin == null)
                {
                    Error(ErrorType.InternalError, "Plugin uri '" + dr1["uri"] + "' not found in assembly '" + dr1["assembly"] + "'", "", null);
                    return null;
                }

            }
            catch (Exception ex)
            {
                Error(ErrorType.InternalError, "Erro on load assembly '" + dr1["assembly"] + "'", ex.Message, null);
                return null;
            }


            List<String> log = new List<String>();

            //Verifica todas as dependências

            List<PluginConnectorConfigActions> pgConfigActions = new List<PluginConnectorConfigActions>();
            pgConfigActions.AddRange(selectedPlugin.GetConfigActions());

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {
                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["roles"]).ToArray());

                //Exclui todos os mapeamentos de role deste resource plugin
                ExecuteNonQuery(sqlConnection, "delete from resource_plugin_role where resource_plugin_id = @resource_plugin_id", CommandType.Text,par,  trans);

                for (Int32 i = 0; i < lst.Count; i++)
                //foreach (Dictionary<String, Object> field in mapping)
                {
                    if (!(lst[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Role " + i + " is not valid", "", null);
                        return null;
                    }

                    Dictionary<String, Object> role = (Dictionary<String, Object>)lst[i];

                    if (!role.ContainsKey("role_id"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter role_id is not defined in mapping " + i, "", null);
                        return null;
                    }

                    Int64 roleId = 0;
                    if (!String.IsNullOrWhiteSpace((String)role["role_id"]))
                    {
                        try
                        {
                            roleId = Int64.Parse(role["role_id"].ToString());
                        }
                        catch
                        {
                            Error(ErrorType.InvalidRequest, "Parameter role_id is not a long integer on role item " + i, "", null);
                            return null;
                        }
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is empty on role item " + i, "", null);
                        return null;
                    }


                    DataTable dtRole = ExecuteDataTable(sqlConnection, "select r.* from role r with(nolock) inner join context c with(nolock) on c.id = r.context_id where c.enterprise_id = @enterprise_id and r.id = " + roleId, CommandType.Text, par, trans);
                    if ((dtRole == null) || (dtRole.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Role on role list " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return null;
                    }

                    String roleName = "item " + i;
                    try
                    {
                        roleName = dtRole.Rows[0]["name"].ToString();
                    }
                    catch { }

                    if ((!role.ContainsKey("actions")) && (!role.ContainsKey("filters")) && (!role.ContainsKey("time_acl")))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter actions, filters and time_acl are not defined in role " + roleName, "", null);
                        return null;
                    }

                    
                    if ((!role.ContainsKey("actions")) || !(role["actions"] is ArrayList))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter action in role " + roleName + " is not valid", "", null);
                        return null;
                    }


                    if ((!role.ContainsKey("time_acl")) || !(role["time_acl"] is ArrayList))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter time_acl in role " + roleName + " is not valid", "", null);
                        return null;
                    }


                    if ((!role.ContainsKey("filters")) || !(role["filters"] is ArrayList))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter filters in role " + roleName + " is not valid", "", null);
                        return null;
                    }

                    if ((!role.ContainsKey("actions") || (role.ContainsKey("actions") && ((ArrayList)role["actions"]).Count == 0)) && (!role.ContainsKey("filters") || (role.ContainsKey("filters") && ((ArrayList)role["filters"]).Count == 0)) && (!role.ContainsKey("time_acl") || (role.ContainsKey("time_acl") && ((ArrayList)role["time_acl"]).Count == 0)))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter actions, expressions and time_acl are empty in role " + roleName, "", null);
                        return null;
                    }
                    
                    //Insere a role, caso não exista
                    ExecuteNonQuery(sqlConnection, "insert into resource_plugin_role (resource_plugin_id, role_id) select @resource_plugin_id, " + roleId + " WHERE not exists (select 1 from resource_plugin_role WHERE resource_plugin_id = @resource_plugin_id and [role_id] = " + roleId + ")", CommandType.Text,par,  trans);
                    log.Add(roleName + " mapped");

                    //Trata e insere as ações
                    if (role.ContainsKey("actions"))
                    {
                        //Exclui todas as ações atuais
                        ExecuteNonQuery(sqlConnection, "delete from resource_plugin_role_action where resource_plugin_id = @resource_plugin_id and [role_id] = " + roleId, CommandType.Text,par,  trans);

                        List<Object> actList = new List<Object>();
                        actList.AddRange(((ArrayList)role["actions"]).ToArray());

                        for (Int32 u = 0; u < actList.Count; u++)
                        {
                            if (!(actList[u] is Dictionary<String, Object>))
                            {
                                Error(ErrorType.InvalidRequest, "Action " + u + " in role " + roleName + " is not valid", "", null);
                                return null;
                            }

                            Dictionary<String, Object> act = (Dictionary<String, Object>)actList[u];
                            if (!act.ContainsKey("key"))
                            {
                                Error(ErrorType.InvalidRequest, "Parameter key is not defined in action " + u + " on role " + roleName, "", null);
                                return null;
                            }

                            if (!act.ContainsKey("add_value"))
                            {
                                Error(ErrorType.InvalidRequest, "Parameter add_value is not defined in action " + u + " on role " + roleName, "", null);
                                return null;
                            }

                            String key = act["key"].ToString();
                            PluginConnectorConfigActions pgAct = pgConfigActions.Find(a => (a.Key == key));
                            if (pgAct == null)
                            {
                                Error(ErrorType.InvalidRequest, "Plugin " + dr1["name"] + " does not contain the type of action defined of the value of parameter key in action " + u + " on role " + roleName, "", null);
                                return null;
                            }

                            String add = act["add_value"].ToString();
                            String del = (act.ContainsKey("del_value") && !String.IsNullOrWhiteSpace(act["del_value"].ToString()) ? act["del_value"].ToString() : add);
                            String additional = (act.ContainsKey("additional_data") ? act["additional_data"].ToString() : "");

                            //Adiciona ação
                            log.Add("Action " + pgAct.Name + " added in role " + roleName + " -> Add: " + pgAct.Field.Name + " = " + add + ", del: " + pgAct.Field.Name + " = " + del + ". Aditional data: " + additional);

                            DbParameterCollection parAct = new DbParameterCollection();
                            parAct.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                            parAct.Add("@role_id", typeof(Int64)).Value = roleId;
                            parAct.Add("@action_key", typeof(String)).Value = pgAct.Key;
                            parAct.Add("@action_add_value", typeof(String)).Value = add;
                            parAct.Add("@action_del_value", typeof(String)).Value = del;
                            parAct.Add("@additional_data", typeof(String)).Value = additional;

                            ExecuteNonQuery(sqlConnection, "insert into resource_plugin_role_action (resource_plugin_id, [role_id], [action_key], [action_add_value], [action_del_value], [additional_data]) values (@resource_plugin_id, @role_id, @action_key, @action_add_value, @action_del_value, @additional_data) ", CommandType.Text,parAct,  trans);
                            
                            parAct.Clear();
                            parAct = null;
                        }
                    }


                    //Trata e insere as expressões
                    if (role.ContainsKey("filters"))
                    {
                        //Exclui todas as expressões atuais
                        ExecuteNonQuery(sqlConnection, "delete from resource_plugin_role_filter where resource_plugin_id = @resource_plugin_id and [role_id] = " + roleId, CommandType.Text,par,  trans);

                        List<Object> filterList = new List<Object>();
                        filterList.AddRange(((ArrayList)role["filters"]).ToArray());

                        for (Int32 u = 0; u < filterList.Count; u++)
                        {
                            Int64 lfId = 0;
                            try
                            {
                                lfId = Int64.Parse(filterList[u].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Filter id " + filterList[i].ToString() + " in role " + roleName + " is not valid", "", null);
                                return null;
                            }

                            //Checa se o filtro existe
                            DataTable dtFilter = ExecuteDataTable(sqlConnection, "select f.* from filters f with(nolock) where f.enterprise_id = @enterprise_id and f.id = " + lfId, CommandType.Text, par, trans);
                            if ((dtFilter == null) || (dtFilter.Rows.Count == 0))
                            {
                                Error(ErrorType.InvalidRequest, "Filter id " + lfId + " in role " + roleName + " not exists or is not a chield of this enterprise.", "", null);
                                return null;
                            }

                            //Adiciona a expressão
                            log.Add("Filter " + dtFilter.Rows[0]["name"] + " added in role " + roleName);

                            DbParameterCollection parExp = new DbParameterCollection();
                            parExp.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                            parExp.Add("@role_id", typeof(Int64)).Value = roleId;
                            parExp.Add("@filter_id", typeof(Int64)).Value = lfId;

                            ExecuteNonQuery(sqlConnection, "insert into resource_plugin_role_filter (resource_plugin_id, [role_id], [filter_id]) select @resource_plugin_id, @role_id, @filter_id where not exists (select 1 from resource_plugin_role_filter with(nolock) where resource_plugin_id = @resource_plugin_id and role_id = @role_id and filter_id = @filter_id) ", CommandType.Text,parExp,  trans);

                            parExp.Clear();
                            parExp = null;
                        }
                    }
                    
                    //Trata e insere as regras de horário
                    if (role.ContainsKey("time_acl"))
                    {
                        //Exclui todas as regras de horário atuais
                        ExecuteNonQuery(sqlConnection, "delete from resource_plugin_role_time_acl where resource_plugin_id = @resource_plugin_id and [role_id] = " + roleId, CommandType.Text,par,  trans);

                        List<Object> actList = new List<Object>();
                        actList.AddRange(((ArrayList)role["time_acl"]).ToArray());

                        for (Int32 u = 0; u < actList.Count; u++)
                        {
                            if (!(actList[u] is Dictionary<String, Object>))
                            {
                                Error(ErrorType.InvalidRequest, "Time control " + u + " in role " + roleName + " is not valid", "", null);
                                return null;
                            }

                            Dictionary<String, Object> tAcl = (Dictionary<String, Object>)actList[u];
                            if (!tAcl.ContainsKey("type"))
                            {
                                Error(ErrorType.InvalidRequest, "Parameter type is not defined in time control " + u + " on role " + roleName, "", null);
                                return null;
                            }

                            String type = tAcl["type"].ToString();
                            List<DayOfWeek> wd = new List<DayOfWeek>();
                            if (type.ToLower().Trim() == "specifictime")
                            {
                                if (!tAcl.ContainsKey("start_time"))
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter start_time is not defined in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }

                                if (!tAcl.ContainsKey("end_time"))
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter end_time is not defined in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }

                                if (!tAcl.ContainsKey("week_day"))
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter week_day is not defined in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }

                                String tf = "{0:00}";

                                try
                                {
                                    String[] tm = tAcl["start_time"].ToString().Split(":".ToCharArray());

                                    DateTime tmp = DateTime.ParseExact("1970-01-01 " + String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]), "yyyy-MM-dd HH:mm", null);
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter start_time is not valid in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }


                                try
                                {
                                    String[] tm = tAcl["end_time"].ToString().Split(":".ToCharArray());

                                    DateTime tmp = DateTime.ParseExact("1970-01-01 " + String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]), "yyyy-MM-dd HH:mm", null);
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter end_time is not valid in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }

                                if (!(tAcl["week_day"] is ArrayList))
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter week_day is not valid in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }


                                
                                try
                                {
                                    List<Object> wdTemp = new List<Object>();
                                    wdTemp.AddRange(((ArrayList)tAcl["week_day"]).ToArray());

                                    
                                    foreach (String w in wdTemp)
                                    {
                                        switch (w.ToLower())
                                        {
                                            case "sunday":
                                                wd.Add(DayOfWeek.Sunday);
                                                break;

                                            case "monday":
                                                wd.Add(DayOfWeek.Monday);
                                                break;

                                            case "tuesday":
                                                wd.Add(DayOfWeek.Tuesday);
                                                break;

                                            case "wednesday":
                                                wd.Add(DayOfWeek.Wednesday);
                                                break;

                                            case "thursday":
                                                wd.Add(DayOfWeek.Thursday);
                                                break;

                                            case "friday":
                                                wd.Add(DayOfWeek.Friday);
                                                break;

                                            case "saturday":
                                                wd.Add(DayOfWeek.Saturday);
                                                break;

                                            case "":
                                                break;

                                            default:
                                                throw new Exception("Invalid week day '" + w + "'");
                                                break;
                                        }
                                    }
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Parameter week_day is not valid in time control " + u + " on role " + roleName, "", null);
                                    return null;
                                }
                            }

                            TimeACL.TimeAccess ta = new TimeACL.TimeAccess();
                            ta.FromString(type, tAcl["start_time"].ToString(), tAcl["end_time"].ToString(), "");
                            ta.WeekDay = wd;

                            if (ta.EndTime < ta.StartTime)
                            {
                                Error(ErrorType.InvalidRequest, "Start time should not greater than end time. End time should not less then start time. In time control " + u + " on role " + roleName, "", null);
                                return null;
                            }

                            //Adiciona ação
                            log.Add("Time access control " + ta.ToString());

                            DbParameterCollection parTAcl = new DbParameterCollection();
                            parTAcl.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
                            parTAcl.Add("@role_id", typeof(Int64)).Value = roleId;
                            parTAcl.Add("@time_acl", typeof(String)).Value = ta.ToJsonString();

                            ExecuteNonQuery(sqlConnection, "insert into resource_plugin_role_time_acl (resource_plugin_id, [role_id], [time_acl]) values (@resource_plugin_id, @role_id, @time_acl) ", CommandType.Text,parTAcl,  trans);

                            parTAcl.Clear();
                            parTAcl = null;
                        }
                    }

                }



                AddUserLog(sqlConnection, LogKey.ResourcePluginRole_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin roles changed", String.Join("\r\n", log), trans);

                //Se o recurso x plugin estiver habilitado marca para que o proxy realize o download das configurações
                if ((Boolean)dr1["resource_plugin_enabled"] && (Boolean)dr1["resource_enabled"])
                    ExecuteNonQuery(sqlConnection, "update proxy set config = 1 where id = " + dr1["proxy_id"], CommandType.Text,par,  trans);

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update roles", ex.Message, null);
                return null;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }

            //Retorna os dados
            return get(sqlConnection, parameters, false);
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> changeparameters(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("configparameters"))
            {
                Error(ErrorType.InvalidRequest, "Parameter configparameters is not defined.", "", null);
                return null;
            }

            if (!(parameters["configparameters"] is Dictionary<String, Object>))
            {
                Error(ErrorType.InvalidRequest, "Parameter configparameters is not valid.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "select p.*, r.proxy_id, rp.enabled resource_plugin_enabled, r.enabled resource_enabled, rp.mail_domain, rp.name_field_id, rp.mail_field_id, rp.login_field_id from resource_plugin rp with(nolock) inner join plugin p with(nolock) on p.id = rp.plugin_id inner join resource r with(nolock) on r.id = rp.resource_id WHERE rp.id = @resource_plugin_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            DataRow dr1 = dtResourcePlugin.Rows[0];

            FileInfo assemblyFile = null;
            try
            {
                assemblyFile = new FileInfo(Path.Combine(GetDBConfig(sqlConnection, "pluginFolder"), dr1["assembly"].ToString()));
            }
            catch
            {
                assemblyFile = null;
            }

            if ((assemblyFile == null) || (!assemblyFile.Exists))
            {
                Error(ErrorType.InternalError, "Plugin assembly file not found", "", null);
                return null;
            }

            PluginConnectorBase selectedPlugin = null;
            try
            {

                Byte[] rawAssembly = File.ReadAllBytes(assemblyFile.FullName);
                List<PluginConnectorBase> p1 = Plugins.GetPlugins<PluginConnectorBase>(rawAssembly);

                Array.Clear(rawAssembly, 0, rawAssembly.Length);
                rawAssembly = null;

                foreach (PluginConnectorBase p in p1)
                    if (p.GetPluginId().AbsoluteUri.ToLower() == dr1["uri"].ToString().ToLower())
                        selectedPlugin = p;

                if (selectedPlugin == null)
                {
                    Error(ErrorType.InternalError, "Plugin uri '" + dr1["uri"] + "' not found in assembly '" + dr1["assembly"] + "'", "", null);
                    return null;
                }

            }
            catch (Exception ex)
            {
                Error(ErrorType.InternalError, "Erro on load assembly '" + dr1["assembly"] + "'", ex.Message, null);
                return null;
            }


            List<String> log = new List<String>();

            //Verifica todas as dependências

            List<PluginConfigFields> pgConfigFields = new List<PluginConfigFields>();
            pgConfigFields.AddRange(selectedPlugin.GetConfigFields());

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {
                Dictionary<String, Object> config = (Dictionary<String, Object>)parameters["configparameters"];
                foreach (String key in config.Keys)
                {
                    String val = null;

                    PluginConfigFields field = pgConfigFields.Find(f => (f.Key == key));
                    if (field != null)
                    {

                        //Verifica o tipo de dado esperado e realiza os tratamentos e consistência
                        switch (field.Type)
                        {

                            case PluginConfigTypes.Int32:
                                try
                                {
                                    Int32 tmp = Int32.Parse(config[key].ToString());
                                    val = tmp.ToString();
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a integer.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.Int64:
                                try
                                {
                                    Int64 tmp = Int64.Parse(config[key].ToString());
                                    val = tmp.ToString();
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a long integer.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.DateTime:
                                try
                                {
                                    DateTime tmp = DateTime.Parse(config[key].ToString());
                                    val = tmp.ToString("o");
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a date and time.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.Directory:
                                try
                                {
                                    DirectoryInfo tmp = new DirectoryInfo(config[key].ToString());
                                    val = tmp.FullName;
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a directory.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.StringFixedList:
                                foreach (String s in field.ListValue)
                                    if (s == config[key].ToString())
                                        val = s;

                                if (val == null)
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a item of the fixed list.", "", null);
                                    return null;
                                }

                                break;

                            case PluginConfigTypes.Base64FileData:
                                try
                                {
                                    Byte[] tmp = Convert.FromBase64String(config[key].ToString().Replace("\r", "").Replace("\n", ""));
                                    val = config[key].ToString().Replace("\r", "").Replace("\n", "");
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a base64 file data.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.Boolean:
                                try
                                {
                                    Boolean tmp = Boolean.Parse(config[key].ToString());
                                    val = tmp.ToString();
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a boolean.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.Uri:
                                try
                                {
                                    Uri tmp = new Uri(config[key].ToString());
                                    val = tmp.AbsoluteUri;
                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a uri.", "", null);
                                    return null;
                                }
                                break;

                            case PluginConfigTypes.StringList:
                                try
                                {
                                    if (config[key] is ArrayList)
                                    {
                                        List<Object> lst = new List<Object>();
                                        lst.AddRange(((ArrayList)config[key]).ToArray());

                                        foreach (String li in lst)
                                        {

                                        }

                                        //Verificar como é realizada a leitura para poder gravar no mesmo padrão no banco
                                        throw new NotImplementedException();
                                    }

                                }
                                catch
                                {
                                    Error(ErrorType.InvalidRequest, "Configuration parameter " + key + "is not a string list.", "", null);
                                    return null;
                                }
                                break;

                            default:
                                val = config[key].ToString().Trim();
                                break;

                        }

                    }
                    else
                    {
                        //Não encontrou o campo no plugin, grava a informação no banco como recebido, sem tratamento e verificação
                        val = config[key].ToString();
                    }

                    if (val != null)
                    {
                        //Grava no banco


                        log.Add(key + " changed to '" + val + "'");
                        ExecuteNonQuery(sqlConnection, "delete from resource_plugin_par where resource_plugin_id = @resource_plugin_id and [key] = '" + key + "'", CommandType.Text,par,  trans);
                        ExecuteNonQuery(sqlConnection, "insert into resource_plugin_par (resource_plugin_id, [key], [value]) values (@resource_plugin_id, '" + key + "', '" + val + "') ", CommandType.Text,par,  trans);

                    }
                }


                AddUserLog(sqlConnection, LogKey.ResourcePluginParameters_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin plugin parameters changed", String.Join("\r\n", log), trans);

                //Se o recurso x plugin estiver habilitado marca para que o proxy realize o download das configurações
                if ((Boolean)dr1["resource_plugin_enabled"] && (Boolean)dr1["resource_enabled"])
                    ExecuteNonQuery(sqlConnection, "update proxy set config = 1 where id = " + dr1["proxy_id"], CommandType.Text,par,  trans);

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch(Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update plugin parameters", "", null);
                return null;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }

            //Retorna os dados
            return get(sqlConnection, parameters, false);
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> change(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.proxy_id, r.enabled resource_enabled, r.name resource_name, p.name plugin_name, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            List<String> log = new List<String>();

            String updateSQL = "";
            Boolean update = false;
            foreach (String key in parameters.Keys)
                switch (key)
                {
                    case "mail_domain":
                        String mail_domain = parameters["mail_domain"].ToString();
                        if ((!String.IsNullOrWhiteSpace(mail_domain)) && (mail_domain != (String)dtResourcePlugin.Rows[0]["mail_domain"]))
                        {
                            par.Add("@mail_domain", typeof(String), mail_domain.Length).Value = mail_domain;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " mail_domain = @mail_domain";
                            update = true;

                            log.Add("Mail domain changed from '" + dtResourcePlugin.Rows[0]["name"] + "' to '" + mail_domain + "'");
                        }
                        break;

                    case "enabled":
                        //Não altera este status, para isso ha um método específico, para poder verificar todas as dependências
                        break;

                    case "resourceid":
                        if (!String.IsNullOrWhiteSpace((String)parameters["resourceid"]))
                        {
                            Int64 resourceid = 0;
                            try
                            {
                                resourceid = Int64.Parse((String)parameters["resourceid"]);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter resourceid is not a long integer.", "", null);
                                return null;
                            }

                            if (resourceid.ToString() != dtResourcePlugin.Rows[0]["resource_id"].ToString())
                            {
                                
                                //Verifica se o recurso existe e pertence a mesma empresa
                                DataTable dtRes = ExecuteDataTable(sqlConnection, "select r.* from resource r with(nolock) inner join context c with(nolock) on c.id = r.context_id where c.enterprise_id = @enterprise_id and r.id = " + resourceid, CommandType.Text, par, null);
                                if ((dtRes == null) || (dtRes.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New resource not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                par.Add("@resource_id", typeof(Int64)).Value = resourceid;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " resource_id = @resource_id";
                                update = true;

                                log.Add("Resource changed from '" + dtResourcePlugin.Rows[0]["resource_name"] + "' to '" + dtRes.Rows[0]["name"] + "'");
                            }

                        }
                        break;

                    case "pluginid":
                        if (!String.IsNullOrWhiteSpace((String)parameters["pluginid"]))
                        {
                            Int64 pluginid = 0;
                            try
                            {
                                pluginid = Int64.Parse((String)parameters["pluginid"]);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter pluginid is not a long integer.", "", null);
                                return null;
                            }

                            if (pluginid.ToString() != dtResourcePlugin.Rows[0]["plugin_id"].ToString())
                            {

                                //Verifica se o recurso existe e pertence a mesma empresa
                                DataTable dtPlug = ExecuteDataTable(sqlConnection, "select * from plugin p with(nolock) where p.enterprise_id in (0,@enterprise_id) and p.id = " + pluginid, CommandType.Text, par, null);
                                if ((dtPlug == null) || (dtPlug.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New plugin not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                par.Add("@plugin_id", typeof(Int64)).Value = pluginid;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " plugin_id = @plugin_id";
                                update = true;

                                log.Add("Plugin changed from '" + dtResourcePlugin.Rows[0]["plugin_name"] + "' to '" + dtPlug.Rows[0]["name"] + "'");
                            }

                        }
                        break;


                    case "permit_add_entity":
                        if (!String.IsNullOrWhiteSpace(parameters["permit_add_entity"].ToString()))
                        {
                            Boolean permit_add_entity = false;
                            try
                            {
                                permit_add_entity = Boolean.Parse(parameters["permit_add_entity"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter permit_add_entity is not a boolean.", "", null);
                                return null;
                            }

                            if (permit_add_entity != (Boolean)dtResourcePlugin.Rows[0]["permit_add_entity"])
                            {
                                par.Add("@permit_add_entity", typeof(Boolean)).Value = permit_add_entity;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " permit_add_entity = @permit_add_entity";
                                update = true;

                                log.Add("Permit add entity changed from '" + dtResourcePlugin.Rows[0]["permit_add_entity"] + "' to '" + permit_add_entity + "'");
                            }

                        }
                        break;

                    case "build_login":
                        if (!String.IsNullOrWhiteSpace((String)parameters["build_login"].ToString()))
                        {
                            Boolean build_login = false;
                            try
                            {
                                build_login = Boolean.Parse(parameters["build_login"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter build_login is not a boolean.", "", null);
                                return null;
                            }

                            if (build_login != (Boolean)dtResourcePlugin.Rows[0]["build_login"])
                            {
                                par.Add("@build_login", typeof(Boolean)).Value = build_login;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " build_login = @build_login";
                                update = true;

                                log.Add("Build login changed from '" + dtResourcePlugin.Rows[0]["build_login"] + "' to '" + build_login + "'");
                            }

                        }
                        break;

                    case "build_mail":
                        if (!String.IsNullOrWhiteSpace((String)parameters["build_mail"].ToString()))
                        {
                            Boolean build_mail = false;
                            try
                            {
                                build_mail = Boolean.Parse(parameters["build_mail"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter build_mail is not a boolean.", "", null);
                                return null;
                            }

                            if (build_mail != (Boolean)dtResourcePlugin.Rows[0]["build_mail"])
                            {
                                par.Add("@build_mail", typeof(Boolean)).Value = build_mail;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " build_mail = @build_mail";
                                update = true;

                                log.Add("Build e-mail changed from '" + dtResourcePlugin.Rows[0]["build_mail"] + "' to '" + build_mail + "'");
                            }

                        }
                        break;

                    case "enable_import":
                        if (!String.IsNullOrWhiteSpace((String)parameters["enable_import"].ToString()))
                        {
                            Boolean enable_import = false;
                            try
                            {
                                enable_import = Boolean.Parse(parameters["enable_import"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter enable_import is not a boolean.", "", null);
                                return null;
                            }

                            if (enable_import != (Boolean)dtResourcePlugin.Rows[0]["enable_import"])
                            {
                                par.Add("@enable_import", typeof(Boolean)).Value = enable_import;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " enable_import = @enable_import";
                                update = true;

                                log.Add("Enable import changed from '" + dtResourcePlugin.Rows[0]["enable_import"] + "' to '" + enable_import + "'");
                            }

                        }
                        break;

                    case "enable_deploy":
                        if (!String.IsNullOrWhiteSpace((String)parameters["enable_deploy"].ToString()))
                        {
                            Boolean enable_deploy = false;
                            try
                            {
                                enable_deploy = Boolean.Parse(parameters["enable_deploy"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter enable_deploy is not a boolean.", "", null);
                                return null;
                            }

                            if (enable_deploy != (Boolean)dtResourcePlugin.Rows[0]["enable_deploy"])
                            {
                                par.Add("@enable_deploy", typeof(Boolean)).Value = enable_deploy;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " enable_deploy = @enable_deploy";
                                update = true;

                                log.Add("Enable deploy changed from '" + dtResourcePlugin.Rows[0]["enable_deploy"] + "' to '" + enable_deploy + "'");
                            }

                        }
                        break;

                    case "deploy_all":
                        if (!String.IsNullOrWhiteSpace((String)parameters["deploy_all"].ToString()))
                        {
                            Boolean deploy_all = false;
                            try
                            {
                                deploy_all = Boolean.Parse(parameters["deploy_all"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter deploy_all is not a boolean.", "", null);
                                return null;
                            }

                            if (deploy_all != (Boolean)dtResourcePlugin.Rows[0]["deploy_all"])
                            {
                                par.Add("@deploy_all", typeof(Boolean)).Value = deploy_all;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " deploy_all = @deploy_all";
                                update = true;

                                log.Add("Deploy all changed from '" + dtResourcePlugin.Rows[0]["deploy_all"] + "' to '" + deploy_all + "'");
                            }

                        }
                        break;

                    case "deploy_after_login":
                        if (!String.IsNullOrWhiteSpace((String)parameters["deploy_after_login"].ToString()))
                        {
                            Boolean deploy_after_login = false;
                            try
                            {
                                deploy_after_login = Boolean.Parse(parameters["deploy_after_login"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter deploy_after_login is not a boolean.", "", null);
                                return null;
                            }

                            if (deploy_after_login != (Boolean)dtResourcePlugin.Rows[0]["deploy_after_login"])
                            {
                                par.Add("@deploy_after_login", typeof(Boolean)).Value = deploy_after_login;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " deploy_after_login = @deploy_after_login";
                                update = true;

                                log.Add("Deploy after login changed from '" + dtResourcePlugin.Rows[0]["deploy_after_login"] + "' to '" + deploy_after_login + "'");
                            }

                        }
                        break;

                    case "password_after_login":
                        if (!String.IsNullOrWhiteSpace((String)parameters["password_after_login"].ToString()))
                        {
                            Boolean password_after_login = false;
                            try
                            {
                                password_after_login = Boolean.Parse(parameters["password_after_login"].ToString());
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter password_after_login is not a boolean.", "", null);
                                return null;
                            }

                            if (password_after_login != (Boolean)dtResourcePlugin.Rows[0]["password_after_login"])
                            {
                                par.Add("@password_after_login", typeof(Boolean)).Value = password_after_login;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " password_after_login = @password_after_login";
                                update = true;

                                log.Add("Deploy password after login changed from '" + dtResourcePlugin.Rows[0]["password_after_login"] + "' to '" + password_after_login + "'");
                            }

                        }
                        break;

                    case "name_field_id":
                        if (!String.IsNullOrWhiteSpace((String)parameters["name_field_id"]))
                        {
                            Int64 name_field_id = 0;
                            try
                            {
                                name_field_id = Int64.Parse((String)parameters["name_field_id"]);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter name_field_id is not a long integer.", "", null);
                                return null;
                            }

                            if (name_field_id.ToString() != dtResourcePlugin.Rows[0]["name_field_id"].ToString())
                            {

                                //Verifica se o recurso existe e pertence a mesma empresa
                                DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field f where f.enterprise_id = @enterprise_id and f.id = " + name_field_id, CommandType.Text, par, null);
                                if ((dtField == null) || (dtField.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New name field not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                par.Add("@name_field_id", typeof(Int64)).Value = name_field_id;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " name_field_id = @name_field_id";
                                update = true;

                                log.Add("Name field changed from '" + dtResourcePlugin.Rows[0]["name_field_name"] + "' to '" + dtField.Rows[0]["name"] + "'");
                            }

                        }
                        break;

                    case "mail_field_id":
                        if (!String.IsNullOrWhiteSpace((String)parameters["mail_field_id"]))
                        {
                            Int64 mail_field_id = 0;
                            try
                            {
                                mail_field_id = Int64.Parse((String)parameters["mail_field_id"]);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter mail_field_id is not a long integer.", "", null);
                                return null;
                            }

                            if (mail_field_id.ToString() != dtResourcePlugin.Rows[0]["mail_field_id"].ToString())
                            {

                                //Verifica se o recurso existe e pertence a mesma empresa
                                DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field f where f.enterprise_id = @enterprise_id and f.id = " + mail_field_id, CommandType.Text, par, null);
                                if ((dtField == null) || (dtField.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New mail field not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                par.Add("@mail_field_id", typeof(Int64)).Value = mail_field_id;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " mail_field_id = @mail_field_id";
                                update = true;

                                log.Add("Mail field changed from '" + dtResourcePlugin.Rows[0]["mail_field_name"] + "' to '" + dtField.Rows[0]["name"] + "'");
                            }

                        }
                        break;

                    case "login_field_id":
                        if (!String.IsNullOrWhiteSpace((String)parameters["login_field_id"]))
                        {
                            Int64 login_field_id = 0;
                            try
                            {
                                login_field_id = Int64.Parse((String)parameters["login_field_id"]);
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter login_field_id is not a long integer.", "", null);
                                return null;
                            }

                            if (login_field_id.ToString() != dtResourcePlugin.Rows[0]["login_field_id"].ToString())
                            {

                                //Verifica se o recurso existe e pertence a mesma empresa
                                DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field f where f.enterprise_id = @enterprise_id and f.id = " + login_field_id, CommandType.Text, par, null);
                                if ((dtField == null) || (dtField.Rows.Count == 0))
                                {
                                    Error(ErrorType.InvalidRequest, "New mail field not exists or is not a chield of this enterprise.", "", null);
                                    return null;
                                }

                                par.Add("@login_field_id", typeof(Int64)).Value = login_field_id;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " login_field_id = @login_field_id";
                                update = true;

                                log.Add("Login field changed from '" + dtResourcePlugin.Rows[0]["login_field_name"] + "' to '" + dtField.Rows[0]["name"] + "'");
                            }

                        }
                        break;


                    case "deploy_password_hash":
                        if (!String.IsNullOrWhiteSpace((String)parameters["deploy_password_hash"]))
                        {
                            String hash = "none";
                            switch (parameters["deploy_password_hash"].ToString().ToLower())
                            {
                                case "md5":
                                case "sha1":
                                case "sha256":
                                case "sha512":
                                    hash = parameters["deploy_password_hash"].ToString().ToLower();
                                    break;
                            }

                            if (hash != dtResourcePlugin.Rows[0]["deploy_password_hash"].ToString())
                            {
                                par.Add("@deploy_password_hash", typeof(String)).Value = hash;
                                if (updateSQL != "") updateSQL += ", ";
                                updateSQL += " deploy_password_hash = @deploy_password_hash";
                                update = true;

                                log.Add("Deploy password hash changed from '" + dtResourcePlugin.Rows[0]["deploy_password_hash"] + "' to '" + hash.ToUpper() + "'");
                            }

                        }
                        break;

                }

            if (update)
            {
                updateSQL = "update resource_plugin set " + updateSQL + " where id = @resource_plugin_id";
                ExecuteNonQuery(sqlConnection, updateSQL, CommandType.Text, par);
                AddUserLog(sqlConnection, LogKey.ResourcePlugin_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Resource x Plugin changed", String.Join("\r\n", log));


                //Se o recurso x plugin estiver habilitado marca para que o proxy realize o download das configurações
                if ((Boolean)dtResourcePlugin.Rows[0]["enabled"] && (Boolean)dtResourcePlugin.Rows[0]["resource_enabled"])
                    ExecuteNonQuery(sqlConnection, "update proxy set config = 1 where id = " + dtResourcePlugin.Rows[0]["proxy_id"], CommandType.Text, par);

            }

            //Atualiza a busca com os dados atualizados
            dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.proxy_id, r.enabled resource_enabled, r.name resource_name, p.name plugin_name, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);

            DataRow dr1 = dtResourcePlugin.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("name", dr1["name"]);
            newItem.Add("resource_name", dr1["resource_name"]);
            newItem.Add("plugin_id", dr1["plugin_id"]);
            newItem.Add("plugin_name", dr1["plugin_name"]);
            newItem.Add("resource_plugin_id", dr1["id"]);
            newItem.Add("resource_id", dr1["resource_id"]);
            newItem.Add("permit_add_entity", dr1["permit_add_entity"]);
            newItem.Add("enabled", dr1["enabled"]);
            newItem.Add("mail_domain", dr1["mail_domain"]);
            newItem.Add("build_login", dr1["build_login"]);
            newItem.Add("build_mail", dr1["build_mail"]);
            newItem.Add("enable_import", dr1["enable_import"]);
            newItem.Add("enable_deploy", dr1["enable_deploy"]);
            newItem.Add("order", dr1["order"]);
            newItem.Add("name_field_id", dr1["name_field_id"]);
            newItem.Add("mail_field_id", dr1["mail_field_id"]);
            newItem.Add("login_field_id", dr1["login_field_id"]);
            newItem.Add("deploy_after_login", dr1["deploy_after_login"]);
            newItem.Add("password_after_login", dr1["password_after_login"]);
            newItem.Add("deploy_process", dr1["deploy_process"]);
            newItem.Add("deploy_all", dr1["deploy_all"]);
            newItem.Add("deploy_password_hash", (dr1["deploy_password_hash"] != DBNull.Value ? dr1["deploy_password_hash"].ToString().ToLower() : "none"));
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            Dictionary<string, object> newItem2 = new Dictionary<string, object>();
            newItem2.Add("resource_name", dr1["resource_name"]);
            newItem2.Add("plugin_name", dr1["plugin_name"]);
            newItem2.Add("name_field_name", dr1["name_field_name"]);
            newItem2.Add("mail_field_name", dr1["mail_field_name"]);
            newItem2.Add("login_field_name", dr1["login_field_name"]);

            result.Add("related_names", newItem2);

            return result;

        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> list(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            String text = "";

            if (parameters.ContainsKey("text"))
                text = (String)parameters["text"];

            if (String.IsNullOrWhiteSpace(text))
                text = "";

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@text", typeof(String), text.Length).Value = text;

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
            sql += "    ROW_NUMBER() OVER (ORDER BY (r.name + ' x ' + p.name)) AS [row_number], rp.id ";
            sql += "     FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id INNER JOIN proxy p1 on r.proxy_id = p1.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id";
            sql += "     WHERE c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and (r.name like '%'+@text+'%' or p.name like '%'+@text+'%')");

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

                        case "pluginid":
                            try
                            {
                                sql += " and p.id = " + Int64.Parse(filter[k].ToString()).ToString();
                            }
                            catch { }
                            break;

                        case "resourceid":
                            try
                            {
                                sql += " and r.id = " + Int64.Parse(filter[k].ToString()).ToString();
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

            DataTable dtRoles = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
            if ((dtRoles != null) && (dtRoles.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtRoles.Rows)
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("resourcepluginid", dr1["id"]);
                    param.Add("checkconfig", (parameters.ContainsKey("checkconfig") && (parameters["checkconfig"] is Boolean) && ((Boolean)parameters["checkconfig"])));

                    Dictionary<string, object> newItem = get(sqlConnection, param, true);

                    result.Add(newItem);
                }

            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.search'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> identity(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.proxy_id, r.enabled resource_enabled, r.name resource_name, p.name plugin_name, f1.name name_field_name, f2.name mail_field_name, f3.name login_field_name FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id left join field f1 on f1.id = rp.name_field_id left join field f2 on f2.id = rp.mail_field_id left join field f3 on f3.id = rp.login_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return null;
            }

            Boolean deleted = false;
            if ((parameters.ContainsKey("deleted")) && (parameters["deleted"] is Boolean))
                deleted = (Boolean)parameters["deleted"];

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
            sql += "  SELECT";
            sql += "    ROW_NUMBER() OVER (ORDER BY e.full_name) AS [row_number], e.*";
            sql += "    from [identity] i with(nolock) inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id";
            sql += "    inner join entity e with(nolock) on e.id = i.entity_id";
            sql += "    inner join context c with(nolock) on c.id = e.context_id";
            sql += "  WHERE ";
            sql += " (" + (deleted ? "" : "e.deleted = 0 and i.deleted = 0 and ") + " c.enterprise_id = @enterprise_id and rp.id = @resource_plugin_id)";
            sql += " ) SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtUsers = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User list not found.", "", null);
                return null;
            }

            foreach (DataRow dr in dtUsers.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();
                newItem.Add("userid", dr["id"]);
                newItem.Add("alias", dr["alias"]);
                newItem.Add("login", dr["login"]);
                newItem.Add("full_name", dr["full_name"]);
                newItem.Add("create_date", (Int32)((((DateTime)dr["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds));
                newItem.Add("change_password", (dr["change_password"] != DBNull.Value ? (Int32)((((DateTime)dr["change_password"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                newItem.Add("last_login", (dr["last_login"] != DBNull.Value ? (Int32)((((DateTime)dr["last_login"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                newItem.Add("must_change_password", dr["must_change_password"]);
                newItem.Add("locked", dr["locked"]);

                result.Add(newItem);
            }

            return result;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean addidentity(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }

            String userid = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(userid))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return false;
            }

            List<Int64> users = new List<Int64>();
            String[] t = userid.Split(",".ToCharArray());
            foreach (String u in t)
                try
                {
                    Int64 tmp = Int64.Parse(u);
                    users.Add(tmp);
                }
                catch
                {
                    Error(ErrorType.InvalidRequest, "Parameter users is not a long integer.", "", null);
                    return false;
                }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;

            DataTable dtResourcePlugin = ExecuteDataTable(sqlConnection, "SELECT rp.*, (r.name + ' x ' + p.name) as name, r.proxy_id, r.id resource_id, r.enabled resource_enabled, r.name resource_name, p.name plugin_name, p.id plugin_id FROM resource_plugin rp with(nolock) INNER JOIN resource r with(nolock) ON r.id = rp.resource_id INNER JOIN plugin p with(nolock) ON p.id = rp.plugin_id INNER JOIN context c with(nolock) ON r.context_id = c.id left join field f1 on f1.id = rp.name_field_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResourcePlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResourcePlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            //Busca campos identificadores deste plugin
            List<String> idFields = new List<string>();
            DataTable dtResourcePluginIds = ExecuteDataTable(sqlConnection, "select * from resource_plugin_mapping rpm with(nolock) WHERE (is_id = 1 or is_unique_property = 1) AND rpm.resource_plugin_id = @resource_plugin_id", CommandType.Text, par, null);
            if (dtResourcePluginIds != null)
                foreach (DataRow dr in dtResourcePluginIds.Rows)
                    idFields.Add(dr["field_id"].ToString());

            SqlTransaction trans = sqlConnection.BeginTransaction();

            try
            {

                List<String> logAll = new List<string>();

                foreach (Int64 u in users)
                {
                    //Verifica a existência do usuário no mesmo contexto
                    DataTable dtEntity = ExecuteDataTable(sqlConnection, "select distinct e.* from entity e with(nolock) inner join resource r with(nolock) on e.context_id = r.context_id inner join resource_plugin rp with(nolock) on r.id = rp.resource_id WHERE e.id = " + u, CommandType.Text, par, trans);
                    if ((dtEntity == null) || (dtEntity.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "New entity field not exists or is not a chield of this context.", "", null);
                        return false;
                    }

                    //Adiciona o identity, se houver resgata o ID
                    DbParameterCollection parId = new DbParameterCollection();
                    parId.Add("@entityId", typeof(Int64)).Value = u;
                    parId.Add("@pluginId", typeof(Int64)).Value = dtResourcePlugin.Rows[0]["plugin_id"];
                    parId.Add("@resourceId", typeof(Int64)).Value = dtResourcePlugin.Rows[0]["resource_id"];

                    DataTable dtNewIdentity = ExecuteDataTable(sqlConnection, "sp_new_identity", CommandType.StoredProcedure, parId, trans);
                    if ((dtNewIdentity == null) || (dtNewIdentity.Rows.Count == 0))
                    {
                        Error(ErrorType.InternalError, "Erro on insert new identity of " + dtEntity.Rows[0]["full_name"], "", null);
                        return false;
                    }

                    Int64 identityId = (Int64)dtNewIdentity.Rows[0]["identity_id"];


                    //Se for novo verifica os proximos passos, caso não ignora
                    if ((Boolean)dtNewIdentity.Rows[0]["new_identity"])
                    {
                        logAll.Add("New identity of resource x plugin " + dtResourcePlugin.Rows[0]["name"] + " added in entity " + dtEntity.Rows[0]["full_name"]);

                        List<String> log = new List<string>();

                        //Verifica se encontra os campos obrigatórios

                        if (idFields.Count > 0)
                        {
                            Boolean ifeOK = false;

                            //Procura primeiro resource x plugin de entrada
                            DataTable dtIfe = ExecuteDataTable(sqlConnection, "select distinct ife.field_id, ife.value, f.name field_name from [identity] i with(nolock) inner join identity_field ife with(nolock) on i.id = ife.identity_id inner join field f on f.id = ife.field_id inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id inner join resource_plugin_mapping rpm with(nolock) on rpm.resource_plugin_id = rp.id WHERE rp.permit_add_entity = 1 and rpm.field_id in (" + String.Join(",", idFields) + ") and i.entity_id = " + u, CommandType.Text, null, trans);
                            if ((dtIfe != null) && (dtIfe.Rows.Count > 0))
                            {
                                DbParameterCollection par2 = new DbParameterCollection();
                                par2.Add("@identity_id", typeof(Int64)).Value = identityId;
                                par2.Add("@field_id", typeof(Int64)).Value = dtIfe.Rows[0]["field_id"];
                                par2.Add("@value", typeof(String)).Value = dtIfe.Rows[0]["value"];

                                //Insere os valores, caso não exista
                                ExecuteNonQuery(sqlConnection, "insert into identity_field (identity_id, field_id, value) SELECT @identity_id, @field_id, @value WHERE not exists (select 1 from identity_field where identity_id = @identity_id and field_id = @field_id)", CommandType.Text,par2,  trans);
                                log.Add("Field value added: Field name = " + dtIfe.Rows[0]["field_name"] + ", Value = " + dtIfe.Rows[0]["value"]);
                                logAll.Add("Field value added: Field name = " + dtIfe.Rows[0]["field_name"] + ", Value = " + dtIfe.Rows[0]["value"]);
                                ifeOK = true;
                            }

                            //Procura nos outros resource x plugin
                            dtIfe = ExecuteDataTable(sqlConnection, "select distinct ife.field_id, ife.value, f.name field_name from [identity] i with(nolock) inner join identity_field ife with(nolock) on i.id = ife.identity_id inner join field f on f.id = ife.field_id inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id inner join resource_plugin_mapping rpm with(nolock) on rpm.resource_plugin_id = rp.id WHERE rp.permit_add_entity = 0 and rpm.field_id in (" + String.Join(",", idFields) + ") and i.entity_id = " + u, CommandType.Text, null, trans);
                            if ((dtIfe != null) && (dtIfe.Rows.Count > 0))
                            {
                                DbParameterCollection par2 = new DbParameterCollection();
                                par2.Add("@identity_id", typeof(Int64)).Value = identityId;
                                par2.Add("@field_id", typeof(Int64)).Value = dtIfe.Rows[0]["field_id"];
                                par2.Add("@value", typeof(String)).Value = dtIfe.Rows[0]["value"];

                                //Insere os valores, caso não exista
                                ExecuteNonQuery(sqlConnection, "insert into identity_field (identity_id, field_id, value) SELECT @identity_id, @field_id, @value WHERE not exists (select 1 from identity_field where identity_id = @identity_id and field_id = @field_id)", CommandType.Text,par2,  trans);
                                log.Add("Field value added: Field name = " + dtIfe.Rows[0]["field_name"] + ", Value = " + dtIfe.Rows[0]["value"]);
                                logAll.Add("Field value added: Field name = " + dtIfe.Rows[0]["field_name"] + ", Value = " + dtIfe.Rows[0]["value"]);
                                ifeOK = true;
                            }

                            if (!ifeOK)
                            {
                                Error(ErrorType.InternalError, "Has no id or unique field to insert on identity", "", null);
                                return false;
                            }
                        }

                        AddUserLog(sqlConnection, LogKey.User_IdentityNew, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, u, identityId, "Identity added on resource x plugin " + dtResourcePlugin.Rows[0]["name"], String.Join("\r\n", log), trans);
                    }
                    else
                    {
                        logAll.Add("Identity of resource x plugin " + dtResourcePlugin.Rows[0]["name"] + " already exists on entity " + dtEntity.Rows[0]["full_name"]);
                    }

                    logAll.Add("");
                }

                AddUserLog(sqlConnection, LogKey.ResourcePluginIdentity_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Identity added", String.Join("\r\n", logAll), trans);

                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InternalError, "Erro on proccess identities", ex.Message, null);
                return false;
            }
            finally
            {
                //Saiu por algum erro, faz o rollback das transações
                if (trans != null)
                    trans.Rollback();
            }

            return true;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteidentity(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }


            String resourceplugin = parameters["resourcepluginid"].ToString();
            if (String.IsNullOrWhiteSpace(resourceplugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return false;
            }

            Int64 resourcepluginid = 0;
            try
            {
                resourcepluginid = Int64.Parse(resourceplugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not a long integer.", "", null);
                return false;
            }


            String user = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(user))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return false;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(user);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return false;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcepluginid;
            par.Add("@entity_id", typeof(Int64)).Value = userid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "select rp.*, (r.name + ' x ' + p.name) as name from resource_plugin rp with(nolock) inner join resource r with(nolock) on r.id = rp.resource_id inner join plugin p with(nolock) on p.id = rp.plugin_id inner join context c with(nolock) on c.id = r.context_id WHERE rp.id = @resource_plugin_id AND c.enterprise_id = @enterprise_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Resource x Plugin not found.", "", null);
                return false;
            }

            DataTable dtIdentity = ExecuteDataTable(sqlConnection, "select i.* from [identity] i with(nolock) inner join entity e with(nolock) on e.id = i.entity_id inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id WHERE rp.id = @resource_plugin_id and e.id = @entity_id", CommandType.Text, par, null);
            if ((dtIdentity == null) || (dtIdentity.Rows.Count == 0))
            {
                Error(ErrorType.InvalidRequest, "Identity not exists or is not a chield of this resource x plugin.", "", null);
                return false;
            }

            try
            {

                foreach (DataRow dr in dtIdentity.Rows)
                    if ((dr["id"] != DBNull.Value) && (dr["entity_id"] != DBNull.Value))
                    {
                        AddUserLog(sqlConnection, LogKey.User_IdentityDeleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], (Int64)dr["id"], "Identity deleted of resource x plugin " + dtResource.Rows[0]["name"], "");
                        ExecuteNonQuery(sqlConnection, "delete from [identity] where id = " + dr["id"], CommandType.Text, par);
                        ExecuteNonQuery(sqlConnection, "insert into deploy_now (entity_id) values(" + dr["entity_id"] + ")", CommandType.Text,null,  null);
                    }

            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Erro on delete identity of resource x plugin " + dtResource.Rows[0]["name"], ex.Message, null);
                return false;
            }

            return true;
        }


    }
}
