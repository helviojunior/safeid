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
    internal class SystemRole : APIBase
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
                    return newrole(sqlConnection, parameters);
                    break;

                case "get":
                    return get(sqlConnection, parameters);
                    break;

                case "list":
                case "search":
                    return list(sqlConnection, parameters);
                    break;

                case "permissions":
                    return permissions(sqlConnection, parameters);
                    break;

                case "permissionstree":
                    return permissionstree(sqlConnection, parameters);
                    break;

                case "users":
                    return users(sqlConnection, parameters);
                    break;

                case "change":
                    return change(sqlConnection, parameters);
                    break;

                case "changepermissions":
                    return changepermissions(sqlConnection, parameters);
                    break;

                case "delete":
                    return delete(sqlConnection, parameters);
                    break;

                case "deleteallusers":
                    return deleteallusers(sqlConnection, parameters);
                    break;

                case "deleteuser":
                    return deleteuser(sqlConnection, parameters);
                    break;

                case "adduser":
                    return adduser(sqlConnection, parameters);
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
        private Dictionary<String, Object> newrole(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            par.Add("@name", typeof(String), name.Length).Value = name;
            par.Add("@parent_id", typeof(Int64)).Value = parentid;
            par.Add("@system_admin", typeof(Boolean)).Value = false;
            par.Add("@enterprise_admin", typeof(Int64)).Value = (parameters.ContainsKey("enterprise_admin") && (parameters["enterprise_admin"] is Boolean) && (Boolean)parameters["enterprise_admin"]);

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "sp_new_sys_role", CommandType.StoredProcedure, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return null;
            }

            parameters.Add("roleid", dtUsers.Rows[0]["id"]);

            return get(sqlConnection, parameters);
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> get(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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


            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select r.*, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id) from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return null;
            }


            DataRow dr1 = dtUsers.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("role_id", dr1["id"]);
            newItem.Add("parent_id", dr1["parent_id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("entity_qty", dr1["entity_qty"]);
            newItem.Add("enterprise_admin", dr1["ea"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            if (parameters.ContainsKey("permissions") && (parameters["permissions"] is Boolean) && (((Boolean)parameters["permissions"])))
            {
                Dictionary<String, Object> per = new Dictionary<string, object>();
                per.Add("roleid", dr1["id"]);
                newItem.Add("permissions", permissions(sqlConnection, per));
            }
                    
            result.Add("info", newItem);


            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean delete(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtSysRole = ExecuteDataTable(sqlConnection, "select *, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id), last_admin = case when r.ea = 1 and not exists (select 1 from sys_role r1 where r1.enterprise_id = r.enterprise_id and r1.ea = 1 and r1.id <> r.id) then cast(1 as bit) else cast(0 as bit) end, last_sa = case when r.sa = 1 and not exists (select 1 from sys_role r1 where r1.enterprise_id = r.enterprise_id and r1.sa = 1 and r1.id <> r.id) then cast(1 as bit) else cast(0 as bit) end from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
            if (dtSysRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtSysRole.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtSysRole.Rows[0]["entity_qty"] > 0)
            {
                Error(ErrorType.SystemError, "System role is being used and can not be deleted.", "", null);
                return false;
            }

            if ((Boolean)dtSysRole.Rows[0]["last_admin"])
            {
                Error(ErrorType.SystemError, "System role is the last role with enterprise admin permission, can not be deleted.", "", null);
                return false;
            }

            if ((Boolean)dtSysRole.Rows[0]["last_sa"])
            {
                Error(ErrorType.SystemError, "System role is the last role with system admin permission, can not be deleted.", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "delete from sys_role where id = @role_id", CommandType.Text, par);
            AddUserLog(sqlConnection, LogKey.SystemRole_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "System role " + dtSysRole.Rows[0]["name"] + " deleted", "");
                        
            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteallusers(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtSysRole = ExecuteDataTable(sqlConnection, "select *, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id), last_admin = case when r.ea = 1 and not exists (select 1 from sys_role r1 where r1.enterprise_id = r.enterprise_id and r1.ea = 1 and r1.id <> r.id) then cast(1 as bit) else cast(0 as bit) end from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id and r.sa = 0", CommandType.Text, par, null);
            if (dtSysRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtSysRole.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return false;
            }

            if ((Boolean)dtSysRole.Rows[0]["last_admin"])
            {
                Error(ErrorType.SystemError, "System role is the last role with enterprise admin permission, can not be delete all users.", "", null);
                return false;
            }

            DataTable dtSysRoleUsers = ExecuteDataTable(sqlConnection, "select e.id entity_id, r.* from entity e with(nolock) inner join sys_entity_role er on e.id = er.entity_id inner join sys_role r on r.id = er.role_id WHERE r.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
            if (dtSysRoleUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "delete from sys_entity_role where role_id = @role_id", CommandType.Text, par);

            foreach (DataRow dr in dtSysRoleUsers.Rows)
                if (dr["entity_id"] != DBNull.Value)
                {
                    AddUserLog(sqlConnection, LogKey.User_SystemRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], 0, "Entity unbind to system role " + dtSysRole.Rows[0]["name"], ((Boolean)dtSysRole.Rows[0]["ea"] ? "Enterprise admin" : ""));
                }

            
            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteuser(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtSysRole = ExecuteDataTable(sqlConnection, "select *, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id), last_admin = case when r.ea = 1 and not exists (select 1 from sys_role r1 where r1.enterprise_id = r.enterprise_id and r1.ea = 1 and r1.id <> r.id) then cast(1 as bit) else cast(0 as bit) end from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id and r.sa = 0", CommandType.Text, par, null);
            if (dtSysRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtSysRole.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return false;
            }

            DataTable dtSysRoleUsers = ExecuteDataTable(sqlConnection, "select e.id entity_id, r.* from entity e with(nolock) inner join sys_entity_role er on e.id = er.entity_id inner join sys_role r on r.id = er.role_id WHERE r.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
            if (dtSysRoleUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtSysRoleUsers.Rows.Count > 0)
            {

                if ((Boolean)dtSysRole.Rows[0]["last_admin"] && ((Int32)dtSysRole.Rows[0]["entity_id"] == 1))
                {
                    Error(ErrorType.SystemError, "Entity " + dtSysRoleUsers.Rows[0]["full_name"] + " is a last user on a system role and this role is the last role with enterprise admin permission, can not be delete this user.", "", null);
                    return false;
                }


                ExecuteNonQuery(sqlConnection, "delete from sys_entity_role where role_id = @role_id and entity_id = @entity_id", CommandType.Text, par);

                foreach (DataRow dr in dtSysRoleUsers.Rows)
                    if (dr["entity_id"] != DBNull.Value)
                    {
                        AddUserLog(sqlConnection, LogKey.User_SystemRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], 0, "Entity unbind to system role " + dtSysRole.Rows[0]["name"], ((Boolean)dtSysRole.Rows[0]["ea"] ? "Enterprise admin" : ""));
                    }
            }

            return true;
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean adduser(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtSysRole = ExecuteDataTable(sqlConnection, "select *, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id), last_admin = case when r.ea = 1 and not exists (select 1 from sys_role r1 where r1.enterprise_id = r.enterprise_id and r1.ea = 1 and r1.id <> r.id) then cast(1 as bit) else cast(0 as bit) end from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id and r.sa = 0", CommandType.Text, par, null);
            if (dtSysRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtSysRole.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return false;
            }

            foreach (Int64 u in users)
            {
                DbParameterCollection par2 = new DbParameterCollection();
                par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                par2.Add("@role_id", typeof(Int64)).Value = roleid;
                par2.Add("@entity_id", typeof(Int64)).Value = u;

                DataTable dtRet = ExecuteDataTable(sqlConnection, "insert into sys_entity_role (entity_id, role_id) select @entity_id, @role_id WHERE not exists (select 1 from sys_entity_role where entity_id = @entity_id and role_id = @role_id)", CommandType.Text, par2);

                AddUserLog(sqlConnection, LogKey.User_SystemRoleBind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, u, 0, "Entity bind to system role " + dtSysRole.Rows[0]["name"].ToString(), ((Boolean)dtSysRole.Rows[0]["ea"] ? "Enterprise admin" : ""));
            }
            
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

            DataTable dtSysRole = ExecuteDataTable(sqlConnection, "select r.*, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id) from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);
            if (dtSysRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtSysRole.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return null;
            }


            List<String> log = new List<String>();

            String updateSQL = "";
            Boolean update = false;
            if (parameters["name"] != null)
            {
                String name = parameters["name"].ToString();
                if (!String.IsNullOrWhiteSpace(name))
                {
                    par.Add("@name", typeof(String), name.Length).Value = name;
                    if (updateSQL != "") updateSQL += ", ";
                    updateSQL += " name = @name";
                    update = true;

                    log.Add("Name changed from '" + dtSysRole.Rows[0]["name"] + "' to '" + name + "'");
                }
            }

            if ((parameters["enterprise_admin"] != null) && (parameters["enterprise_admin"] is Boolean))
            {

                par.Add("@enterprise_admin", typeof(Boolean)).Value = (Boolean)parameters["enterprise_admin"];
                if (updateSQL != "") updateSQL += ", ";
                updateSQL += " ea = @enterprise_admin";
                update = true;

                log.Add("Enterprise admin changed from '" + (Boolean)dtSysRole.Rows[0]["ea"] + "' to '" + (Boolean)parameters["enterprise_admin"] + "'");

            }

            if (update)
            {
                updateSQL ="update sys_role set " + updateSQL +  " where id = @role_id";
                ExecuteNonQuery(sqlConnection, updateSQL, CommandType.Text, par);
                AddUserLog(sqlConnection, LogKey.SystemRole_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "System role changed", String.Join("\r\n", log));
            }
            
            return get(sqlConnection, parameters);

        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> changepermissions(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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


            if (!parameters.ContainsKey("permissions"))
            {
                Error(ErrorType.InvalidRequest, "Parameter permissions is not defined.", "", null);
                return null;
            }

            if (!(parameters["permissions"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter permissions is invalid.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@role_id", typeof(Int64)).Value = roleid;

            DataTable dtSysRole = ExecuteDataTable(sqlConnection, "select r.*, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id) from sys_role r WHERE r.enterprise_id = @enterprise_id and r.id = @role_id order by r.name", CommandType.Text, par, null);
            if (dtSysRole == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtSysRole.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "System role not found.", "", null);
                return null;
            }

            SqlTransaction trans = sqlConnection.BeginTransaction();
            try
            {

                List<String> log = new List<String>();

                List<String> perList = new List<String>();
                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["permissions"]).ToArray());

                foreach (String p in lst)
                {
                    
                    try
                    {
                        Int64 permissionid = Int64.Parse(p);

                        //Verifica se a permissão existe
                        DataTable dtP = ExecuteDataTable(sqlConnection, "select p.*, m.name module_name, sm.name submodule_name, sm.[api_module] + '.' + p.[key] api_key from sys_permission p inner join sys_sub_module sm on sm.id = p.submodule_id inner join sys_module m on m.id = sm.module_id WHERE p.id = " + p, CommandType.Text, null, trans);
                        if ((dtP == null) || (dtP.Rows.Count == 0))
                        {
                            Error(ErrorType.InvalidRequest, "Permission '" + p + "' not found.", "", null);
                            return null;
                        }

                        ExecuteNonQuery(sqlConnection, "insert into sys_role_permission (role_id, permission_id) select @role_id, " + dtP.Rows[0]["id"] + " WHERE not exists(select 1 from sys_role_permission where role_id = @role_id and permission_id = " + dtP.Rows[0]["id"] + ")", CommandType.Text,par,  trans);

                        perList.Add(dtP.Rows[0]["id"].ToString());
                        log.Add("Permission linked: " + dtP.Rows[0]["module_name"] + " => " + dtP.Rows[0]["api_key"]);
                    }
                    catch
                    {
                        Error(ErrorType.InvalidRequest, "Permission '"+ p + "' is not a long integer.", "", null);
                        return null;
                    }


                }

                //Exclui todas as outras não listadas
                ExecuteNonQuery(sqlConnection, "delete from sys_role_permission WHERE role_id = @role_id and permission_id not in ("+ String.Join(",",perList) +")", CommandType.Text,par,  trans);
                AddUserLog(sqlConnection, LogKey.SystemRolePermission_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "System role permissions changed", String.Join("\r\n", log), trans);

                trans.Commit();
                trans = null;
            }
            finally
            {
                if (trans != null)
                    trans.Rollback();
            }

            Dictionary<String, Object> parR = new Dictionary<string, object>();
            parR.Add("roleid", roleid);
            parR.Add("permissions", true);

            return get(sqlConnection, parR);

        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Object> list(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
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
            sql += "    ROW_NUMBER() OVER (ORDER BY r.name) AS [row_number], r.*, entity_qty = (select COUNT(distinct e.id) from sys_entity_role er inner join entity e with(nolock) on e.id = er.entity_id where er.role_id = r.id) ";
            sql += "     from sys_role r  ";
            sql += "     where r.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and r.name like '%'+@text+'%'");
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
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("role_id", dr1["id"]);
                    newItem.Add("parent_id", dr1["parent_id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("entity_qty", dr1["entity_qty"]);
                    newItem.Add("enterprise_admin", dr1["ea"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    if (parameters.ContainsKey("permissions") && (parameters["permissions"] is Boolean) && (((Boolean)parameters["permissions"])))
                    {
                        Dictionary<String, Object> per = new Dictionary<string,object>();
                        per.Add("roleid", dr1["id"]);
                        newItem.Add("permissions", permissions(sqlConnection, per));
                    }
                    

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
        private List<Dictionary<String, Object>> users(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            sql += "    from entity e with(nolock) inner join sys_entity_role er on e.id = er.entity_id inner join sys_role r on r.id = er.role_id";
            sql += "  WHERE ";
            sql += " (" + (deleted ? "" : "e.deleted = 0 and") + " r.enterprise_id = @enterprise_id and r.id = @role_id)";
            sql += " ) SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            //DataTable dtUsers = ExecuteDataTable(sqlConnection, "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where " + (deleted ? "" : "e.deleted = 0 and") + " r.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and e.full_name like '%'+@text+'%' or e.login like '%'+@text+'%' ") + " order by e.full_name", CommandType.Text, par, null);
            DataTable dtUsers = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User list is empty.", "", null);
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
        /// Método privado para processamento do método 'user.search'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> permissions(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            
            String sql = "";
            sql += "  SELECT";
            sql += "     p.*, m.name module_name, sm.name sub_module_name";
            sql += "    from sys_permission p inner join sys_role_permission rp on p.id = rp.permission_id";
            sql += "    inner join sys_role r on r.id = rp.role_id";
            sql += "    inner join sys_sub_module sm on sm.id = p.submodule_id";
            sql += "    inner join sys_module m on m.id = sm.module_id";
            sql += "  WHERE ";
            sql += "  r.enterprise_id = @enterprise_id and r.id = @role_id";

            DataTable dtRolePermission = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
            if (dtRolePermission == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr in dtRolePermission.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();
                newItem.Add("permission_id", dr["id"]);
                newItem.Add("name", dr["name"]);
                newItem.Add("key", dr["key"]);
                newItem.Add("module_name", dr["module_name"]);
                newItem.Add("sub_module_name", dr["sub_module_name"]);

                result.Add(newItem);
            }

            return result;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.search'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> permissionstree(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();
            
            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;

            DataTable dtModules = ExecuteDataTable(sqlConnection, "select * from sys_module m order by name", CommandType.Text, null, null);
            if (dtModules == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            foreach (DataRow dr in dtModules.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();
                List<Dictionary<String, Object>> submodules = new List<Dictionary<string, object>>();
                newItem.Add("module_id", dr["id"]);
                newItem.Add("name", dr["name"]);
                newItem.Add("key", dr["key"]);

                DataTable dtSubModules = ExecuteDataTable(sqlConnection, "select * from sys_sub_module m where m.module_id = " + dr["id"] + " order by m.name", CommandType.Text, null, null);
                if ((dtSubModules != null) && (dtSubModules.Rows.Count > 0))
                    foreach (DataRow drS in dtSubModules.Rows)
                    {
                        Dictionary<String, Object> s = new Dictionary<string, object>();
                        List<Dictionary<String, Object>> permissions = new List<Dictionary<string, object>>();

                        s.Add("submodule_id", drS["id"]);
                        s.Add("name", drS["name"]);
                        s.Add("key", drS["key"]);
                        s.Add("api_module", drS["api_module"]);

                        DataTable dtPermissions = ExecuteDataTable(sqlConnection, "select * from sys_permission p where p.submodule_id = " + drS["id"] + " order by p.name", CommandType.Text, null, null);
                        if ((dtPermissions != null) && (dtPermissions.Rows.Count > 0))
                            foreach (DataRow drP in dtPermissions.Rows)
                            {
                                Dictionary<String, Object> np = new Dictionary<string, object>();

                                np.Add("permission_id", drP["id"]);
                                np.Add("name", drP["name"]);
                                np.Add("key", drP["key"]);

                                permissions.Add(np);
                            }

                        s.Add("permissions", permissions);
                        submodules.Add(s);
                    }

                newItem.Add("submodules", submodules);

                result.Add(newItem);
            }

            return result;

        }
        
    }
}
