/// 
/// @file User.cs
/// <summary>
/// Implementações da classe User. 
/// </summary>
/// @author Helvio Junior <helvio_junior@hotmail.com>
/// @date 15/10/2013
/// $Id: User.cs, v1.0 2013/10/15 Helvio Junior $


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
using IAM.GlobalDefs.Messages;
using SafeTrend.Data;
using IAM.UserProcess;
using IAM.License;
using System.Text.RegularExpressions;
using IAM.PluginInterface;
using SafeTrend.WebAPI;
using IAM.Workflow;
using System.Net.Mail;

namespace IAM.WebAPI.Classes
{
    internal class User : IAMAPIBase
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

            method = method.ToLower();
            String[] mp = method.Split(".".ToCharArray(), 2);

            if (mp.Length != 2)
                return null;

            if (this.GetType().Name.ToLower() != mp[0])
                return null;

            //Primeiro case para validar autenticação
            switch (mp[1])
            {
                case "login":
                    //Este método não precisa verificar autenticação
                    break;

                default:
                    //Para todos os outros verifica autenticação
                    Acl = ValidateCtrl(database, method, auth, parameters, ExternalAccessControl);
                    if (!Acl.Result)
                    {
                        Error(ErrorType.InvalidParameters, "Not authorized", "", null);
                        return null;
                    }
                    break;
            }

            //Segundo case para execução dos métodos
            switch (mp[1])
            {
                case "new":
                    return newuser(database, parameters);
                    break;

                case "changeproperty":
                    return changeproperty(database, parameters);
                    break;

                case "changecontainer":
                    return changecontainer(database, parameters);
                    break;

                case "login":
                    return login(database, parameters);
                    break;

                case "logout":
                    return logout(database, auth);
                    break;

                case "deleteidentity":
                    return deleteidentity(database, parameters);
                    break;

                case "unlockidentity":
                    return unlockidentity(database, parameters);
                    break;

                case "resetpassword":
                    return resetpwd(database, parameters);
                    break;

                case "changepassword":
                    return changepassword(database, parameters);
                    break;

                case "search":
                case "list":
                    return search(database, parameters);
                    break;

                case "get":
                    return get(database, parameters);
                    break;

                case "deploy":
                    return deploy(database, parameters, false, true);
                    break;

                case "lock":
                    return deploy(database, parameters, true, false);
                    break;

                case "unlock":
                    return deploy(database, parameters, false, false);
                    break;

                case "delete":
                    return delete(database, parameters, true);
                    break;

                case "undelete":
                    return delete(database, parameters, false);
                    break;

                case "logs":
                    return logs(database, parameters);
                    break;

                case "auth":
                    return authUser(database, parameters);
                    break;

                case "accessrequest":
                    return accessrequest(database, parameters);
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
        private Dictionary<String, Object> newuser(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("resourcepluginid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not defined.", "", null);
                return null;
            }

            Int64 rpId = 0;
            try
            {
                rpId = Int64.Parse(parameters["resourcepluginid"].ToString());
            }
            catch {
                Error(ErrorType.InvalidRequest, "Parameter resourcepluginid is not valid.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("properties"))
            {
                Error(ErrorType.InvalidRequest, "Parameter properties is not defined.", "", null);
                return null;
            }

            if (!(parameters["properties"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter properties is not valid.", "", null);
                return null;
            }


            //Realiza o mesmo processamento do Engine
            StringBuilder tLog = new StringBuilder();
            Int64 userId = 0;
            try
            {
                RegistryProcessStarter starter = null;

                PluginConfig pluginConfig = null;
                using (DataTable dtContext = database.ExecuteDataTable( "select p.scheme, rp.*, c.id context_id, p.uri from resource_plugin rp with(nolock) inner join plugin p with(nolock) on rp.plugin_id = p.id inner join resource r with(nolock) on rp.resource_id = r.id inner join context c with(nolock) on r.context_id = c.id where rp.id = " + rpId, CommandType.Text, null))
                {
                    if ((dtContext != null) && (dtContext.Rows.Count > 0))
                    {
                        pluginConfig = new PluginConfig(database.Connection, dtContext.Rows[0]["scheme"].ToString(), (Int64)dtContext.Rows[0]["plugin_id"], (Int64)dtContext.Rows[0]["id"]);

                        starter = new RegistryProcessStarter(
                            this._enterpriseId,
                            (Int64)dtContext.Rows[0]["context_id"],
                            new Uri(dtContext.Rows[0]["uri"].ToString()),
                            (Int64)dtContext.Rows[0]["resource_id"],
                            (Int64)dtContext.Rows[0]["plugin_id"],
                            (Int64)dtContext.Rows[0]["id"],
                            "API.user.new",
                            Guid.NewGuid().ToString(), 
                            "");
                    }
                }

                if (pluginConfig == null)
                    throw new Exception("Resource x plugin not found");


                LicenseControl lic = LicenseChecker.GetLicenseData(database.Connection, null, this._enterpriseId);

                if (!lic.Valid)
                {
                    Error(ErrorType.InvalidRequest, "License error: " + lic.Error, "", null);
                    return null;
                }

                if ((lic.Entities > 0) && (lic.Count > lic.Entities))
                {
                    Error(ErrorType.InvalidRequest, "License error: License limit (" + lic.Entities + " entities) exceeded", "", null);
                    return null;
                }

                PluginConnectorBaseImportPackageUser pkg = new PluginConnectorBaseImportPackageUser("API.user.new");
                

                List<Object> lst = new List<Object>();
                lst.AddRange(((ArrayList)parameters["properties"]).ToArray());


                for (Int32 i = 0; i < lst.Count; i++)
                //foreach (Dictionary<String, Object> field in mapping)
                {
                    if (!(lst[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Property " + i + " is not valid", "", null);
                        return null;
                    }

                    Dictionary<String, Object> field = (Dictionary<String, Object>)lst[i];

                    if (!field.ContainsKey("field_id"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is not defined in property " + i, "", null);
                        return null;
                    }

                    Int64 fieldId = 0;
                    if (!String.IsNullOrWhiteSpace((String)field["field_id"]))
                    {
                        try
                        {
                            fieldId = Int64.Parse(field["field_id"].ToString());
                        }
                        catch
                        {
                            Error(ErrorType.InvalidRequest, "Parameter field_id is not a long integer on property " + i, "", null);
                            return null;
                        }
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is empty on property " + i, "", null);
                        return null;
                    }

                    if (!field.ContainsKey("value"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter value is not defined in property " + i, "", null);
                        return null;
                    }

                    if (String.IsNullOrWhiteSpace((String)field["value"]))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter value is empty on property " + i, "", null);
                        return null;
                    }


                    DataTable dtField = database.ExecuteDataTable( "select * from field where enterprise_id = "+ this._enterpriseId +" and id = " + fieldId, CommandType.Text, null, null);
                    if ((dtField == null) || (dtField.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Field on property " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return null;
                    }

                    DataTable dtFieldMapping = database.ExecuteDataTable( "select f.*, rpm.data_name from field f with(nolock) inner join resource_plugin_mapping rpm with(nolock) on rpm.field_id = f.id where f.enterprise_id = " + this._enterpriseId + " and rpm.field_id = " + fieldId + " and rpm.resource_plugin_id = " + rpId, CommandType.Text, null, null);
                    if ((dtFieldMapping == null) || (dtFieldMapping.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Field on property " + i + " not exists on resource x plugin mapping.", "", null);
                        return null;
                    }
                    else
                    {
                        pkg.properties.Add(new PluginConnectorBasePackageData(dtFieldMapping.Rows[0]["data_name"].ToString(), (String)field["value"], dtFieldMapping.Rows[0]["data_type"].ToString()));
                    }

                }

                if (pkg.properties.Count == 0)
                {
                    Error(ErrorType.InvalidRequest, "Properties is empty.", "", null);
                    return null;
                }

                starter.package = SafeTrend.Json.JSON.Serialize2(pkg);
                pkg.Dispose();
                pkg = null;

                EnterpriseKeyConfig k = new EnterpriseKeyConfig(database.Connection, this._enterpriseId);

                LockRules lockRules = new LockRules();
                IgnoreRules ignoreRules = new IgnoreRules();
                RoleRules roleRules = new RoleRules();
                lockRules.GetDBConfig(database.Connection);
                ignoreRules.GetDBConfig(database.Connection);
                roleRules.GetDBConfig(database.Connection);

                //Realiza todo o processamento deste registro
                using (RegistryProcess proc = new RegistryProcess(database.Connection, pluginConfig, starter))
                {

                    RegistryProcess.ProccessLog log = new RegistryProcess.ProccessLog(delegate(String text)
                    {
                        tLog.AppendLine(text);
                    });

                    proc.OnLog += log;
                    RegistryProcessStatus status = proc.Process(k, lockRules, ignoreRules, roleRules, lic);
                    proc.OnLog -= log;

                    userId = proc.EntityId;

                    database.AddUserLog( LogKey.Import, null, "API", (status == RegistryProcessStatus.Error ? UserLogLevel.Error : UserLogLevel.Info), 0, 0, 0, starter.resourceId, starter.pluginId, proc.EntityId, proc.IdentityId, "Import processed", tLog.ToString());

                    if (status == RegistryProcessStatus.OK)
                    {
                        //Deixa transcorrer normalmente
                    }
                    else if (status == RegistryProcessStatus.Ignored)
                    {
                        Error(ErrorType.InvalidRequest, "Registry ignored by filter. Please see log for details.", "", null);
                        return null;
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Error on process. Please see log for details.", "", null);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ErrorType.InvalidRequest, "Error on process: " + ex.Message, "", null);
                return null;
            }
            finally
            {
                tLog = null;
            }

            if (userId != 0)
            {
                Dictionary<String, Object> newData = new Dictionary<String, Object>();
                newData.Add("userid", userId);
                return get(database, newData);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean changecontainer(IAMDatabase database, Dictionary<String, Object> parameters)
        {


            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return false;
            }

            Int64 userId = 0;
            try
            {
                userId = Int64.Parse(parameters["userid"].ToString());
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return false;
            }

            if (!parameters.ContainsKey("containerid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not defined.", "", null);
                return false;
            }

            Int64 containerid = 0;
            try
            {
                containerid = Int64.Parse(parameters["containerid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter containerid is not a long integer.", "", null);
                return false;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userId;

            DataTable dtUsers = database.ExecuteDataTable("select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 and  c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
            if ((dtUsers == null) || (dtUsers.Rows.Count == 0))
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return false;
            }

            if (containerid > 0)
            {
                par = new DbParameterCollection();
                par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                par.Add("@container_id", typeof(Int64)).Value = containerid;

                DataTable dtContainer = database.ExecuteDataTable("select c.*, c1.enterprise_id, c1.name context_name, entity_qty = (select COUNT(distinct e.id) from entity e with(nolock) inner join entity_container ec with(nolock) on e.id = ec.entity_id where ec.container_id = c.id) from container c with(nolock) inner join context c1 with(nolock) on c1.id = c.context_id where c1.enterprise_id = @enterprise_id and c.id = @container_id order by c.name", CommandType.Text, par, null);
                if (dtContainer == null)
                {
                    Error(ErrorType.InternalError, "", "", null);
                    return false;
                }

                if (dtContainer.Rows.Count == 0)
                {
                    Error(ErrorType.InvalidRequest, "Container not found.", "", null);
                    return false;
                }
            }


            SqlTransaction trans = (SqlTransaction)database.BeginTransaction();

            try
            {
                DbParameterCollection par2 = new DbParameterCollection();
                par2.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
                par2.Add("@container_id", typeof(Int64)).Value = containerid;
                par2.Add("@entity_id", typeof(Int64)).Value = userId;

                //Select all old containers
                DataTable drContainers = database.ExecuteDataTable("select c.* from entity_container e inner join container c on c.id = e.container_id where e.entity_id = @entity_id", CommandType.Text, par2, trans);
                if ((drContainers != null) && (drContainers.Rows.Count > 0))
                {
                    foreach (DataRow dr in drContainers.Rows)
                        if ((Int64)dr["id"] == containerid)
                            database.AddUserLog(LogKey.User_ContainerRoleUnbind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userId, 0, "Identity unbind to container " + dr["name"].ToString(), "", Acl.EntityId, trans);
                }

                if (containerid > 0)
                {
                    DataTable dtRet = database.ExecuteDataTable("sp_insert_entity_to_container", CommandType.StoredProcedure, par2, trans);

                    if ((dtRet != null) && (dtRet.Rows.Count > 0))
                    {
                        database.AddUserLog(LogKey.User_ContainerRoleBind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userId, 0, "Identity bind to container " + dtRet.Rows[0]["name"].ToString(), "", Acl.EntityId, trans);
                    }
                }
                else
                {
                    database.ExecuteNonQuery("delete from entity_container where entity_id = " + userId, CommandType.Text, null, trans);
                    database.AddUserLog(LogKey.User_ContainerRoleBind, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userId, 0, "Identity bind to root container ", "", Acl.EntityId, trans);
                }

                database.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + userId + ")", CommandType.Text, null, trans);
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
        private Dictionary<String, Object> changeproperty(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }

            Int64 userId = 0;
            try
            {
                userId = Int64.Parse(parameters["userid"].ToString());
            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not valid.", "", null);
                return null;
            }


            if (!parameters.ContainsKey("properties"))
            {
                Error(ErrorType.InvalidRequest, "Parameter properties is not defined.", "", null);
                return null;
            }

            if (!(parameters["properties"] is ArrayList))
            {
                Error(ErrorType.InvalidRequest, "Parameter properties is not valid.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userId;

            DataTable dtUsers = database.ExecuteDataTable( "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 and  c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
            if ((dtUsers == null) || (dtUsers.Rows.Count == 0))
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return null;
            }

            List<Object> lst = new List<Object>();
            lst.AddRange(((ArrayList)parameters["properties"]).ToArray());


            List<UserDataFields> properties = new List<UserDataFields>();
            StringBuilder tLog = new StringBuilder();
            SqlTransaction trans = null;
            try
            {

                for (Int32 i = 0; i < lst.Count; i++)
                {
                    if (!(lst[i] is Dictionary<String, Object>))
                    {
                        Error(ErrorType.InvalidRequest, "Property " + i + " is not valid", "", null);
                        return null;
                    }

                    Dictionary<String, Object> field = (Dictionary<String, Object>)lst[i];

                    if (!field.ContainsKey("field_id"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is not defined in property " + i, "", null);
                        return null;
                    }

                    Int64 fieldId = 0;
                    if (!String.IsNullOrWhiteSpace((String)field["field_id"]))
                    {
                        try
                        {
                            fieldId = Int64.Parse(field["field_id"].ToString());
                        }
                        catch
                        {
                            Error(ErrorType.InvalidRequest, "Parameter field_id is not a long integer on property " + i, "", null);
                            return null;
                        }
                    }
                    else
                    {
                        Error(ErrorType.InvalidRequest, "Parameter field_id is empty on property " + i, "", null);
                        return null;
                    }

                    if (!field.ContainsKey("value"))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter value is not defined in property " + i, "", null);
                        return null;
                    }

                    if (String.IsNullOrWhiteSpace((String)field["value"]))
                    {
                        Error(ErrorType.InvalidRequest, "Parameter value is empty on property " + i, "", null);
                        return null;
                    }


                    DataTable dtField = database.ExecuteDataTable( "select * from field where enterprise_id = " + this._enterpriseId + " and id = " + fieldId, CommandType.Text, null, null);
                    if ((dtField == null) || (dtField.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Field on property " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return null;
                    }

                    //Teste o tipo do item
                    try
                    {
                        properties.Add(new UserDataFields(fieldId, (String)dtField.Rows[0]["name"], (String)dtField.Rows[0]["data_type"], (String)field["value"]));
                    }
                    catch (Exception ex)
                    {
                        Error(ErrorType.InvalidRequest, "Field on property " + i + " not is valid.", "", null);
                        return null;
                    }
                }

                List<UserDataFields> toDelete = new List<UserDataFields>();
                
                //Lista propriedades atuais
                DataTable dtProperty = database.ExecuteDataTable( "select e.*, f.name field_name, f.data_type from entity_field e with(nolock) inner join field f  with(nolock) on e.field_id = f.id where e.entity_id = @user_id", CommandType.Text, par, null);
                if ((dtProperty != null) && (dtProperty.Rows.Count > 0))
                    foreach (DataRow dr in dtProperty.Rows)
                    {
                        UserDataFields find = properties.Find(f => (f.Mapping.field_id == (Int64)dr["field_id"] && f.StringValue == (String)dr["value"]));
                        if (find == null)
                        {
                            toDelete.Add(new UserDataFields((Int64)dr["field_id"], (String)dr["field_name"], (String)dr["data_type"], (String)dr["value"]));
                        }
                        else
                        {
                            tLog.AppendLine("Field '" + find.Mapping.field_name + "' with value '" + find.Value + "' not changed");
                            properties.Remove(find);
                        }
                    }


                trans = (SqlTransaction)database.BeginTransaction();

                foreach (UserDataFields d in toDelete)
                {
                    tLog.AppendLine("Field '" + d.Mapping.field_name + "' with value '" + d.Value + "' deleted");

                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@user_id", typeof(Int64)).Value = userId;
                    par2.Add("@field_id", typeof(Int64)).Value = d.Mapping.field_id;
                    par2.Add("@value", typeof(String)).Value = d.Value;

                    database.ExecuteNonQuery( "delete from entity_field where entity_id = @user_id and field_id = @field_id and value = @value", CommandType.Text, par2, trans);
                }


                foreach (UserDataFields a in properties)
                {
                    tLog.AppendLine("Field '" + a.Mapping.field_name + "' with value '" + a.Value + "' inserted");

                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@user_id", typeof(Int64)).Value = userId;
                    par2.Add("@field_id", typeof(Int64)).Value = a.Mapping.field_id;
                    par2.Add("@value", typeof(String)).Value = a.Value;

                    database.ExecuteNonQuery( "insert into entity_field (entity_id, field_id, value) values(@user_id, @field_id, @value)", CommandType.Text, par2, trans);
                }

                database.AddUserLog( LogKey.User_PropertyChanged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userId, 0, "Properties changed", tLog.ToString(), Acl.EntityId, trans);

                trans.Commit();
                trans = null;
            }
            catch (Exception ex)
            {
                if (trans != null)
                    trans.Rollback();
                trans = null;

                Error(ErrorType.InvalidRequest, "Error on process: " + ex.Message, "", null);
                return null;
            }
            finally
            {
                tLog = null;
            }

            if (userId != 0)
            {
                Dictionary<String, Object> newData = new Dictionary<String, Object>();
                newData.Add("userid", userId);
                return get(database, newData);
            }
            else
            {
                return null;
            }

        }



        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> resetpwd(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }


            String user = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(user))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(user);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable( "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 and  c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return null;
            }


            Boolean mustChange = false;

            if ((parameters.ContainsKey("must_change")) && (parameters["must_change"] is Boolean) && ((Boolean)parameters["must_change"]))
                mustChange = true;


            String pwdMethod = "random";
            String pwdValue = "";

            using (DataTable dtRules = database.ExecuteDataTable( "select password_rule from context c with(nolock) where c.id = " + dtUsers.Rows[0]["context_id"].ToString() + " and (c.password_rule is not null and rtrim(LTRIM(c.password_rule)) <> '')"))
            {
                if ((dtRules != null) && (dtRules.Rows.Count > 0))
                {
                    String v = dtRules.Rows[0]["password_rule"].ToString().Trim();

                    if (v.IndexOf("[") != -1)
                    {
                        Regex rex = new Regex(@"(.*?)\[(.*?)\]");
                        Match m = rex.Match(v);
                        if (m.Success)
                        {
                            pwdMethod = m.Groups[1].Value.ToLower();
                            pwdValue = m.Groups[2].Value;
                        }
                    }
                    else
                    {
                        pwdMethod = v;
                    }
                }
            }

            switch (pwdMethod)
            {
                case "default":
                    //Nada a senha ja foi definida
                    break;

                case "field":
                    Int64 fieldId = 0;
                    Int64.TryParse(pwdValue, out fieldId);
                    using (DataTable dtFields = database.ExecuteDataTable( "select * from identity_field ife with(nolock) inner join entity e with(nolock) on ife.entity_id = e.id where e.id = " + dtUsers.Rows[0]["id"].ToString() + " and ife.field_id = " + fieldId))
                        if ((dtFields != null) && (dtFields.Rows.Count > 0))
                        {
                            pwdValue = dtFields.Rows[0]["value"].ToString();
                        }
                    break;

                default: //Random
                    pwdValue = "";
                    break;
            }

            //Se a senha continua vazia, gera uma randômica
            if ((pwdValue == null) || (pwdValue == ""))
            {
                pwdValue = Password.RandomPassword.Generate(14, 16);
                pwdMethod = "random";
            }

            
            String pwd = "";
            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(database.Connection, this._enterpriseId))
            using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(pwdValue)))
                pwd = Convert.ToBase64String(cApi.ToBytes());


            database.ExecuteNonQuery( "update entity set password = '" + pwd + "', must_change_password = "+ (mustChange ? "1" : "0") +" where id = " + userid, CommandType.Text,null,  null);

            database.AddUserLog( LogKey.User_PasswordReseted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "Password reseted", "New password: " + pwdValue + "\r\nUser " + (mustChange ? "" : "not") + " must change password on next logon.", Acl.EntityId);

            database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + userid + ")", CommandType.Text,null,  null);

            result.Add("success", true);
            result.Add("method", pwdMethod);

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.logs'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> logs(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }


            String user = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(user))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(user);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable( "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
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

            DataRow dr = dtUsers.Rows[0];

            Dictionary<String, Object> newItem = new Dictionary<string, object>();
            newItem.Add("login", dr["login"]);
            newItem.Add("full_name", dr["full_name"]);

            result.Add("info", newItem);

            List<Object> tmpItem = new List<Object>();

            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT";
            sql += "    ROW_NUMBER() OVER (ORDER BY l.date desc) AS [row_number], l.*, res.name resource_name";
            sql += "    from logs l with(nolock) left join [identity] i with(nolock) on i.id = l.identity_id left join resource res with(nolock) on res.id = l.resource_id";
            sql += "  WHERE";
            sql += "    l.entity_id = " + userid;


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

            //DataTable dtLogs = database.ExecuteDataTable( "select l.*, res.name resource_name from logs l with(nolock) left join [identity] i with(nolock) on i.id = l.identity_id left join resource res with(nolock) on res.id = l.resource_id where l.entity_id = " + userid + " order by l.date desc");
            DataTable dtLogs = database.ExecuteDataTable( sql);
            if ((dtLogs != null) || (dtLogs.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtLogs.Rows)
                {
                    newItem = new Dictionary<string, object>();
                    newItem.Add("log_id", dr1["id"]);
                    newItem.Add("date", (Int32)((((DateTime)dr1["date"]) - new DateTime(1970, 1, 1)).TotalSeconds));
                    newItem.Add("source", dr1["source"]);
                    newItem.Add("level", dr1["level"]);
                    newItem.Add("identity_id", dr1["identity_id"]);
                    newItem.Add("resource_name", dr1["resource_name"]);
                    newItem.Add("text", dr1["text"]);
                    newItem.Add("additional_data", dr1["additional_data"]);

                    tmpItem.Add(newItem);
                }

            }


            result.Add("logs", tmpItem);


            return result;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.deploy'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        /// <param name="locked">Usuário bloqueado?</param>
        /// <param name="deployOnly">Somente deploy</param>
        private Dictionary<String, Object> deploy(IAMDatabase database, Dictionary<String, Object> parameters, Boolean locked, Boolean deployOnly)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }


            String user = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(user))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(user);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable( "select e.* from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return null;
            }

            if (!deployOnly)
            {
                database.ExecuteNonQuery( "update entity set locked = " + (locked ? "1" : "0") + " where id = " + userid, CommandType.Text,null,  null);

                database.AddUserLog( (locked ? LogKey.User_Locked : LogKey.User_Unlocked), null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "User " + (locked ? "locked" : "unlocked") + " through API", "", Acl.EntityId);

            }

            database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + userid + ")", CommandType.Text,null,  null);
            database.AddUserLog( LogKey.User_DeployMark, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "User data marked for replication through API", "", Acl.EntityId);


            result.Add("success", true);

            return result;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.delete' e 'user.undelete'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        /// <param name="locked">Usuário bloqueado?</param>
        /// <param name="deployOnly">Somente deploy</param>
        private Dictionary<String, Object> delete(IAMDatabase database, Dictionary<String, Object> parameters, Boolean delete = false)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }


            String user = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(user))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(user);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable( "select e.* from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }else if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return null;
            }
            else if ((Boolean)dtUsers.Rows[0]["deleted"])
            {
                Error(ErrorType.InvalidRequest, "User already deleted.", "", null);
                return null;
            }

            database.ExecuteNonQuery( "update entity set deleted = " + (delete ? "1" : "0") + ", deleted_date = " + (delete ? "getdate()" : " null") + " where id = " + userid, CommandType.Text,null,  null);

            database.AddUserLog( (delete ? LogKey.User_Deleted : LogKey.User_Undeleted), null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "User " + (delete ? "deleted" : "undeleted") + " through API", "", Acl.EntityId);

            database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + userid + ")", CommandType.Text,null,  null);

            result.Add("success", true);

            return result;

        }

        /// <summary>
        /// Método privado para processamento do método 'user.get'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteidentity(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
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

            if (!parameters.ContainsKey("identityid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter identityid is not defined.", "", null);
                return false;
            }


            String identity = parameters["identityid"].ToString();
            if (String.IsNullOrWhiteSpace(identity))
            {
                Error(ErrorType.InvalidRequest, "Parameter identityid is not defined.", "", null);
                return false;
            }

            Int64 identityid = 0;
            try
            {
                identityid = Int64.Parse(identity);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter identityid is not a long integer.", "", null);
                return false;
            }


            Int64 entityId = 0;
            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;
            par.Add("@identity_id", typeof(Int64)).Value = identityid;

            DataTable dtUsers = database.ExecuteDataTable( "select e.* from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and e.id = @user_id and identity_id = @identity_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return false;
            }

            try
            {
                entityId = Int64.Parse(dtUsers.Rows[0]["id"].ToString());
            }
            catch { }


            if (entityId == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return false;
            }


            database.ExecuteNonQuery( "delete from [identity] where id = @identity_id", CommandType.Text, par);

            DbParameterCollection par2 = new DbParameterCollection();
            par2.Add("@entity_id", typeof(Int64)).Value = entityId;

            database.ExecuteNonQuery("sp_rebuild_entity_keys2", CommandType.StoredProcedure, par2, null);


            return true;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.get'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean unlockidentity(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
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

            if (!parameters.ContainsKey("identityid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter identityid is not defined.", "", null);
                return false;
            }


            String identity = parameters["identityid"].ToString();
            if (String.IsNullOrWhiteSpace(identity))
            {
                Error(ErrorType.InvalidRequest, "Parameter identityid is not defined.", "", null);
                return false;
            }

            Int64 identityid = 0;
            try
            {
                identityid = Int64.Parse(identity);

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter identityid is not a long integer.", "", null);
                return false;
            }



            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;
            par.Add("@identity_id", typeof(Int64)).Value = identityid;

            DataTable dtUsers = database.ExecuteDataTable( "select e.* from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and e.id = @user_id and identity_id = @identity_id", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return false;
            }

            SqlTransaction trans = (SqlTransaction)database.BeginTransaction();
            try
            {
                database.ExecuteNonQuery( "update [identity] set temp_locked = 0 where id = @identity_id",  CommandType.Text,par, trans);
                database.ExecuteNonQuery( "insert into identity_acl_ignore (identity_id) values(@identity_id)", CommandType.Text,par,  trans);
                
                trans.Commit();
            }
            catch(Exception ex) {
                trans.Rollback();
                throw ex;
            }
            return true;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.get'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String,Object> get(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<String, Object>();

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }


            String user = parameters["userid"].ToString();
            if (String.IsNullOrWhiteSpace(user))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return null;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(user);

            }
            catch {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return null;
            }


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;

            DataTable dtUsers = database.ExecuteDataTable( "select e.*, identity_qty = (select COUNT(distinct i.id) from [identity] i with(nolock) where i.entity_id = e.id) from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and e.id = @user_id order by e.resource_name, e.name, e.value", CommandType.Text, par, null);
            if (dtUsers == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return null;
            }

            if (dtUsers.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return null;
            }

            DataRow dr = dtUsers.Rows[0];

            Dictionary<String, Object> newItem = new Dictionary<string, object>();
            newItem.Add("userid", dr["id"]);
            newItem.Add("alias", dr["alias"]);
            newItem.Add("login", dr["login"]);
            newItem.Add("context_id", dr["context_id"]);
            newItem.Add("locked", dr["locked"]);
            newItem.Add("full_name", dr["full_name"]);
            newItem.Add("create_date", (Int32)((((DateTime)dr["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds));
            newItem.Add("change_password", (dr["change_password"] != DBNull.Value ? (Int32)((((DateTime)dr["change_password"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("last_login", (dr["last_login"] != DBNull.Value ? (Int32)((((DateTime)dr["last_login"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
            newItem.Add("must_change_password", dr["must_change_password"]);
            newItem.Add("identity_qty", dr["identity_qty"]);
            newItem.Add("container_id", dr["container_id"]);

            result.Add("info", newItem);

            DataTable dtGeneral = database.ExecuteDataTable( "select top 1 e.id enterprise_id, e.name enterprise_name, c.id context_id, c.name context_name from enterprise e with(nolock) inner join context c with(nolock) on e.id = c.enterprise_id where c.id = " + dr["context_id"]);
            if ((dtGeneral != null) || (dtGeneral.Rows.Count > 0))
            {
                newItem = new Dictionary<string, object>();
                newItem.Add("enterprise_name", dtGeneral.Rows[0]["enterprise_name"]);
                newItem.Add("context_name", dtGeneral.Rows[0]["context_name"]);
                newItem.Add("container_path", Container.getPath(database, this._enterpriseId, (Int64)dr["container_id"], true));

                result.Add("general", newItem);
            }
            

            List<Object> tmpItem = new List<object>();
            foreach (DataRow dr1 in dtUsers.Rows)
            {

                newItem = new Dictionary<string, object>();
                newItem.Add("resource_name", dr1["resource_name"]);
                newItem.Add("name", dr1["name"]);
                newItem.Add("field_id", (Int64)dr1["field_id"]);
                newItem.Add("value", dr1["value"]);

                tmpItem.Add(newItem);

            }

            result.Add("properties", tmpItem);

            tmpItem = new List<object>();
            DataTable dtRoles = database.ExecuteDataTable( "select resource_name, identity_id, name from vw_entity_roles where id = " + userid);
            if ((dtRoles != null) || (dtRoles.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtRoles.Rows)
                {
                    newItem = new Dictionary<string, object>();
                    newItem.Add("resource_name", dr1["resource_name"]);
                    newItem.Add("name", dr1["name"]);

                    tmpItem.Add(newItem);
                }

            }
            result.Add("roles", tmpItem);

            tmpItem = new List<object>();
            DataTable dtIdentities = database.ExecuteDataTable( "select i.*, r.name resource_name from [identity] i with(nolock) inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id inner join resource r with(nolock) on rp.resource_id = r.id where i.deleted = 0 and i.entity_id = " + userid);
            if ((dtIdentities != null) || (dtIdentities.Rows.Count > 0))
            {

                foreach (DataRow dr1 in dtIdentities.Rows)
                {
                    newItem = new Dictionary<string, object>();
                    newItem.Add("identity_id", dr1["id"]);
                    newItem.Add("temp_locked", (Boolean)dr1["temp_locked"]);
                    newItem.Add("resource_name", dr1["resource_name"]);
                    newItem.Add("create_date", (dr["create_date"] != DBNull.Value ? (Int32)((((DateTime)dr["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));

                    tmpItem.Add(newItem);
                }

            }
            result.Add("identities", tmpItem);
            
            return result;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.search'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private List<Dictionary<String, Object>> search(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            String text = "";

            if (parameters.ContainsKey("text"))
                text = (String)parameters["text"];

            if (String.IsNullOrWhiteSpace(text))
                text = "";

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@text", typeof(String)).Value = text;

            Boolean deleted = false;
            if ((parameters.ContainsKey("deleted")) && (parameters["deleted"] is Boolean))
                deleted = (Boolean)parameters["deleted"];


            List<String> additional_fields = new List<String>();

            if ((parameters.ContainsKey("additional_field")) && (parameters["additional_field"] is String) && (!String.IsNullOrWhiteSpace((String)parameters["additional_field"])))
                additional_fields.AddRange(((String)parameters["additional_field"]).Split(",".ToCharArray()));

            DataTable addFields = null;
            List<Int64> fieldsEntities = new List<Int64>();
            if (additional_fields.Count > 0)
            {
                addFields = database.ExecuteDataTable( "select id from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and name in ('" + String.Join("','", additional_fields) + "') and e.value like '%'+@text+'%'", CommandType.Text, par, null);
                if ((addFields != null) && (addFields.Rows.Count >= 0))
                {
                    foreach (DataRow dr in addFields.Rows)
                        fieldsEntities.Add((Int64)dr["id"]);
                }
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

            String sql = "";
            sql += "WITH result_set AS (";
            sql += "  SELECT";
            sql += "    ROW_NUMBER() OVER (ORDER BY e.full_name) AS [row_number], e.*, c.name context_name";
            sql += "    , identity_qty = (select COUNT(distinct i.id) from [identity] i with(nolock) where i.entity_id = e.id)";
            sql += "    from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id";
            sql += "  WHERE ";
            sql += " ((" + (deleted ? "" : "e.deleted = 0 and") + " c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and (e.full_name like '%'+@text+'%' or e.login like '%'+@text+'%')") + ")";
            
            if (fieldsEntities.Count > 0)
                sql += " OR e.id in ("+ String.Join(",",fieldsEntities) +")";

            sql += ")";

            if ((parameters.ContainsKey("filter")) && (parameters["filter"] is Dictionary<String, Object>))
            {
                Dictionary<String, Object> filter = (Dictionary<String, Object>)parameters["filter"];
                foreach (String k in filter.Keys)
                    switch (k.ToLower())
                    {
                        case "contextid":
                            try
                            {
                                sql += " and c.id = " + Int64.Parse(filter[k].ToString()).ToString();
                            }
                            catch { }
                            break;

                        case "containerid":
                            try
                            {
                                sql += " and exists (select 1 from entity_container ec with(nolock) where ec.entity_id = e.id and ec.container_id = " + Int64.Parse(filter[k].ToString()).ToString() + ") ";
                            }
                            catch { }
                            break;
                    }
            }


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
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return null;
            }

            foreach (DataRow dr in dtUsers.Rows)
            {
                Dictionary<String, Object> newItem = new Dictionary<string, object>();
                newItem.Add("userid", dr["id"]);
                newItem.Add("alias", dr["alias"]);
                newItem.Add("login", dr["login"]);
                newItem.Add("full_name", dr["full_name"]);
                newItem.Add("context_name", dr["context_name"]);
                newItem.Add("create_date", (Int32)((((DateTime)dr["create_date"]) - new DateTime(1970, 1, 1)).TotalSeconds));
                newItem.Add("change_password", (dr["change_password"] != DBNull.Value ? (Int32)((((DateTime)dr["change_password"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                newItem.Add("last_login", (dr["last_login"] != DBNull.Value ? (Int32)((((DateTime)dr["last_login"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                newItem.Add("must_change_password", dr["must_change_password"]);
                newItem.Add("locked", dr["locked"]);
                newItem.Add("identity_qty", dr["identity_qty"]);

                if (fieldsEntities.Count > 0)
                {
                    addFields = database.ExecuteDataTable( "select name, resource_name, value from vw_entity_all_data e with(nolock) where id = " + dr["id"] + " and e.enterprise_id = @enterprise_id and name in ('" + String.Join("','", additional_fields) + "') and e.value like '%'+@text+'%'", CommandType.Text, par, null);
                    if ((addFields != null) && (addFields.Rows.Count >= 0))
                    {
                        List<Dictionary<String, Object>> lst = new List<Dictionary<string, object>>();
                        foreach (DataRow dr2 in addFields.Rows)
                        {
                            Dictionary<String, Object> newAddItem = new Dictionary<string, object>();

                            newAddItem.Add("resource_name", dr2["resource_name"]);
                            newAddItem.Add("name", dr2["name"]);
                            newAddItem.Add("value", dr2["value"]);

                            lst.Add(newAddItem);
                        }

                        newItem.Add("additional_data", lst);
                    }
                }

                result.Add(newItem);
            }

            return result;

        }
        
        /// <summary>
        /// Método privado para processamento do método 'user.changepassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> changepassword(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<string, object>();


            if (!parameters.ContainsKey("user") && !parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter user and userid is not defined.", "", null);
                return null;
            }

            String user = (parameters.ContainsKey("user") ? parameters["user"].ToString() : "");
            Int64 userid = 0;
            if (parameters.ContainsKey("userid"))
            {
                try
                {
                    userid = Int64.Parse(parameters["userid"].ToString());
                }
                catch
                {
                    Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                    return null;
                }
            }

            if (String.IsNullOrWhiteSpace(user) && userid <= 0)
            {
                Error(ErrorType.InvalidRequest, "Parameter user and userid is wrong.", "", null);
                return null;
            }

            if (!parameters.ContainsKey("password"))
            {
                Error(ErrorType.InvalidRequest, "Parameter password is not defined.", "", null);
                return null;
            }

            String password = parameters["password"].ToString();


            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;
            par.Add("@user", typeof(String)).Value = user;

            DataTable c = database.ExecuteDataTable( "select distinct e.id from vw_entity_ids e inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and " + (userid > 0 ? "e.id = @user_id " : "e.value = @user"), CommandType.Text, par, null);
            if ((c != null) && (c.Rows.Count > 0))
            {
                if (c.Rows.Count == 1)
                {

                    UserPasswordStrength usrCheck = new UserPasswordStrength(database.Connection, (Int64)c.Rows[0]["id"]);
                    UserPasswordStrengthResult check = usrCheck.CheckPassword(password);
                    if (check.HasError)
                    {

                        String txt = "* Number of Characters: " + (!check.LengthError ? "OK" : "Fail") + ", ";
                        txt += "* Uppercase Letters:  " + (!check.UpperCaseError ? "OK" : "Fail") + ", ";
                        txt += "* Lowercase Letters: " + (!check.LowerCaseError ? "OK" : "Fail") + ", ";
                        txt += "* Numbers: " + (!check.DigitError ? "OK" : "Fail") + ", ";
                        txt += "* Symbols:  " + (!check.SymbolError ? "OK" : "Fail");

                        Dictionary<String, Object> addRet = new Dictionary<string, object>();
                        addRet.Add("text", txt);

                        addRet.Add("number_char", check.LengthError);
                        addRet.Add("uppercase", check.UpperCaseError);
                        addRet.Add("lowercase", check.LowerCaseError);
                        addRet.Add("numbers", check.DigitError);
                        addRet.Add("symbols", check.SymbolError);
                        addRet.Add("name_part", check.NameError);

                        Error(ErrorType.InvalidRequest, "Passwords must meet complexity requirements.", "", addRet);

                        return null;
                    }
                    else
                    {

                        Boolean mustChange = false;

                        if ((parameters.ContainsKey("must_change")) && (parameters["must_change"] is Boolean) && ((Boolean)parameters["must_change"]))
                            mustChange = true;

                        using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(database.Connection, this._enterpriseId))
                        using (CryptApi cApi1 = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(password)))
                        {
                            DbParameterCollection pPar = new DbParameterCollection();
                            String b64 = Convert.ToBase64String(cApi1.ToBytes());
                            pPar.Add("@password", typeof(String)).Value = b64;


                            database.ExecuteNonQuery( "update entity set password = @password, change_password = getdate(), last_login = getdate(), recovery_code = null, " + (mustChange ? " must_change_password = 1 " : " must_change_password = 0 ") + " where id = " + c.Rows[0]["id"], CommandType.Text, pPar);
                        }



                        String sPs = "";
                        try
                        {
                            sPs += "Length = " + password.Length + Environment.NewLine;
                            sPs += "Contains Uppercase? " + check.HasUpperCase + Environment.NewLine;
                            sPs += "Contains Lowercase? " + check.HasLowerCase + Environment.NewLine;
                            sPs += "Contains Symbol? " + check.HasSymbol + Environment.NewLine;
                            sPs += "Contains Number? " + check.HasDigit + Environment.NewLine;
                            sPs += "Contains part of the name/username? " + check.HasNamePart + Environment.NewLine;

                        }
                        catch { }


                        database.AddUserLog(LogKey.User_PasswordChanged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)c.Rows[0]["id"], 0, "Password changed", "Password changed through API" + Environment.NewLine + sPs, Acl.EntityId);

                        //Cria o pacote com os dados atualizados deste usuário 
                        //Este processo vija agiliar a aplicação das informações pelos plugins

                        database.ExecuteNonQuery( "insert into deploy_now (entity_id) values(" + c.Rows[0]["id"] + ")", CommandType.Text, null, null);

                        result.Add("success", true);

                    }
                }
                else
                {
                    //has too many users with id blabla
                    Error(ErrorType.InvalidRequest, "Has too many users with " + (userid > 0 ? "id '" + userid + "'" : "username '" + parameters["user"] + "'") + ".", "", null);
                    return null;
                }
            }
            else
            {
                Error(ErrorType.InvalidRequest, (userid > 0 ? "User id '" + userid + "'" : "User '" + parameters["user"] + "'") + " not found.", "", null);
                return null;
            }
            

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.logout'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="auth">String de autenticação</param>
        private Dictionary<String, Object> logout(IAMDatabase database, String auth)
        {
            database.ExecuteNonQuery( String.Format("delete from entity_auth where auth_key = '{0}'", auth), CommandType.Text, null);
            
            Dictionary<String, Object> result = new Dictionary<string, object>();
            result.Add("success", true);

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.auth'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> authUser(IAMDatabase database, Dictionary<String, Object> parameters)
        {
            Dictionary<String, Object> result = new Dictionary<string, object>();

            if (!parameters.ContainsKey("user") && !parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter user and userid is not defined.", "", null);
                return null;
            }

            String user = (parameters.ContainsKey("user") ? parameters["user"].ToString() : "");
            Int64 userid = 0;
            if (parameters.ContainsKey("userid"))
            {
                try
                {
                    userid = Int64.Parse(parameters["userid"].ToString());
                }
                catch
                {
                    Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                    return null;
                }
            }

            if (String.IsNullOrWhiteSpace(user) && userid <= 0)
            {
                Error(ErrorType.InvalidRequest, "Parameter user and userid is wrong.", "", null);
                return null;
            }

            if (!parameters.ContainsKey("md5_password"))
            {
                Error(ErrorType.InvalidRequest, "Parameter md5_password is not defined.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user_id", typeof(Int64)).Value = userid;
            par.Add("@user", typeof(String)).Value = user;

            DataTable tmp = database.ExecuteDataTable( "select * from vw_entity_logins where enterprise_id = @enterprise_id and (id = @user_id or (login = @user or value = @user))", CommandType.Text, par, null);
            if ((tmp == null) || (tmp.Rows.Count == 0))
            {
                Error(ErrorType.InvalidParameters, "User not found.", "", null);
                return null;
            }

            //Caso haja mesmo login em contextos diferentes verifica todos eles
            foreach (DataRow dr in tmp.Rows)
            {
                String md5Pass = "";
                using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(database.Connection, this._enterpriseId))
                using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(tmp.Rows[0]["password"].ToString())))
                    md5Pass = IAM.CA.CATools.MD5Checksum(cApi.clearData);

                if ((!String.IsNullOrWhiteSpace(md5Pass)) && md5Pass == parameters["md5_password"].ToString())
                {
                    if ((Boolean)tmp.Rows[0]["locked"])
                    {
                        database.AddUserLog( LogKey.User_AccessDenied, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Fail on user/password auth through API (user locked)", "", Acl.EntityId);
                        continue;
                    }

                    result.Add("userid", tmp.Rows[0]["id"]);
                    result.Add("login", tmp.Rows[0]["login"]);
                    result.Add("must_change", tmp.Rows[0]["must_change_password"]);

                    List<Object> tmpItem = new List<Object>();
                    DataTable dtRoles = database.ExecuteDataTable( "select resource_name, identity_id, name from vw_entity_roles where id = " + tmp.Rows[0]["id"]);
                    if ((dtRoles != null) || (dtRoles.Rows.Count > 0))
                    {

                        foreach (DataRow dr1 in dtRoles.Rows)
                        {
                            Dictionary<string, object> newItem = new Dictionary<string, object>();
                            newItem.Add("resource_name", dr1["resource_name"]);
                            newItem.Add("name", dr1["name"]);

                            tmpItem.Add(newItem);
                        }

                    }

                    result.Add("roles", tmpItem);

                    result.Add("success", true);

                    database.AddUserLog( LogKey.User_Logged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Success on user/password auth through API", "", Acl.EntityId);

                    return result;
                }
                else
                {
                    database.AddUserLog(LogKey.User_WrongPassword, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Fail on user/password auth through API (wrong password)", "MD5 Password hash: " + parameters["md5_password"].ToString(), Acl.EntityId);
                }
            }

            //Nenhum dos usuários deu match na senha então retorna este erro
            Error(ErrorType.InvalidParameters, "Password is incorrect.", "", null);
            return null;

        }

        /// <summary>
        /// Método privado para processamento do método 'user.login'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> login(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("user") || !parameters.ContainsKey("password"))
            {
                Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user", typeof(String)).Value = parameters["user"];

            DataTable tmp = database.ExecuteDataTable( "select * from vw_entity_logins where deleted = 0 and enterprise_id = @enterprise_id and (login = @user or value = @user)", CommandType.Text, par, null);
            if ((tmp == null) || (tmp.Rows.Count == 0))
            {
                Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                return null;
            }
            else if ((Boolean)tmp.Rows[0]["locked"])
            {
                database.AddUserLog( LogKey.User_AccessDenied, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Error generating session id through API (user locked)", "", Acl.EntityId);

                Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                return null;
            }

            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(database.Connection, this._enterpriseId))
            using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(tmp.Rows[0]["password"].ToString())))
                if (Encoding.UTF8.GetString(cApi.clearData) == parameters["password"].ToString())
                {

                    Boolean userData = false;
                    if ((parameters.ContainsKey("userData")) && (parameters["userData"] is Boolean))
                        userData = (Boolean)parameters["userData"];

                    //Retorna a chame de autenticação válida
                    //Adiciona a chave de autenticação caso não exista
                    par = new DbParameterCollection();
                    par.Add("@entityId", typeof(Int64)).Value = (Int64)tmp.Rows[0]["id"];

                    DataTable dtUserKey = database.ExecuteDataTable( "[sp_new_auth_key]", CommandType.StoredProcedure, par);
                    if ((dtUserKey == null) || (dtUserKey.Rows.Count == 0))
                    {
                        database.AddUserLog( LogKey.API_Error, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Error generating session id through API: Return of sp_new_auth_key " + (dtUserKey == null ? "is null" : "has no rows"), "", Acl.EntityId);

                        Error(ErrorType.InvalidParameters, "Error generating session id.", "", null);
                        return null;
                    }

                    database.ExecuteNonQuery( "update entity set last_login = getdate() where id = " + tmp.Rows[0]["id"], CommandType.Text, null);
                    database.AddUserLog( LogKey.User_Logged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "User logged through API", "");

                    Dictionary<String, Object> result = new Dictionary<string, object>();
                    
                    if (parameters.ContainsKey("userData") && (parameters["userData"] is Boolean) && ((Boolean)parameters["userData"]))
                    {
                        result.Add("userid", dtUserKey.Rows[0]["id"]);
                        result.Add("alias", dtUserKey.Rows[0]["alias"]);
                        result.Add("login", dtUserKey.Rows[0]["login"]);
                        result.Add("full_name", dtUserKey.Rows[0]["full_name"]);
                        result.Add("create_date", (Int32)((((DateTime)dtUserKey.Rows[0]["create_date"]) - new DateTime(1970,1,1)).TotalSeconds));
                        result.Add("change_password", (dtUserKey.Rows[0]["change_password"] != DBNull.Value ? (Int32)((((DateTime)dtUserKey.Rows[0]["change_password"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                        result.Add("must_change_password", dtUserKey.Rows[0]["must_change_password"]);
                    }

                    result.Add("sessionid", dtUserKey.Rows[0]["auth_key"]);
                    result.Add("create_time", (dtUserKey.Rows[0]["start_date"] != DBNull.Value ? (Int32)((((DateTime)dtUserKey.Rows[0]["start_date"]) - new DateTime(1970, 1, 1)).TotalSeconds) : 0));
                    result.Add("expires", (dtUserKey.Rows[0]["end_date"] != DBNull.Value ? (Int32)((((DateTime)dtUserKey.Rows[0]["end_date"]) - ((DateTime)dtUserKey.Rows[0]["start_date"])).TotalSeconds) : 0));
                    result.Add("success", true);

                    return result;
                }
                else
                {
                    database.AddUserLog(LogKey.User_WrongPassword, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Fail on user login through API (wrong password)", "Password: " + parameters["password"].ToString());

                    Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                    return null;
                }

        }


        /// <summary>
        /// Método privado para processamento do método 'user.resetpassword'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean accessrequest(IAMDatabase database, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("workflowid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not defined.", "", null);
                return false;
            }

            Int64 workflowid = 0;
            try
            {
                workflowid = Int64.Parse(parameters["workflowid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter workflowid is not a long integer.", "", null);
                return false;
            }

            if (!parameters.ContainsKey("userid"))
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not defined.", "", null);
                return false;
            }

            Int64 userid = 0;
            try
            {
                userid = Int64.Parse(parameters["userid"].ToString());

            }
            catch
            {
                Error(ErrorType.InvalidRequest, "Parameter userid is not a long integer.", "", null);
                return false;
            }


            String description = parameters["description"].ToString();
            if (String.IsNullOrWhiteSpace(description))
            {
                Error(ErrorType.InvalidRequest, "Parameter description is not defined.", "", null);
                return false;
            }

            //Previnir injection
            description = System.Web.HttpUtility.HtmlEncode(description);

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@workflow_id", typeof(Int64)).Value = workflowid;

            DataTable dtPlugin = database.ExecuteDataTable("select w.id from st_workflow w with(nolock) inner join context c with(nolock) on c.id = w.context_id where c.enterprise_id = @enterprise_id and w.id = @workflow_id", CommandType.Text, par, null);
            if (dtPlugin == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtPlugin.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "Access workflow not found.", "", null);
                return false;
            }

            DataTable dtUser = database.ExecuteDataTable("select e.* from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = " + userid, CommandType.Text, par, null);
            if (dtUser == null)
            {
                Error(ErrorType.InternalError, "", "", null);
                return false;
            }

            if (dtUser.Rows.Count == 0)
            {
                Error(ErrorType.InvalidRequest, "User not found.", "", null);
                return false;
            }

            WorkflowConfig workflow = null;
            try
            {
                workflow = new WorkflowConfig();
                workflow.GetDatabaseData(database, (Int64)dtPlugin.Rows[0]["id"]);
            }
            catch(Exception ex)
            {
                Error(ErrorType.SystemError, "Fail on get workflow config", ex.Message, null);
                return false;
            }

            Object trans = database.BeginTransaction();
            Int64 requestId = 0;
            try
            {

                //Resgata somente a primeira atividade, para seguir o fluxo de aprovação
                WorkflowActivity activity = null;
                if (workflow.Activities != null)
                    foreach (WorkflowActivity act in workflow.Activities)
                    {
                        if (activity == null)
                            activity = act;

                        if (act.ExeutionOrder < activity.ExeutionOrder)
                            activity = act;
                    }

                if (activity == null)
                    throw new Exception("Activity is empty");

                using (DbParameterCollection par2 = new DbParameterCollection())
                {
                    par2.Add("@entity_id", typeof(Int64)).Value = userid;
                    par2.Add("@workflow_id", typeof(Int64)).Value = workflow.WorkflowId;
                    par2.Add("@create_date", typeof(DateTime)).Value = DateTime.Now;
                    par2.Add("@status", typeof(Int32)).Value = (Int32)WorkflowRequestStatus.Waiting;
                    par2.Add("@description", typeof(String)).Value = description;
                    par2.Add("@start_date", typeof(DateTime)).Value = new DateTime(1970, 1, 1);
                    par2.Add("@end_date", typeof(DateTime)).Value = new DateTime(1970, 1, 1);

                    requestId = database.ExecuteScalar<Int64>("INSERT INTO [st_workflow_request] ([entity_id],[workflow_id],[create_date],[status],[description],[start_date],[end_date]) VALUES(@entity_id,@workflow_id,@create_date,@status,@description,@start_date,@end_date); SELECT SCOPE_IDENTITY()", CommandType.Text, par2, trans);

                    par2.Clear();
                    par2.Add("@workflow_request_id", typeof(Int64)).Value = requestId;
                    par2.Add("@status", typeof(String)).Value = (Int32)WorkflowRequestStatus.Waiting;
                    par2.Add("@description", typeof(String)).Value = "Aguardando análise";
                    par2.Add("@activity_id", typeof(Int64)).Value = activity.ActivityId;
                    par2.Add("@executed_by_entity_id", typeof(Int64)).Value = userid;

                    database.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, trans);
                }

                try
                {
                    Dictionary<Int64, List<String>> mails = new Dictionary<long, List<string>>();

                    //Owner do workflow
                    DataTable dtUserMails = database.ExecuteDataTable("select distinct entity_id, mail, full_name from vw_entity_mails where entity_id in (" + workflow.Owner + ")", CommandType.Text, null, trans);
                    if ((dtUserMails != null) && (dtUserMails.Rows.Count > 0))
                        foreach (DataRow dr in dtUserMails.Rows)
                            try
                            {
                                MailAddress m = new MailAddress(dr["mail"].ToString());

                                if (!mails.ContainsKey((Int64)dr["entity_id"]))
                                    mails.Add((Int64)dr["entity_id"], new List<string>());

                                mails[(Int64)dr["entity_id"]].Add(m.Address);
                            }
                            catch { }


                    if (activity != null)
                        if ((activity.ManualApproval != null) && ((activity.ManualApproval.EntityApprover > 0) || (activity.ManualApproval.RoleApprover > 0)))
                        {
                            dtUserMails = database.ExecuteDataTable("select distinct entity_id, mail, full_name from vw_entity_mails where entity_id in (" + activity.ManualApproval.EntityApprover + ") or entity_id in (select i.entity_id from identity_role ir with(nolock) inner join [identity] i with(nolock) on i.id = ir.identity_id where ir.role_id = " + activity.ManualApproval.RoleApprover + ")", CommandType.Text, null, trans);
                            if ((dtUserMails != null) && (dtUserMails.Rows.Count > 0))
                                foreach (DataRow dr in dtUserMails.Rows)
                                    try
                                    {
                                        MailAddress m = new MailAddress(dr["mail"].ToString());

                                        if (!mails.ContainsKey((Int64)dr["entity_id"]))
                                            mails.Add((Int64)dr["entity_id"], new List<string>());

                                        mails[(Int64)dr["entity_id"]].Add(m.Address);
                                    }
                                    catch { }
                        }


                    if (mails.Count > 0)
                    {
                        foreach (Int64 admin_id in mails.Keys)
                            try
                            {
                                Dictionary<String, String> vars = new Dictionary<string, string>();
                                vars.Add("workflow_name", workflow.Name);
                                vars.Add("user_name", dtUser.Rows[0]["full_name"].ToString());
                                vars.Add("user_login", dtUser.Rows[0]["login"].ToString());
                                vars.Add("user_id", dtUser.Rows[0]["id"].ToString());
                                vars.Add("admin_id", admin_id.ToString());
                                vars.Add("description", description);
                                vars.Add("approval_link", "%enterprise_uri%/admin/access_request/" + requestId + "/allow/");
                                vars.Add("deny_link", "%enterprise_uri%/admin/access_request/" + requestId + "/deny/");

                                MessageBuilder msgAdm = MessageBuilder.BuildFromTemplate(database, this._enterpriseId, "access_request_admin", String.Join(",", mails[admin_id]), vars, trans);
                                msgAdm.SaveToDb(database, trans);
                            }
                            catch { }
                    }
                }
                catch { }

                try
                {
                    //E-mail para o usuário
                    DataTable dtUserMails = database.ExecuteDataTable("select distinct mail from vw_entity_mails where entity_id = " + userid, CommandType.Text, null, trans);
                    if ((dtUserMails != null) && (dtUserMails.Rows.Count > 0))
                    {
                        List<String> mails = new List<string>();

                        foreach (DataRow dr in dtUserMails.Rows)
                        {
                            try
                            {
                                MailAddress m = new MailAddress(dr["mail"].ToString());
                                mails.Add(m.Address);
                            }
                            catch { }
                        }

                        if (mails.Count > 0)
                        {

                            Dictionary<String, String> vars = new Dictionary<string, string>();
                            vars.Add("workflow_name", workflow.Name);
                            vars.Add("user_name", dtUser.Rows[0]["full_name"].ToString());
                            vars.Add("user_login", dtUser.Rows[0]["login"].ToString());
                            vars.Add("user_id", dtUser.Rows[0]["id"].ToString());

                            String steps = "";

                            workflow.Activities.Sort(delegate(WorkflowActivity a1, WorkflowActivity a2) { return a1.ExeutionOrder.CompareTo(a2.ExeutionOrder); });

                            steps += "<p style=\"padding-left: 30px;\"><table style=\"table-layout: fixed; float: left; border-collapse: collapse; border-spacing: 3px;\"><tbody>";

                            Int32 step = 1;
                            foreach (WorkflowActivity act in workflow.Activities)
                                steps += "<tr style=\"text-align: right;\"><td><strong>" + step++ + ":&nbsp;</strong></td><td>" + act.Name + "<td></tr>";

                            steps += "</tbody></table></p>";

                            vars.Add("steps", steps);

                            //Insere as mensagens de e-mail

                            //access_request

                            MessageBuilder msg1 = MessageBuilder.BuildFromTemplate(database, this._enterpriseId, "access_request", String.Join(",", mails), vars, trans);
                            msg1.SaveToDb(database, trans);


                        }
                    }

                }
                catch { }

                database.Commit();
                
            }
            catch (Exception ex)
            {
                database.Rollback();
                Error(ErrorType.SystemError, "Fail on insert access request", ex.Message, null);
                return false;
            }

            return true;
        }


    }
}
