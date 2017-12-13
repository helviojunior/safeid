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
using SafeTrend.Data;
using IAM.UserProcess;
using IAM.License;
using System.Text.RegularExpressions;
using IAM.PluginInterface;

namespace IAM.WebAPI.Classes
{
    internal class User : APIBase
    {

        public override event Error Error;
        public override event ExternalAccessControl ExternalAccessControl;

        private Int64 _enterpriseId;
        private AccessControl acl;

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

            //Primeiro case para validar autenticação
            switch (mp[1])
            {
                case "login":
                    //Este método não precisa verificar autenticação
                    break;

                default:
                    //Para todos os outros verifica autenticação
                    acl = ValidateCtrl(sqlConnection, method, auth, parameters, ExternalAccessControl);
                    if (!acl.Result)
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
                    return newuser(sqlConnection, parameters);
                    break;

                case "changeproperty":
                    return changeproperty(sqlConnection, parameters);
                    break;

                case "login":
                    return login(sqlConnection, parameters);
                    break;

                case "logout":
                    return logout(sqlConnection, auth);
                    break;

                case "deleteidentity":
                    return deleteidentity(sqlConnection, parameters);
                    break;

                case "unlockidentity":
                    return unlockidentity(sqlConnection, parameters);
                    break;

                case "resetpassword":
                    return resetpwd(sqlConnection, parameters);
                    break;

                case "changepassword":
                    return changepassword(sqlConnection, parameters);
                    break;

                case "search":
                case "list":
                    return search(sqlConnection, parameters);
                    break;

                case "get":
                    return get(sqlConnection, parameters);
                    break;

                case "deploy":
                    return deploy(sqlConnection, parameters, false, true);
                    break;

                case "lock":
                    return deploy(sqlConnection, parameters, true, false);
                    break;

                case "unlock":
                    return deploy(sqlConnection, parameters, false, false);
                    break;

                case "delete":
                    return delete(sqlConnection, parameters, true);
                    break;

                case "undelete":
                    return delete(sqlConnection, parameters, false);
                    break;

                case "logs":
                    return logs(sqlConnection, parameters);
                    break;

                case "auth":
                    return authUser(sqlConnection, parameters);
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
        private Dictionary<String, Object> newuser(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
                using (DataTable dtContext = ExecuteDataTable(sqlConnection, "select p.scheme, rp.*, c.id context_id, p.uri from resource_plugin rp with(nolock) inner join plugin p with(nolock) on rp.plugin_id = p.id inner join resource r with(nolock) on rp.resource_id = r.id inner join context c with(nolock) on r.context_id = c.id where rp.id = " + rpId, CommandType.Text, null))
                {
                    if ((dtContext != null) && (dtContext.Rows.Count > 0))
                    {
                        pluginConfig = new PluginConfig(sqlConnection, dtContext.Rows[0]["scheme"].ToString(), (Int64)dtContext.Rows[0]["plugin_id"], (Int64)dtContext.Rows[0]["id"]);

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


                LicenseControl lic = LicenseChecker.GetLicenseData(sqlConnection, null, this._enterpriseId);

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

                PluginConnectorBaseImportPackage pkg = new PluginConnectorBaseImportPackage("API.user.new");
                

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


                    DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field where enterprise_id = "+ this._enterpriseId +" and id = " + fieldId, CommandType.Text, null, null);
                    if ((dtField == null) || (dtField.Rows.Count == 0))
                    {
                        Error(ErrorType.InvalidRequest, "Field on property " + i + " not exists or is not a chield of this enterprise.", "", null);
                        return null;
                    }

                    DataTable dtFieldMapping = ExecuteDataTable(sqlConnection, "select f.*, rpm.data_name from field f with(nolock) inner join resource_plugin_mapping rpm with(nolock) on rpm.field_id = f.id where f.enterprise_id = " + this._enterpriseId + " and rpm.field_id = " + fieldId + " and rpm.resource_plugin_id = " + rpId, CommandType.Text, null, null);
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

                starter.package = JsonBase.JSON.Serialize2(pkg);
                pkg.Dispose();
                pkg = null;

                EnterpriseKeyConfig k = new EnterpriseKeyConfig(sqlConnection, this._enterpriseId);

                LockRules lockRules = new LockRules();
                IgnoreRules ignoreRules = new IgnoreRules();
                RoleRules roleRules = new RoleRules();
                lockRules.GetDBConfig(sqlConnection);
                ignoreRules.GetDBConfig(sqlConnection);
                roleRules.GetDBConfig(sqlConnection);

                //Realiza todo o processamento deste registro
                using (RegistryProcess proc = new RegistryProcess(sqlConnection, pluginConfig, starter))
                {

                    RegistryProcess.ProccessLog log = new RegistryProcess.ProccessLog(delegate(String text)
                    {
                        tLog.AppendLine(text);
                    });

                    proc.OnLog += log;
                    RegistryProcessStatus status = proc.Process(k, lockRules, ignoreRules, roleRules, lic);
                    proc.OnLog -= log;

                    userId = proc.EntityId;

                    AddUserLog(sqlConnection, LogKey.Import, null, "API", (status == RegistryProcessStatus.Error ? UserLogLevel.Error : UserLogLevel.Info), 0, 0, 0, starter.resourceId, starter.pluginId, proc.EntityId, proc.IdentityId, "Import processed", tLog.ToString());

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
                return get(sqlConnection, newData);
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
        private Dictionary<String, Object> changeproperty(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 and  c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
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


                    DataTable dtField = ExecuteDataTable(sqlConnection, "select * from field where enterprise_id = " + this._enterpriseId + " and id = " + fieldId, CommandType.Text, null, null);
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
                DataTable dtProperty = ExecuteDataTable(sqlConnection, "select e.*, f.name field_name, f.data_type from entity_field e with(nolock) inner join field f  with(nolock) on e.field_id = f.id where e.entity_id = @user_id", CommandType.Text, par, null);
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


                trans = sqlConnection.BeginTransaction();

                foreach (UserDataFields d in toDelete)
                {
                    tLog.AppendLine("Field '" + d.Mapping.field_name + "' with value '" + d.Value + "' deleted");

                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@user_id", typeof(Int64)).Value = userId;
                    par2.Add("@field_id", typeof(Int64)).Value = d.Mapping.field_id;
                    par2.Add("@value", typeof(String)).Value = d.Value;

                    ExecuteNonQuery(sqlConnection, "delete from entity_field where entity_id = @user_id and field_id = @field_id and value = @value", CommandType.Text, par2, trans);
                }


                foreach (UserDataFields a in properties)
                {
                    tLog.AppendLine("Field '" + a.Mapping.field_name + "' with value '" + a.Value + "' inserted");

                    DbParameterCollection par2 = new DbParameterCollection();
                    par2.Add("@user_id", typeof(Int64)).Value = userId;
                    par2.Add("@field_id", typeof(Int64)).Value = a.Mapping.field_id;
                    par2.Add("@value", typeof(String)).Value = a.Value;

                    ExecuteNonQuery(sqlConnection, "insert into entity_field (entity_id, field_id, value) values(@user_id, @field_id, @value)", CommandType.Text, par2, trans);
                }

                AddUserLog(sqlConnection, LogKey.User_PropertyChanged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userId, 0, "Properties changed", tLog.ToString(), acl.EntityId, trans);

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
                return get(sqlConnection, newData);
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
        private Dictionary<String, Object> resetpwd(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 and  c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
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

            using (DataTable dtRules = ExecuteDataTable(sqlConnection, "select password_rule from context c with(nolock) where c.id = " + dtUsers.Rows[0]["context_id"].ToString() + " and (c.password_rule is not null and rtrim(LTRIM(c.password_rule)) <> '')"))
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
                    using (DataTable dtFields = ExecuteDataTable(sqlConnection, "select * from identity_field ife with(nolock) inner join entity e with(nolock) on ife.entity_id = e.id where e.id = " + dtUsers.Rows[0]["id"].ToString() + " and ife.field_id = " + fieldId))
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
            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(sqlConnection, this._enterpriseId))
            using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(pwdValue)))
                pwd = Convert.ToBase64String(cApi.ToBytes());


            ExecuteNonQuery(sqlConnection, "update entity set password = '" + pwd + "', must_change_password = "+ (mustChange ? "1" : "0") +" where id = " + userid, CommandType.Text,null,  null);

            AddUserLog(sqlConnection, LogKey.User_PasswordReseted, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "Password reseted", "New password: " + pwdValue + "\r\nUser " + (mustChange ? "" : "not") + " must change password on next logon.", acl.EntityId);

            ExecuteNonQuery(sqlConnection, "insert into deploy_now (entity_id) values(" + userid + ")", CommandType.Text,null,  null);

            result.Add("success", true);
            result.Add("method", pwdMethod);

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.logs'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> logs(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
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

            //DataTable dtLogs = ExecuteDataTable(sqlConnection, "select l.*, res.name resource_name from logs l with(nolock) left join [identity] i with(nolock) on i.id = l.identity_id left join resource res with(nolock) on res.id = l.resource_id where l.entity_id = " + userid + " order by l.date desc");
            DataTable dtLogs = ExecuteDataTable(sqlConnection, sql);
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
        private Dictionary<String, Object> deploy(SqlConnection sqlConnection, Dictionary<String, Object> parameters, Boolean locked, Boolean deployOnly)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select e.* from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
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
                ExecuteNonQuery(sqlConnection, "update entity set locked = " + (locked ? "1" : "0") + " where id = " + userid, CommandType.Text,null,  null);

                AddUserLog(sqlConnection, (locked ? LogKey.User_Locked : LogKey.User_Unlocked), null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "User " + (locked ? "locked" : "unlocked") + " through API", "", acl.EntityId);

            }

            ExecuteNonQuery(sqlConnection, "insert into deploy_now (entity_id) values(" + userid + ")", CommandType.Text,null,  null);
            AddUserLog(sqlConnection, LogKey.User_DeployMark, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "User data marked for replication through API", "", acl.EntityId);


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
        private Dictionary<String, Object> delete(SqlConnection sqlConnection, Dictionary<String, Object> parameters, Boolean delete = false)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select e.* from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and e.id = @user_id", CommandType.Text, par, null);
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

            ExecuteNonQuery(sqlConnection, "update entity set deleted = " + (delete ? "1" : "0") + ", deleted_date = " + (delete ? "getdate()" : " null") + " where id = " + userid, CommandType.Text,null,  null);

            AddUserLog(sqlConnection, (delete ? LogKey.User_Deleted : LogKey.User_Undeleted), null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, userid, 0, "User " + (delete ? "deleted" : "undeleted") + " through API", "", acl.EntityId);

            ExecuteNonQuery(sqlConnection, "insert into deploy_now (entity_id) values(" + userid + ")", CommandType.Text,null,  null);

            result.Add("success", true);

            return result;

        }

        /// <summary>
        /// Método privado para processamento do método 'user.get'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean deleteidentity(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select e.* from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and e.id = @user_id and identity_id = @identity_id", CommandType.Text, par, null);
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

            ExecuteNonQuery(sqlConnection, "delete from [identity] where id = @identity_id", CommandType.Text, par);

            return true;

        }


        /// <summary>
        /// Método privado para processamento do método 'user.get'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Boolean unlockidentity(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select e.* from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and e.id = @user_id and identity_id = @identity_id", CommandType.Text, par, null);
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

            SqlTransaction trans = sqlConnection.BeginTransaction();
            try
            {
                ExecuteNonQuery(sqlConnection, "update [identity] set temp_locked = 0 where id = @identity_id",  CommandType.Text,par, trans);
                ExecuteNonQuery(sqlConnection, "insert into identity_acl_ignore (identity_id) values(@identity_id)", CommandType.Text,par,  trans);
                
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
        private Dictionary<String,Object> get(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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

            DataTable dtUsers = ExecuteDataTable(sqlConnection, "select e.*, identity_qty = (select COUNT(distinct i.id) from [identity] i with(nolock) where i.entity_id = e.id) from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and e.id = @user_id order by e.resource_name, e.name, e.value", CommandType.Text, par, null);
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
            

            result.Add("info", newItem);

            
            DataTable dtGeneral = ExecuteDataTable(sqlConnection, "select top 1 e.id enterprise_id, e.name enterprise_name, c.id context_id, c.name context_name from enterprise e with(nolock) inner join context c with(nolock) on e.id = c.enterprise_id where c.id = " + dr["context_id"]);
            if ((dtGeneral != null) || (dtGeneral.Rows.Count > 0))
            {
                newItem = new Dictionary<string, object>();
                newItem.Add("enterprise_name", dtGeneral.Rows[0]["enterprise_name"]);
                newItem.Add("context_name", dtGeneral.Rows[0]["context_name"]);

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
            DataTable dtRoles = ExecuteDataTable(sqlConnection, "select resource_name, identity_id, name from vw_entity_roles where id = " + userid);
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
            DataTable dtIdentities = ExecuteDataTable(sqlConnection, "select i.*, r.name resource_name from [identity] i with(nolock) inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id inner join resource r with(nolock) on rp.resource_id = r.id where i.deleted = 0 and i.entity_id = " + userid);
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
        private List<Dictionary<String, Object>> search(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {
            List<Dictionary<String, Object>> result = new List<Dictionary<String, Object>>();

            String text = "";

            if (parameters.ContainsKey("text"))
                text = (String)parameters["text"];

            if (String.IsNullOrWhiteSpace(text))
                text = "";

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@text", typeof(String), text.Length).Value = text;

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
                addFields = ExecuteDataTable(sqlConnection, "select id from vw_entity_all_data e with(nolock) where e.enterprise_id = @enterprise_id and name in ('" + String.Join("','", additional_fields) + "') and e.value like '%'+@text+'%'", CommandType.Text, par, null);
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
                    }
            }


            sql += " ) SELECT";
            sql += "  *";
            sql += " FROM";
            sql += "  result_set";
            sql += " WHERE";
            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;

            //DataTable dtUsers = ExecuteDataTable(sqlConnection, "select * from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where " + (deleted ? "" : "e.deleted = 0 and") + " c.enterprise_id = @enterprise_id " + (String.IsNullOrWhiteSpace(text) ? "" : " and e.full_name like '%'+@text+'%' or e.login like '%'+@text+'%' ") + " order by e.full_name", CommandType.Text, par, null);
            DataTable dtUsers = ExecuteDataTable(sqlConnection, sql, CommandType.Text, par, null);
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
                    addFields = ExecuteDataTable(sqlConnection, "select name, resource_name, value from vw_entity_all_data e with(nolock) where id = " + dr["id"] + " and e.enterprise_id = @enterprise_id and name in ('" + String.Join("','", additional_fields) + "') and e.value like '%'+@text+'%'", CommandType.Text, par, null);
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
        private Dictionary<String, Object> changepassword(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            par.Add("@user", typeof(String), user.Length).Value = user;

            DataTable c = ExecuteDataTable(sqlConnection, "select distinct e.id from vw_entity_ids e inner join context c with(nolock) on c.id = e.context_id where c.enterprise_id = @enterprise_id and " + (userid > 0 ? "e.id = @user_id " : "e.value = @user"), CommandType.Text, par, null);
            if ((c != null) && (c.Rows.Count > 0))
            {
                if (c.Rows.Count == 1)
                {

                    UserPasswordStrength usrCheck = new UserPasswordStrength(sqlConnection, (Int64)c.Rows[0]["id"]);
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

                        using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(sqlConnection, this._enterpriseId))
                        using (CryptApi cApi1 = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(password)))
                        {
                            DbParameterCollection pPar = new DbParameterCollection();
                            String b64 = Convert.ToBase64String(cApi1.ToBytes());
                            pPar.Add("@password", typeof(String), b64.Length).Value = b64;


                            ExecuteNonQuery(sqlConnection, "update entity set password = @password, change_password = getdate(), last_login = getdate(), recovery_code = null, " + (mustChange ? " must_change_password = 1 " : " must_change_password = 0 ") + " where id = " + c.Rows[0]["id"], CommandType.Text, pPar);
                        }


                        AddUserLog(sqlConnection, LogKey.User_PasswordChanged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)c.Rows[0]["id"], 0, "Password changed", "Password changed through API", acl.EntityId);

                        //Cria o pacote com os dados atualizados deste usuário 
                        //Este processo vija agiliar a aplicação das informações pelos plugins

                        ExecuteNonQuery(sqlConnection, "insert into deploy_now (entity_id) values(" + c.Rows[0]["id"] + ")", CommandType.Text, null, null);

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
        private Dictionary<String, Object> logout(SqlConnection sqlConnection, String auth)
        {
            ExecuteNonQuery(sqlConnection, String.Format("delete from entity_auth where auth_key = '{0}'", auth), CommandType.Text, null);
            
            Dictionary<String, Object> result = new Dictionary<string, object>();
            result.Add("success", true);

            return result;
        }


        /// <summary>
        /// Método privado para processamento do método 'user.auth'
        /// </summary>
        /// <param name="sqlConnection">Conexão com o banco de dados MS-SQL</param>
        /// <param name="parameters">Dicionário (String, Object) contendo todos os parâmetros necessários</param>
        private Dictionary<String, Object> authUser(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
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
            par.Add("@user", typeof(String), user.Length).Value = user;

            DataTable tmp = ExecuteDataTable(sqlConnection, "select * from vw_entity_logins where enterprise_id = @enterprise_id and (id = @user_id or (login = @user or value = @user))", CommandType.Text, par, null);
            if ((tmp == null) || (tmp.Rows.Count == 0))
            {
                Error(ErrorType.InvalidParameters, "User not found.", "", null);
                return null;
            }

            //Caso haja mesmo login em contextos diferentes verifica todos eles
            foreach (DataRow dr in tmp.Rows)
            {
                String md5Pass = "";
                using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(sqlConnection, this._enterpriseId))
                using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(tmp.Rows[0]["password"].ToString())))
                    md5Pass = IAM.CA.CATools.MD5Checksum(cApi.clearData);

                if ((!String.IsNullOrWhiteSpace(md5Pass)) && md5Pass == parameters["md5_password"].ToString())
                {
                    if ((Boolean)tmp.Rows[0]["locked"])
                    {
                        AddUserLog(sqlConnection, LogKey.User_AccessDenied, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Fail on user/password auth through API (user locked)", "", acl.EntityId);
                        continue;
                    }

                    result.Add("userid", tmp.Rows[0]["id"]);
                    result.Add("login", tmp.Rows[0]["login"]);
                    result.Add("must_change", tmp.Rows[0]["must_change_password"]);

                    List<Object> tmpItem = new List<Object>();
                    DataTable dtRoles = ExecuteDataTable(sqlConnection, "select resource_name, identity_id, name from vw_entity_roles where id = " + tmp.Rows[0]["id"]);
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

                    AddUserLog(sqlConnection, LogKey.User_Logged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Success on user/password auth through API", "", acl.EntityId);

                    return result;
                }
                else
                {
                    AddUserLog(sqlConnection, LogKey.User_WrongPassword, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Fail on user/password auth through API (wrong password)", "", acl.EntityId);
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
        private Dictionary<String, Object> login(SqlConnection sqlConnection, Dictionary<String, Object> parameters)
        {

            if (!parameters.ContainsKey("user") || !parameters.ContainsKey("password"))
            {
                Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                return null;
            }

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this._enterpriseId;
            par.Add("@user", typeof(String), parameters["user"].ToString().Length).Value = parameters["user"];

            DataTable tmp = ExecuteDataTable(sqlConnection, "select * from vw_entity_logins where deleted = 0 and enterprise_id = @enterprise_id and (login = @user or value = @user)", CommandType.Text, par, null);
            if ((tmp == null) || (tmp.Rows.Count == 0))
            {
                Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                return null;
            }
            else if ((Boolean)tmp.Rows[0]["locked"])
            {
                AddUserLog(sqlConnection, LogKey.User_AccessDenied, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Error generating session id through API (user locked)", "", acl.EntityId);

                Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                return null;
            }

            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(sqlConnection, this._enterpriseId))
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

                    DataTable dtUserKey = ExecuteDataTable(sqlConnection, "[sp_new_auth_key]", CommandType.StoredProcedure, par);
                    if ((dtUserKey == null) || (dtUserKey.Rows.Count == 0))
                    {
                        AddUserLog(sqlConnection, LogKey.API_Error, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Error generating session id through API: Return of sp_new_auth_key " + (dtUserKey == null ? "is null" : "has no rows"), "", acl.EntityId);

                        Error(ErrorType.InvalidParameters, "Error generating session id.", "", null);
                        return null;
                    }

                    ExecuteNonQuery(sqlConnection, "update entity set last_login = getdate() where id = " + tmp.Rows[0]["id"], CommandType.Text, null);
                    AddUserLog(sqlConnection, LogKey.User_Logged, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "User logged through API", "");

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
                    AddUserLog(sqlConnection, LogKey.User_WrongPassword, null, "API", UserLogLevel.Info, 0, this._enterpriseId, 0, 0, 0, (Int64)tmp.Rows[0]["id"], 0, "Fail on user login through API (wrong password)", "");

                    Error(ErrorType.InvalidParameters, "Login name or password is incorrect.", "", null);
                    return null;
                }

        }
        
    }
}
