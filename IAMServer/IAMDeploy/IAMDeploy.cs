using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
//using IAM.SQLDB;
using IAM.Log;
using IAM.Config;
using IAM.GlobalDefs;
using IAM.CA;
using IAM.PluginInterface;
using IAM.License;
using IAM.GlobalDefs;

namespace IAM.Deploy
{
    public class IAMDeploy: IDisposable
    {
        private String basePath = "";
        private String moduleSender = "";
        private IAMDatabase db;
        private DirectoryInfo outDirBase;
        private StringBuilder executionLog;

        public String ExecutionLog { get { return (executionLog != null ? executionLog.ToString() : ""); } }


        public IAMDeploy(String moduleSender, String connectionString, String basePath)
        {

            this.executionLog = new StringBuilder();
            this.moduleSender = moduleSender;

            db = new IAMDatabase(connectionString);
            db.openDB();

            DirectoryInfo tmp = new DirectoryInfo(basePath);
            if (tmp.Name.ToLower() == "out")
                outDirBase = new DirectoryInfo(tmp.FullName);
            else
                outDirBase = new DirectoryInfo(Path.Combine(basePath, "Out"));

            if (!outDirBase.Exists)
                outDirBase.Create();

        }

        public IAMDeploy(String moduleSender, String sqlServer, String sqlDb, String sqlUsername, String sqlPassword)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            this.executionLog = new StringBuilder();
            this.moduleSender = moduleSender;

            db = new IAMDatabase(sqlServer, sqlDb, sqlUsername, sqlPassword);
            db.openDB();

            outDirBase = new DirectoryInfo(Path.Combine(basePath, "Out"));
            if (!outDirBase.Exists)
                outDirBase.Create();

        }

        public Int32 DeployOne(Int64 entityId)
        {
            this.executionLog.AppendLine("Starting DeployOne");
            return _Deploy(entityId, 0);
        }


        public Int32 DeployResourcePlugin(Int64 resourcePluginId)
        {
            this.executionLog.AppendLine("Starting DeployResourcePlugin");
            return _Deploy(0, resourcePluginId);
        }

        public Int32 DeployAll()
        {
            this.executionLog.AppendLine("Starting DeployAll");
            return _Deploy(0, 0);
        }

        private void DebugLog(Int64 entityId, String text)
        {
            this.executionLog.AppendLine("[" + entityId + "] " + text);
#if DEBUG
            db.AddUserLog(LogKey.Undefined, null, "Deploy", UserLogLevel.Debug, 0, 0, 0, 0, entityId, 0, 0, text);
#endif
        }

        private Int32 _Deploy(Int64 entityId, Int64 resourcePluginId)
        {
            //Busca todos os plugins e recursos a serem publicados
            DataTable dtPlugins = null;
            Dictionary<Int64, LicenseControl> licControl = null;
            DataTable dtEnt = null;
            Int32 packageCount = 0;

            StringBuilder deployLog = new StringBuilder();

            try
            {
                dtPlugins = db.Select("select r.context_id, p.id, p.scheme, p.uri, p.assembly, p.create_date, rp.id resource_plugin_id, r.id resource_id, r.proxy_id, p1.name as proxy_name, p1.id proxy_id, p1.enterprise_id, rp.deploy_after_login, rp.password_after_login, rp.deploy_process, rp.deploy_all, rp.deploy_password_hash, rp.use_password_salt, rp.password_salt_end, rp.password_salt from plugin p with(nolock)  inner join resource_plugin rp with(nolock) on rp.plugin_id = p.id  inner join [resource] r on r.id = rp.resource_id inner join proxy p1 on r.proxy_id = p1.id  where " + (resourcePluginId > 0 ? " rp.id = " + resourcePluginId + " and " : "") + " r.enabled = 1 and rp.enabled = 1 and rp.enable_deploy = 1 order by rp.[order]");
                if ((dtPlugins == null) || (dtPlugins.Rows.Count == 0))
                {
                    if ((entityId > 0) || (resourcePluginId > 0))
                        throw new Exception("0 plugin to process");

                    //TextLog.Log(moduleSender, "\t0 plugin to process");
                    DebugLog(entityId, "0 plugin to process");
                    return 0;
                }

                DebugLog(entityId, dtPlugins.Rows.Count + " plugin to process");

                licControl = new Dictionary<long, LicenseControl>();

                String rolesText = "";

                //Lista todos os plugins e resources habilitados
                foreach (DataRow dr in dtPlugins.Rows)
                {
                    deployLog = new StringBuilder();

                    DebugLog(entityId, "proxy_name = " + dr["proxy_name"].ToString() + ", plugin = " + dr["uri"].ToString() + ", deploy_all? " + dr["deploy_all"].ToString());

                    ProxyConfig config = new ProxyConfig(true);
                    config.GetDBCertConfig(db.Connection, Int64.Parse(dr["enterprise_id"].ToString()), dr["proxy_name"].ToString());

                    DirectoryInfo proxyDir = new DirectoryInfo(Path.Combine(outDirBase.FullName, dr["proxy_id"].ToString() + "_" + dr["proxy_name"].ToString() + "\\" + Path.GetFileNameWithoutExtension(dr["assembly"].ToString()) + "\\rp" + dr["resource_plugin_id"].ToString()));

                    List<PluginConnectorBaseDeployPackage> packageList = new List<PluginConnectorBaseDeployPackage>();
                    List<Int64> roles = new List<Int64>();

                    Int64 enterpriseId = (Int64)dr["enterprise_id"];

                    LicenseControl lic = null;
                    if (!licControl.ContainsKey(enterpriseId))
                    {
                        lic = LicenseChecker.GetLicenseData(db.Connection, null, enterpriseId);
                        licControl.Add(enterpriseId, lic);
                    }
                    else
                    {
                        lic = licControl[enterpriseId];
                    }

                    if (!lic.Valid)
                    {
                        if (!lic.Notified)
                            db.AddUserLog(LogKey.Licence_error, null, "Deploy", UserLogLevel.Error, (Int64)dr["proxy_id"], (Int64)dr["enterprise_id"], 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "License error: " + lic.Error);
                        lic.Notified = true;
                        continue;
                    }


                    if (!(Boolean)dr["deploy_all"])
                    {
                        //Busca os "roles" top
                        String rolesSQL = "select rpr.* from resource_plugin_role rpr with(nolock) inner join resource_plugin rp on rpr.resource_plugin_id = rp.id where rp.resource_id =  " + dr["resource_id"].ToString() + " and rp.plugin_id = " + dr["id"];
                        DebugLog(entityId, "Role SQL = " + rolesSQL);

                        DataTable dtRoles = db.Select(rolesSQL);
                        if (dtRoles == null)
                        {
                            db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Error, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "DB error: " + (((db.LastDBError != null) && (db.LastDBError != "")) ? db.LastDBError : ""));
                            continue;
                        }

                        List<String> roleNames = new List<String>();

                        //Busca toda a arvore de "roles" a se buscar
                        foreach (DataRow drR in dtRoles.Rows)
                        {
                            DataTable dtR = db.Select("select * from dbo.fn_selectRoleTree(" + drR["role_id"] + ")");
                            if (dtR == null)
                                continue;

                            foreach (DataRow drRT in dtR.Rows)
                                if (!roles.Contains((Int64)drRT["role_id"]))
                                {
                                    roleNames.Add(drRT["name"].ToString());
                                    roles.Add((Int64)drRT["role_id"]);
                                }
                        }

                        if (roles.Count == 0)
                        {
                            db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Info, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "Not found roles x identities to deploy");
                            continue;
                        }

                        //Para efeitos de log captura o nome dos roles
                        rolesText = String.Join(", ", roleNames);

                        dtRoles.Clear();
                        dtRoles = null;
                    }

                    //Seleciona todas as entidades do mesmo contexto
                    //Esta listagem considera somente as entidades pertencentes aos plugins de entrada
                    String sql = "select e.id, e.last_login, e.change_password, i.id identity_id from entity e with(nolock) inner join resource r with(nolock) on e.context_id = r.context_id inner join [identity] i with(nolock) on i.entity_id = e.id inner join [resource_plugin] rp with(nolock) on i.resource_plugin_id = rp.id where i.deleted = 0 and e.deleted = 0 {0} and e.context_id = " + dr["context_id"] + (entityId > 0 ? " and e.id = " + entityId : "") + " and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) group by e.id, e.last_login, e.change_password, i.id";

                    if (!(Boolean)dr["deploy_all"])
                        sql = "select e.id, e.last_login, e.change_password, i.id identity_id from entity e with(nolock) inner join resource r with(nolock) on e.context_id = r.context_id inner join [identity] i with(nolock) on i.entity_id = e.id inner join [resource_plugin] rp with(nolock) on i.resource_plugin_id = rp.id inner join identity_role ir with(nolock) on ir.identity_id = i.id  inner join (select rpr.role_id from	resource_plugin_role rpr with(nolock) inner join resource_plugin rp with(nolock) on rp.id = rpr.resource_plugin_id inner join resource r with(nolock) on r.id = rp.resource_id where r.id = " + dr["resource_id"].ToString() + ") ro on ro.role_id =  ir.role_id where i.deleted = 0 and e.deleted = 0 {0} and ir.role_id in (" + String.Join(",", roles) + ")" + (entityId > 0 ? " and e.id = " + entityId : "") + " and not exists (select 1 from identity_block_inheritance bi where bi.identity_id = i.id) and e.context_id = " + dr["context_id"] + " group by e.id, e.last_login, e.change_password, i.id";

                    DebugLog(entityId, String.Format(sql, "and rp.enable_import = 1 and rp.permit_add_entity = 1"));

                    //Lista todas as entidades e identidades para exportar
                    dtEnt = db.Select(String.Format(sql, "and rp.enable_import = 1 and rp.permit_add_entity = 1"));
                    if (dtEnt == null)
                    {
                        DebugLog(entityId, "SQL result is empty");
                        db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Error, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "DB error: " + (((db.LastDBError != null) && (db.LastDBError != "")) ? db.LastDBError : ""));
                        continue;
                    }

                    if (dtEnt.Rows.Count == 0)
                    {
                        DebugLog(entityId, "SQL result is empty, trying with all plugins");
                        DebugLog(entityId, String.Format(sql, ""));

                        //Lista todas as entidades e identidades para exportar
                        dtEnt = db.Select(String.Format(sql, ""));
                        if (dtEnt == null)
                        {
                            DebugLog(entityId, "SQL result is empty");
                            db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Error, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "DB error: " + (((db.LastDBError != null) && (db.LastDBError != "")) ? db.LastDBError : ""));
                            continue;
                        }
                    }
                    sql = null;

                    DebugLog(entityId, "SQL result count " + dtEnt.Rows.Count);

                    if ((dtEnt.Rows.Count > 0) && (entityId == 0))
                        deployLog.AppendLine("Starting check to deploy " + dtEnt.Rows.Count + " identities for " + ((!(Boolean)dr["deploy_all"]) ? rolesText : "all users"));

                    Int32 total = dtEnt.Rows.Count;
                    Int32 licError = 0;
                    Int32 loguedIgnore = 0;
                    Int32 deploy = 0;

                    //db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Info, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "Deploy with " + dtEnt.Rows.Count + " identities for " + ((!(Boolean)dr["deploy_all"]) ? rolesText : "all users"));
                    foreach (DataRow drE in dtEnt.Rows)
                    {

                        //Checagens de licenciamento
                        lic.Count++;

                        if ((lic.Entities > 0) && (lic.Count > lic.Entities))
                        {
                            db.AddUserLog(LogKey.Licence_error, null, "Deploy", UserLogLevel.Error, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], (Int64)drE["id"], (Int64)drE["identity_id"], "License error: License limit (" + lic.Entities + " entities) exceeded");
                            licError++;
                            continue;
                        }

                        try
                        {
                            if (((Boolean)dr["deploy_after_login"]) && (drE["last_login"] == DBNull.Value))
                            {
                                db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Info, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], (Int64)drE["id"], (Int64)drE["identity_id"], "User NOT addedd in deploy package because the user is not logged in yet");
                                loguedIgnore++;
                                continue;
                            }

                            //db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Info, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], (Int64)drE["id"], (Int64)drE["identity_id"], "Identity addedd in deploy package");

                            PluginConnectorBaseDeployPackage newPkg = DeployPackage.GetPackage(db, (Int64)dr["proxy_id"], (Int64)dr["resource_plugin_id"], (Int64)drE["id"], (Int64)drE["identity_id"], (Boolean)dr["password_after_login"], (drE["change_password"] == DBNull.Value ? null : (DateTime?)drE["change_password"]), (dr["deploy_password_hash"] == DBNull.Value ? "none" : dr["deploy_password_hash"].ToString()), (Boolean)dr["use_password_salt"], (Boolean)dr["password_salt_end"], dr["password_salt"].ToString());
                            packageList.Add(newPkg);

                            deploy++;

#if DEBUG
                            try
                            {
                                db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Debug, 0, enterpriseId, 0, (Int64)dr["resource_id"], (Int64)dr["id"], newPkg.entityId, newPkg.identityId, "Package generated: " + newPkg.pkgId, SafeTrend.Json.JSON.Serialize<PluginConnectorBaseDeployPackage>(newPkg));
                            }
                            catch { }
#endif

                            packageCount++;
                        }
                        catch (Exception ex)
                        {
                            db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Info, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], (Int64)drE["id"], (Int64)drE["identity_id"], "Erro on deploy user: " + ex.Message);
                        }

                        if (packageList.Count > 500)
                        {
                            SaveToSend(enterpriseId, proxyDir, config, packageList);
                            packageList.Clear();
                        }
                    }

                    if (packageList.Count > 0)
                        SaveToSend(enterpriseId, proxyDir, config, packageList);

                    if (packageList != null)
                    {

                        for (Int32 i = 0; i < packageList.Count; i++)
                            packageList[i].Dispose();

                        packageList.Clear();
                    }

                    packageList = null;
                    config = null;


                    deployLog.AppendLine("Total identities: " + total);
                    deployLog.AppendLine("Ignored by licence check: " + licError);
                    deployLog.AppendLine("Ignored by first login rule: " + loguedIgnore);
                    deployLog.AppendLine("Published: " + deploy);

                    db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Info, (Int64)dr["proxy_id"], 0, 0, (Int64)dr["resource_id"], (Int64)dr["id"], 0, 0, "Deploy package generated for " + ((!(Boolean)dr["deploy_all"]) ? rolesText : "all users"), deployLog.ToString());


                }

                db.closeDB();
                db.Dispose();
            }
            catch(Exception ex)
            {
                DebugLog(entityId, "Erro on Deploy: " + ex.Message);
                throw ex;
            }
            finally
            {

                deployLog.Clear();
                deployLog = null;

                if (dtPlugins != null) dtPlugins.Clear();
                dtPlugins = null;

                if (dtEnt != null) dtEnt.Clear();
                dtEnt = null;

                if (licControl != null)
                {
                    try
                    {
                        List<Int64> k = new List<Int64>();
                        k.AddRange(licControl.Keys);

                        foreach (Int64 l in k)
                            if (licControl[l] != null)
                            {
                                licControl[l].Dispose();
                                licControl[l] = null;
                            }

                        k.Clear();
                    }
                    catch { }
                }
                licControl = null;

            }

            return packageCount;
        }

        private void SaveToSend(Int64 enterpriseId, DirectoryInfo saveTo, ProxyConfig config, List<PluginConnectorBaseDeployPackage> packages)
        {
            if ((packages == null) || (packages.Count == 0))
                return;

            Byte[] jData = Encoding.UTF8.GetBytes(SafeTrend.Json.JSON.Serialize <List<PluginConnectorBaseDeployPackage>>(packages));
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
            using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), jData))
            {
                if (!saveTo.Exists)
                    saveTo.Create();

                FileInfo f = new FileInfo(Path.Combine(saveTo.FullName, DateTime.Now.ToString("yyyyMMddHHmss-ffffff")) + ".iamdat");

                File.WriteAllBytes(f.FullName, cApi.ToBytes());
#if DEBUG
                db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Info, 0, enterpriseId, 0, 0, 0, 0, 0, "File to send created " + f.Name + " (" + packages.Count + ")");

                try
                {
                    foreach(PluginConnectorBaseDeployPackage pkg in packages)
                        db.AddUserLog(LogKey.Deploy, null, "Deploy", UserLogLevel.Debug, 0, enterpriseId, 0, 0, 0, pkg.entityId, pkg.identityId, "Saving package ID: " + pkg.pkgId, SafeTrend.Json.JSON.Serialize<PluginConnectorBaseDeployPackage>(pkg));
                }
                catch { }

#endif
            }

        }

        public void Dispose()
        {
            this.basePath = null;
            this.moduleSender = null;

            this.outDirBase = null;

            if (this.db != null) this.db.Dispose();
            this.db = null;

            this.executionLog.Clear();
            this.executionLog = null;
        }

    }
}
