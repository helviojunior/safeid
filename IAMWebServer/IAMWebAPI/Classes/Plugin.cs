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
using System.IO;
using IAM.Config;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using IAM.PluginManager;
using IAM.PluginInterface;
using SafeTrend.WebAPI;

namespace IAM.WebAPI.Classes
{
    /// <summary>
    /// Classe Role, derivada da classe APIBase
    /// Implementa os métodos role.*
    /// </summary>
    internal class Plugin : IAMAPIBase
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
                    return newplugin(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "list":
                case "search":
                    return list(database, parameters);
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
        private Dictionary<String, Object> newplugin(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            throw new NotImplementedException();

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

            if (!parameters.ContainsKey("pluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not defined.", "", null);
                return null;
            }


            String plugin = parameters["pluginid"].ToString();
            if (String.IsNullOrWhiteSpace(plugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not defined.", "", null);
                return null;
            }

            Int64 pluginid = 0;
            try
            {
                pluginid = Int64.Parse(plugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@plugin_id", typeof(Int64)).Value = pluginid;


            DataTable dtPlugin = database.ExecuteDataTable( "select p.*, resource_plugin_qty = (select COUNT(distinct rp1.plugin_id) from resource_plugin rp1 where rp1.plugin_id = p.id) from plugin p with(nolock) where (p.enterprise_id = @enterprise_id or p.enterprise_id = 0) and p.id = @plugin_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Plugin not found.", "", null);
                return null;
            }


            DataRow dr1 = dtPlugin.Rows[0];

            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("enterprise_id", dr1["enterprise_id"]);
            newItem.Add("plugin_id", dr1["id"]);
            newItem.Add("name", dr1["name"]);
            newItem.Add("scheme", dr1["scheme"]);
            newItem.Add("uri", dr1["uri"]);
            newItem.Add("assembly", dr1["assembly"]);
            newItem.Add("resource_plugin_qty", dr1["resource_plugin_qty"]);
            newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

            result.Add("info", newItem);

            if (parameters.ContainsKey("parameters") && (parameters["parameters"] is Boolean) && ((Boolean)parameters["parameters"]))
            {
                FileInfo assemblyFile = null;

                try
                {
                    assemblyFile = new FileInfo(Path.Combine(database.GetDBConfig("pluginFolder"), dr1["assembly"].ToString()));
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

                try
                {
                    PluginBase selectedPlugin = null;

                    Byte[] rawAssembly = File.ReadAllBytes(assemblyFile.FullName);
                    List<PluginBase> p1 = Plugins.GetPlugins<PluginBase>(rawAssembly);
                        
                    Array.Clear(rawAssembly, 0, rawAssembly.Length);
                    rawAssembly = null;

                    foreach (PluginBase p in p1)
                        if (p.GetPluginId().AbsoluteUri.ToLower() == dr1["uri"].ToString().ToLower())
                            selectedPlugin = p;

                    if (selectedPlugin == null)
                    {
                        Error(ErrorType.InternalError, "Plugin uri '" + dr1["uri"] + "' not found in assembly '" + dr1["assembly"] + "'", "", null);
                        return null;
                    }

                    List<Dictionary<string, object>> pars = new List<Dictionary<string, object>>();

                    foreach (PluginConfigFields f in selectedPlugin.GetConfigFields())
                    {
                        Dictionary<string, object> newPar = new Dictionary<string, object>();

                        newPar.Add("name", f.Name);
                        newPar.Add("key", f.Key);
                        newPar.Add("description", f.Description);
                        newPar.Add("default_value", f.DefaultValue);
                        newPar.Add("type", f.Type.ToString().ToLower());
                        newPar.Add("import_required", f.ImportRequired);
                        newPar.Add("deploy_required", f.DeployRequired);
                        newPar.Add("list_value", f.ListValue);

                        pars.Add(newPar);
                    }

                    result.Add("parameters", pars);

                    if (selectedPlugin is PluginConnectorBase)
                    {
                        List<Dictionary<string, object>> actions = new List<Dictionary<string, object>>();

                        foreach (PluginConnectorConfigActions af in ((PluginConnectorBase)selectedPlugin).GetConfigActions())
                        {
                            Dictionary<string, object> newAction = new Dictionary<string, object>();

                            newAction.Add("name", af.Name);
                            newAction.Add("key", af.Key);
                            newAction.Add("description", af.Description);
                            newAction.Add("field_name", af.Field.Name);
                            newAction.Add("field_key", af.Field.Key);
                            newAction.Add("field_description", af.Field.Description);
                            newAction.Add("macros", af.Macro);

                            actions.Add(newAction);
                        }

                        result.Add("actions", actions);

                    }

                }
                catch(Exception ex) {
                    Error(ErrorType.InternalError, "Erro on load assembly '" + dr1["assembly"] + "'", ex.Message, null);
                    return null;
                }

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

            if (!parameters.ContainsKey("pluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not defined.", "", null);
                return false;
            }


            String plugin = parameters["pluginid"].ToString();
            if (String.IsNullOrWhiteSpace(plugin))
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not defined.", "", null);
                return false;
            }

            Int64 pluginid = 0;
            try
            {
                pluginid = Int64.Parse(plugin);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter pluginid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@plugin_id", typeof(Int64)).Value = pluginid;

            DataTable dtPlugin = database.ExecuteDataTable( "select p.*, resource_plugin_qty = (select COUNT(distinct rp1.plugin_id) from resource_plugin rp1 where rp1.plugin_id = p.id) from plugin p with(nolock) where p.enterprise_id = @enterprise_id and p.id = @plugin_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Plugin not found.", "", null);
                return false;
            }

            //Verifica se está sendo usado
            if ((Int32)dtPlugin.Rows[0]["resource_plugin_qty"] > 0)
            {
                Error(ErrorType.SystemError, "Plugin is being used and can not be deleted.", "", null);
                return false;
            }

            //Localiza o arquivo físico
            FileInfo assemblyFile = null;
            try
            {
                DirectoryInfo pluginsDir = null;

                pluginsDir = new DirectoryInfo(database.GetDBConfig( "pluginFolder"));

                if (pluginsDir.Exists)
                    assemblyFile = new FileInfo(Path.Combine(pluginsDir.FullName, dtPlugin.Rows[0]["assembly"].ToString()));
            }
            catch
            {
                assemblyFile = null;
            }

            if ((assemblyFile == null) || (!assemblyFile.Exists))
            {
                Error(ErrorType.SystemError, "Plugin physical file not found.", "", null);
                return false;
            }

            SqlTransaction trans = (SqlTransaction)database.BeginTransaction();
            try
            {
                database.ExecuteNonQuery( "delete from plugin where id = @plugin_id", CommandType.Text,par,  trans);
                database.AddUserLog( LogKey.Plugin_Deleted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, 0, 0, "Plugin " + dtPlugin.Rows[0]["name"] + " deleted", "", trans);

                assemblyFile.Delete();

                trans.Commit();
            }
            catch {
                trans.Rollback();
                Error(ErrorType.SystemError, "Fail on delete physical file", "", null);
                return false;
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
            sql += "    ROW_NUMBER() OVER (ORDER BY p.uri) AS [row_number], p.*, resource_plugin_qty = (select COUNT(distinct rp1.plugin_id) from resource_plugin rp1 where rp1.plugin_id = p.id) ";
            sql += "     from plugin p with(nolock) ";
            sql += "     where ((p.enterprise_id = 0 or p.enterprise_id = @enterprise_id) " + (String.IsNullOrWhiteSpace(text) ? "" : " and p.name like '%'+@text+'%'") + ")";
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
                    newItem.Add("plugin_id", dr1["id"]);
                    newItem.Add("name", dr1["name"]);
                    newItem.Add("scheme", dr1["scheme"]);
                    newItem.Add("uri", dr1["uri"]);
                    newItem.Add("assembly", dr1["assembly"]);
                    newItem.Add("resource_plugin_qty", dr1["resource_plugin_qty"]);
                    newItem.Add("create_date", (dr1["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr1["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    result.Add(newItem);
                }

            }

            return result;
        }


    }
}
