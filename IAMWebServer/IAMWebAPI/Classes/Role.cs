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
    internal class Role : IAMAPIBase
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
                    return newrole(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "list":
                case "search":
                    return list(database, parameters);
                    break;

                case "users":
                    return users(database, parameters);
                    break;

                case "change":
                    return change(database, parameters);
                    break;

                case "delete":
                    return delete(database, parameters);
                    break;

                case "deleteallusers":
                    return deleteallusers(database, parameters);
                    break;

                case "deleteuser":
                    return deleteuser(database, parameters);
                    break;

                case "adduser":
                    return adduser(database, parameters);
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
        private Dictionary<String, Object> newrole(IAMDatabase database, Dictionary<String, Object> parameters)
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

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return null;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return null;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_id", typeof(Int64)).Value = roleid;


            DataTable dtUsers = database.ExecuteDataTable( "select r.*, c.enterprise_id, entity_qty = (select COUNT(distinct i.entity_id) from identity_role ir inner join [identity] i with(nolock) on ir.identity_id = i.id where ir.role_id = r.id) from role r inner join context c with(nolock) on c.id = r.context_id where c.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);
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
        private Boolean delete(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_id", typeof(Int64)).Value = roleid;

            DataTable dtUsers = database.ExecuteDataTable( "select c.enterprise_id, r.name as role_name, ir.*, i.entity_id from role r inner join context c with(nolock) on c.id = r.context_id left join identity_role ir on r.id = ir.role_id left join [identity] i with(nolock) on ir.identity_id = i.id where c.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Role not found.", "", null);
                return false;
            }


            database.ExecuteNonQuery( "delete from role where id = @role_id", CommandType.Text, par);
            database.AddUserLog( LogKey.Role_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Role " + dtUsers.Rows[0]["role_name"] + " deleted", "");


            foreach (DataRow dr in dtUsers.Rows)
                if ((dr["identity_id"] != DBNull.Value) && (dr["entity_id"] != DBNull.Value))
                {
                    database.AddUserLog( LogKey.User_IdentityRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], (Int64)dr["identity_id"], "Identity unbind to role " + dr["role_name"], "");
                    database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + dr["entity_id"] + ")", CommandType.Text,null,  null);
                }

            
            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteallusers(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_id", typeof(Int64)).Value = roleid;



            DataTable dtUsers = database.ExecuteDataTable( "select c.enterprise_id, r.name as role_name, ir.*, i.entity_id from role r inner join context c with(nolock) on c.id = r.context_id left join identity_role ir on r.id = ir.role_id left join [identity] i with(nolock) on ir.identity_id = i.id where c.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Role not found.", "", null);
                return false;
            }


            database.ExecuteNonQuery( "delete from identity_role where role_id = @role_id", CommandType.Text, par);
            
            foreach (DataRow dr in dtUsers.Rows)
                if ((dr["identity_id"] != DBNull.Value) && (dr["entity_id"] != DBNull.Value))
                {
                    database.AddUserLog( LogKey.User_IdentityRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], (Int64)dr["identity_id"], "Identity unbind to role " + dr["role_name"], "");
                    database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + dr["entity_id"] + ")",CommandType.Text, null,  null);
                }

            
            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteuser(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
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
            par.Add("@role_id", typeof(Int64)).Value = roleid;
            par.Add("@entity_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable( "select c.enterprise_id, r.name as role_name, ir.*, i.entity_id from role r inner join context c with(nolock) on c.id = r.context_id left join identity_role ir on r.id = ir.role_id left join [identity] i with(nolock) on ir.identity_id = i.id and i.entity_id = @entity_id where c.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Role not found.", "", null);
                return false;
            }

            foreach (DataRow dr in dtUsers.Rows)
                if ((dr["identity_id"] != DBNull.Value) && (dr["entity_id"] != DBNull.Value))
                {
                    database.AddUserLog( LogKey.User_IdentityRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], (Int64)dr["identity_id"], "Identity unbind to role " + dr["role_name"], "");
                    database.ExecuteNonQuery( "delete from identity_role where role_id = @role_id and identity_id = " + dr["identity_id"], CommandType.Text, par);
                    database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + dr["entity_id"] + ")", CommandType.Text,null,  null);
                }


            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean adduser(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return false;
            }

            String userid = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(userid))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return false;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
                return false;
            }

            List<Int64> users = new List<Int64>();
            String[] t = userid.Split(",".ToCharArray());
            foreach(String u in t)
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
            par.Add("@role_id", typeof(Int64)).Value = roleid;

            DataTable dtUsers = database.ExecuteDataTable( "select r.*, c.enterprise_id, entity_qty = (select COUNT(distinct i.entity_id) from identity_role ir inner join [identity] i with(nolock) on ir.identity_id = i.id where ir.role_id = r.id) from role r inner join context c with(nolock) on c.id = r.context_id where c.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Role not found.", "", null);
                return false;
            }

            foreach (Int64 u in users)
            {
                DbParameterCollection par2 = new DbParameterCollection();
                par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                par2.Add("@role_id", typeof(Int64)).Value = roleid;
                par2.Add("@entity_id", typeof(Int64)).Value = u;
                
                DataTable dtRet = database.ExecuteDataTable( "sp_insert_entity_to_role", CommandType.StoredProcedure, par2);

                if ((dtRet != null) && (dtRet.Rows.Count > 0))
                {
                    database.AddUserLog( LogKey.User_IdentityRoleBind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, u, (Int64)dtRet.Rows[0]["identity_id"], "Identity bind to role " + dtRet.Rows[0]["role_name"].ToString(), "");
                }
            }
            
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

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return null;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return null;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_id", typeof(Int64)).Value = roleid;

            DataTable dtUsers = database.ExecuteDataTable( "select r.*, c.enterprise_id, entity_qty = (select COUNT(distinct i.entity_id) from identity_role ir inner join [identity] i with(nolock) on ir.identity_id = i.id where ir.role_id = r.id) from role r inner join context c with(nolock) on c.id = r.context_id where c.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);
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

            List<String> log = new List<String>();

            String updateSQL = "update role set ";
            Boolean update = false;
            if (parameters["name"] != null)
            {
                String name = parameters["name"].ToString();
                if (!String.IsNullOrWhiteSpace(name))
                {
                    par.Add("@name", typeof(String)).Value = name;
                    updateSQL += "name = @name";
                    update = true;

                    log.Add("Name changed from '" + dtUsers.Rows[0]["name"] + "' to '" + name + "'");
                }

            }

            if (update)
            {
                updateSQL += " where id = @role_id";
                database.ExecuteNonQuery( updateSQL, CommandType.Text, par);
                database.AddUserLog( LogKey.Role_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Role changed", String.Join("\r\n", log));
            }

            //Atualiza a busca com os dados atualizados
            dtUsers = database.ExecuteDataTable( "select r.*, c.enterprise_id, entity_qty = (select COUNT(distinct i.entity_id) from identity_role ir inner join [identity] i with(nolock) on ir.identity_id = i.id where ir.role_id = r.id) from role r inner join context c with(nolock) on c.id = r.context_id where c.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);

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
            sql += "    ROW_NUMBER() OVER (ORDER BY r.name) AS [row_number], r.*, c.enterprise_id, entity_qty = (select COUNT(distinct i.entity_id) from identity_role ir inner join [identity] i with(nolock) on ir.identity_id = i.id where ir.role_id = r.id) ";
            sql += "     from role r inner join context c with(nolock) on c.id = r.context_id  ";
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
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("role_id", dr1["id"]);
                    newItem.Add("parent_id", dr1["parent_id"]);
                    newItem.Add("context_id", dr1["context_id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("entity_qty", dr1["entity_qty"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

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
        private List<Dictionary<String, Object>> users(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("roleid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return null;
            }


            String role = parameters["roleid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not defined.", "", null);
                return null;
            }

            Int64 roleid = 0;
            try
            {
                roleid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter roleid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_id", typeof(Int64)).Value = roleid;

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
            sql += "    from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id";
            sql += "    inner join [identity] i with(nolock) on i.entity_id = e.id";
            sql += "    inner join identity_role ir on ir.identity_id = i.id";
            sql += "    inner join role r on ir.role_id = r.id";
            sql += "  WHERE ";
            sql += " (" + (deleted ? "" : "e.deleted = 0 and") + " c.enterprise_id = @enterprise_id and r.id = @role_id)";
            sql += " ) SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            //DataTable dtUsers = database.ExecuteDataTable( "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where " + (deleted ? "" : "e.deleted = 0 and") + " c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and e.full_name like '%'+@text+'%' or e.login like '%'+@text+'%' ") + " order by e.full_name", CommandType.Text, par, null);
            DataTable dtUsers = database.ExecuteDataTable( sql, CommandType.Text, par, null);
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
        

    }
}
