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
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using IAM.Filters;
using SafeTrend.WebAPI;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Filter : IAMAPIBase
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
                    return newfilter(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "list":
                case "search":
                    return list(database, parameters);
                    break;

                case "use":
                    return use(database, parameters);
                    break;
                    
                case "delete":
                    return delete(database, parameters);
                    break;

                case "change":
                    return change(database, parameters);
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
        private Dictionary<String, Object> newfilter(IAMDatabase database, Dictionary<String, Object> parameters)
        {


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


            DbParameterCollection par2 = new DbParameterCollection();
            par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par2.Add("@filter_name", typeof(String)).Value = name;

            DataTable dtF1 = database.ExecuteDataTable( "select * from filters with(nolock) where enterprise_id = @enterprise_id and name = @filter_name", CommandType.Text, par2, null);
            if ((dtF1 != null) && (dtF1.Rows.Count > 0))
            {
                Error(ErrorType.InvalidRequest, "Filter with the same name already exists.", "", null);
                return null;
            }


            List<String> log = new List<String>();
            Boolean updateName = false;
            Boolean updateConditions = false;
            FilterRule filterData = getFilterData(database, "", parameters, log, out updateName, out updateConditions);

            if (filterData == null)
                return null;

            if (String.IsNullOrEmpty(filterData.FilterName))
            {
                Error(ErrorType.InvalidRequest, "Parameter name is not defined.", "", null);
                return null;
            }

            if (filterData.FilterGroups.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Filter conditions is empty.", "", null);
                return null;
            }

            DataTable dtFilter = null;

            SqlTransaction trans = (SqlTransaction)(SqlTransaction)database.BeginTransaction();
            try
            {
                DbParameterCollection par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                par.Add("@filter_name", typeof(String)).Value = filterData.FilterName;

                dtFilter = database.ExecuteDataTable( "sp_new_filter", CommandType.StoredProcedure, par, trans);
                if ((dtFilter == null) && (dtFilter.Rows.Count == 0))
                {
                    Error(ErrorType.InvalidRequest, "Error on insert filter.", "", null);
                    return null;
                }

                if (updateConditions && filterData.FilterGroups.Count > 0)
                {
                    foreach (FilterGroup g in filterData.FilterGroups)
                    {
                        foreach (FilterCondition f in g.FilterRules)
                        {
                            DbParameterCollection p2 = new DbParameterCollection();
                            p2.Add("@filter_id", typeof(Int64)).Value = (Int64)dtFilter.Rows[0]["id"];
                            p2.Add("@group_id", typeof(String)).Value = g.GroupId;
                            p2.Add("@group_selector", typeof(String)).Value = g.Selector.ToString();
                            p2.Add("@field_id", typeof(String)).Value = f.FieldId;
                            p2.Add("@text", typeof(String)).Value = f.DataString;
                            p2.Add("@condition", typeof(String)).Value = f.ConditionType.ToString();
                            p2.Add("@selector", typeof(String)).Value = f.Selector.ToString();

                            log.Add("Condition inserted: group = " + g.GroupId + ", condition = " + f.ToString());

                            database.ExecuteNonQuery( "insert into filters_conditions ([filter_id] ,[group_id] ,[group_selector] ,[field_id] ,[text] ,[condition] ,[selector]) VALUES (@filter_id,@group_id,@group_selector,@field_id,@text,@condition,@selector)", CommandType.Text, p2, trans);
                        }
                    }
                    log.Add("");

                }

                    database.AddUserLog(LogKey.Filter_Inserted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Filter added", String.Join("\r\n", log), Acl.EntityId, trans);

                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on insert filter", "", null);
                return null;
            }
            finally
            {
                //Saída sem aviso, ou seja, erro
                if (trans != null)
                    trans.Rollback();
            }



            Dictionary<String, Object> parameters2 = new Dictionary<string, object>();
            parameters2.Add("filterid", dtFilter.Rows[0]["id"]);

            return get(database, parameters2);

        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> get(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("filterid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return null;
            }


            String filter = parameters["filterid"].ToString();
            if (String.IsNullOrWhiteSpace(filter))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return null;
            }

            Int64 filterid = 0;
            try
            {
                filterid = Int64.Parse(filter);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@filter_id", typeof(Int64)).Value = filterid;


            DataTable dtFilter = database.ExecuteDataTable( "select f.*, ignore_qty = (select COUNT(distinct i1.filter_id) from resource_plugin_ignore_filter i1 with(nolock) where i1.filter_id = f.id), lock_qty = (select COUNT(distinct l1.filter_id) from resource_plugin_lock_filter l1 with(nolock) where l1.filter_id = f.id), role_qty = (select COUNT(distinct r1.filter_id) from resource_plugin_role_filter r1 with(nolock) where r1.filter_id = f.id)  from filters f with(nolock) where f.enterprise_id = @enterprise_id and f.id = @filter_id", CommandType.Text, par, null);
            if (dtFilter == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtFilter.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Filter not found.", "", null);
                return null;
            }


            DataRow dr1 = dtFilter.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("filter_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("ignore_qty", dr1["ignore_qty"]);
            newItem.Add("lock_qty", dr1["lock_qty"]);
            newItem.Add("role_qty", dr1["role_qty"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            

            //Lista as condições
            List<Dictionary<String, Object>> conditions = new List<Dictionary<string, object>>();

            FilterRule f = new FilterRule(dr1["name"].ToString());
            DataTable dt2 = database.ExecuteDataTable( "select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = " + dr1["id"] + " order by f.group_id, f1.name");
            if ((dt2 != null) || (dt2.Rows.Count > 0))
                foreach (DataRow dr2 in dt2.Rows)
                {
                    Dictionary<string, object> c1 = new Dictionary<string, object>();
                    c1.Add("group_id", dr2["group_id"].ToString());
                    c1.Add("group_selector", dr2["group_selector"].ToString());
                    c1.Add("field_id", (Int64)dr2["field_id"]);
                    c1.Add("field_name", dr2["field_name"].ToString());
                    c1.Add("data_type", dr2["data_type"].ToString());
                    c1.Add("text", dr2["text"].ToString());
                    c1.Add("condition", dr2["condition"].ToString());
                    c1.Add("selector", dr2["selector"].ToString());
                    
                    conditions.Add(c1);

                    f.AddCondition(dr2["group_id"].ToString(), dr2["group_selector"].ToString(), (Int64)dr2["field_id"], dr2["field_name"].ToString(), dr2["data_type"].ToString(), dr2["text"].ToString(), dr2["condition"].ToString(), dr2["selector"].ToString());
                }

            newItem.Add("conditions_description", f.ToString());
            newItem.Add("conditions", conditions);

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

            if (!parameters.ContainsKey("filterid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return false;
            }


            String filter = parameters["filterid"].ToString();
            if (String.IsNullOrWhiteSpace(filter))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return false;
            }

            Int64 filterid = 0;
            try
            {
                filterid = Int64.Parse(filter);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@filter_id", typeof(Int64)).Value = filterid;


            DataTable dtFilter = database.ExecuteDataTable( "select f.*, ignore_qty = (select COUNT(distinct i1.filter_id) from resource_plugin_ignore_filter i1 with(nolock) where i1.filter_id = f.id), lock_qty = (select COUNT(distinct l1.filter_id) from resource_plugin_lock_filter l1 with(nolock) where l1.filter_id = f.id), role_qty = (select COUNT(distinct r1.filter_id) from resource_plugin_role_filter r1 with(nolock) where r1.filter_id = f.id)  from filters f with(nolock) where f.enterprise_id = @enterprise_id and f.id = @filter_id", CommandType.Text, par, null);
            if (dtFilter == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtFilter.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Filter not found.", "", null);
                return false;
            }


            //Verifica se está sendo usado
            if (((Int32)dtFilter.Rows[0]["ignore_qty"] > 0) || ((Int32)dtFilter.Rows[0]["lock_qty"] > 0) || ((Int32)dtFilter.Rows[0]["role_qty"] > 0))
            {
                Error(ErrorType.SystemError, "Filter is being used and can not be deleted.", "", null);
                return false;
            }

            database.ExecuteNonQuery( "delete from filters where id = @filter_id", CommandType.Text, par);
            database.AddUserLog( LogKey.Filter_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Filter " + dtFilter.Rows[0]["name"] + " deleted", "", Acl.EntityId);
            
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
            sql += "    ROW_NUMBER() OVER (ORDER BY f.name) AS [row_number], f.*, ";
            sql += "    ignore_qty = (select COUNT(distinct i1.filter_id) from resource_plugin_ignore_filter i1 with(nolock) where i1.filter_id = f.id), ";
            sql += "    lock_qty = (select COUNT(distinct l1.filter_id) from resource_plugin_lock_filter l1 with(nolock) where l1.filter_id = f.id),";
            sql += "    role_qty = (select COUNT(distinct r1.filter_id) from resource_plugin_role_filter r1 with(nolock) where r1.filter_id = f.id) ";
            sql += "     from filters f ";
            sql += "     where (f.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and f.name like '%'+@text+'%'") + ")";
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
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("filter_id", dr1["id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("ignore_qty", dr1["ignore_qty"]);
                    newItem.Add("lock_qty", dr1["lock_qty"]);
                    newItem.Add("role_qty", dr1["role_qty"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));


                    //Lista as condições
                    List<Dictionary<String, Object>> conditions = new List<Dictionary<string, object>>();

                    FilterRule f = new FilterRule(dr1["name"].ToString());
                    DataTable dt2 = database.ExecuteDataTable( "select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = " + dr1["id"] + "  order by f.group_id, f1.name");
                    if ((dt2 != null) || (dt2.Rows.Count > 0))
                        foreach (DataRow dr2 in dt2.Rows)
                        {
                            
                            Dictionary<string, object> c1 = new Dictionary<string, object>();
                            c1.Add("group_id", dr2["group_id"].ToString());
                            c1.Add("group_selector", dr2["group_selector"].ToString());
                            c1.Add("field_id", (Int64)dr2["field_id"]);
                            c1.Add("field_name", dr2["field_name"].ToString());
                            c1.Add("data_type", dr2["data_type"].ToString());
                            c1.Add("text", dr2["text"].ToString());
                            c1.Add("condition", dr2["condition"].ToString());
                            c1.Add("selector", dr2["selector"].ToString());

                            conditions.Add(c1);

                            f.AddCondition(dr2["group_id"].ToString(), dr2["group_selector"].ToString(), (Int64)dr2["field_id"], dr2["field_name"].ToString(), dr2["data_type"].ToString(), dr2["text"].ToString(), dr2["condition"].ToString(), dr2["selector"].ToString());
                        }

                    newItem.Add("conditions_description", f.ToString());
                    newItem.Add("conditions", conditions);

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
        private List<Dictionary<String, Object>> use(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            if (!parameters.ContainsKey("filterid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return null;
            }


            String filter = parameters["filterid"].ToString();
            if (String.IsNullOrWhiteSpace(filter))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return null;
            }

            Int64 filterid = 0;
            try
            {
                filterid = Int64.Parse(filter);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not a long integer.", "", null);
                return null;
            }


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


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@filter_id", typeof(Int64)).Value = filterid;


            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT ";
            sql += "    ROW_NUMBER() OVER (ORDER BY f.resource_plugin_name) AS [row_number], f.* ";
            sql += "     from vw_filters_use f ";
            sql += "     where f.enterprise_id = @enterprise_id and f.filter_id = @filter_id";
            sql += ") SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            DataTable dtFilters = database.ExecuteDataTable( sql, CommandType.Text, par, null);
            if ((dtFilters != null) && (dtFilters.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtFilters.Rows)
                {
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("enterprise_id", dr1["enterprise_id"]);
                    newItem.Add("context_id", dr1["context_id"]);
                    newItem.Add("filter_id", dr1["filter_id"]);
                    newItem.Add("resource_plugin_id", dr1["resource_plugin_id"]);
                    newItem.Add("resource_plugin_name", dr1["resource_plugin_name"]);
                    newItem.Add("role_qty", dr1["role_qty"]);
                    newItem.Add("ignore_qty", dr1["ignore_qty"]);
                    newItem.Add("lock_qty", dr1["lock_qty"]);

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
        private Dictionary<String, Object> change(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("filterid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return null;
            }


            String filter = parameters["filterid"].ToString();
            if (String.IsNullOrWhiteSpace(filter))
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not defined.", "", null);
                return null;
            }

            Int64 filterid = 0;
            try
            {
                filterid = Int64.Parse(filter);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter filterid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@filter_id", typeof(Int64)).Value = filterid;


            DataTable dtFilter = database.ExecuteDataTable( "select f.* from filters f with(nolock) where f.enterprise_id = @enterprise_id and f.id = @filter_id", CommandType.Text, par, null);
            if (dtFilter == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtFilter.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Filter not found.", "", null);
                return null;
            }

            List<String> log = new List<String>();
            Boolean updateName = false;
            Boolean updateConditions = false;
            FilterRule filterData = getFilterData(database, dtFilter.Rows[0]["name"].ToString(), parameters, log, out updateName, out updateConditions);

            if (filterData == null)
                return null;

            if (updateName || updateConditions)
            {
                SqlTransaction trans = (SqlTransaction)database.BeginTransaction();
                try
                {
                    if (updateName)
                    {
                        if (filterData.FilterName != dtFilter.Rows[0]["name"].ToString())
                        {
                            par.Add("@name", typeof(String)).Value = filterData.FilterName;

                            log.Add("Name changed from '" + dtFilter.Rows[0]["name"] + "' to '" + filterData.FilterName + "'");

                            database.ExecuteNonQuery( "update filters set name = @name where id = @filter_id", CommandType.Text, par, trans);
                        }
                    }

                    if (updateConditions && filterData.FilterGroups.Count > 0)
                    {
                        //Busca todas as regras deste filtro no DB
                        DataTable dtFilterConditions = database.ExecuteDataTable( "select fc.* from filters_conditions fc with(nolock) where fc.filter_id = @filter_id",CommandType.Text, par, trans);

                        List<String> contains = new List<String>();
                        List<DbParameterCollection> newItems = new List<DbParameterCollection>();

                        foreach (FilterGroup g in filterData.FilterGroups)
                        {
                            foreach (FilterCondition f in g.FilterRules)
                            {
                                Boolean addNew = false;

                                if ((dtFilterConditions != null) && (dtFilterConditions.Rows.Count > 0))
                                {
                                    String s = "group_id = '" + g.GroupId + "' and field_id = " + f.FieldId + " and text = '" + f.DataString + "' and condition = '" + f.ConditionType.ToString() + "'";
                                    DataRow[] sel = dtFilterConditions.Select(s);
                                    if (sel.Length > 0)
                                    {
                                        contains.Add(sel[0]["id"].ToString());
                                        
                                        //Atualiza 
                                        if ((sel[0]["group_selector"].ToString().ToLower() != g.Selector.ToString().ToLower()) || (sel[0]["selector"].ToString().ToLower() != f.Selector.ToString().ToLower()))
                                        {
                                            DbParameterCollection p3 = new DbParameterCollection();
                                            p3.Add("@condition_id", typeof(Int64)).Value = (Int64)sel[0]["id"];
                                            p3.Add("@group_selector", typeof(String)).Value = g.Selector.ToString();
                                            p3.Add("@selector", typeof(String)).Value = f.Selector.ToString();

                                            log.Add("Condition updated: group = " + g.GroupId + ", selector = " + f.Selector.ToString() + ", condition = " + f.ToString());
                                            database.ExecuteNonQuery( "update filters_conditions set [group_selector] = @group_selector, [selector] = @selector where id = @condition_id", CommandType.Text, p3,  trans);
                                        }
                                    }
                                    else
                                    {
                                        addNew = true;
                                    }

                                }
                                else
                                {
                                    addNew = true;
                                }

                                //Adiciona a condição
                                if (addNew)
                                {
                                    DbParameterCollection p2 = new DbParameterCollection();
                                    p2.Add("@filter_id", typeof(Int64)).Value = filterid;
                                    p2.Add("@group_id", typeof(String)).Value = g.GroupId;
                                    p2.Add("@group_selector", typeof(String)).Value = g.Selector.ToString();
                                    p2.Add("@field_id", typeof(String)).Value = f.FieldId;
                                    p2.Add("@text", typeof(String)).Value = f.DataString;
                                    p2.Add("@condition", typeof(String)).Value = f.ConditionType.ToString();
                                    p2.Add("@selector", typeof(String)).Value = f.Selector.ToString();

                                    newItems.Add(p2);
                                    
                                    log.Add("Condition inserted: group = " + g.GroupId + ", condition = " + f.ToString());  
                                }

                            }
                        }
                        log.Add("");

                        //Deleta as condições que não estão sendo utilizadas
                        if (contains.Count > 0)
                        {
                            DataTable dtFc = database.ExecuteDataTable( "select f.*, f1.name field_name, f1.data_type from filters_conditions f with(nolock) inner join field f1 with(nolock) on f1.id = f.field_id where f.filter_id = @filter_id and f.id not in (" + String.Join(",", contains) + ")",CommandType.Text, par, trans);
                            if ((dtFc != null) && (dtFc.Rows.Count > 0))
                            {
                                FilterRule fdTmp = new FilterRule("");
                                foreach (DataRow dr2 in dtFc.Rows)
                                    fdTmp.AddCondition(dr2["group_id"].ToString(), dr2["group_selector"].ToString(), (Int64)dr2["field_id"], dr2["field_name"].ToString(), dr2["data_type"].ToString(), dr2["text"].ToString(), dr2["condition"].ToString(), dr2["selector"].ToString());

                                foreach (FilterGroup g in fdTmp.FilterGroups)
                                    foreach (FilterCondition f in g.FilterRules)
                                        log.Add("Condition deleted: group = " + g.GroupId + ", condition = " + f.ToString());
                            }

                            database.ExecuteNonQuery( "delete from  filters_conditions where filter_id = @filter_id and id not in (" + String.Join(",", contains) + ")", CommandType.Text,par,  trans);
                        }
                        else
                        {
                            database.ExecuteNonQuery( "delete from  filters_conditions where filter_id = @filter_id", CommandType.Text,par,  trans);
                        }

                        foreach (DbParameterCollection p2 in newItems)
                            database.ExecuteNonQuery( "insert into filters_conditions ([filter_id] ,[group_id] ,[group_selector] ,[field_id] ,[text] ,[condition] ,[selector]) VALUES (@filter_id,@group_id,@group_selector,@field_id,@text,@condition,@selector)", CommandType.Text,p2,  trans);

                    }

                    log.Add("");
                    log.Add("Filtro:");
                    log.Add(filterData.ToString());


                    database.AddUserLog( LogKey.Filter_Changed, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Filter changed", String.Join("\r\n", log), Acl.EntityId, trans);

                    trans.Commit();
                    trans = null;
                }
                catch (Exception ex)
                {
                    Error(ErrorType.InvalidRequest, "Error on update filter", "", null);
                    return null;
                }
                finally
                {
                    //Saída sem aviso, ou seja, erro
                    if (trans != null)
                        trans.Rollback();
                }

            }

            return get(database, parameters);

        }

        private FilterRule getFilterData(IAMDatabase database, String name, Dictionary<String, Object> parameters, List<String> log, out Boolean updateName, out Boolean updateConditions)
        {

            FilterRule filterData = new FilterRule(name);

            updateName = false;
            updateConditions = false;
            if (parameters["name"] != null)
            {
                String n = parameters["name"].ToString();
                if (!String.IsNullOrWhiteSpace(n))
                {
                    filterData.FilterName = n;
                    updateName = true;
                }
            }

            if (parameters["conditions"] != null)
            {
                if (!(parameters["conditions"] is ArrayList))
                {
                    Error(ErrorType.InvalidRequest, "Parameter conditions is not valid.", "", null);
                    return null;
                }

                List<Object> conditionList = new List<Object>();
                conditionList.AddRange(((ArrayList)parameters["conditions"]).ToArray());

                DataTable dtField = database.ExecuteDataTable( "select * from field f with(nolock) where f.enterprise_id = " + this._enterpriseId, CommandType.Text, null, null);
                if ((dtField == null) && (dtField.Rows.Count == 0))
                {
                    Error(ErrorType.InvalidRequest, "Field list not found.", "", null);
                    return null;
                }

                for (Int32 u = 0; u < conditionList.Count; u++)
                {
                    if (!(conditionList[u] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Condition " + u + " is not valid", "", null);
                        return null;
                    }

                    Dictionary<String, Object> c1 = (Dictionary<String, Object>)conditionList[u];
                    if (!c1.ContainsKey("group_id"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter group_id is not defined in contition " + u, "", null);
                        return null;
                    }
                    if (!c1.ContainsKey("group_selector"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter group_selector is not defined in contition " + u, "", null);
                        return null;
                    }
                    if (!c1.ContainsKey("field_id"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is not defined in contition " + u, "", null);
                        return null;
                    }
                    if (!c1.ContainsKey("text"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter text is not defined in contition " + u, "", null);
                        return null;
                    }
                    if (!c1.ContainsKey("condition"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter condition is not defined in contition " + u, "", null);
                        return null;
                    }
                    if (!c1.ContainsKey("selector"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter selector is not defined in contition " + u, "", null);
                        return null;
                    }

                    //Resgata o nome e o tipo de dados do campo
                    DataRow[] f = dtField.Select("id = " + c1["field_id"].ToString());
                    if (f.Length == 0)
                    {
                        Error(ErrorType.InvalidRequest, "Field " + c1["field_id"].ToString() + " not exists or is not a chield of this enterprise in condition " + u, "", null);
                        return null;
                    }

                    filterData.AddCondition(
                        c1["group_id"].ToString(),
                        c1["group_selector"].ToString(),
                        Int64.Parse(f[0]["id"].ToString()),
                        f[0]["name"].ToString(),
                        f[0]["data_type"].ToString(),
                        c1["text"].ToString(),
                        c1["condition"].ToString(),
                        c1["selector"].ToString());
                }

                updateConditions = true;
            }

            return filterData;
        }

    }
}
