using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
//using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Globalization;

using IAM.Config;
using IAM.PluginManager;
using IAM.PluginInterface;
using IAM.Log;
using IAM.CA;
using IAM.GlobalDefs;
using IAM.License;
using SafeTrend.Json;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.UserProcess
{
    public enum RegistryProcessStatus
    {
        OK = 1,
        Error,
        Ignored
    }

    public class RegistryProcessStarter : IDisposable
    {
        public Int64 contextId { get; protected set; }
        public Int64 enterpriseId { get; protected set; }
        public Uri pluginUri { get; protected set; }
        public Int64 resourceId { get; protected set; }
        public Int64 pluginId { get; protected set; }
        public Int64 resourcePluginId { get; protected set; }
        public String importId { get; protected set; }
        public String packageId { get; protected set; }
        public String package { get; set; }
        public Int64 packageTrackId { get; set; }


        public RegistryProcessStarter(Int64 enterpriseId, Int64 contextId, Uri pluginUri, Int64 resourceId, Int64 pluginId, Int64 resourcePluginId, String importId, String packageId, String package)
        {
            this.pluginUri = pluginUri;
            this.resourcePluginId = resourcePluginId;
            this.resourceId = resourceId;
            this.pluginId = pluginId;
            this.importId = importId;
            this.packageId = packageId;
            this.enterpriseId = enterpriseId;
            this.contextId = contextId;
            this.package = package;
        }

        public void Dispose()
        {
            this.pluginUri = null;
            this.importId = null;
            this.packageId = null;
            this.package = null;
        }
    }

    public class RegistryProcess : IAMDatabase, IDisposable
    {
        private Uri pluginUri;
        private Int64 resourcePluginId;
        private Int64 resourceId;
        private Int64 pluginId;
        private String importId;
        private String packageId;
        //private String package;
        private PluginConnectorBaseImportPackageUser package;
        private Int64 enterpriseId;
        private Int64 contextId;
        private PluginConfig pluginConfig;
        private UserData userData;
        private Int64 packageTrackId;

        private StringBuilder internalLog;

        public Int64 IdentityId { get { return (userData != null ? userData.IdentityId : 0); } }
        public Int64 EntityId { get { return (userData != null ? userData.EntityId : 0); } }
        public Boolean NewUser { get { return (userData != null ? userData.NewUser : false); } }

        //private Boolean permitAddEntity;
        public delegate void ProccessLog(String text);
        public event ProccessLog OnLog;

        //private SqlConnection conn;
        IAMDatabase db;
        IAMDatabase dbAux;

        public RegistryProcess(String server, String dbName, String username, String password, PluginConfig pluginConfig, RegistryProcessStarter starter)
        {
            this.db = new IAMDatabase(server, dbName, username, password);
            this.dbAux = new IAMDatabase(server, dbName, username, password);

            iRegistryProcess(pluginConfig, starter);
        }


        public RegistryProcess(SqlConnection connection, PluginConfig pluginConfig, RegistryProcessStarter starter)
        {

            this.db = new IAMDatabase(connection);
            this.dbAux = new IAMDatabase(connection);

            iRegistryProcess(pluginConfig, starter);
        }

        private void iRegistryProcess(PluginConfig pluginConfig, RegistryProcessStarter starter)
        {

            this.pluginUri = starter.pluginUri;
            this.resourcePluginId = starter.resourcePluginId;
            this.resourceId = starter.resourceId;
            this.pluginId = starter.pluginId;
            this.importId = starter.importId;
            this.packageId = starter.packageId;
            this.enterpriseId = starter.enterpriseId;
            //this.conn = new SqlConnection(;
            this.pluginConfig = pluginConfig;
            this.contextId = starter.contextId;

            this.internalLog = new StringBuilder();

            TestTimer tmp = new TestTimer("RegistryProcess->Deserialize package", null);

            try
            {
                this.package = JSON.Deserialize<PluginConnectorBaseImportPackageUser>(starter.package);
            }
            catch (Exception ex)
            {
                throw new Exception("Error on deserialize package", ex);
            }
            tmp.Stop(dbAux.Connection, null);


#if DEBUG
            //this.db.Debug = true;
#endif

            if (pluginConfig == null)
                throw new Exception("Resource x plugin not found");

        }

        public void Log(String text)
        {
            this.internalLog.AppendLine(text);

            if (OnLog != null)
                OnLog(text);
        }

        public RegistryProcessStatus Process(EnterpriseKeyConfig enterpriseKey, LockRules lockRules, IgnoreRules ignoreRules, RoleRules roleRules, LicenseControl lic)
        {
            List<UserDataFields> fieldsData = null;
            List<UserDataFields> filter = null;
            TestTimer tmp = null;
            Boolean showError = true;


            SqlTransaction trans = null;

            try
            {
                RegistryProcess.ProccessLog dLog = new RegistryProcess.ProccessLog(delegate(String text)
                {
#if DEBUG
                    Log("\t{profile} " + text);
#endif
                });

                tmp = new TestTimer("Process->Starting", dLog);

                Log("Starting registry processor");
                Log("");

                Log("Plugin Config");
                Log(pluginConfig.ToString());
                Log("");

                Log("Registry data:");
                Log("\tGenerated Date: " + package.build_data);
                Log("\tContext id: " + this.contextId);
                Log("\tResource plugin id: " + this.resourcePluginId);
                Log("\tResource id: " + this.resourceId);
                Log("\tPlugin: " + this.pluginUri);
                Log("\tImport id: " + this.importId);
                Log("\tPackage id: " + this.packageId);
                Log("\tContainer: " + package.container);
                Log("\tGroups: " + (package.groups != null ? String.Join(", ", package.groups) : ""));
                Log("");

                if (this.pluginConfig.mapping == null)
                {
                    if (!pluginConfig.enable_import)
                        showError = false;

                    throw new Exception("Plugin mapping is null");
                }

                if (this.pluginConfig.mapping.Count == 0)
                {
                    if (!pluginConfig.enable_import)
                        showError = false;

                    throw new Exception("Plugin mapping is empty");
                }

                String where = "ci.status = 'F' and ci.resource_plugin_id = '" + this.resourcePluginId + "' and  ci.import_id = '" + this.importId + "' and ci.package_id = '" + this.packageId + "'";

                tmp.Stop(dbAux.Connection, null);


                /*
                ======================================
                == Resgata Package Track ID*/


                try
                {
                    DbParameterCollection par = new DbParameterCollection();
                    
                    par.Add("@date", typeof(DateTime)).Value = this.package.GetBuildDate();
                    par.Add("@package_id", typeof(String), this.package.pkgId.Length).Value = this.package.pkgId;

                    this.packageTrackId = dbAux.ExecuteScalar<Int64>("select id from st_package_track where flow = 'inbound' and date = @date and package_id = @package_id", System.Data.CommandType.Text, par, null);
                }
                catch (Exception ex)
                {
#if DEBUG
                    internalLog.AppendLine("Error getting package track entity id: " + ex.Message);
#endif
                }

                /*
                == Final do resgate Package Track ID
                ======================================*/


                /*
                ======================================
                == Monta tabela de filtragem*/

                tmp = new TestTimer("Process->Filter table", dLog);


                filter = new List<UserDataFields>();

                //Adiciona os mapeamentos que são ID ou único para filtragem
                foreach (PluginConnectorBasePackageData data in package.properties)
                {
                    if (String.IsNullOrWhiteSpace(data.dataValue))
                        continue;

                    foreach (PluginConfigMapping m in this.pluginConfig.mapping)
                        if ((m.is_id || m.is_unique_property) && (m.data_name.ToLower() == data.dataName.ToLower()) && !filter.Exists(f => (f.Mapping.field_id == m.field_id && f.Equal(data.dataValue.Trim()))))
                            filter.Add(new UserDataFields((PluginConfigMapping)m.Clone(), data.dataValue.Trim()));
                }

                Log("Filter data:");
                foreach (UserDataFields f in filter)
                    Log("\t[" + f.Mapping.data_name.ToLower() + "] is " + (f.Mapping.is_id ? "ID" : "Unique field") + " = " + f.Value);
                Log("");


                tmp.Stop(dbAux.Connection, null);


                /*
                == Final tabela de filtragem
                ======================================*/

                /*
                ======================================
                == Monta tabela de dados*/

                tmp = new TestTimer("Process->Data table", dLog);


                //Monta tabela de dados com base no mapeamento e dados recebidos
                fieldsData = new List<UserDataFields>();

                foreach (PluginConnectorBasePackageData data in package.properties)
                {
                    if (String.IsNullOrWhiteSpace(data.dataValue))
                        continue;

                    foreach (PluginConfigMapping m in this.pluginConfig.mapping)
                        if ((m.data_name.ToLower() == data.dataName.ToLower()) && !fieldsData.Exists(f => (f.Mapping.field_id == m.field_id && f.Equal(data.dataValue.Trim()))))
                            try
                            {
                                fieldsData.Add(new UserDataFields((PluginConfigMapping)m.Clone(), data.dataValue.Trim()));
                            }
                            catch (Exception ex2)
                            {
                                Log(ex2.Message);
                            }
                }

                Log("Proccess data: " + (fieldsData.Count == 0 ? "empty" : ""));
                foreach (UserDataFields f in fieldsData)
                    Log("\t[" + f.Mapping.data_name.ToLower() + "] Flags (" + (f.Mapping.is_login ? "is_login " : "") + (f.Mapping.is_name ? "is_name " : "") + (f.Mapping.is_password ? "is_password " : "") + ") " + (f.Mapping.is_id ? "is ID" : (f.Mapping.is_unique_property ? "is Unique field" : "")) + " = " + (f.Mapping.is_password ? "*****" : f.Value));
                Log("");

                tmp.Stop(dbAux.Connection, null);


                /*
                == Final tabela de dados
                ======================================*/


                /*
                ======================================
                == Cria o objeto do usuário e tenta localiza-lo*/
                tmp = new TestTimer("Process->Create user object", dLog);

                userData = new UserData(db.Connection, this.pluginConfig, enterpriseKey, enterpriseId, contextId, resourcePluginId, resourceId, pluginId, pluginConfig.mail_domain, pluginConfig.mail_field_id, filter, fieldsData, package.container);
                userData.OnLog += Log;
                userData.CheckUser();

                tmp.Stop(dbAux.Connection, null);
                
                tmp = new TestTimer("Process->Check exists and import enabled", dLog);

                //Não existe e não é possível adicionar
                if ((userData.EntityId == 0) && ((!pluginConfig.permit_add_entity) || (!pluginConfig.enable_import)))
                {
                    String sId = "";
                    foreach (UserDataFields f in filter)
                    {
                        if (sId != "") sId += ", ";
                        sId += f.Mapping.data_name + " = " + f.Value;
                    }

                    //Add identity to audit
                    userData.AddToAudit("not_exists", null);

                    throw new Exception("Entity not found and this plugin " + (!pluginConfig.enable_import ? "is disabled to import" : "not permit add entity") + ": " + sId);
                    return RegistryProcessStatus.Error;
                }


                tmp.Stop(dbAux.Connection, null);

                tmp = new TestTimer("Process->Check deleted", dLog);


                if (userData.Deleted)
                {
                    String sId = "";
                    foreach (UserDataFields f in filter)
                    {
                        if (sId != "") sId += ", ";
                        sId += f.Mapping.data_name + " = " + f.Value;
                    }

                    //Add identity to audit
                    //userData.AddToAudit("deleted");

                    throw new Exception("Entity found but marked as deleted: " + sId);
                    return RegistryProcessStatus.Error;
                }


                tmp.Stop(dbAux.Connection, null);

                //Verifica se o registro deve ser ignorado
                //Se sim, nada será realizado, nem bloqueio, nem explusão, nem adição....
                tmp = new TestTimer("Process->Check ignore", dLog);
                if (userData.Ignore(ignoreRules, this.pluginUri))
                {

                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcePluginId;
                    par.Add("@import_id", typeof(String)).Value = importId;
                    par.Add("@package_id", typeof(String)).Value = packageId;
                    par.Add("@status", typeof(String)).Value = 'F';
                    par.Add("@new_status", typeof(String)).Value = 'I';

                    ExecuteNonQuery(db.Connection, "sp_migrate_imported2", CommandType.StoredProcedure, par, null);

                    par.Clear();
                    par = null;

                    return RegistryProcessStatus.Ignored;
                }

                tmp.Stop(dbAux.Connection, null);


                //Esta parte do código está propositalmente depois da verificação de existência e se permite add o login
                //Pois este código é dispendioso, e só deve ser executado quando realmente necessario
                tmp = new TestTimer("Process->Check lock", dLog);
                userData.CheckLock(lockRules, this.pluginUri);
                tmp.Stop(dbAux.Connection, null);

                if ((userData.EntityId == 0) && (userData.Locked))
                {


                    tmp = new TestTimer("Process->Check exists and locked", dLog);

                    String sId = "";
                    foreach (UserDataFields f in filter)
                    {
                        if (sId != "") sId += ", ";
                        sId += f.Mapping.data_name + " = " + f.Value;
                    }

                    //userData.AddToAudit("locked", trans);

                    throw new Exception("Entity not found and this user is locked: " + sId);
                    return RegistryProcessStatus.Error;
                }
                else if (userData.EntityId == 0)//Não existe a entidade
                {

                    tmp = new TestTimer("Process->Add entity (check lic)", dLog);

                    lic.Count++;

                    if ((lic.Entities > 0) && (lic.Count > lic.Entities))
                    {
                        String sId = "";
                        foreach (UserDataFields f in filter)
                        {
                            if (sId != "") sId += ", ";
                            sId += f.Mapping.data_name + " = " + f.Value;
                        }

                        throw new Exception("License error: Entity not found and license limit (" + lic.Entities + " entities) exceeded. " + sId);
                        return RegistryProcessStatus.Error;
                    }

                    tmp.Stop(dbAux.Connection, null);


                    userData.NewUser = true;

                    tmp = new TestTimer("Process->Add entity (UpdateName)", dLog);


                    userData.UpdateName();


                    tmp.Stop(dbAux.Connection, null);


                    //Cria o login
                    tmp = new TestTimer("Process->Add entity (MakeLogin)", dLog);

                    //Define o campo de login com base nas informações recebidas
                    foreach (UserDataFields f in fieldsData)
                        if (f.Mapping.is_login && !String.IsNullOrEmpty(f.Value.ToString()) && !String.IsNullOrWhiteSpace(f.Value.ToString()))
                            userData.Login = f.Value.ToString();

                    Log("Build login...");
                    userData.MakeLogin(pluginConfig.build_login, null);

                    tmp.Stop(dbAux.Connection, null);


                    tmp = new TestTimer("Process->Add entity (MakeEmail)", dLog);

                    //Cria o e-mail
                    Log("Build e-mail...");
                    if (pluginConfig.build_mail)
                        userData.MakeEmail(null, pluginConfig.mail_domain, pluginConfig.mail_field_id);

                    tmp.Stop(dbAux.Connection, null);

                    if (userData.FullName == null)
                        userData.FullName = userData.Login;

                    trans = db.Connection.BeginTransaction();

                    tmp = new TestTimer("Process->Add entity", dLog);

                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@resourcePluginId", typeof(Int64)).Value = resourcePluginId;
                    par.Add("@alias", typeof(String)).Value = userData.FullName;
                    par.Add("@full_name", typeof(String)).Value = userData.FullName;

                    DataTable dtEnt = ExecuteDataTable(db.Connection, "sp_new_entity_and_identity", CommandType.StoredProcedure, par, trans);
                    if ((dtEnt == null) || (dtEnt.Rows.Count == 0))
                        throw new Exception("Erro on insert entity & identity");

                    par.Clear();
                    par = null;

                    userData.EntityId = (Int64)dtEnt.Rows[0]["id"];
                    userData.IdentityId = (Int64)dtEnt.Rows[0]["identity_id"];

                    Log("New entity/identity");

                    AddUserLog(db.Connection, LogKey.User_Added, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resourceId, this.pluginId, userData.EntityId, userData.IdentityId, "User added in IAM Database", this.internalLog.ToString(), trans);

                    tmp.Stop(dbAux.Connection, null);


                }
                else if (userData.IdentityId == 0)//Existe a entidade porém não a identidade
                {
                    tmp = new TestTimer("Process->Add identity", dLog);


                    trans = db.Connection.BeginTransaction();

                    DbParameterCollection par1 = new DbParameterCollection();
                    par1.Add("@entityId", typeof(Int64)).Value = userData.EntityId;
                    par1.Add("@resourcePluginId", typeof(Int64)).Value = resourcePluginId;

                    DataTable dtEnt = ExecuteDataTable(db.Connection, "sp_new_identity", CommandType.StoredProcedure, par1, trans);
                    if ((dtEnt == null) || (dtEnt.Rows.Count == 0))
                        throw new Exception("Erro on insert identity");

                    par1.Clear();
                    par1 = null;

                    if ((Boolean)dtEnt.Rows[0]["new_identity"])
                        Log("New identity");

                    userData.IdentityId = (Int64)dtEnt.Rows[0]["identity_id"];

                    AddUserLog(db.Connection, LogKey.User_Added, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resourceId, this.pluginId, userData.EntityId, userData.IdentityId, "Identity added", this.internalLog.ToString(), trans);

                    tmp.Stop(dbAux.Connection, null);

                }

                try
                {
                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@entity_id", typeof(Int64)).Value = userData.EntityId;
                    par.Add("@date", typeof(DateTime)).Value = this.package.GetBuildDate();
                    par.Add("@package_id", typeof(String), this.package.pkgId.Length).Value = this.package.pkgId;

                    dbAux.ExecuteNonQuery("UPDATE st_package_track SET entity_id = @entity_id where flow = 'inbound' and date = @date and package_id = @package_id", System.Data.CommandType.Text, par, null);
                }
                catch(Exception ex) {
#if DEBUG
                    internalLog.AppendLine("Error updating package track entity id: " + ex.Message);
#endif
                }

                if (trans == null)
                    trans = db.Connection.BeginTransaction();

                try
                {

                    tmp = new TestTimer("Process->Lockunlock", dLog);

                    //Só permite alterar este status se for um plugin de entrada
                    if ((pluginConfig.permit_add_entity) && (userData.Locked != userData.LastLocked))
                    {
                        Log((userData.Locked ? "Locking user" : "Unlocking user"));
                        AddUserLog(db.Connection, (userData.Locked ? LogKey.User_Locked : LogKey.User_Unlocked), null, "Engine", UserLogLevel.Debug, 0, 0, 0, this.resourceId, this.pluginId, userData.EntityId, userData.IdentityId, (userData.Locked ? "Locking user" : "Unlocking user"), (userData != null ? userData.LockedInfo : ""), trans);
                    }
                    else
                    {
                        //Caso não permitido retorna ao estado anterior
                        userData.Locked = userData.LastLocked;
                    }

                    tmp.Stop(dbAux.Connection, null);
                    tmp = new TestTimer("Process->UpdateFields", dLog);


                    //Atualiza as propriedades (fields)
                    Log("Updating user values...");
                    userData.UpdateFields(trans, pluginConfig.enable_import);

                    tmp.Stop(dbAux.Connection, null);


                    if (pluginConfig.enable_import)
                    {
                        tmp = new TestTimer("Process->BuildPassword", dLog);

                        Log("Building password...");
                        userData.BuildPassword(trans);

                        tmp.Stop(dbAux.Connection, null);
                        tmp = new TestTimer("Process->UpdateUser", dLog);

                        //Registro tudo que está pendente no banco
                        Log("Updating user data (name, login and password)...");
                        userData.UpdateUser(trans);

                        tmp.Stop(dbAux.Connection, null);
                        tmp = new TestTimer("Process->UpdateGroups", dLog);

                        //Registro tudo que está pendente no banco
                        if (pluginConfig.import_groups)
                        {
                            Log("Updating user groups...");
                            userData.UpdateGroups(trans, package.groups);
                        }

                        tmp.Stop(dbAux.Connection, null);
                    }

                    tmp = new TestTimer("Process->update collector_imports", dLog);

                    //Excluir estes registros processados
                    //ExecuteNonQuery(conn,"delete from collector_imports where " + where.Replace("ci.", ""), CommandType.Text, null, trans);
                    //ExecuteNonQuery(conn,"update collector_imports set status = 'I' where " + where.Replace("ci.", ""), CommandType.Text, null, trans);

                    /*	@plugin_uri varchar(500),
	                @resource_id bigint,
                    @import_id varchar(40),
                    @registry_id varchar(40),
                    @status varchar(2),
                    @new_status varchar(2)*/


                    tmp.Stop(dbAux.Connection, null);
                    tmp = new TestTimer("Process->Commit", dLog);

                    Log("Commit user data on database");
                    trans.Commit();
                    trans = null;

                    //try to rebuild user index
                    for (Int32 i = 0; i <= 5; i++)
                    {
                        try
                        {
                            if (pluginConfig.enable_import)
                            {
                                userData.RebuildIndexes(null);
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch {
                            Thread.Sleep(2000);
                        }
                    }

                    tmp.Stop(dbAux.Connection, null);

                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@resource_plugin_id", typeof(Int64)).Value = resourcePluginId;
                    par.Add("@import_id", typeof(String)).Value = importId;
                    par.Add("@package_id", typeof(String)).Value = packageId;
                    par.Add("@status", typeof(String)).Value = 'F';
                    par.Add("@new_status", typeof(String)).Value = 'I';

                    ExecuteNonQuery(db.Connection, "sp_migrate_imported2", CommandType.StoredProcedure, par, null);

                    par.Clear();
                    par = null;


                    /*
                    ======================================*/

                }
                catch (Exception ex)
                {

                    if (trans != null)
                        trans.Rollback();

                    trans = null;

                    throw ex;
                }


                tmp = new TestTimer("Process->UpdateRoles", dLog);


                //Por fim verifica as roles
                if (pluginConfig.enable_import)
                    userData.UpdateRoles(null, roleRules, this.pluginUri);


                tmp.Stop(dbAux.Connection, null);


                try
                {

                    dbAux.AddPackageTrack(this.packageTrackId, "engine", "Process sucess: " + this.internalLog.ToString());

                }
                catch { }

#if DEBUG
                AddUserLog(dbAux.Connection, LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Debug, 0, 0, 0, this.resourceId, this.pluginId, (userData != null ? userData.EntityId : 0), (userData != null ? userData.IdentityId : 0), "User process status", this.internalLog.ToString());
#endif

                Log("Success");
                return RegistryProcessStatus.OK;
            }
            catch (Exception ex)
            {

                if (tmp != null)
                    tmp.Stop(dbAux.Connection, null);

                String traceError = "";
                traceError += "Erro: " + ex.Message + ex.StackTrace;

                Log("Erro: " + ex.Message);
                if (ex.InnerException != null)
                    Log("Erro: " + ex.InnerException.Message);

#if DEBUG
                Log("StackTrace: " + ex.StackTrace);
#endif

                if (showError)
                {

                    if (ex is SqlException)
                    {
                        AddUserLog(dbAux.Connection, LogKey.User_ImportError, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resourceId, this.pluginId, (userData != null ? userData.EntityId : 0), (userData != null ? userData.IdentityId : 0), ex.Message, SafeTrend.Json.JSON.Serialize2(new { import_id = importId, package_id = packageId, db_laet_error = LastDBError }));
                    }
                    else
                    {
                        AddUserLog(dbAux.Connection, LogKey.User_ImportError, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resourceId, this.pluginId, (userData != null ? userData.EntityId : 0), (userData != null ? userData.IdentityId : 0), ex.Message, SafeTrend.Json.JSON.Serialize2(new { import_id = importId, package_id = packageId, trace_error = traceError }));
                    }
                }

                try
                {

                    dbAux.AddPackageTrack(this.packageTrackId, "engine", "Process error: " + SafeTrend.Json.JSON.Serialize2(new { error_message = ex.Message, error_stack_trace = ex.StackTrace, import_id = importId, package_id = packageId, trace_error = traceError }));

                }
                catch { }

                //Se o erro for de deadlock, mantem o registro na base para ser reprocessado
                if (!(ex is SqlException) || ((ex is SqlException) && (ex.Message.IndexOf("deadlock") == -1)))
                {
                    ExecuteNonQuery(dbAux.Connection, "update collector_imports set status = 'E' where status = 'F' and resource_plugin_id = '" + this.resourcePluginId + "' and  import_id = '" + this.importId + "' and package_id = '" + this.packageId + "'", CommandType.Text, null);
                    ExecuteNonQuery(dbAux.Connection, "delete from collector_imports where status = 'E' and resource_plugin_id = '" + this.resourcePluginId + "' and  import_id = '" + this.importId + "' and package_id = '" + this.packageId + "'", CommandType.Text, null);
                }

                //Console.ReadLine();

                //System.Diagnostics.Process.GetCurrentProcess().Kill();
                //throw ex;


                if (trans != null)
                    trans.Rollback();

                trans = null;

                return RegistryProcessStatus.Error;
            }
            finally
            {
                Log("End of registry processor");

                if (fieldsData != null) fieldsData.Clear();
                fieldsData = null;

            }

        }

        public void Dispose()
        {
            this.pluginUri = null;
            this.importId = null;
            this.packageId = null;

            if (this.userData != null) userData.Dispose();
            this.userData = null;

            if (this.package != null) this.package.Dispose();
            this.package = null;


            if (this.db != null) this.db.closeDB();
            if (this.dbAux != null) this.dbAux.closeDB();


        }
    }
}
