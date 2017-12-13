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
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.WebAPI;
using IAM.AuthPlugins;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Enterprise : IAMAPIBase
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
                    return newenterprise(database, parameters);
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
        private Dictionary<String, Object> newenterprise(IAMDatabase database, Dictionary<String, Object> parameters)
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

            throw new NotImplementedException();

            if (!parameters.ContainsKey("password_rule"))
            {
                Error(ErrorType.InvalidRequest, "Parameter password_rule is not defined.", "", null);
                return null;
            }


            String password_rule = parameters["password_rule"].ToString();
            if (String.IsNullOrWhiteSpace(password_rule))
            {
                Error(ErrorType.InvalidRequest, "Parameter password_rule is not defined.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("password_length"))
            {
                Error(ErrorType.InvalidRequest, "Parameter password_length is not defined.", "", null);
                return null;
            }

            String pwdlength = parameters["password_length"].ToString();
            if (String.IsNullOrWhiteSpace(pwdlength))
            {
                Error(ErrorType.InvalidRequest, "Parameter password_length is not defined.", "", null);
                return null;
            }

            Int32 password_length = 0;
            try
            {
                password_length = Int32.Parse(pwdlength);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter password_length is not a integer.", "", null);
                return null;
            }

            Boolean password_upper_case = true;
            if (parameters.ContainsKey("password_upper_case"))
            {
                try
                {
                    password_upper_case = Boolean.Parse(parameters["password_upper_case"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter password_upper_case is not a boolean.", "", null);
                    return null;
                }
            }

            Boolean password_lower_case = true;
            if (parameters.ContainsKey("password_lower_case"))
            {
                try
                {
                    password_lower_case = Boolean.Parse(parameters["password_lower_case"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter password_lower_case is not a boolean.", "", null);
                    return null;
                }
            }

            Boolean password_digit = true;
            if (parameters.ContainsKey("password_digit"))
            {
                try
                {
                    password_digit = Boolean.Parse(parameters["password_digit"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter password_digit is not a boolean.", "", null);
                    return null;
                }
            }

            Boolean password_symbol = true;
            if (parameters.ContainsKey("password_symbol"))
            {
                try
                {
                    password_symbol = Boolean.Parse(parameters["password_symbol"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter password_symbol is not a boolean.", "", null);
                    return null;
                }
            }

            Boolean password_no_name = true;
            if (parameters.ContainsKey("password_no_name"))
            {
                try
                {
                    password_no_name = Boolean.Parse(parameters["password_no_name"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter password_no_name is not a boolean.", "", null);
                    return null;
                }
            }
            
            //Valida a regra de senha
            String pwdMethod = "";
            String pwdValue = "";
            Regex rex = new Regex(@"(.*?)\[(.*?)\]");
            Match m = rex.Match(password_rule);
            if (m.Success)
            {
                pwdMethod = m.Groups[1].Value.ToLower();
                pwdValue = m.Groups[2].Value;
            }

            if (pwdMethod.ToLower() == "default")
            {
                if (String.IsNullOrEmpty(pwdValue))
                {
                    Error(ErrorType.InvalidRequest, "Password rule error: not valid password for method 'default'.", "", null);
                    return null;
                }
                else
                {
                    password_rule = "default[" + pwdValue + "]";
                }
            }
            else if (pwdMethod.ToLower() == "random")
            {
                password_rule = "random[]";
            }
            else
            {
                Error(ErrorType.InvalidRequest, "Password rule error: has no valid method.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@name", typeof(String)).Value = name;
            par.Add("@password_rule", typeof(String)).Value = password_rule;
            par.Add("@pwd_length", typeof(Int32)).Value = password_length;
            par.Add("@pwd_upper_case", typeof(Boolean)).Value = password_upper_case;
            par.Add("@pwd_lower_case", typeof(Boolean)).Value = password_lower_case;
            par.Add("@pwd_digit", typeof(Boolean)).Value = password_digit;
            par.Add("@pwd_symbol", typeof(Boolean)).Value = password_symbol;
            par.Add("@pwd_no_name", typeof(Boolean)).Value = password_no_name;

            DataTable dtUsers = database.ExecuteDataTable( "sp_new_context", CommandType.StoredProcedure, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Context not found.", "", null);
                return null;
            }

            DataRow dr1 = dtUsers.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("context_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("password_rule", dr1["password_rule"]);
            newItem.Add("auth_key_time", dr1["auth_key_time"]);
            newItem.Add("password_length", dr1["pwd_length"]);
            newItem.Add("password_upper_case", (Boolean)dr1["pwd_upper_case"]);
            newItem.Add("password_lower_case", (Boolean)dr1["pwd_lower_case"]);
            newItem.Add("password_digit", (Boolean)dr1["pwd_digit"]);
            newItem.Add("password_symbol", (Boolean)dr1["pwd_symbol"]);
            newItem.Add("password_no_name", (Boolean)dr1["pwd_no_name"]);
            newItem.Add("entity_qty", (Int32)dr1["entity_qty"]);
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

            if (!parameters.ContainsKey("enterpriseid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter enterpriseid is not defined.", "", null);
                return null;
            }

            String enterprise = parameters["enterpriseid"].ToString();
            if (String.IsNullOrWhiteSpace(enterprise))
            {
                Error(ErrorType.InvalidRequest, "Parameter enterpriseid is not defined.", "", null);
                return null;
            }

            Int64 enterpriseid = 0;
            try
            {
                enterpriseid = Int64.Parse(enterprise);
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter enterpriseid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseid;

            DataTable dtEnterprise = database.ExecuteDataTable( "select e.* from enterprise e with(nolock) where e.id = @enterprise_id order by e.name", CommandType.Text, par, null);
            if (dtEnterprise == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtEnterprise.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Enterprise not found.", "", null);
                return null;
            }

            DataRow dr1 = dtEnterprise.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("fqdn", dr1["fqdn"]);
            newItem.Add("server_cert", dr1["server_cert"]);
            newItem.Add("language", dr1["language"]);
            newItem.Add("auth_plugin", dr1["auth_plugin"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            DataTable dtEnterpriseFqdn = database.ExecuteDataTable( "select * from enterprise_fqdn_alias where enterprise_id = " + dr1["id"], CommandType.Text, null, null);
            if ((dtEnterpriseFqdn != null) && (dtEnterpriseFqdn.Rows.Count > 0))
            {
                List<String> fqdn = new List<String>();
                foreach(DataRow dr in dtEnterpriseFqdn.Rows)
                    fqdn.Add(dr["fqdn"].ToString());

                result.Add("fqdn_alias", fqdn);
            }

            DataTable dtEnterpriseAuthPars = database.ExecuteDataTable("select * from dbo.enterprise_auth_par where enterprise_id = " + dr1["id"] + " and plugin = '" + dr1["auth_plugin"] + "'", CommandType.Text, null, null);
            if ((dtEnterpriseAuthPars != null) && (dtEnterpriseAuthPars.Rows.Count > 0))
            {
                List<Dictionary<string, object>> p1 = new List<Dictionary<string, object>>();
                
                foreach (DataRow dr in dtEnterpriseAuthPars.Rows)
                {
                    Dictionary<string, object> newItem2 = new Dictionary<string, object>();

                    newItem2.Add("key", dr["key"].ToString());
                    newItem2.Add("value", dr["value"].ToString());

                    p1.Add(newItem2);
                }

                result.Add("auth_parameters", p1);
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

            throw new NotImplementedException();

            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return false;
            }


            String context = parameters["contextid"].ToString();
            if (String.IsNullOrWhiteSpace(context))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return false;
            }

            Int64 contextid = 0;
            try
            {
                contextid = Int64.Parse(context);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable( "select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Context not found.", "", null);
                return false;
            }

            if ((Int32)dtUsers.Rows[0]["entity_qty"] > 0)
            {
                Error(ErrorType.InvalidRequest, "Context is not empty.", "", null);
                return false;
            }

            database.ExecuteNonQuery( "delete from context where id = @context_id", CommandType.Text, par);

            database.AddUserLog( LogKey.Context_Deleted, null, "API", UserLogLevel.Error, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Context " + dtUsers.Rows[0]["name"] + " deleted", "");

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

            if (!parameters.ContainsKey("enterpriseid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter enterpriseid is not defined.", "", null);
                return null;
            }

            String enterprise = parameters["enterpriseid"].ToString();
            if (String.IsNullOrWhiteSpace(enterprise))
            {
                Error(ErrorType.InvalidRequest, "Parameter enterpriseid is not defined.", "", null);
                return null;
            }

            Int64 enterpriseid = 0;
            try
            {
                enterpriseid = Int64.Parse(enterprise);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter enterpriseid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseid;

            DataTable dtEnterprise = database.ExecuteDataTable( "select * from enterprise where id = @enterprise_id", CommandType.Text, par, null);
            if (dtEnterprise == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtEnterprise.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Enterprise not found.", "", null);
                return null;
            }

            List<String> hosts = new List<String>();
            Dictionary<String, String> pgValues = new Dictionary<string, string>();
            Uri pluginUri = null;

            String updateSQL = "update enterprise set ";
            String updateFields = "";
            Boolean update = false;
            Boolean updateHosts = false;
            Boolean updateAuthPars = false;

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

                    case "auth_plugin":

                        String auth_plugin = parameters["auth_plugin"].ToString();
                        if (!String.IsNullOrWhiteSpace(auth_plugin))
                        {

                            try
                            {
                                Uri tmp = new Uri(auth_plugin);
                                if (tmp.Scheme.ToLower() != "auth")
                                    throw new Exception();
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter auth_plugin is not a valid uri.", "", null);
                                return null;
                            }

                            try
                            {
                                AuthBase plugin = AuthBase.GetPlugin(new Uri(auth_plugin));
                                if (plugin == null)
                                    throw new Exception();
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, MessageResource.GetMessage("invalid_auth_service"), "", null);
                                break;
                            }


                            par.Add("@auth_plugin", typeof(String)).Value = auth_plugin;
                            if (updateFields != "") updateFields += ", ";
                            updateFields += "auth_plugin = @auth_plugin";
                            update = true;

                        }
                        else
                        {
                            Error(ErrorType.InvalidRequest, "Parameter auth_plugin is empty.", "", null);
                            return null;
                        }
                        break;

                    case "fqdn_alias":
                        if (parameters[key] is ArrayList)
                        {
                            updateHosts = true;

                            List<Object> ht = new List<Object>();
                            ht.AddRange(((ArrayList)parameters[key]).ToArray());
                            foreach (String host in ht)
                            {
                                if (!String.IsNullOrWhiteSpace(host))
                                {
                                    try
                                    {
                                        Uri tmp = new Uri("http://" + host);
                                        hosts.Add(host);
                                    }
                                    catch
                                    {
                                        Error(ErrorType.InvalidRequest, "Parameter fqdn_alias->" + host + " is not a valid hostname.", "", null);
                                        return null;
                                    }
                                }
                            }

                        }
                        break;


                    case "auth_paramters":
                        if (parameters[key] is Dictionary<String,Object>)
                        {

                            if (!parameters.ContainsKey("auth_plugin"))
                            {
                                Error(ErrorType.InvalidRequest, "Parameter auth_plugin is not defined.", "", null);
                                return null;
                            }

                            if (String.IsNullOrWhiteSpace(parameters["auth_plugin"].ToString()))
                            {
                                Error(ErrorType.InvalidRequest, "Parameter auth_plugin is not defined.", "", null);
                                return null;
                            }

                            try
                            {
                                Uri tmp = new Uri(parameters["auth_plugin"].ToString());
                                if (tmp.Scheme.ToLower() != "auth")
                                    throw new Exception();
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, "Parameter auth_plugin is not a valid uri.", "", null);
                                return null;
                            }

                            AuthBase plugin = null;
                            try
                            {
                                plugin = AuthBase.GetPlugin(new Uri(parameters["auth_plugin"].ToString()));
                                if (plugin == null)
                                    throw new Exception();
                            }
                            catch
                            {
                                Error(ErrorType.InvalidRequest, MessageResource.GetMessage("invalid_auth_service"), "", null);
                                break;
                            }

                            pluginUri = plugin.GetPluginId();

                            updateAuthPars = true;

                            Dictionary<String, Object> p1 = (Dictionary<String, Object>)parameters[key];

                            AuthConfigFields[] fields = plugin.GetConfigFields();
                            foreach (AuthConfigFields f in fields)
                            {
                                String value = "";

                                if (p1.ContainsKey(f.Key))
                                    value = p1[f.Key].ToString();

                                if (!String.IsNullOrEmpty(value))
                                    pgValues.Add(f.Key, value);

                                if (f.Required && !pgValues.ContainsKey(f.Key))
                                {
                                    Error(ErrorType.InvalidRequest, MessageResource.GetMessage("required_field") + " " + f.Name, "", null);
                                    break;
                                }
                            }
                            
                        }
                        break;
                }

            }

            if (update)
            {
                updateSQL += updateFields + " where id = @enterprise_id";
                database.ExecuteNonQuery( updateSQL, CommandType.Text, par);
            }

            if (updateHosts)
            {
                foreach (String host in hosts)
                {
                    if (!String.IsNullOrWhiteSpace(host))
                    {
                        DbParameterCollection par1 = new DbParameterCollection();
                        par1.Add("@enterprise_id", typeof(Int64)).Value = enterpriseid;
                        par1.Add("@fqdn", typeof(String)).Value = host;

                        database.ExecuteNonQuery( "insert into enterprise_fqdn_alias (enterprise_id, fqdn) select @enterprise_id, @fqdn where not exists (select 1 from enterprise_fqdn_alias where enterprise_id = @enterprise_id and fqdn = @fqdn) ", CommandType.Text, par1);
                    }
                }

                database.ExecuteNonQuery( "delete from enterprise_fqdn_alias where enterprise_id = @enterprise_id " + (hosts.Count > 0 ? " and fqdn not in ('" + String.Join("', '", hosts) + "')" : ""), CommandType.Text, par);
            }


            if (updateAuthPars)
            {

                database.ExecuteNonQuery("delete from enterprise_auth_par where enterprise_id = @enterprise_id and plugin = '"+ pluginUri.AbsoluteUri +"'", CommandType.Text, par);

                foreach (String key in pgValues.Keys)
                {
                    if (!String.IsNullOrWhiteSpace(pgValues[key]))
                    {
                        DbParameterCollection par1 = new DbParameterCollection();
                        par1.Add("@enterprise_id", typeof(Int64)).Value = enterpriseid;
                        par1.Add("@plugin", typeof(String)).Value = pluginUri.AbsoluteUri;
                        par1.Add("@key", typeof(String)).Value = key;
                        par1.Add("@value", typeof(String)).Value = pgValues[key];

                        database.ExecuteNonQuery("insert into enterprise_auth_par (enterprise_id, plugin,[key],[value]) VALUES(@enterprise_id, @plugin, @key, @value)", CommandType.Text, par1);
                    }
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
        private List<Object> list(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            throw new NotImplementedException();

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
            sql += "    ROW_NUMBER() OVER (ORDER BY c.name) AS [row_number], c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) ";
            sql += "     from context c with(nolock)  ";
            sql += "     where c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and c.name like '%'+@text+'%'");
            sql += ") SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtContext = database.ExecuteDataTable( sql, CommandType.Text, par, null);
            if ((dtContext != null) && (dtContext.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtContext.Rows)
                {
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("context_id", dr1["id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("password_rule", dr1["password_rule"]);
                    newItem.Add("auth_key_time", dr1["auth_key_time"]);
                    newItem.Add("password_length", dr1["pwd_length"]);
                    newItem.Add("password_upper_case", (Boolean)dr1["pwd_upper_case"]);
                    newItem.Add("password_lower_case", (Boolean)dr1["pwd_lower_case"]);
                    newItem.Add("password_digit", (Boolean)dr1["pwd_digit"]);
                    newItem.Add("password_symbol", (Boolean)dr1["pwd_symbol"]);
                    newItem.Add("password_no_name", (Boolean)dr1["pwd_no_name"]);
                    newItem.Add("entity_qty", (Int32)dr1["entity_qty"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    result.Add(newItem);
                }

            }

            return result;
        }




    }
}
