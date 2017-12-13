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
using System.Text.RegularExpressions;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.WebAPI;
using System.Collections;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Context : IAMAPIBase
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
                    return newcontext(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "getloginrules":
                    return getloginrules(database, parameters);
                    break;

                case "getmailrules":
                    return getmailrules(database, parameters);
                    break;

                case "list":
                case "search":
                    return list(database, parameters);
                    break;

                case "change":
                    return change(database, parameters);
                    break;

                case "changeloginrules":
                    return changeloginrules(database, parameters);
                    break;


                case "changemailrules":
                    return changemailrules(database, parameters);
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
        private Dictionary<String, Object> newcontext(IAMDatabase database, Dictionary<String, Object> parameters)
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

            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }


            String context = parameters["contextid"].ToString();
            if (String.IsNullOrWhiteSpace(context))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }

            Int64 contextid = 0;
            try
            {
                contextid = Int64.Parse(context);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;


            DataTable dtUsers = database.ExecuteDataTable( "select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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
        private List<Dictionary<String, Object>> getloginrules(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<string, object>>();

            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }


            String context = parameters["contextid"].ToString();
            if (String.IsNullOrWhiteSpace(context))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }

            Int64 contextid = 0;
            try
            {
                contextid = Int64.Parse(context);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable("select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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


            DataTable dtRules = database.ExecuteDataTable("select * from login_rule with(nolock) where context_id = @context_id order by [order]", CommandType.Text, par, null);
            if (dtRules == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr in dtRules.Rows)
            {

                Dictionary<string, object> newItem = new Dictionary<string, object>();
                newItem.Add("enterprise_id", dtUsers.Rows[0]["enterprise_id"]);
                newItem.Add("context_id", dtUsers.Rows[0]["id"]);
                newItem.Add("rule_id", dr["id"]);
                newItem.Add("name", dr["name"]);
                newItem.Add("rule", dr["rule"]);
                newItem.Add("order", (Int32)dr["order"]);

                result.Add(newItem);
            }

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.getmailrules'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> getmailrules(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<string, object>>();

            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }


            String context = parameters["contextid"].ToString();
            if (String.IsNullOrWhiteSpace(context))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }

            Int64 contextid = 0;
            try
            {
                contextid = Int64.Parse(context);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable("select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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


            DataTable dtRules = database.ExecuteDataTable("select * from st_mail_rule with(nolock) where context_id = @context_id order by [order]", CommandType.Text, par, null);
            if (dtRules == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr in dtRules.Rows)
            {

                Dictionary<string, object> newItem = new Dictionary<string, object>();
                newItem.Add("enterprise_id", dtUsers.Rows[0]["enterprise_id"]);
                newItem.Add("context_id", dtUsers.Rows[0]["id"]);
                newItem.Add("rule_id", dr["id"]);
                newItem.Add("name", dr["name"]);
                newItem.Add("rule", dr["rule"]);
                newItem.Add("order", (Int32)dr["order"]);

                result.Add(newItem);
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

            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }

            String context = parameters["contextid"].ToString();
            if (String.IsNullOrWhiteSpace(context))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return null;
            }

            Int64 contextid = 0;
            try
            {
                contextid = Int64.Parse(context);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable( "select c.* from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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


            String updateSQL = "update context set ";
            String updateFields = "";
            Boolean update = false;

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

                    case "password_rule":
                        String password_rule = parameters["password_rule"].ToString();
                        if (!String.IsNullOrWhiteSpace(password_rule))
                        {

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


                            par.Add("@password_rule", typeof(String)).Value = password_rule;
                            if (updateFields != "") updateFields += ", ";
                            updateFields += "password_rule = @password_rule";
                            update = true;
                        }
                        else
                        {
                            Error(ErrorType.InvalidRequest, "Parameter password_rule is empty.", "", null);
                            return null;
                        }
                        break;

                    case "password_length":
                        String pwdlength = parameters["password_length"].ToString();
                        if (!String.IsNullOrWhiteSpace(pwdlength))
                        {

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

                            par.Add("@password_length", typeof(Int32)).Value = password_length;
                            if (updateFields != "") updateFields += ", ";
                            updateFields += "pwd_length = @password_length";
                            update = true;
                        }
                        else
                        {
                            Error(ErrorType.InvalidRequest, "Parameter name is not defined.", "", null);
                            return null;
                        }
                        break;

                    case "password_upper_case":
                        Boolean password_upper_case = true;

                        try
                        {
                            password_upper_case = Boolean.Parse(parameters["password_upper_case"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter password_upper_case is not a boolean.", "", null);
                            return null;
                        }

                        par.Add("@password_upper_case", typeof(Boolean)).Value = password_upper_case;
                        if (updateFields != "") updateFields += ", ";
                        updateFields += "pwd_upper_case = @password_upper_case";
                        update = true;
                        break;


                    case "password_lower_case":
                        Boolean password_lower_case = true;

                        try
                        {
                            password_lower_case = Boolean.Parse(parameters["password_lower_case"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter password_lower_case is not a boolean.", "", null);
                            return null;
                        }

                        par.Add("@password_lower_case", typeof(Boolean)).Value = password_lower_case;
                        if (updateFields != "") updateFields += ", ";
                        updateFields += "pwd_lower_case = @password_lower_case";
                        update = true;
                        break;


                    case "password_digit":
                        Boolean password_digit = true;

                        try
                        {
                            password_digit = Boolean.Parse(parameters["password_digit"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter password_digit is not a boolean.", "", null);
                            return null;
                        }

                        par.Add("@password_digit", typeof(Boolean)).Value = password_digit;
                        if (updateFields != "") updateFields += ", ";
                        updateFields += "pwd_digit = @password_digit";
                        update = true;
                        break;


                    case "password_symbol":
                        Boolean password_symbol = true;

                        try
                        {
                            password_symbol = Boolean.Parse(parameters["password_symbol"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter password_symbol is not a boolean.", "", null);
                            return null;
                        }

                        par.Add("@password_symbol", typeof(Boolean)).Value = password_symbol;
                        if (updateFields != "") updateFields += ", ";
                        updateFields += "pwd_symbol = @password_symbol";
                        update = true;
                        break;


                    case "password_no_name":
                        Boolean password_no_name = true;

                        try
                        {
                            password_no_name = Boolean.Parse(parameters["password_no_name"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter password_no_name is not a boolean.", "", null);
                            return null;
                        }

                        par.Add("@password_no_name", typeof(Boolean)).Value = password_no_name;
                        if (updateFields != "") updateFields += ", ";
                        updateFields += "pwd_no_name = @password_no_name";
                        update = true;
                        break;

                }
            }

            if (update)
            {
                updateSQL += updateFields + " where id = @context_id";
                database.ExecuteNonQuery( updateSQL, CommandType.Text, par);
            }

            //Atualiza a busca com os dados atualizados
            dtUsers = database.ExecuteDataTable( "select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) where e.context_id = c.id) from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
            
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
        private Boolean changeloginrules(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return false;
            }

            if (!parameters.ContainsKey("rules"))
            {
                Error(ErrorType.InvalidRequest, "Parameter rules is not defined.", "", null);
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

            
            if (!(parameters["rules"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter rules is not valid.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable("select c.* from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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

            List<Object> rules = new List<Object>();
            rules.AddRange(((ArrayList)parameters["rules"]).ToArray());

            if (rules.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Rule list is empty.", "", null);
                return false;
            }

            SqlTransaction trans = (SqlTransaction)database.BeginTransaction();

            try
            {
                List<String> insertedPar = new List<string>();


                for (Int32 i = 0; i < rules.Count; i++)
                {
                    if (!(rules[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Rule " + i + " is not valid", "", null);
                        return false;
                    }


                    Dictionary<String, Object> r = (Dictionary<String, Object>)rules[i];

                    if (!r.ContainsKey("name"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter name is not defined in rule " + i, "", null);
                        return false;
                    }

                    if (!r.ContainsKey("rule"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter rule is not defined in rule " + i, "", null);
                        return false;
                    }

                    if (!r.ContainsKey("order"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter order is not defined in rule " + i, "", null);
                        return false;
                    }


                    Int32 order = 0;
                    if ((r["order"] != null) && (!String.IsNullOrWhiteSpace(r["order"].ToString())))
                    {
                        try
                        {
                            order = Int32.Parse(r["order"].ToString());
                        }
                        catch
                        {
                            Error(ErrorType.InvalidRequest, "Parameter order is not a integer on rule " + i, "", null);
                            return false;
                        }
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Parameter order is empty on rule " + i, "", null);
                        return false;
                    }

                    String name = r["name"].ToString();
                    String rule = r["rule"].ToString();

                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@context_id", typeof(Int64)).Value = contextid;
                    par2.Add("@name", typeof(String)).Value = name;
                    par2.Add("@rule", typeof(String)).Value = rule;
                    par2.Add("@order", typeof(Int32)).Value = order;

                    database.ExecuteNonQuery("insert into login_rule (context_id, [name], [rule], [order]) select @context_id, @name, @rule, @order where not exists (select 1 from login_rule where context_id = @context_id and [rule] = @rule)", CommandType.Text, par2, trans);
                    database.ExecuteNonQuery("update login_rule set [name] = @name, [order] = @order where context_id = @context_id and [rule] = @rule", CommandType.Text, par2, trans);

                    insertedPar.Add(rule);
                }

                if (insertedPar.Count > 0)
                {

                    database.ExecuteNonQuery("delete from login_rule where context_id = @context_id and [rule] not in ('" + String.Join("','", insertedPar) + "')", CommandType.Text, par, trans);
                }
                else
                {
                    database.ExecuteNonQuery("delete from login_rule where context_id = @context_id", CommandType.Text, par, trans);
                }

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update login rules", ex.Message, null);
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
        private Boolean changemailrules(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            if (!parameters.ContainsKey("contextid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter contextid is not defined.", "", null);
                return false;
            }

            if (!parameters.ContainsKey("rules"))
            {
                Error(ErrorType.InvalidRequest, "Parameter rules is not defined.", "", null);
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


            if (!(parameters["rules"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter rules is not valid.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable("select c.* from context c with(nolock) where c.enterprise_id = @enterprise_id and c.id = @context_id order by c.name", CommandType.Text, par, null);
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

            List<Object> rules = new List<Object>();
            rules.AddRange(((ArrayList)parameters["rules"]).ToArray());

            if (rules.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Rule list is empty.", "", null);
                return false;
            }

            SqlTransaction trans = (SqlTransaction)database.BeginTransaction();

            try
            {
                List<String> insertedPar = new List<string>();


                for (Int32 i = 0; i < rules.Count; i++)
                {
                    if (!(rules[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Rule " + i + " is not valid", "", null);
                        return false;
                    }


                    Dictionary<String, Object> r = (Dictionary<String, Object>)rules[i];

                    if (!r.ContainsKey("name"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter name is not defined in rule " + i, "", null);
                        return false;
                    }

                    if (!r.ContainsKey("rule"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter rule is not defined in rule " + i, "", null);
                        return false;
                    }

                    if (!r.ContainsKey("order"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter order is not defined in rule " + i, "", null);
                        return false;
                    }


                    Int32 order = 0;
                    if ((r["order"] != null) && (!String.IsNullOrWhiteSpace(r["order"].ToString())))
                    {
                        try
                        {
                            order = Int32.Parse(r["order"].ToString());
                        }
                        catch
                        {
                            Error(ErrorType.InvalidRequest, "Parameter order is not a integer on rule " + i, "", null);
                            return false;
                        }
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Parameter order is empty on rule " + i, "", null);
                        return false;
                    }

                    String name = r["name"].ToString();
                    String rule = r["rule"].ToString();

                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@context_id", typeof(Int64)).Value = contextid;
                    par2.Add("@name", typeof(String)).Value = name;
                    par2.Add("@rule", typeof(String)).Value = rule;
                    par2.Add("@order", typeof(Int32)).Value = order;

                    database.ExecuteNonQuery("insert into st_mail_rule (context_id, [name], [rule], [order]) select @context_id, @name, @rule, @order where not exists (select 1 from login_rule where context_id = @context_id and [rule] = @rule)", CommandType.Text, par2, trans);
                    database.ExecuteNonQuery("update st_mail_rule set [name] = @name, [order] = @order where context_id = @context_id and [rule] = @rule", CommandType.Text, par2, trans);

                    insertedPar.Add(rule);
                }

                if (insertedPar.Count > 0)
                {

                    database.ExecuteNonQuery("delete from st_mail_rule where context_id = @context_id and [rule] not in ('" + String.Join("','", insertedPar) + "')", CommandType.Text, par, trans);
                }
                else
                {
                    database.ExecuteNonQuery("delete from st_mail_rule where context_id = @context_id", CommandType.Text, par, trans);
                }

                //Depois de gravar tudo, se tiver OK commita na base
                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on update login rules", ex.Message, null);
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
