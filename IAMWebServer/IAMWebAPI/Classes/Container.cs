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

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Container : IAMAPIBase
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
                    return newcontainer(database, parameters);
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

                case "deleteallusers":
                    return deleteallusers(database, parameters);
                    break;

                case "deleteuser-fsdfdlks":
                    //return deleteuser(database, parameters);
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
        private Dictionary<String, Object> newcontainer(IAMDatabase database, Dictionary<String, Object> parameters)
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

            if (parentid > 0)
            {
                DataTable dtPar = database.ExecuteDataTable("select * from [container] c with(nolock) where c.id = " + parentid + " and c.context_id = " + contextid);
                if ((dtPar == null) || (dtPar.Rows.Count == 0))
                {
                    Error(ErrorType.InvalidRequest, "Parent container is not a chield of this context", "", null);
                    return null;
                }
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@container_name", typeof(String)).Value = name;
            par.Add("@parent_id", typeof(Int64)).Value = parentid;
            par.Add("@context_id", typeof(Int64)).Value = contextid;

            DataTable dtUsers = database.ExecuteDataTable("sp_new_container", CommandType.StoredProcedure, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                return null;
            }


            //Atualiza a busca com os dados atualizados
            Dictionary<String, Object> par2 = new Dictionary<string, object>();
            par2.Add("containerid", dtUsers.Rows[0]["id"]);
            return get(database, par2);

        }

        public static String getPath(IAMDatabase database, Int64 enterprise_id, Int64 container_id)
        {
            return getPath(database, enterprise_id, container_id, false);
        }

        public static String getPath(IAMDatabase database, Int64 enterprise_id, Int64 container_id, Boolean show_atual)
        {
            List<String> path = new List<string>();

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = enterprise_id;

            DataTable dtContainers = database.ExecuteDataTable("select c.*, c1.enterprise_id, c1.name context_name, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id) from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id where c1.enterprise_id = @enterprise_id order by c.name", CommandType.Text, par, null);
            if ((dtContainers != null) && (dtContainers.Rows.Count > 0))
            {

                Func<Int64, Boolean> chields = null;
                chields = new Func<Int64, Boolean>(delegate(Int64 root)
                {
                    
                    foreach (DataRow dr in dtContainers.Rows)
                        if (((Int64)dr["id"] == root))
                        {
                            if ((Int64)dr["parent_id"] == root)
                                break;

                            path.Add(dr["name"].ToString());
                            chields((Int64)dr["parent_id"]);
                            break;
                        }

                    return true;
                });

                foreach (DataRow dr in dtContainers.Rows)
                    if (((Int64)dr["id"] == container_id))
                    {
                        if(show_atual) path.Add(dr["name"].ToString());
                        chields((Int64)dr["parent_id"]);
                    }
            }

            path.Reverse();
            return "\\" + String.Join("\\", path);
        }

        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> get(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return null;
            }


            String container = parameters["containerid"].ToString();
            if (String.IsNullOrWhiteSpace(container))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return null;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(container);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@container_id", typeof(Int64)).Value = containerid;


            DataTable dtUsers = database.ExecuteDataTable("select c.*, c1.enterprise_id, c1.name context_name, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id) from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id where c1.enterprise_id = @enterprise_id and c.id = @container_id order by c.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                return null;
            }


            DataRow dr1 = dtUsers.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("container_id", dr1["id"]);
            newItem.Add("context_id", dr1["context_id"]);
            newItem.Add("context_name", dr1["context_name"]);
            newItem.Add("parent_id", dr1["parent_id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("path", getPath(database, this._enterpriseId, (Int64)dr1["id"]));
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
        private Boolean delete(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }


            String container = parameters["containerid"].ToString();
            if (String.IsNullOrWhiteSpace(container))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(container);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@container_id", typeof(Int64)).Value = containerid;

            DataTable dtUsers = database.ExecuteDataTable("select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id), chield_qty = (select COUNT(distinct chield.id) from container chield with(nolock) where chield.parent_id = c.id) from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id  where c1.enterprise_id = @enterprise_id and c.id = @container_id order by c.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                return false;
            }

            if ((Int32)dtUsers.Rows[0]["entity_qty"] > 0)
            {
                Error(ErrorType.InvalidRequest, "Container is not empty.", "", null);
                return false;
            }


            if ((Int32)dtUsers.Rows[0]["chield_qty"] > 0)
            {
                Error(ErrorType.InvalidRequest, "Container has chield containers.", "", null);
                return false;
            }


            database.ExecuteNonQuery( "delete from container where id = @container_id", CommandType.Text, par);

            database.AddUserLog( LogKey.Context_Deleted, null, "API", UserLogLevel.Error, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Container " + dtUsers.Rows[0]["name"] + " deleted", "");

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

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return null;
            }

            String container = parameters["containerid"].ToString();
            if (String.IsNullOrWhiteSpace(container))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return null;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(container);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@container_id", typeof(Int64)).Value = containerid;

            DataTable dtUsers = database.ExecuteDataTable("select c.* from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id where c1.enterprise_id = @enterprise_id and c.id = @container_id order by c.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                return null;
            }


            String updateSQL = "update container set ";
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

                    case "parentid":
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

                        if (parentid > 0)
                        {
                            if (parentid == (Int64)dtUsers.Rows[0]["context_id"])
                            {
                                Error(ErrorType.InvalidRequest, "Parent container can not be this container", "", null);
                                return null;
                            }

                            DataTable dtPar = database.ExecuteDataTable("select * from [container] c with(nolock) where c.id = " + parentid + " and c.context_id = " + dtUsers.Rows[0]["context_id"]);
                            if ((dtPar == null) || (dtPar.Rows.Count == 0))
                            {
                                Error(ErrorType.InvalidRequest, "Parent container is not a chield of this context", "", null);
                                return null;
                            }
                        }

                        par.Add("@parent_id", typeof(Int64)).Value = parentid;
                        if (updateFields != "") updateFields += ", ";
                        updateFields += "parent_id = @parent_id";
                        update = true;

                        break;

                }
            }

            if (update)
            {
                updateSQL += updateFields + " where id = @container_id";
                database.ExecuteNonQuery( updateSQL, CommandType.Text, par);
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


            //select c.*, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id) from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id where c1.enterprise_id = @enterprise_id and c.id = @container_id order by c.name

            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT ";
            sql += "    ROW_NUMBER() OVER (ORDER BY c.name) AS [row_number], c.*, c1.enterprise_id, c1.name context_name, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id) ";
            sql += "     from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id  ";
            sql += "     where c1.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and c.name like '%'+@text+'%'");
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
                    newItem.Add("container_id", dr1["id"]);
                    newItem.Add("context_id", dr1["context_id"]);
                    newItem.Add("context_name", dr1["context_name"]);
                    newItem.Add("parent_id", dr1["parent_id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("path", getPath(database, this._enterpriseId, (Int64)dr1["id"]));
                    newItem.Add("entity_qty", (Int32)dr1["entity_qty"]);
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
        private Boolean adduser(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }


            String role = parameters["containerid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }

            String userid = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(userid))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return false;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
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
            par.Add("@container_id", typeof(Int64)).Value = containerid;

            DataTable dtUsers = database.ExecuteDataTable("select c.*, c1.enterprise_id, c1.name context_name, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id) from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id where c1.enterprise_id = @enterprise_id and c.id = @container_id order by c.name", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                return false;
            }

            try
            {
                SqlTransaction trans = (SqlTransaction)database.BeginTransaction();

                foreach (Int64 u in users)
                {
                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                    par2.Add("@container_id", typeof(Int64)).Value = containerid;
                    par2.Add("@entity_id", typeof(Int64)).Value = u;

                    //Select all old containers
                    DataTable drContainers = database.ExecuteDataTable("select c.* from entity_container e inner join container c on c.id = e.container_id where e.entity_id = @entity_id", CommandType.Text, par2, trans);
                    if ((drContainers != null) && (drContainers.Rows.Count > 0))
                    {
                        foreach (DataRow dr in drContainers.Rows)
                            if ((Int64)dr["id"] == containerid)
                                database.AddUserLog(LogKey.User_ContainerRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, u, 0, "Identity unbind to container " + dr["name"].ToString(), "", Acl.EntityId, trans);
                    }

                    DataTable dtRet = database.ExecuteDataTable("sp_insert_entity_to_container", CommandType.StoredProcedure, par2, trans);

                    if ((dtRet != null) && (dtRet.Rows.Count > 0))
                    {
                        database.AddUserLog(LogKey.User_ContainerRoleBind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, u, 0, "Identity bind to container " + dtRet.Rows[0]["name"].ToString(), "", Acl.EntityId, trans);
                        database.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + u + ")", CommandType.Text, null, trans);
                    }
                }
                database.Commit();
            }
            catch (Exception ex)
            {
                database.Rollback();

                Error(ErrorType.InvalidRequest, "Error on bind user to container", ex.Message, null);
                return false;
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

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }


            String role = parameters["containerid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@container_id", typeof(Int64)).Value = containerid;

            DataTable dtUsers = database.ExecuteDataTable("select c.*, e.entity_id from entity_container e inner join container c on c.id = e.container_id inner join context c1 on c.context_id = c1.id where c1.enterprise_id = @enterprise_id and  e.container_id = @container_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                return false;
            }

            database.ExecuteNonQuery("delete from entity_container where container_id = @container_id", CommandType.Text, par);

            foreach (DataRow dr in dtUsers.Rows)
                if (dr["entity_id"] != DBNull.Value)
                {
                    database.AddUserLog(LogKey.User_ContainerRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], 0, "Identity unbind to container " + dr["name"], "");
                    database.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + dr["entity_id"] + ")", CommandType.Text, null, null);
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

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }


            String role = parameters["containerid"].ToString();
            if (String.IsNullOrWhiteSpace(role))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(role);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
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
            par.Add("@role_id", typeof(Int64)).Value = containerid;
            par.Add("@entity_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable("select c.enterprise_id, r.name as role_name, ir.*, i.entity_id from role r inner join context c with(nolock) on c.id = r.context_id left join identity_role ir on r.id = ir.role_id left join [identity] i with(nolock) on ir.identity_id = i.id and i.entity_id = @entity_id where c.enterprise_id = @enterprise_id and r.id = @role_id", CommandType.Text, par, null);
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
                    database.AddUserLog(LogKey.User_IdentityRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)dr["entity_id"], (Int64)dr["identity_id"], "Identity unbind to role " + dr["role_name"], "");
                    database.ExecuteNonQuery("delete from identity_role where role_id = @role_id and identity_id = " + dr["identity_id"], CommandType.Text, par);
                    database.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + dr["entity_id"] + ")", CommandType.Text, null, null);
                }


            return true;
        }


    }
}
