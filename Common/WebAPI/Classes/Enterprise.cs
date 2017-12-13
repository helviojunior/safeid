﻿/// 
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

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Enterprise : APIBase
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
                    return newenterprise(sqlConnection, parameters);
                    break;

                case "get":
                    return get(sqlConnection, parameters);
                    break;

                case "list":
                case "search":
                    return list(sqlConnection, parameters);
                    break;

                case "change":
                    return change(sqlConnection, parameters);
                    break;

                case "delete":
                    return delete(sqlConnection, parameters);
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
        private Dictionary<String, Object> newenterprise(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            par.Add("@name", typeof(String), name.Length).Value = name;
            par.Add("@password_rule", typeof(String), password_rule.Length).Value = password_rule;
            par.Add("@pwd_length", typeof(Int32)).Value = password_length;
            par.Add("@pwd_upper_case", typeof(Boolean)).Value = password_upper_case;
            par.Add("@pwd_lower_case", typeof(Boolean)).Value = password_lower_case;
            par.Add("@pwd_digit", typeof(Boolean)).Value = password_digit;
            par.Add("@pwd_symbol", typeof(Boolean)).Value = password_symbol;
            par.Add("@pwd_no_name", typeof(Boolean)).Value = password_no_name;

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "sp_new_context", CommandType.StoredProcedure, par, null);
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
        private Dictionary<String, Object> get(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtEnterprise = ExecuteDataTable(sqlConnection, "select e.* from enterprise e with(nolock) where e.id = @enterprise_id order by e.name", CommandType.Text, par, null);
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
            newItem.Add("cas_service", dr1["cas_service"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            DataTable dtEnterpriseFqdn = ExecuteDataTable(sqlConnection, "select * from enterprise_fqdn_alias where enterprise_id = " + dr1["id"], CommandType.Text, null, null);
            if ((dtEnterpriseFqdn != null) && (dtEnterpriseFqdn.Rows.Count > 0))
            {
                List<String> fqdn = new List<String>();
                foreach(DataRow dr in dtEnterpriseFqdn.Rows)
                    fqdn.Add(dr["fqdn"].ToString());

                result.Add("fqdn_alias", fqdn);
            }
            

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean delete(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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

            ExecuteNonQuery(sqlConnection, "delete from context where id = @context_id", CommandType.Text, par);

            AddUserLog(sqlConnection, LogKey.Context_Deleted, null, "API", UserLogLevel.Error, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Context " + dtUsers.Rows[0]["name"] + " deleted", "");

            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> change(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtEnterprise = ExecuteDataTable(sqlConnection, "select * from enterprise where id = @enterprise_id", CommandType.Text, par, null);
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


            String updateSQL = "update enterprise set ";
            String updateFields = "";
            Boolean update = false;
            Boolean updateHosts = false;

            foreach (String key in parameters.Keys)
            {
                switch (key.ToLower())
                {
                    case "name":
                        String name = parameters["name"].ToString();
                        if (!String.IsNullOrWhiteSpace(name))
                        {
                            par.Add("@name", typeof(String), name.Length).Value = name;
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

                    case "cas_service":
                        String cas_service = parameters["cas_service"].ToString();
                        if (!String.IsNullOrWhiteSpace(cas_service))
                        {
                            try
                            {
                                Uri tmp = new Uri(cas_service);

                                par.Add("@cas_service", typeof(String), cas_service.Length).Value = cas_service;
                                if (updateFields != "") updateFields += ", ";
                                updateFields += "cas_service = @cas_service";
                                update = true;
                            }
                            catch {
                                Error(ErrorType.InvalidRequest, "Parameter cas_service is not a valid uri.", "", null);
                                return null;
                            }
                        }
                        else
                        {
                            Error(ErrorType.InvalidRequest, "Parameter cas_service is empty.", "", null);
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
                }

            }

            if (update)
            {
                updateSQL += updateFields + " where id = @enterprise_id";
                ExecuteNonQuery(sqlConnection, updateSQL, CommandType.Text, par);
            }

            if (updateHosts)
            {
                foreach (String host in hosts)
                {
                    if (!String.IsNullOrWhiteSpace(host))
                    {
                        DbParameterCollection par1 = new DbParameterCollection();
                        par1.Add("@enterprise_id", typeof(Int64)).Value = enterpriseid;
                        par1.Add("@fqdn", typeof(String), host.Length).Value = host;

                        ExecuteNonQuery(sqlConnection, "insert into enterprise_fqdn_alias (enterprise_id, fqdn) select @enterprise_id, @fqdn where not exists (select 1 from enterprise_fqdn_alias where enterprise_id = @enterprise_id and fqdn = @fqdn) ", CommandType.Text, par1);
                    }
                }

                ExecuteNonQuery(sqlConnection, "delete from enterprise_fqdn_alias where enterprise_id = @enterprise_id " + (hosts.Count > 0 ? " and fqdn not in ('" + String.Join("', '", hosts) + "')" : ""), CommandType.Text, par);
            }

            //Atualiza a busca com os dados atualizados
            dtEnterprise = ExecuteDataTable(sqlConnection, "select e.* from enterprise e with(nolock) where e.id = @enterprise_id order by e.name", CommandType.Text, par, null);

            DataRow dr1 = dtEnterprise.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("fqdn", dr1["fqdn"]);
            newItem.Add("server_cert", dr1["server_cert"]);
            newItem.Add("language", dr1["language"]);
            newItem.Add("cas_service", dr1["cas_service"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            DataTable dtEnterpriseFqdn = ExecuteDataTable(sqlConnection, "select * from enterprise_fqdn_alias where enterprise_id = " + dr1["id"], CommandType.Text, null, null);
            if ((dtEnterpriseFqdn != null) && (dtEnterpriseFqdn.Rows.Count > 0))
            {
                List<String> fqdn = new List<String>();
                foreach (DataRow dr in dtEnterpriseFqdn.Rows)
                    fqdn.Add(dr["fqdn"].ToString());

                result.Add("fqdn_alias", fqdn);
            }
            
            return result;

        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Object> list(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            sql += "    ROW_NUMBER() OVER (ORDER BY c.name) AS [row_number], c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) ";
            sql += "     from context c with(nolock)  ";
            sql += "     where c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and c.name like '%'+@text+'%'");
            sql += ") SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtContext = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
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
