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

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Field : APIBase
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
                    return newfield(sqlConnection, parameters);
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
        private Dictionary<String, Object> newfield(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            if (!parameters.ContainsKey("data_type"))
            {
                Error(ErrorType.InvalidRequest, "Parameter data_type is not defined.", "", null);
                return null;
            }

            String data_type = parameters["data_type"].ToString();
            if (String.IsNullOrWhiteSpace(data_type))
            {
                Error(ErrorType.InvalidRequest, "Parameter data_type is not defined.", "", null);
                return null;
            }

            Boolean public_field = false;
            if (parameters.ContainsKey("public_field"))
            {
                try
                {
                    public_field = Boolean.Parse(parameters["public_field"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter public_field is not a boolean.", "", null);
                    return null;
                }
            }

            Boolean user_field = false;
            if (parameters.ContainsKey("user_field"))
            {
                try
                {
                    user_field = Boolean.Parse(parameters["user_field"].ToString());
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Parameter user_field is not a boolean.", "", null);
                    return null;
                }
            }

            switch (data_type.ToLower())
            {
                case "string":
                case "datetime":
                case "numeric":
                    break;

                default:
                    Error(ErrorType.InvalidRequest, "Data type is not recognized.", "", null);
                    return null;
                    break;
            }


            DbParameterCollection par2 = new DbParameterCollection();
            par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par2.Add("@field_name", typeof(String), name.Length).Value = name;

            DataTable dtF1 = ExecuteDataTable(sqlConnection, "select * from field with(nolock) where enterprise_id = @enterprise_id and name = @field_name", CommandType.Text, par2, null);
            if ((dtF1 != null) && (dtF1.Rows.Count > 0))
            {
                Error(ErrorType.InvalidRequest, "Field with the same name already exists.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@data_type", typeof(String), data_type.Length).Value = data_type;
            par.Add("@field_name", typeof(String), name.Length).Value = name;
            par.Add("@public", typeof(Boolean)).Value = public_field;
            par.Add("@user", typeof(Boolean)).Value = user_field;

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "sp_new_field", CommandType.StoredProcedure, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Field not found.", "", null);
                return null;
            }

            DataRow dr1 = dtUsers.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("field_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("public_field", dr1["public"]);
            newItem.Add("user_field", dr1["user"]);

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

            if (!parameters.ContainsKey("fieldid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not defined.", "", null);
                return null;
            }


            String field = parameters["fieldid"].ToString();
            if (String.IsNullOrWhiteSpace(field))
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not defined.", "", null);
                return null;
            }

            Int64 fieldid = 0;
            try
            {
                fieldid = Int64.Parse(field);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@field_id", typeof(Int64)).Value = fieldid;

            DataTable dtResource = ExecuteDataTable(sqlConnection, "select * from field with(nolock) where enterprise_id = @enterprise_id and id = @field_id", CommandType.Text, par, null);
            if (dtResource == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtResource.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Field not found.", "", null);
                return null;
            }

            DataRow dr1 = dtResource.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("field_id", dr1["id"]);
            newItem.Add("data_type", dr1["data_type"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("public_field", dr1["public"]);
            newItem.Add("user_field", dr1["user"]);

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

            if (!parameters.ContainsKey("fieldid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not defined.", "", null);
                return false;
            }


            String field = parameters["fieldid"].ToString();
            if (String.IsNullOrWhiteSpace(field))
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not defined.", "", null);
                return false;
            }

            Int64 fieldid = 0;
            try
            {
                fieldid = Int64.Parse(field);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@field_id", typeof(Int64)).Value = fieldid;

            DataTable dtField = ExecuteDataTable(sqlConnection, "select f.*, qty = (select COUNT(*) from resource_plugin rp with(nolock) where name_field_id = f.id or mail_field_id = f.id or login_field_id = f.id) + (select COUNT(*) from resource_plugin_mapping rpm with(nolock) where rpm.field_id = f.id) from field f with(nolock) where f.enterprise_id = @enterprise_id and f.id = @field_id", CommandType.Text, par, null);
            if (dtField == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtField.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Field not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtField.Rows[0]["qty"] > 0)
            {
                Error(ErrorType.SystemError, "Field is being used and can not be deleted.", "", null);
                return false;
            }

            ExecuteNonQuery(sqlConnection, "delete from field where id = @field_id", CommandType.Text, par);
            AddUserLog(sqlConnection, LogKey.Field_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Field " + dtField.Rows[0]["name"] + " deleted", "");

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

            if (!parameters.ContainsKey("fieldid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not defined.", "", null);
                return null;
            }


            String field = parameters["fieldid"].ToString();
            if (String.IsNullOrWhiteSpace(field))
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not defined.", "", null);
                return null;
            }

            Int64 fieldid = 0;
            try
            {
                fieldid = Int64.Parse(field);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter fieldid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@field_id", typeof(Int64)).Value = fieldid;

            DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field with(nolock) where enterprise_id = @enterprise_id and id = @field_id", CommandType.Text, par, null);
            if (dtField == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtField.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Field not found.", "", null);
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
                        if ((!String.IsNullOrWhiteSpace(name)) && (name != (String)dtField.Rows[0]["name"]))
                        {

                            DbParameterCollection par2 = new DbParameterCollection();
                            par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                            par2.Add("@field_name", typeof(String), name.Length).Value = name;

                            DataTable dtF1 = ExecuteDataTable(sqlConnection, "select * from field with(nolock) where enterprise_id = @enterprise_id and name = @field_name", CommandType.Text, par2, null);
                            if ((dtF1 != null) && (dtF1.Rows.Count > 0))
                            {
                                Error(ErrorType.InvalidRequest, "Field with the same name already exists.", "", null);
                                return null;
                            }


                            par.Add("@name", typeof(String), name.Length).Value = name;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " name = @name";
                            update = true;

                            log.Add("Name changed from '" + dtField.Rows[0]["name"] + "' to '" + name + "'");
                        }
                        break;

                    case "data_type":
                        String data_type = parameters["data_type"].ToString();
                        if ((!String.IsNullOrWhiteSpace(data_type)) && (data_type != (String)dtField.Rows[0]["data_type"]))
                        {
                            switch (data_type.ToLower())
                            {
                                case "string":
                                case "datetime":
                                case "numeric":
                                    break;

                                default:
                                    Error(ErrorType.InvalidRequest, "Data type is not recognized.", "", null);
                                    return null;
                                    break;
                            }

                            par.Add("@data_type", typeof(String), data_type.Length).Value = data_type;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " data_type = @data_type";
                            update = true;

                            log.Add("Data type changed from '" + dtField.Rows[0]["data_type"] + "' to '" + data_type + "'");
                        }
                        break;

                    case "public_field":
                        Boolean public_field = true;
                        try
                        {
                            public_field = Boolean.Parse(parameters["public_field"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter public_field is not a boolean.", "", null);
                            return null;
                        }

                        if (public_field != (Boolean)dtField.Rows[0]["public"])
                        {
                            par.Add("@public_field", typeof(Boolean)).Value = public_field;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " [public] = @public_field";
                            update = true;
                            log.Add("Changed to a " + (public_field ? "" : "non ") + "field");
                        }
                        break;

                    case "user_field":
                        Boolean user_field = true;
                        try
                        {
                            user_field = Boolean.Parse(parameters["user_field"].ToString());
                        }
                        catch (Exception ex)
                        {
                            Error(ErrorType.InvalidRequest, "Parameter user_field is not a boolean.", "", null);
                            return null;
                        }

                        if (user_field != (Boolean)dtField.Rows[0]["user"])
                        {
                            par.Add("@user_field", typeof(Boolean)).Value = user_field;
                            if (updateSQL != "") updateSQL += ", ";
                            updateSQL += " [user] = @user_field";
                            update = true; 
                            log.Add("Changed to " + (user_field ? "an" : "a non ") + "user editable field");
                        }
                        break;

                }

            if (update)
            {
                updateSQL = "update field set " + updateSQL + " where id = @field_id";
                ExecuteNonQuery(sqlConnection, updateSQL, CommandType.Text, par);
                AddUserLog(sqlConnection, LogKey.Field_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Field changed", String.Join("\r\n", log));
            }

            //Atualiza a busca com os dados atualizados
            dtField = ExecuteDataTable(sqlConnection, "select * from field with(nolock) where enterprise_id = @enterprise_id and id = @field_id", CommandType.Text, par, null);

            DataRow dr1 = dtField.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("field_id", dr1["id"]);
            newItem.Add("data_type", dr1["data_type"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("public_field", dr1["public"]);
            newItem.Add("user_field", dr1["user"]);

            result.Add("info", newItem);

            return result;

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
            sql += "    ROW_NUMBER() OVER (ORDER BY f.name) AS [row_number], f.* ";
            sql += "     from field f with(nolock)";
            sql += "     where f.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and f.name like '%'+@text+'%'");
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
                    newItem.Add("field_id", dr1["id"]);
                    newItem.Add("data_type", dr1["data_type"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("public_field", dr1["public"]);
                    newItem.Add("user_field", dr1["user"]);

                    result.Add(newItem);
                }

            }

            return result;
        }


    }
}
