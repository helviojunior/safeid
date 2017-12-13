/// 
/// @file Logs.cs
/// <summary>
/// Implementações da classe Logs. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 19/11/2013
/// $Id: Logs.cs, v1.0 2013/11/19 Helvio Junior $


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
using System.Text.RegularExpressions;

namespace IAM.WebAPI.Classes
{
    internal class Logs : APIBase
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

            
            //Para todos os outros verifica autenticação
            AccessControl ac = ValidateCtrl(sqlConnection, method, auth, parameters, ExternalAccessControl); 
            if (!ac.Result)
            {
                Error(ErrorType.InvalidParameters, "Not authorized", "", null);
                return null;
            }


            //Segundo case para execução dos métodos
            switch (mp[1])
            {
                case "list":
                case "search":
                    return list(sqlConnection, parameters);
                    break;

                case "get":
                    return get(sqlConnection, parameters);
                    break;

                default:
                    Error(ErrorType.InvalidRequest, "JSON-rpc method is unknow.", "", null);
                    return null;
                    break;
            }

            return null;
        }


        /// <summary>
        /// Método privado para processamento do método 'logs.list'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Object get(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("logid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter logid is not defined.", "", null);
                return null;
            }


            String logid = parameters["logid"].ToString();
            if (String.IsNullOrWhiteSpace(logid))
            {
                Error(ErrorType.InvalidRequest, "Parameter logid is not defined.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@id", typeof(String), logid.Length).Value = logid;

            String sql = "";
            sql += "SELECT l.*, res.name resource_name, e.full_name executed_by_name";
            sql += "    from logs l with(nolock) ";
            sql += "    left join [identity] i with(nolock) on i.id = l.identity_id";
            sql += "    left join resource res with(nolock) on res.id = l.resource_id";
            sql += "    left join entity e with(nolock) on e.id = l.executed_by_entity_id";
            sql += "  WHERE";
            sql += "    l.id = @id";

            DataTable dtLogs = ExecuteDataTable(sqlConnection, sql, CommandType.Text,  par, null);
            if ((dtLogs != null) && (dtLogs.Rows.Count > 0))
            {
                DataRow dr1 = dtLogs.Rows[0];
                Dictionary<string, object> newItem = new Dictionary<string, object>();
                newItem.Add("log_id", dr1["id"]);
                newItem.Add("date", (Int32)((((DateTime)dr1["date"]) - new DateTime(1970, 1, 1)).TotalSeconds));
                newItem.Add("source", dr1["source"]);
                newItem.Add("level", dr1["level"]);
                newItem.Add("identity_id", dr1["identity_id"]);
                newItem.Add("resource_name", dr1["resource_name"]);
                newItem.Add("text", dr1["text"]);
                newItem.Add("additional_data", dr1["additional_data"]);
                newItem.Add("executed_by_entity_id", (Int64)dr1["executed_by_entity_id"]);
                newItem.Add("executed_by_name", (dr1["executed_by_name"] == DBNull.Value ? "System" : dr1["executed_by_name"].ToString()));

                return newItem;
            }

            return null;

        }


        /// <summary>
        /// Método privado para processamento do método 'logs.list'
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

            List<Object> tmpItem = new List<Object>();

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@text", typeof(String), text.Length).Value = text;

            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT";
            sql += "    ROW_NUMBER() OVER (ORDER BY l.date desc) AS [row_number], l.*, res.name resource_name";
            sql += "    from logs l with(nolock) left join [identity] i with(nolock) on i.id = l.identity_id left join resource res with(nolock) on res.id = l.resource_id";
            sql += "  WHERE";
            sql += "    l.entity_id = 0 and (l.enterprise_id = @enterprise_id or l.enterprise_id = 0)";

            if (!String.IsNullOrWhiteSpace(text))
                sql += "    and l.text like '%'+@text+'%'";


            if ((parameters.ContainsKey("filter")) && (parameters["filter"] is Dictionary<String, Object>))
            {
                Dictionary<String, Object> filter = (Dictionary<String, Object>)parameters["filter"];
                foreach (String k in filter.Keys)
                    switch (k.ToLower())
                    {
                        case "source":
                            try
                            {
                                if (!String.IsNullOrEmpty((String)filter[k]))
                                    sql += " and l.source = '" + filter[k].ToString() + "'";
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

            DataTable dtLogs = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
            if ((dtLogs != null) && (dtLogs.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtLogs.Rows)
                {
                    Dictionary<string, object> newItem = new Dictionary<string, object>();
                    newItem.Add("log_id", dr1["id"]);
                    newItem.Add("date", (Int32)((((DateTime)dr1["date"]) - new DateTime(1970, 1, 1)).TotalSeconds));
                    newItem.Add("source", dr1["source"]);
                    newItem.Add("level", dr1["level"]);
                    newItem.Add("identity_id", dr1["identity_id"]);
                    newItem.Add("resource_name", dr1["resource_name"]);
                    newItem.Add("text", dr1["text"]);
                    newItem.Add("additional_data", dr1["additional_data"]);

                    result.Add(newItem);
                }

            }

            return result;

        }
                        
    }
}
