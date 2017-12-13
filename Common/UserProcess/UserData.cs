using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using IAM.CA;
using IAM.Password;
using IAM.Config;
using System.Runtime.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;
using IAM.Filters;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.UserProcess
{
    
    public class UserData: IAMDatabase, IDisposable
    {
        public Int64 EntityId { get; set; }
        public Int64 IdentityId { get; set; }
        public Boolean Locked { get; set; }
        public Boolean Deleted { get; set; }
        public Boolean LastLocked { get; set; }
        public String Login { get; set; }
        public String FullName { get; set; }
        public String Email { get; set; }
        public Boolean NewUser { get; set; }
        public String AtualPassword { get; set; }
        public String Password { get; set; }
        public Boolean MustChangePassword { get; set; }
        //public Boolean BuildLogin { get; set; }
        //public Boolean BuildEmail { get; set; }
        public Boolean BlockInheritance { get; set; }
        public String LockedInfo { get; set; }

        private List<String> emails;
        private List<UserDataFields> fields;

        private Int64 resource;
        private Int64 contextId;
        private Int64 enterpriseId;
        private Int64 pluginId;
        private Int64 mailField;
        private Int64 resourcePluginId;
        private String mailDomain;
        private SqlConnection conn;
        private EnterpriseKeyConfig enterpriseKey;
        //private EnterpriseEntityIds ids;
        private Boolean logPassword = false;
        private RegistryProcess.ProccessLog dLog = null;
        private PluginConfig pluginConfig;
        private String container;
        private List<UserDataFields> filter;

        public delegate void ProccessLog(String text);
        public event ProccessLog OnLog;

        public void Log(String text)
        {
            if (OnLog != null)
                OnLog(text);
        }

        public UserData(SqlConnection connection, PluginConfig pluginConfig, EnterpriseKeyConfig enterpriseKey, Int64 enterpriseId, Int64 contextId, Int64 resourcePluginId, Int64 resource, Int64 pluginId, String mailDomain, Int64 mailField, List<UserDataFields> filter, List<UserDataFields> fields, String container)
        {
            this.resource = resource;
            this.contextId = contextId;
            this.pluginId = pluginId;
            this.conn = connection;
            this.enterpriseKey = enterpriseKey;
            this.enterpriseId = enterpriseId;
            this.mailField = mailField;
            this.mailDomain = mailDomain;
            this.pluginConfig = pluginConfig;
            this.container = container;
            this.resourcePluginId = resourcePluginId;

            //Valores padrão
            this.NewUser = false;
            this.EntityId = this.IdentityId = 0;
            this.Locked = false;
            this.Deleted = false;
            this.LastLocked = false;
            this.Login = null;
            this.FullName = null;
            this.Password = null;
            this.Email = null;

            this.fields = fields;
            
            //Verifica se há campos para identificar o usuário, se não apresenta erro
            if (filter == null || filter.Count == 0)
            {
                AddToAudit("input_filter_empty", null);
                throw new Exception("Input filter data is empty");
            }

            this.filter = filter;

        }

        public void CheckUser()
        {

            dLog = new RegistryProcess.ProccessLog(delegate(String text)
            {
                Log("\t{profile} " + text);
            });

            /*
             * Alterado em 03/02/2015
            List<String> sql = new List<string>();
            foreach (UserDataFields u in filter)
                sql.Add("(ife.field_id = " + u.Mapping.field_id + " and ife.value = '" + u.Value + "')");

            foreach (UserDataFields u in fields)
                if (u.Mapping.is_login)
                    sql.Add("(e.login = '" + u.Value + "')");

            //DataTable dtEntity = Select(conn, "select e.id, e.login, e.full_name, e.[password], e.locked, e.deleted, e.identity_id, e.resource_plugin_id, this_resource_plugin = case when e.resource_id = " + this.resource + " and e.plugin_id = " + this.pluginId + " then CAST(1 as bit) else CAST(0 as bit) end, block_inheritance = case when exists (select 1 from identity_block_inheritance bi where bi.identity_id = e.identity_id) then cast(1 as bit) else cast(0 as bit) end from dbo.vw_entity_all_data e where e.context_id = " + this.contextId + " and (" + String.Join(" or ", sql) + ")");
            DataTable dtEntity = Select(connection, "select distinct e.id, e.login, e.full_name, e.[password],  e.locked,  e.deleted,  i.id identity_id, i.resource_plugin_id, block_inheritance = case when bi.identity_id is null then cast(0 as bit) else cast(1 as bit) end from entity e with(nolock) inner join [identity] i with(nolock) on e.id = i.entity_id inner join identity_field ife with(nolock) on ife.identity_id = i.id left join identity_block_inheritance bi with(nolock) on bi.identity_id = i.id where e.context_id = " + contextId + " and (" + String.Join(" or ", sql) + ")");
            */
            List<String> sql = new List<string>();
            Log("Filtering user by:");
            foreach (UserDataFields u in this.filter)
            {
                Log("\t[" + u.Mapping.data_name + "," + u.Mapping.field_id + "] = " + u.Value);
                sql.Add("(ei.field_id = " + u.Mapping.field_id + " and ei.value = '" + u.Value + "')");
            }

            String query = "select e.id, e.login, e.full_name, e.[password], e.locked, e.deleted, identity_id = isnull(i.id,0), ei.resource_plugin_id, block_inheritance = case when bi.identity_id is null then cast(0 as bit) else cast(1 as bit) end from entity e with(nolock) inner join [entity_keys] ei  with(nolock) on ei.entity_id = e.id left join [identity] i with(nolock) on e.id = i.entity_id and ei.identity_id = i.id left join identity_block_inheritance bi with(nolock) on bi.identity_id = i.id  where e.context_id = " + contextId + " and (" + String.Join(" or ", sql) + ")";

            DataTable dtEntity = Select(this.conn, query);

            if (dtEntity == null)
            {
                Log("Erro on select user data: " + LastDBError);
                throw new Exception("Erro on select user data", new Exception(LastDBError));
            }

            StringBuilder txtEntities = new StringBuilder();
            Log("");
            Log("Found " + dtEntity.Rows.Count + " entity/identy:");
            txtEntities.AppendLine("Found " + dtEntity.Rows.Count + " entity/identy:");
            Int32 regcount = 0;
            foreach (DataRow dr in dtEntity.Rows)
            {
                regcount++;
                Log("\tRegistry " + regcount);
                txtEntities.AppendLine("\tRegistry " + regcount);
                foreach (DataColumn dc in dtEntity.Columns)
                    if (dc.ColumnName.ToLower() != "password")
                        try
                        {
                            Log("\t\t[" + dc.ColumnName + "] = " + dr[dc.ColumnName]);
                            txtEntities.AppendLine("\t\t[" + dc.ColumnName + "] = " + dr[dc.ColumnName]);
                        }
                        catch { }
            }

            
            List<String> tmpEntityies = new List<String>();
            foreach (DataRow dr in dtEntity.Rows)
            {
                if (!tmpEntityies.Contains(dr["id"].ToString()))
                    tmpEntityies.Add(dr["id"].ToString());
            }

            if (tmpEntityies.Count > 1)
            {
                String errorLog = "Integrity check error: Multiplus entities (" + String.Join(", ", tmpEntityies) + ") found at this filtered data";

                foreach (DataRow dr in dtEntity.Rows)
                    AddUserLog(conn, LogKey.Dencrypt_Error, null, "Engine", UserLogLevel.Warning, 0, 0, 0, 0, 0, (Int64)dtEntity.Rows[0]["id"], (Int64)dtEntity.Rows[0]["identity_id"], errorLog, txtEntities.ToString());

                Log("");
                Log(errorLog);

                throw new Exception(errorLog);
            }
            tmpEntityies.Clear();


            if (dtEntity.Rows.Count > 0)
            {
                this.EntityId = (Int64)dtEntity.Rows[0]["id"];
                this.Locked = (Boolean)dtEntity.Rows[0]["locked"];
                this.LastLocked = (Boolean)dtEntity.Rows[0]["locked"];
                this.BlockInheritance = (Boolean)dtEntity.Rows[0]["block_inheritance"];
                this.Deleted = (Boolean)dtEntity.Rows[0]["deleted"];
                this.FullName = dtEntity.Rows[0]["full_name"].ToString();
                this.Login = dtEntity.Rows[0]["login"].ToString();

                try
                {
                    if (this.enterpriseKey == null)
                        throw new Exception("Enterprise key is null");

                    if (this.enterpriseKey.ServerPKCS12Cert == null)
                        throw new Exception("Server certificate is null");

                    if (dtEntity.Rows[0]["password"] == DBNull.Value)
                        throw new Exception("User password is null");

                    if ((dtEntity.Rows[0]["password"] != DBNull.Value) && (!String.IsNullOrWhiteSpace(dtEntity.Rows[0]["password"].ToString())))
                    {
                        using (CryptApi cApi = CryptApi.ParsePackage(this.enterpriseKey.ServerPKCS12Cert, Convert.FromBase64String(dtEntity.Rows[0]["password"].ToString().Trim())))
                            this.AtualPassword = Encoding.UTF8.GetString(cApi.clearData);
                    }

                }
                catch (Exception ex)
                {
                    Log("Error dencrypting password" + ex.Message);
                    AddUserLog(conn, LogKey.Dencrypt_Error, null, "Engine", UserLogLevel.Warning, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Error dencrypting password", ex.Message);
                }

                foreach (DataRow dr in dtEntity.Rows)
                    if (resourcePluginId == (Int64)dr["resource_plugin_id"])
                        this.IdentityId = (Int64)dr["identity_id"];

            }
            else
            {
                this.NewUser = true;
            }

            sql.Clear();
            sql = null;

            emails = new List<string>();
            foreach (UserDataFields u in fields)
            {
                //Nome
                if (u.StringValue.ToLower().IndexOf("@" + mailDomain.ToLower()) > 0)
                {
                    emails.Add(u.StringValue);
                }

            }

        }

        public void AddToAudit(String eventName, SqlTransaction transaction)
        {
            //Adiciona informações para audtoria do usuário
            String aId = this.Login;
            String aName = this.FullName;

            try
            {
                
                if (String.IsNullOrEmpty(aId))
                    foreach (UserDataFields u in fields)
                        if (u.Mapping.is_id)
                        {
                            aId = u.StringValue;
                        }
                        else if (u.Mapping.is_name)
                        {
                            aName = u.StringValue.Trim(" \t".ToCharArray());

                            try
                            {
                                aName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(aName.ToLower());
                            }
                            catch { }
                        }


                if (String.IsNullOrEmpty(aId))
                    foreach (UserDataFields u in fields)
                        if (u.Mapping.is_login)
                        {
                            aId = u.StringValue;
                            break;
                        }
                        


                if (String.IsNullOrEmpty(aId))
                    foreach (UserDataFields u in fields)
                        if (u.Mapping.is_unique_property)
                        {
                            aId = u.StringValue;
                            break;
                        }

                if ((aId == null) && (aName != null))
                    aId = aName;

                if ((aId != null) && (aName == null))
                    aName = aId;

                if (aId == null)
                    aId = "";

                if (aName == null)
                    aName = "";

                List<Object> oF = new List<object>();
                foreach (UserDataFields u in fields)
                    oF.Add(u.ToSerialObject());


                DbParameterCollection par = new DbParameterCollection();
                par.Add("@pluginId", typeof(Int64)).Value = this.pluginId;
                par.Add("@resourceId", typeof(Int64)).Value = this.resource;
                par.Add("@id", typeof(String)).Value = aId;
                par.Add("@full_name", typeof(String)).Value = aName;
                par.Add("@event", typeof(String)).Value = eventName;
                par.Add("@fields", typeof(String)).Value = SafeTrend.Json.JSON.Serialize2(oF);

                ExecuteNonQuery(conn,"sp_new_audit_identity", CommandType.StoredProcedure, par, transaction);

            }
            catch(Exception ex) {
                Log("AddToAudit> " + ex.Message);
            }

        }

        public void UpdateName()
        {

            foreach (UserDataFields u in fields)
            {
                //Nome
                if (u.Mapping.is_name)
                {
                    this.FullName = u.StringValue.Trim(" \t".ToCharArray());

                    try
                    {
                        this.FullName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(this.FullName.ToLower());
                        break;
                    }
                    catch { }

                }

            }

        }

        public void UpdateGroups(SqlTransaction trans, List<String> groups)
        {
            if (this.IdentityId == 0)
                return;

            if ((groups == null) || (groups.Count == 0))
                return;

            List<String> addGrp = new List<string>();
            foreach (String g in groups)
                addGrp.Add(g.ToLower());

            //Verifica os grupos que ja estão mapeados
            Dictionary<String, Int64> roles = new Dictionary<string, long>();
            DataTable dtRoles = ExecuteDataTable("select a.* from resource_plugin_role_action a inner join resource_plugin rp on rp.id = a.resource_plugin_id where a.action_key = 'group' and a.action_add_value in ('" + String.Join("','", addGrp) + "') and rp.resource_id = " + this.resource + " and rp.plugin_id = " + this.pluginId, trans);
            if ((dtRoles != null) && (dtRoles.Rows.Count > 0))
            {
                //
                foreach (DataRow dr in dtRoles.Rows)
                {
                    ExecuteNonQuery("IF (NOT EXISTS (SELECT 1 FROM identity_role with(nolock) WHERE identity_id = " + this.IdentityId + " and role_id = " + dr["role_id"] + ")) begin insert into identity_role (identity_id, role_id, [auto]) values (" + this.IdentityId + "," + dr["role_id"] + ",1) end", trans);
                    addGrp.Remove(dr["action_add_value"].ToString().ToLower());
                }
            }

            //Verifica os grupos não existente no sistema para cria-los
            foreach (String g in addGrp)
                using (DbParameterCollection par = new DbParameterCollection())
                {
                    String groupName = g;

                    try
                    {
                        groupName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(groupName);
                    }
                    catch { }

                    par.Add("@context_id", typeof(Int64)).Value = this.contextId;
                    par.Add("@identity_id", typeof(Int64)).Value = this.IdentityId;
                    par.Add("@role_name", typeof(String)).Value = groupName;

                    ExecuteNonQuery("IF (NOT EXISTS (SELECT 1 FROM [role] WHERE context_id = @context_id and name = @role_name)) begin insert into [role] (context_id, [name]) values (@context_id, @role_name) end insert into identity_role (identity_id, role_id, [auto]) select @identity_id, id, 1 from [role] WHERE context_id = @context_id and name = @role_name", CommandType.Text, par, trans);
                }

            //

        }

        public void UpdateRoles(SqlTransaction trans, RoleRules roleRules, Uri pluginUri)
        {
            //Não faz verificação qaundo a herança está bloqueada
            if (this.BlockInheritance)
                return;

            List<RoleRuleItem> rri = roleRules.GetItem(this.resource, pluginUri.AbsoluteUri);
            if ((rri == null) || (rri.Count == 0))
                return;

            FilterRuleCollection masterCol = new FilterRuleCollection();
            foreach (RoleRuleItem i in rri)
                foreach (FilterRule f in ((FilterRuleCollection)i.FilterRuleCollection))
                    masterCol.AddFilterRule(f);

            FilterChecker chk = new FilterChecker(masterCol);
            try
            {
                foreach (UserDataFields f in this.fields)
                    chk.AddFieldData(f.Mapping.field_id, f.Mapping.data_type, f.StringValue);
            }
            catch (Exception ex)
            {
                Log("Erro on load properties of role checker: " + ex.Message);
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on load properties of role checker: " + ex.Message);
                return;
            }

            if (chk.DataCount == 0)
                return;


            Log("Starting role checker...");

            Dictionary<Int64, String> identityRoles = new Dictionary<Int64, String>();

            try
            {

                //Verifica se se enquadra nas regras
                foreach (FilterMatch m in chk.Matches())
                    if (m.Success)
                    {
                        //Verifica em qual role deu match
                        foreach (RoleRuleItem i in rri)
                            foreach (FilterRule f in ((FilterRuleCollection)i.FilterRuleCollection))
                                if (f.ToSqlString() == m.Filter.ToSqlString())
                                {

                                    String info = GetDataText("Proccess data", this.fields);
                                    info += "Role: " + i.Name + Environment.NewLine;
                                    info += "Filter: " + m.Filter.FilterName + Environment.NewLine;
                                    info += m.Filter.ToString();

                                    if (!identityRoles.ContainsKey(i.RoleId))
                                        identityRoles.Add(i.RoleId, info);

                                    break;
                                }
                    }
            }
            catch (Exception ex)
            {
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on role checker: " + ex.Message);
                throw new Exception("Erro on role checker: " + ex.Message);
            }


            try
            {

                if (identityRoles.Count > 0)
                {

                    //Remove as roles que não estão nesta lista
                    DataTable dtRemove = Select(conn, "select * from identity_role with(nolock) where identity_id = " + this.IdentityId + " and [auto] = 1 and role_id not in (" + String.Join(",", identityRoles.Keys) + ")");
                    if ((dtRemove != null) && (dtRemove.Rows.Count > 0))
                    {
                        foreach (DataRow dr in dtRemove.Rows)
                        {

                            String rName = "";
                            foreach (RoleRuleItem rr in rri)
                                if (rr.RoleId == (Int64)dr["role_id"])
                                    rName = rr.Name;

                            Log("Identity unbind to role " + rName);
                            AddUserLog(conn,LogKey.User_IdentityRoleUnbind, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Identity unbind to role " + rName);
                            ExecuteNonQuery(conn,"delete from identity_role where identity_id = " + this.IdentityId + " and [auto] = 1 and role_id = " + dr["role_id"], CommandType.Text, null);
                        }
                    }

                    //Adiciona as roles
                    foreach (Int64 r in identityRoles.Keys)
                    {
                        DbParameterCollection par = new DbParameterCollection();
                        par.Add("@identity_id", typeof(Int64)).Value = this.IdentityId;
                        par.Add("@role_id", typeof(Int64)).Value = r;

                        Boolean added = ExecuteScalar<Boolean>(conn, "sp_insert_identity_role", CommandType.StoredProcedure, par);

                        if (added)
                        {
                            //Busca o nome para o log
                            String rName = "";
                            foreach (RoleRuleItem rr in rri)
                                if (rr.RoleId == r)
                                    rName = rr.Name;

                            Log("Identity bind to role " + rName);
                            AddUserLog(conn,LogKey.User_IdentityRoleBind, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Identity bind to role " + rName, identityRoles[r]);
                        }
                    }
                }
                else
                {
                    //Remove todas as roles
                    DataTable dtRemove = Select(conn, "select * from identity_role with(nolock) where identity_id = " + this.IdentityId + " and [auto] = 1");
                    if ((dtRemove != null) && (dtRemove.Rows.Count > 0))
                    {
                        foreach (DataRow dr in dtRemove.Rows)
                        {

                            String rName = "";
                            foreach (RoleRuleItem rr in rri)
                                if (rr.RoleId == (Int64)dr["role_id"])
                                    rName = rr.Name;

                            Log("Identity unbind to role " + rName);
                            AddUserLog(conn,LogKey.User_IdentityRoleUnbind, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Identity unbind to role " + rName);
                            ExecuteNonQuery(conn,"delete from identity_role where identity_id = " + this.IdentityId + " and [auto] = 1 and role_id = " + dr["role_id"], CommandType.Text, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Erro on RoleChecker: " + ex.Message);
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on RoleChecker: " + ex.Message);
            }

            Log("Role ended");


        }

        public void UpdateUser(SqlTransaction trans)
        {
            TestTimer tmp = new TestTimer("UserData.UpdateUser->Build values", dLog);


            String sql = "update entity set";

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@entityId", typeof(Int64)).Value = EntityId;

            if (!this.BlockInheritance)
            {
                par.Add("@locked", typeof(Boolean)).Value = this.Locked;
                sql += " locked = @locked";
            }
            else
            {
                sql += " locked = locked"; //Mantem o mesmo valor atual
            }

            if (!String.IsNullOrWhiteSpace(this.Login))
            {
                if (this.Login.Length > 50)
                    this.Login = this.Login.Substring(0, 49);

                par.Add("@login", typeof(String), this.Login.Length).Value = this.Login.ToLower();
                sql += ", login = @login";

                if (this.NewUser && String.IsNullOrWhiteSpace(this.FullName))
                    this.FullName = this.Login;

                LoginCache.AddItem(this.contextId, this.EntityId, this.Login);
            }

            if ((!this.BlockInheritance) && (!String.IsNullOrWhiteSpace(this.FullName)))
            {
                if (this.FullName.Length > 300)
                    this.FullName = this.FullName.Substring(0, 299);

                par.Add("@full_name", typeof(String), this.FullName.Length).Value = this.FullName;
                sql += ", full_name = @full_name";
                sql += ", alias = @alias";

                try
                {
                    String alias = this.FullName.Split(" ".ToCharArray())[0];
                    if (!String.IsNullOrWhiteSpace(alias))
                        par.Add("@alias", typeof(String), alias.Length).Value = alias;
                    else
                        par.Add("@alias", typeof(String), this.FullName.Length).Value = this.FullName;
                }
                catch { }
            }


            tmp.Stop(conn, trans);

            tmp = new TestTimer("UserData.UpdateUser->Crypt password", dLog);
            

            if ((!this.BlockInheritance) && ((!String.IsNullOrWhiteSpace(this.Password)) && (this.Password != this.AtualPassword)))
            {
                try
                {
                    String cPwd = this.Password;
                    using (CryptApi cApi = new CryptApi(this.enterpriseKey.ServerCert, Encoding.UTF8.GetBytes(this.Password)))
                        this.Password = Convert.ToBase64String(cApi.ToBytes());


                    par.Add("@password", typeof(String), this.Password.Length).Value = this.Password;
                    par.Add("@must", typeof(Boolean)).Value = this.MustChangePassword;
                    sql += ", password = @password, change_password = getdate(), must_change_password = @must";

                    if (this.NewUser)
                        AddUserLog(conn,LogKey.User_PasswordCreated, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resource, this.pluginId, EntityId, IdentityId, "Password created", (this.logPassword ? "Password: " + cPwd : ""), trans);
                    else
                        AddUserLog(conn,LogKey.User_PasswordChanged, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resource, this.pluginId, EntityId, IdentityId, "Password changed", (this.logPassword ? "New password: " + cPwd : ""), trans);
                }
                catch (Exception ex)
                {
                    AddUserLog(conn,LogKey.Encrypt_Error, null, "Engine", UserLogLevel.Warning, 0, 0, 0, this.resource, this.pluginId, EntityId, IdentityId, "Error encrypting password", ex.Message, trans);
                }

            }

            sql += " where id = @entityId";

            tmp.Stop(conn, trans);

            tmp = new TestTimer("UserData.UpdateUser->Update", dLog);
            
            ExecuteNonQuery(conn,sql, CommandType.Text, par, trans);


            tmp.Stop(conn, trans);

            tmp = new TestTimer("UserData.UpdateUser->Other", dLog);
            

            if (!String.IsNullOrWhiteSpace(this.Email))
            {

                ExecuteNonQuery(conn,"insert into entity_field select " + this.EntityId + ", " + this.mailField + ", '" + this.Email + "' where not exists (select 1 from entity_field where entity_id = " + this.EntityId + " and field_id = " + this.mailField + " and value = '" + this.Email + "')", CommandType.Text, null, trans);

                //_addIdentityFieldToDb(this.IdentityId, this.mailField, this.Email, trans);
                //ids.AddItem(this.contextId, this.EntityId, this.Email, false, true);

                EmailCache.AddItem(this.contextId, this.EntityId, this.Email);
            }

            //Caso o container tenha sido definido automaticamente, permite a troca de container
            //Caso contrário, não
            if (!String.IsNullOrEmpty(this.container) && (this.container != "\\"))
            {
                if (ExecuteScalar<Boolean>("SELECT case when COUNT(*) = 0 then cast(1 as bit) ELSE cast(0 as bit) END FROM [entity_container] with(nolock) where auto = 0 and entity_id = " + this.EntityId, trans))
                {
                    Int64 containerId = AddContainerTree(trans, this.container);
                    if (containerId > 0)
                        ExecuteNonQuery("DELETE FROM [entity_container] where auto = 1 and entity_id = " + this.EntityId + "; INSERT INTO [entity_container] ([entity_id], [container_id],[auto]) values (" + this.EntityId + "," + containerId + ",1)", trans);
                }
            }


            try
            {
                DbParameterCollection par2 = new DbParameterCollection();
                par2.Add("@entity_id", typeof(Int64)).Value = EntityId;

                ExecuteNonQuery(conn, "sp_rebuild_entity_keys2", CommandType.StoredProcedure, par2, trans);
            }
            catch(Exception ex) {
                AddUserLog(conn, LogKey.User_Update, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Error update entity index", ex.Message, trans);
            }

            AddUserLog(conn,LogKey.User_Update, null, "Engine", UserLogLevel.Info, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Updating user in IAM Database", "", trans);

            //Em caso de alteração de status marca para realizar o deploy em 15 minutos
            //Para em caso de registros duplicados vindos da origem, dar tempo de ocorrer todas as atualizações
            if (this.Locked != this.LastLocked)
                ExecuteNonQuery(conn,"insert into deploy_now values(" + this.EntityId + ", dateadd(MINUTE,15,GETDATE()))", CommandType.Text, null, trans);

            tmp.Stop(conn, trans);

        }


        public Int64 AddContainerTree(SqlTransaction trans, String container)
        {
            String[] tree = container.Trim("\\".ToCharArray()).Split("\\".ToCharArray());

            Int64 lastNode = 0;

            //O Root deve ser o único item com parent = a zero
            /*
            using (DbParameterCollection par = new DbParameterCollection())
            {
                par.Add("@context_id", typeof(Int64)).Value = contextId;
                par.Add("@parent_id", typeof(Int64)).Value = lastNode;
                par.Add("@name", typeof(String)).Value = "Root";

                lastNode = ExecuteScalar<Int64>("IF (NOT EXISTS (SELECT 1 FROM container with(nolock) WHERE context_id = @context_id and parent_id = @parent_id and name = @name)) begin insert into container (context_id, parent_id, [name]) values (@context_id,@parent_id,@name) end SELECT id FROM container with(nolock) WHERE context_id = @context_id and parent_id = @parent_id and name = @name", CommandType.Text, par, trans);
            }*/

            for (Int32 c = 0; c < tree.Length; c++)
            {
                //if ((c == 0) && (tree[c].Trim().ToLower() == "root"))
                //    continue;

                using (DbParameterCollection par = new DbParameterCollection())
                {
                    String containerName = tree[c];

                    try
                    {
                        containerName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(containerName);
                    }
                    catch { }


                    par.Add("@context_id", typeof(Int64)).Value = contextId;
                    par.Add("@parent_id", typeof(Int64)).Value = lastNode;
                    par.Add("@name", typeof(String)).Value = containerName;

                    lastNode = ExecuteScalar<Int64>("IF (NOT EXISTS (SELECT 1 FROM container with(nolock) WHERE context_id = @context_id and parent_id = @parent_id and name = @name)) begin insert into container (context_id, parent_id, [name]) values (@context_id,@parent_id,@name) end SELECT id FROM container with(nolock) WHERE context_id = @context_id and parent_id = @parent_id and name = @name", CommandType.Text, par, trans);
                }
            }

            return lastNode;
        }


        public void UpdateFields(SqlTransaction trans, Boolean enable_import)
        {

            String deleteSQL = "delete from identity_field where identity_id = " + this.IdentityId + " and field_id in ({0})";

            if (this.Login != null)
                foreach (PluginConfigMapping m in this.pluginConfig.mapping)
                    if ((m.is_login) && !this.fields.Exists(f => (f.Mapping.field_id == m.field_id && f.StringValue == this.Login)))
                        this.fields.Add(new UserDataFields((PluginConfigMapping)m.Clone(), this.Login));


            List<String> fds = new List<String>();

            foreach (UserDataFields u in fields)
            {
                fds.Add(u.Mapping.field_id.ToString());

                //Nome
                if (enable_import && u.Mapping.is_login && String.IsNullOrWhiteSpace(this.Login))
                        this.Login = u.StringValue;

                //Trata a senha
                if (enable_import && u.Mapping.is_password && !String.IsNullOrWhiteSpace(u.StringValue))
                {
                    this.Password = u.StringValue;
                    this.MustChangePassword = false;
                }

            }

            //Exclui todos que serão atualizados
            ExecuteNonQuery(conn,String.Format(deleteSQL, String.Join(",", fds)), CommandType.Text, null, trans);

            //Atualiza através de bulk por ser mais performático
            DataTable dtBulk = new DataTable();
            dtBulk.Columns.Add(new DataColumn("identity_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("field_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("value", typeof(String)));

            foreach (UserDataFields f in fields)
                if (!f.Mapping.is_password)
                    dtBulk.Rows.Add(new Object[] { this.IdentityId, f.Mapping.field_id, f.Value });

            BulkCopy(conn, dtBulk, "identity_field", trans);

        }


        public void MakeLogin(Boolean buildNew, SqlTransaction trans)
        {

            TestTimer tmp = null;
            try
            {

                tmp = new TestTimer("UserData.MakeLogin->Check old emails", dLog);

                if (!String.IsNullOrEmpty(this.Login))
                {
                    Log("\tLogins is not empty, exiting!");
                    return;
                }

                //Primeiro verifica nos e-mails se há algum login que possa ser utilizado
                foreach (String e in emails)
                {
                    String login = e.Split("@".ToCharArray())[0].Trim();
                    String domain = e.Split("@".ToCharArray(), 2)[1].Trim();

                    if (mailDomain.ToLower() == domain.ToLower())
                        if (!_loginExists(login))
                        {
                            Log("\tUsing login from e-mail address: " + e);

                            this.Login = login;
                            return;
                        }
                }

                tmp.Stop(conn, trans);


                //Verifica se o login está vindo de um campo ja mapeado
                foreach (UserDataFields u in fields)
                    if (u.Mapping.is_login && !String.IsNullOrEmpty(u.StringValue) && !String.IsNullOrWhiteSpace(u.StringValue))
                    {
                        Log("\tUsing login from imported field: Data Name = " + u.Mapping.data_name + ", " + u.StringValue);
                        this.Login = u.StringValue;
                    }

                if (!String.IsNullOrEmpty(this.Login))
                    return;

                if (!buildNew)
                    return;

                if (String.IsNullOrEmpty(this.FullName))
                {
                    Log("\tFull name is empty, exiting!");
                    throw new Exception("Full name is empty");
                }

                tmp = new TestTimer("UserData.MakeLogin->Load login rules", dLog);

                //Realiza as regras de login pós importação
                using (DataTable dtRules = Select(conn, "select * from login_rule lr with(nolock) inner join resource r with(nolock) on r.context_id = lr.context_id where r.id = " + resource + " order by [order]", trans))
                {

                    tmp.Stop(conn, trans);

                    if ((dtRules == null) && (dtRules.Rows.Count == 0))
                    {
                        Log("\tCreate login rules not found, exiting!");
                        return;
                    }

                    BaseCache.CheckHasOther check = new BaseCache.CheckHasOther(delegate(Int64 context_id, Int64 entity_id, String value)
                    {
                        Boolean e = LoginCache.HasOther(context_id, entity_id, value);
                        Log("\tLogin exists (context = " + context_id + ", entity_id = " + entity_id + ", value = " + value + ")? " + e.ToString());
                        return e;
                    });

                    foreach (DataRow dr in dtRules.Rows)
                    {
                        Log("\tTrying to create login using rule: " + dr["rule"].ToString());

                        tmp = new TestTimer("UserData.MakeLogin->_buildPrefixByRule", dLog);
                        //String login = _buildPrefixByRule(trans, this.FullName, dr["rule"].ToString(), dr["separator"].ToString());
                        String login = _buildPrefixByRule(trans, this.FullName, dr["rule"].ToString(), check);
                        tmp.Stop(conn, trans);
                        tmp = new TestTimer("UserData.MakeLogin->_loginExists (" + login + ")", dLog);

                        Boolean e = _loginExists(login);
                        Log("\tLogin '" + login + "' exists ?: " + e.ToString());
                        if (!e)
                        {
                            this.Login = login;
                            break;
                        }
                        tmp.Stop(conn, trans);
                    }
                }
            }
            finally
            {

                if (tmp != null)
                    tmp.Stop(conn, trans);

                if (String.IsNullOrEmpty(this.Login))
                    throw new Exception("Login is empty");

                Log("\tDefined Login: '" + this.Login);
                Log("");
                LoginCache.AddItem(this.contextId, this.EntityId, this.Login);

                //ids.AddItem(this.contextId, this.EntityId, this.Login, true, false);
            }
        }


        public String MakeEmailPrefixByRules(SqlTransaction trans, String mailDomain, Int64 mailFieldId)
        {
            String mailPrefix = "";
            TestTimer tmp = null;

            Log("\tLooking for e-mail in same mail domain...");
            DataTable dtC = Select(conn, "select * from identity_field ife with(nolock) inner join [identity] i with(nolock) on ife.identity_id = i.id where i.entity_id = " + this.EntityId + " and ife.field_id = " + mailFieldId + " and ife.value like '%@" + mailDomain + "'", trans);

            if ((dtC != null) && (dtC.Rows.Count > 0)) //Email do mesmo domínio ja cadastrado
            {
                mailPrefix = dtC.Rows[0]["value"].ToString().ToLower().Replace("@" + mailDomain.ToLower(), "");
                Log("\tMail prefix found: " + mailPrefix);
            }
            else
            {
                Log("\tE-mail in same mail domain not found");
            }

            if (String.IsNullOrEmpty(mailPrefix))
            {
                Log("\tMail prefix is empty, trying to create a new one");

                try
                {

                    if (String.IsNullOrEmpty(this.FullName))
                        throw new Exception("Full name is empty");

                    tmp = new TestTimer("UserData.MakeLogin->Load e-mail rules", dLog);

                    //Realiza as regras de login pós importação
                    using (DataTable dtRules = Select(conn, "select * from st_mail_rule lr with(nolock) inner join resource r with(nolock) on r.context_id = lr.context_id where r.id = " + resource + " order by [order]", trans))
                    {
                        

                        tmp.Stop(conn, trans);

                        if ((dtRules == null) && (dtRules.Rows.Count == 0))
                        {
                            Log("\tCreate e-mail rules not found, exiting!");
                            return "";
                        }

                        BaseCache.CheckHasOther check = new BaseCache.CheckHasOther(delegate(Int64 context_id, Int64 entity_id, String value)
                        {
                            Boolean e = EmailCache.HasOther(context_id, entity_id, value);
                            Log("\tE-mail exists (context = " + context_id + ", entity_id = " + entity_id + ", value = " + value + ")? " + e.ToString());
                            return e;

                        });

                        foreach (DataRow dr in dtRules.Rows)
                        {
                            Log("\tTrying to create login using rule: " + dr["rule"].ToString());

                            tmp = new TestTimer("UserData.MakeEmailPrefixByRules->_buildPrefixByRule", dLog);
                            String prefix = _buildPrefixByRule(trans, this.FullName, dr["rule"].ToString(), check);
                            tmp.Stop(conn, trans);
                            tmp = new TestTimer("UserData.MakeEmailPrefixByRules->_emailExists (" + prefix + "@" + mailDomain.ToLower() + ")", dLog);

                            Boolean e = _emailExists(prefix + "@" + mailDomain.ToLower());
                            Log("\tMail prefix '" + prefix + "' exists ?: " + e.ToString());
                            if (!_emailExists(prefix + "@" + mailDomain.ToLower()))
                            {
                                mailPrefix = prefix;
                                break;
                            }
                            tmp.Stop(conn, trans);
                        }
                    }
                }
                catch(Exception ex) {
                    Log("Erro creating e-mail pattern: " + ex.Message);
                    AddUserLog(conn, LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro creating e-mail pattern: " + ex.Message);
                }
            }

            return mailPrefix;
        }

        public void MakeEmail(SqlTransaction trans, String mailDomain, Int64 mailFieldId)
        {

            if (String.IsNullOrWhiteSpace(mailDomain))
                return;

            String mailPrefix = "";

            //Primeiramente verifica se existe uma regra para criação de e-mail diferente da criação de login
            Log("\tBuilding mail prefix...");
            mailPrefix = MakeEmailPrefixByRules(trans, mailDomain, mailFieldId);
            Log("\tMail prefix: " + mailPrefix);

            if (String.IsNullOrEmpty(mailPrefix))
            {
                Log("\tMail prefix is empty, trying to use Login as mail prefix");

                if (String.IsNullOrWhiteSpace(this.Login))
                {
                    Log("\tMail login is empty, exiting!");
                    return;
                }

                this.Email = this.Login + "@" + mailDomain;
            }
            else
            {
                this.Email = mailPrefix + "@" + mailDomain;
            }

            Log("\tDefined e-mail: " + this.Email);
            Log("");

            EmailCache.AddItem(this.contextId, this.EntityId, this.Email);

        }


        public void BuildPassword(SqlTransaction trans)
        {
            if (!this.NewUser)
                return;

            if (!String.IsNullOrWhiteSpace(this.Password))
                return;

            String pwdMethod = "random";
            String pwdValue = "";

            using (DataTable dtRules = Select(conn, "select password_rule from context c with(nolock) inner join resource r with(nolock) on r.context_id = c.id where r.id = " + resource + " and (c.password_rule is not null and rtrim(LTRIM(c.password_rule)) <> '')", trans))
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
                    using (DataTable dtFields = Select(conn, "select * from identity_field with(nolock) where identity_id = " + this.IdentityId + " and field_id = " + fieldId, trans))
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
                pwdValue = RandomPassword.Generate(14, 16);

            this.MustChangePassword = true;

            /*
            String pwd = "";
            using (ServerKey sk = new ServerKey(db.Connection, trans))
            using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(pwdValue)))
                pwd = Convert.ToBase64String(cApi.ToBytes());*/

            this.logPassword = true;
            this.Password = pwdValue;
        }


        public Boolean Ignore(IgnoreRules ignoreRules, Uri pluginUri)
        {
            //Nova verificação de ignorar
            FilterRuleCollection col = ignoreRules.GetItem(this.resource, pluginUri.AbsoluteUri);
            if (col == null)
                return false;

            FilterChecker chk = new FilterChecker(col);
            try
            {
                foreach (UserDataFields f in this.fields)
                    chk.AddFieldData(f.Mapping.field_id, f.Mapping.data_type, f.StringValue);
            }
            catch (Exception ex)
            {
                Log("Erro on load properties of ignore checker: " + ex.Message);
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on load properties of ignore checker: " + ex.Message);
                return false;
            }

            if (chk.DataCount == 0)
                return false;

            //por padrão considera o usuário como não ignorado
            Boolean ignored = false;
            try
            {

                //Verifica se se enquadra nas regras para bloqueio
                foreach (FilterMatch m in chk.Matches())
                    if (m.Success)
                    {
                        String info = GetDataText("Proccess data", this.fields);
                        info += "Filter: " + m.Filter.FilterName + Environment.NewLine;
                        info += m.Filter.ToString();

                        AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Warning, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "User data ignored by filter " + m.Filter.FilterName, info);

                        Log("User data ignored by filter " + m.Filter.FilterName);
                        Log("\t" + m.Filter.ToString());

                        ignored = true;

                    }
            }
            catch (Exception ex)
            {
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on ignore checker: " + ex.Message);
                throw new Exception("Erro on ignore checker: " + ex.Message);
            }

            return ignored;
        }

        public String GetDataText(String title, List<UserDataFields> fields)
        {
            String info = "";
            info += title + ": " + (fields.Count == 0 ? "empty" : "") + Environment.NewLine;
            foreach (UserDataFields f in fields)
                info += "\t[" + f.Mapping.data_name.ToLower() + "] " + (f.Mapping.is_id ? "is ID" : (f.Mapping.is_unique_property ? "is Unique field" : "")) + " = " + (f.Mapping.is_password ? "*****" : f.Value) + Environment.NewLine;
            info += Environment.NewLine;

            return info;
        }


        public void CheckLock(LockRules lockRules, Uri pluginUri)
        {
            //Nova verificação de bloqueio
            FilterRuleCollection col = lockRules.GetItem(this.resource, pluginUri.AbsoluteUri);
            if (col == null)
                return;

            FilterChecker chk = new FilterChecker(col);
            try
            {
                foreach (UserDataFields f in this.fields)
                    chk.AddFieldData(f.Mapping.field_id, f.Mapping.data_type, f.StringValue);
            }
            catch (Exception ex)
            {
                Log("Erro on load properties of LockChecker: " + ex.Message);
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on load properties of LockChecker: " + ex.Message);
                return;
            }

            if (chk.DataCount == 0)
                return;

            try
            {
                //por padrão considera o usuário como desbloqueado
                Boolean oldState = this.Locked;
                this.Locked = false;

                LockedInfo = "";

                //Verifica se se enquadra nas regras para bloqueio
                foreach (FilterMatch m in chk.Matches())
                    if (m.Success)
                    {
                        if ((!oldState) && (this.EntityId != 0))
                            AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "User locked by filter " + m.Filter.FilterName, m.Filter.ToString());

                        //Log("User locked by filter " + m.Filter.FilterName);
                        //Log("\t" + m.Filter.ToString());


                        LockedInfo += "User locked by filter " + m.Filter.FilterName + Environment.NewLine;
                        LockedInfo += "\t" + m.Filter.ToString() + Environment.NewLine;
                        LockedInfo += "" + Environment.NewLine;
                        

                        this.Locked = true;
                        
                    }
            }
            catch (Exception ex)
            {
                AddUserLog(conn,LogKey.User_ImportInfo, null, "Engine", UserLogLevel.Error, 0, 0, 0, this.resource, this.pluginId, this.EntityId, this.IdentityId, "Erro on LockChecker: " + ex.Message);
                throw new Exception("Erro on LockChecker: " + ex.Message);
            }

            if (!String.IsNullOrEmpty(LockedInfo))
            {
                LockedInfo += GetDataText("Proccess data", this.fields);
                Log(LockedInfo);
            }

        }

        public void Dispose()
        {
            //Nd
        }


        private String _buildPrefixByRule(SqlTransaction trans, String fullName, String rule, BaseCache.CheckHasOther checkDelegate)
            //private String _buildPrefixByRule(SqlTransaction trans, String fullName, String rule, String separator)
        {
            String login = "";

            String[] nameParts = _removerAcentos(fullName).Trim().Split(" ".ToCharArray());
            String[] parts = rule.ToLower().Split(",".ToCharArray());

            //if (separator == null)
            //    separator = "";

            foreach (String p in parts)
            {
                switch (p.Trim())
                {
                    case "first_name":
                        //if (login != "") login += separator.Trim();
                        login += nameParts[0];
                        break;

                    case "last_name":
                        if (nameParts.Length == 1) continue;
                        //if (login != "") login += separator.Trim();
                        login += nameParts[nameParts.Length - 1];
                        break;

                    case "second_name":
                        if (nameParts.Length == 1) continue;
                        //if (login != "") login += separator.Trim();
                        String p1 = nameParts[1];
                        if ((p1.Length <= 3) && (nameParts.Length >= 3))
                            login += nameParts[2];
                        else
                            login += nameParts[1];

                        break;

                    case "char_first_name":
                        //if (login != "") login += separator.Trim();
                        String t1 = nameParts[0];
                        if (t1.Length > 0)
                            login += t1.Substring(0, 1);
                        break;

                    case "char_last_name":
                        if (nameParts.Length == 1) continue;
                        //if (login != "") login += separator.Trim();
                        String t2 = nameParts[nameParts.Length - 1];
                        if (t2.Length > 0)
                            login += t2.Substring(0, 1);
                        break;

                    case "char_second_name":
                        if (nameParts.Length == 1) continue;
                        //if (login != "") login += separator.Trim();
                        String p2 = nameParts[1];
                        String t3 = "";
                        if ((p2.Length <= 3) && (nameParts.Length >= 3))
                            t3 += nameParts[2];
                        else
                            t3 += nameParts[1];

                        if (t3.Length > 0)
                            login += t3.Substring(0, 1);

                        break;

                    case "dot":
                            login += ".";
                        break;

                    case "hyphen":
                        login += "-";
                        break;


                    case "index":
                        login += "%";
                        break;
                }
            }

            login = login.ToLower();

            Int32 loc = login.IndexOf("%");
            if (loc != -1)
            {
                /*
                List<Int32> exists = new List<Int32>();
                //Seleciona todos os outros para identificar os indices
                //using (DataTable dtRules = Select(conn, "select e.*, ife.value login from entity e inner join [identity] i on e.id = i.entity_id inner join resource_plugin rp on rp.id = i.resource_plugin_id inner join identity_field ife on ife.identity_id = i.id and ife.field_id = rp.login_field_id inner join resource r on r.context_id = e.context_id and rp.resource_id = r.id where r.id = " + this.resource + " and e.id <> " + this.EntityId + " and ife.value like '" + login + "'", trans))
                using (DataTable dtRules = Select(conn, "select * from vw_entity_logins2 l with(nolock) where l.context_id = " + this.contextId + " and l.id <> " + this.EntityId + " and l.login like  '" + login + "'", trans))
                    if ((dtRules != null) && (dtRules.Rows.Count > 0))
                    {
                        foreach (DataRow dr in dtRules.Rows)
                        {
                            Int32 i = -1;
                            String fLogin = dr["login"].ToString();
                            if (fLogin.Length > loc) //O login é o unico e ainda não tem um número indexador
                            {
                                for (Int32 l = 1; i <= fLogin.Length - loc; l++)
                                    try
                                    {
                                        i = Int32.Parse(fLogin.Substring(loc, l));
                                    }
                                    catch
                                    {
                                        break;
                                    }
                            }
                            //Int32.TryParse(fLogin[loc].ToString(), out i);
                            if (i != -1)
                            {
                                if (!exists.Contains(i))
                                    exists.Add(i);
                            }
                        }

                    }

                for (Int32 index = 1; index < Int32.MaxValue; index++)
                    if (!exists.Contains(index))
                    {
                        login = login.Replace("%", index.ToString());
                        break;
                    }*/

                //Novo método
                for (Int32 index = 1; index < Int32.MaxValue; index++)
                {
                    String tst = login.Replace("%", index.ToString());
                    if (!checkDelegate(this.contextId, this.EntityId, tst))
                    {
                        login = tst;
                        break;
                    }
                }
            }

            return login;
        }


        private Boolean _emailExists(String email)
        {
            return EmailCache.HasOther(this.contextId, this.EntityId, email);
        }


        private Boolean _loginExists(String login)
        {

            return LoginCache.HasOther(this.contextId, this.EntityId, login);

            //using (DataTable dtRules = Select(conn, "select * from entity e with(nolock) inner join [resource] r with(nolock) on r.context_id = e.context_id inner join resource_plugin rp with(nolock) on rp.resource_id = r.id where e.id <> " + this.EntityId + " and (e.login = '" + login + "' or exists(select 1 from identity_field ife with(nolock) inner join [identity] i with(nolock) on i.id = ife.identity_id where ife.field_id = rp.login_field_id and [value] = '" + login + "'))"))
            /*using (DataTable dtRules = Select(conn, "select * from entity e with(nolock) inner join [identity] i with(nolock) on e.id = i.entity_id inner join resource_plugin rp with(nolock) on rp.id = i.resource_plugin_id left join identity_field ife with(nolock) on ife.field_id = rp.login_field_id and ife.identity_id = i.id where e.id <> " + this.EntityId + " and e.context_id = " + this.contextId + " and (e.login = '" + login + "' or ife.[value] = '" + login + "')"))
                if ((dtRules != null) && (dtRules.Rows.Count > 0))
                    return true;*/

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@context_id", typeof(Int64)).Value = this.contextId;
            par.Add("@entity_id", typeof(Int64)).Value = this.EntityId;
            par.Add("@login", typeof(String)).Value = login;

            return ExecuteScalar<Boolean>(conn,"sp_check_login_exists", CommandType.StoredProcedure, par);

            //return ids.HasOtherLogin(this.contextId, this.EntityId, login);

            return false;
        }



        private string _removerAcentos(string texto)
        {
            string s = texto.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < s.Length; k++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(s[k]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(s[k]);
                }
            }

            String s1 = sb.ToString();
            s1 = Regex.Replace(s1, "[^0-9a-zA-Z ]+", "");

            return s1;
        }
        



    }
}
