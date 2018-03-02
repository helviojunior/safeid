using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.IO.Compression;

using IAM.PluginInterface;
using IAM.PluginManager;
using IAM.Log;
using IAM.Config;
using IAM.Scheduler;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;
//using IAM.SQLDefs;

//Impersonate
using System.Security.Principal;
using System.Runtime.InteropServices;


namespace IAM.PluginStarter
{
    public class sortOndate : IComparer<FileInfo>
    {
        public int Compare(FileInfo a, FileInfo b)
        {
            if (b.LastWriteTime > a.LastWriteTime) return 1;
            else if (b.LastWriteTime < a.LastWriteTime) return -1;
            else return 0;
        }
    }

    class ConnectorStarter
    {
        /*
        #region Impersonate
        public const int LOGON32_LOGON_INTERACTIVE = 2;
        public const int LOGON32_PROVIDER_DEFAULT = 0;

        WindowsImpersonationContext impersonationContext;

        [DllImport("advapi32.dll")]
        public static extern int LogonUserA(String lpszUserName,
            String lpszDomain,
            String lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken,
            int impersonationLevel,
            ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        private bool impersonateValidUser(String userName, String domain, String password)
        {
            WindowsIdentity tempWindowsIdentity;
            IntPtr token = IntPtr.Zero;
            IntPtr tokenDuplicate = IntPtr.Zero;

            if (RevertToSelf())
            {
                if (LogonUserA(userName, domain, password, LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT, ref token) != 0)
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                        impersonationContext = tempWindowsIdentity.Impersonate();
                        if (impersonationContext != null)
                        {
                            CloseHandle(token);
                            CloseHandle(tokenDuplicate);
                            return true;
                        }
                    }
                }
            }
            if (token != IntPtr.Zero)
                CloseHandle(token);
            if (tokenDuplicate != IntPtr.Zero)
                CloseHandle(tokenDuplicate);
            return false;
        }

        private void undoImpersonation()
        {
            impersonationContext.Undo();
        }

        #endregion Impersonate
        */

        private PluginConnectorBase plugin = null;

        private String basePath;
        private ProxyConfig config;
        private Boolean executing = false;
        private LogProxy logProxy;
        private Timer pluginsTimer;
        private Int64 executionCount = 0;

        public Boolean Executing { get { return executing; } }

        public ConnectorStarter(String ConfigJson, PluginConnectorBase plugin)
        {

            if (plugin == null)
                throw new Exception("Plugin is null");

            if (String.IsNullOrWhiteSpace(ConfigJson))
                throw new Exception("Config is null or empty");

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            config = new ProxyConfig();
            config.FromJsonString(ConfigJson);
            ConfigJson = null;

            this.plugin = plugin;

            logProxy = new LogProxy(basePath, config.server_cert);

            pluginsTimer = new Timer(new TimerCallback(TimerCallback), null, 1000, 60000);

        }

        public void NewConfig(String ConfigJson)
        {

            if (plugin == null)
                throw new Exception("Plugin is null");

            if (String.IsNullOrWhiteSpace(ConfigJson))
                throw new Exception("Config is null or empty");

            ProxyConfig configTmp = new ProxyConfig();
            configTmp.FromJsonString(ConfigJson);
            ConfigJson = null;

            //Se tudo ocorreu sem erro altera a config local

            config.Dispose();
            config = null;

            config = configTmp;
        }

        private void ExecuteConnector()
        {
            ExecuteConnector(false);
        }

        private void ExecuteConnector(Boolean deployOnly)
        {

            List<Int64> resource_plugin = new List<Int64>();

            //Separa os contextos
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
            OpenSSL.X509.X509Certificate cert = CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass);

            try
            {
                foreach (PluginConfig p in config.plugins)
                {
                    if (p.uri.ToLower() == plugin.GetPluginId().AbsoluteUri.ToLower())
                    {
                        JsonGeneric pgConf = new JsonGeneric();
                        try
                        {

                            using (CryptApi cApi = CryptApi.ParsePackage(cert, Convert.FromBase64String(p.parameters)))
                                pgConf.FromJsonString(Encoding.UTF8.GetString(cApi.clearData));

                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Decrypt error1 " + ex.Message);
                        }
                        finally
                        {
                            pgConf = null;
                        }

                        if (!resource_plugin.Contains(p.resource_plugin))
                            resource_plugin.Add(p.resource_plugin);

                    }

                }


                foreach (Int64 rp in resource_plugin)
                {
                    DebugLog("{" + plugin.GetPluginId().AbsoluteUri + "} Resource plugin " + rp);

                    Dictionary<String, Object> connectorConf = new Dictionary<String, Object>();
                    List<PluginConnectorBaseDeployPackageMapping> mapping = new List<PluginConnectorBaseDeployPackageMapping>();

                    Boolean enableDeploy = false;

                    Int64 r = 0;

                    try
                    {

                        foreach (PluginConfig p in config.plugins)
                        {
                            if ((p.uri.ToLower() == plugin.GetPluginId().AbsoluteUri.ToLower()) && (p.resource_plugin == rp))
                            {
                                r = p.resource;

                                Dictionary<String, String> tmp = new Dictionary<string, string>();
                                foreach (PluginConfigMapping m in p.mapping)
                                    mapping.Add(new PluginConnectorBaseDeployPackageMapping(m.data_name, m.data_type, m.is_id, m.is_unique_property, m.is_password, m.is_login, m.is_name));

                                enableDeploy = p.enable_deploy;

                                JsonGeneric pgConf = new JsonGeneric();
                                try
                                {
                                    if (cert == null)
                                        throw new Exception("Certificate is null");

                                    using (CryptApi cApi = CryptApi.ParsePackage(cert, Convert.FromBase64String(p.parameters)))
                                        pgConf.FromJsonString(Encoding.UTF8.GetString(cApi.clearData));
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Decrypt error: " + ex.Message);
                                }

                                if ((pgConf.data == null) || (pgConf.data.Count == 0))
                                    continue;

                                Int32 kCol = pgConf.GetKeyIndex("key");
                                Int32 vCol = pgConf.GetKeyIndex("value");

                                if (!String.IsNullOrWhiteSpace(p.mail_domain))
                                    PluginBase.FillConfig(plugin, ref connectorConf, "iam_mail_domain", p.mail_domain);
                                //connectorConf.Add("iam_mail_domain", p.mail_domain);

                                foreach (String[] d1 in pgConf.data)
                                    PluginBase.FillConfig(plugin, ref connectorConf, d1[kCol], d1[vCol].ToString());
                                /*
                                if (!connectorConf.ContainsKey(d1[kCol]))
                                    connectorConf.Add(d1[kCol], d1[vCol].ToString());*/
                            }

                        }

                        //Deploy ocorre antes da importação
                        //Para que na importação ja apareça os registros que foram publicados pelo deploy
                        try
                        {
                            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());
                            DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(basePath, "In\\" + Path.GetFileNameWithoutExtension(asm.Location) + "\\rp" + rp));

                            DebugLog("{" + plugin.GetPluginId().AbsoluteUri + "} RP =" + rp + ", r = "+ r +" => path " + dirFrom.FullName + ", exists? " + dirFrom.Exists);

                            if (enableDeploy)
                            {
                                //Verifica se há algo para processar
                                if (dirFrom.Exists)
                                    ProcessDeploy(r, rp, connectorConf, mapping);
                            }
                            else
                            {
                                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Deploy disabled");

                                //Exclui os arquivos
                                if (dirFrom.Exists)
                                {
                                    foreach (FileInfo f in dirFrom.GetFiles("*.iamdat"))
                                        f.Delete();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error on deploy: " + ex.Message);
                        }


                        if (!deployOnly)
                        {
                            try
                            {
                                //O import não é desabilitado, pois ele é necessário para relatório de consistência
                                //o Engine não utilizará ele para adicionar novas entidades
                                ProcessImport(r, rp, connectorConf, mapping);
                            }
                            catch (Exception ex)
                            {
                                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error on import: " + ex.Message);
                            }
                        }

                        executionCount++;
                        if (executionCount > 50)
                        {
                            executionCount = 0;
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Cleaning up proccess");
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                        }

                    }
                    catch (Exception ex)
                    {
                        TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error on parse config: " + ex.Message);
                    }
                    finally
                    {
                        connectorConf.Clear();
                        connectorConf = null;

                        mapping.Clear();
                        mapping = null;
                    }

                }
            }
            finally
            {
                cert = null;
                certPass = null;
            }
        }

        private void ProcessDeploy(Int64 resource, Int64 resource_plugin, Dictionary<String, Object> connectorConf, List<PluginConnectorBaseDeployPackageMapping> mapping)
        {
            StringBuilder deployLog = new StringBuilder();
            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Starting deploy thread...");
            deployLog.AppendLine("["+ DateTime.Now.ToString("HH:mm:ss") +"] Starting deploy thread...");

            JsonGeneric notify = new JsonGeneric();
            notify.function = "notify";
            notify.fields = new String[] { "source", "resource", "uri", "entityid", "identityid" };

            JsonGeneric deleted = new JsonGeneric();
            deleted.function = "deleted";
            deleted.fields = new String[] { "source", "resource", "uri", "entityid", "identityid" };

            JsonGeneric records = new JsonGeneric();
            records.function = "ProcessImportV2";
            records.fields = new String[] { "resource_plugin", "package" };

            ImportPackageUserEvent newPackage = new ImportPackageUserEvent(delegate(PluginConnectorBaseImportPackageUser pkg)
            {
                records.data.Add(new String[] { resource_plugin.ToString(), JSON.SerializeToBase64(pkg) });

                try
                {
                    SaveToSend(records, resource_plugin.ToString());
                    records.data.Clear();
                }
                catch { }
                
            });


            List<FileInfo> files = null;
            try
            {

                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());
                DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(basePath, "In\\" + Path.GetFileNameWithoutExtension(asm.Location) + "\\rp" + resource_plugin));
                if (!dirFrom.Exists)//Diretório inexistente
                {
                    deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Path not found " + dirFrom.FullName);
                    return;
                }

                //Ordena os arquivos, do mais antigo para o mais novo
                sortOndate sod = new sortOndate();
                files = new List<FileInfo>();
                files.AddRange(dirFrom.GetFiles("*.iamdat"));
                files.Sort(sod);

                foreach (FileInfo f in files)
                {
                    List<PluginConnectorBaseDeployPackage> fData = null;
                    try
                    {

                        deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Loading file " + f.Name);

                        try
                        {
                            fData = LoadFile(f);
                        }
                        catch (Exception ex)
                        {
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error reading file " + f.FullName.Replace(basePath, "") + ", " + ex.Message);
                            logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Error reading file " + f.FullName.Replace(basePath, "") + ", " + ex.Message, "");
                        }

                        if (fData == null)
                            continue;

                        if (fData.Count == 0)
                            throw new Exception("Package is empty");

                        TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} [" + resource_plugin + "]" + fData.Count + " packages in " + f.Name);
                        deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + fData.Count + " packages in " + f.Name);

                        LogEvent log = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                        {
                            TextLog.Log("PluginStarter", "{" + ((PluginConnectorBase)sender).GetPluginId().AbsoluteUri + "} " + type + ", " + text);
                        });

                        LogEvent2 log2 = new LogEvent2(delegate(Object sender, PluginLogType type, Int64 entityId, Int64 identityId, String text, String additionalData)
                        {
                            logProxy.AddLog(LogKey.Plugin_Event, "Proxy", resource_plugin, resource.ToString(), ((PluginConnectorBase)sender).GetPluginId().AbsoluteUri, (UserLogLevel)((Int32)type), entityId, identityId, text, additionalData);
#if DEBUG
                            TextLog.Log("PluginStarter", (((UserLogLevel)((Int32)type)).ToString()) + " entityId = " + entityId + ", identityId = " + identityId + ", " + text);
#endif
                        });

                        NotityChangeUserEvent log3 = new NotityChangeUserEvent(delegate(Object sender, Int64 entityId, Int64 identityId)
                        {
                            notify.data.Add(new String[] { "Proxy", resource.ToString(), ((PluginConnectorBase)sender).GetPluginId().AbsoluteUri, entityId.ToString(), identityId.ToString() });
                        });

                        NotityChangeUserEvent log4 = new NotityChangeUserEvent(delegate(Object sender, Int64 entityId, Int64 identityId)
                        {
                            deleted.data.Add(new String[] { "Proxy", resource.ToString(), ((PluginConnectorBase)sender).GetPluginId().AbsoluteUri, entityId.ToString(), identityId.ToString() });
                        });

                        plugin.ImportPackageUser += newPackage;
                        plugin.Log += log;
                        plugin.Log2 += log2;
                        plugin.NotityChangeUser += log3;
                        plugin.NotityDeletedUser += log4;

                        //Somente realiza a importação após o deploy se for o Deploy Only, ou seja, a publicação sobre demanda de um usuário estecífico
                        Boolean doImportAfterLogin = (fData.Count == 1);

                        try
                        {
                            foreach (PluginConnectorBaseDeployPackage pkg in fData)
                                try
                                {
                                    deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] EntityId = " + pkg.entityId + ", IdentityId = " + pkg.identityId + ", Pkg id: " + pkg.pkgId + ", user deleted? " + pkg.deleted);

                                    if (pkg.deleted)
                                    {
                                        plugin.ProcessDelete(resource_plugin.ToString(), pkg, connectorConf, mapping);
                                    }
                                    else
                                    {
                                        plugin.ProcessDeploy(resource_plugin.ToString(), pkg, connectorConf, mapping);
                                        if (doImportAfterLogin) plugin.ProcessImportAfterDeploy(resource_plugin.ToString(), pkg, connectorConf, mapping);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, pkg.entityId, pkg.identityId, "error on ProcessDeploy thread of file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""), "");
                                    deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] EntityId = " + pkg.entityId + ", IdentityId = " + pkg.identityId + ",  Error on ProcessDeploy thread of file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
                                }

                        }
                        finally
                        {
                            plugin.Log -= log;
                            plugin.Log2 -= log2;
                            plugin.NotityChangeUser -= log3;
                            plugin.NotityDeletedUser -= log4;
                            plugin.ImportPackageUser -= newPackage;

                            log = null;
                            log2 = null;
                            log3 = null;
                            log4 = null;
                            newPackage = null;
                        }

                        //Salva as notificações
                        if (notify.data.Count > 0)
                            SaveToSend(notify, resource_plugin.ToString() + "notify");

                        //Salva as exclusões
                        if (deleted.data.Count > 0)
                            SaveToSend(deleted, resource_plugin.ToString() + "deleted");

                        try
                        {
                            f.Delete();

                            if (dirFrom.GetFiles("*.iamdat").Length == 0)
                                dirFrom.Delete();

                            if (dirFrom.Parent.GetFiles("*.iamdat").Length == 0)
                                dirFrom.Parent.Delete();
                        }
                        catch(Exception ex) {
                            deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Erro on delete file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
                        }
                    }
                    catch (Exception ex)
                    {
                        logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Erro on deploy thread of file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""), "");
                        deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Erro on deploy thread of file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
                    }
                    finally
                    {
                        if (fData != null)
                        {
                            foreach (PluginConnectorBaseDeployPackage p in fData)
                                p.Dispose();
                        }

                    }
                }

                files.Clear();
            }
            catch (Exception ex)
            {
                logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Erro on deploy thread: " + ex.Message, "");
                deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Erro on deploy thread: " + ex.Message);
                throw ex;
            }
            finally
            {

                //Salva as notificações
                if (notify.data.Count > 0)
                    SaveToSend(notify, resource_plugin.ToString() + "notify");

                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Finishing deploy thread...");
                deployLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Finishing deploy thread...");

                if (files != null) files.Clear();
                files = null;


                logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Info, 0, 0, "Deploy executed", deployLog.ToString());
                
                deployLog.Clear();
                deployLog = null;


                //Salva os logs para envio
                logProxy.SaveToSend(resource_plugin.ToString() + "log");

            }

        }


        private void ProcessImport(Int64 resource, Int64 resource_plugin, Dictionary<String, Object> connectorConf, List<PluginConnectorBaseDeployPackageMapping> mapping)
        {
            StringBuilder importLog = new StringBuilder();
            importLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Starting import thread...");

            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Starting import thread...");

            Int64 count = 0;
            try
            {

                if (connectorConf == null)
                    throw new Exception("connectorConf is null");

                if (mapping == null)
                    throw new Exception("mapping is null");

                String id = Guid.NewGuid().ToString();

                /*
                JsonGeneric records = new JsonGeneric();
                records.function = "ProcessImport";
                records.fields = new String[] { "resource", "uri", "importid", "registryid", "dataname", "datavalue", "datatype" }; */

                JsonGeneric records = new JsonGeneric();
                records.function = "ProcessImportV2";
                records.fields = new String[] { "resource_plugin", "package" };

                JsonGeneric structRecords = new JsonGeneric();
                structRecords.function = "ProcessStructImport";
                structRecords.fields = new String[] { "resource_plugin", "package" };

                String uri = plugin.GetPluginId().AbsoluteUri.ToLower();

                ImportPackageUserEvent newPackage = new ImportPackageUserEvent(delegate(PluginConnectorBaseImportPackageUser pkg)
                {
                    count++;
                    records.data.Add(new String[] { resource_plugin.ToString(), JSON.SerializeToBase64(pkg) });

                    if (records.data.Count >= 500)
                    {
                        try
                        {
                            SaveToSend(records, resource_plugin.ToString());
                            records.data.Clear();
                        }
                        catch { }
                    }
                });


                ImportPackageStructEvent newStructPackage = new ImportPackageStructEvent(delegate(PluginConnectorBaseImportPackageStruct pkg)
                {
                    count++;
                    structRecords.data.Add(new String[] { resource_plugin.ToString(), JSON.SerializeToBase64(pkg) });

                    if (structRecords.data.Count >= 500)
                    {
                        try
                        {
                            SaveToSend(structRecords, resource_plugin.ToString());
                            structRecords.data.Clear();
                        }
                        catch { }
                    }
                });


                /*
                RegistryEvent reg = new RegistryEvent(delegate(String importId, String registryId, String dataName, String dataValue, String dataType)
                {
                    count++;
                    records.data.Add(new String[] { resource.ToString(), uri, importId, registryId, dataName, dataValue, dataType });

                    //Contabiliza a quantidade de registros para separar em vários arquivos
                    if (records.data.Count >= 30000)
                    {
                        //Após 30000 registros monitora a troca de registryId para salvar o arquivo
                        //Evitando que o mesmo registryId tenha dados em arquivos diferentes
                        //Isso evita problemas no servidor

                        if (lastRegistryId != registryId)
                        {
                            try
                            {
                                SaveToSend(records, importId);
                                records.data.Clear();
                            }
                            catch { }
                        }
                    }

                    lastRegistryId = registryId;
                });*/

                LogEvent log = new LogEvent(delegate(Object sender, PluginLogType type, string text)
                {
                    TextLog.Log("PluginStarter", "{" + ((PluginConnectorBase)sender).GetPluginId().AbsoluteUri + "} " + type + ", " + text);
                });


                LogEvent2 log2 = new LogEvent2(delegate(Object sender, PluginLogType type, Int64 entityId, Int64 identityId, String text, String additionalData)
                {
                    logProxy.AddLog(LogKey.Plugin_Event, "Proxy", resource_plugin, resource.ToString(), ((PluginConnectorBase)sender).GetPluginId().AbsoluteUri, (UserLogLevel)((Int32)type), entityId, identityId, text, additionalData);
                });


                plugin.ImportPackageUser += newPackage;
                plugin.ImportPackageStruct += newStructPackage;
                plugin.Log += log;
                plugin.Log2 += log2;

                plugin.ProcessImport(resource_plugin.ToString(), id, connectorConf, mapping);

                plugin.ImportPackageUser -= newPackage;
                plugin.ImportPackageStruct -= newStructPackage;
                plugin.Log -= log;
                plugin.Log2 -= log2;

                newPackage = null;
                log = null;
                uri = null;

                //Salva os registros remanescentes
                if (records.data.Count > 0)
                    SaveToSend(records, id + "-user");

                if (structRecords.data.Count > 0)
                    SaveToSend(structRecords, id + "-struct");


                importLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Imported "+ count +" items...");
                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Imported " + count + " items...");
            }
            catch (Exception ex)
            {
                logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Erro on import thread: " + ex.Message, "");
                importLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Erro on import thread: " + ex.Message);
                throw ex;
            }
            finally
            {
                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Finishing import thread");
                importLog.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] Finishing import thread...");

                if (count > 0)
                {
                    logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Info, 0, 0, "Import executed", importLog.ToString());
                }
                else
                {
#if DEBUG
                    //Mesmo log anterior, porém para mostrar quando estiver em debug
                    logProxy.AddLog(LogKey.Proxy_Event, "Proxy", resource_plugin, resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Info, 0, 0, "Import executed", importLog.ToString());    
#endif
                }
                
                importLog.Clear();
                importLog = null;


                //Salva os logs para envio
                logProxy.SaveToSend(resource_plugin.ToString() + "log");

            }
        }

        private List<PluginConnectorBaseDeployPackage> LoadFile(FileInfo file)
        {
            Byte[] fData = File.ReadAllBytes(file.FullName);
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
            try
            {
                using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), fData))
                {
                    List<PluginConnectorBaseDeployPackage> data = null;
                    data = JSON.Deserialize<List<PluginConnectorBaseDeployPackage>>(Encoding.UTF8.GetString(cApi.clearData));
                    return data;
                }
            }
            finally
            {
                certPass = null;
                fData = new Byte[0];
            }
        }

        private void SaveToSend(JsonGeneric data, String prefix)
        {
            if ((data.data == null) || (data.data.Count == 0))
                return;

            Byte[] jData = data.ToJsonBytes();

            using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(config.server_cert)), jData))
            {
                DirectoryInfo dirTo = new DirectoryInfo(Path.Combine(basePath, "Out"));
                if (!dirTo.Exists)
                    dirTo.Create();

                FileInfo f = new FileInfo(Path.Combine(dirTo.FullName, DateTime.Now.ToString("yyyyMMddHHmss-ffffff") + "-" + prefix) + ".iamdat");

                File.WriteAllBytes(f.FullName, cApi.ToBytes());

                TextLog.Log("PluginStarter", "File to send created " + f.Name + " (" + data.data.Count + ")");

                data.data.Clear();
            }

        }

        private void TimerCallback(Object state)
        {
            if (executing)
                return;

            executing = true;
            try
            {

                TimerExecution();
            }
            catch (Exception ex)
            {
                TextLog.Log("PluginStarter", "Erro on execute timer event " + plugin.GetPluginId().AbsoluteUri + ", " + ex.Message);
            }
            executing = false;
        }

        private void TimerExecution()
        {
            DebugLog("TimerExecution> Start");

            //Atualiza a config
            try
            {
                String ConfigJson = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(basePath, "config.json")));
                ProxyConfig tmpConfig = new ProxyConfig();
                tmpConfig.FromJsonString(ConfigJson);
                ConfigJson = null;

                config.Dispose();
                config = null;

                config = tmpConfig;
            }
            catch { }

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());

            foreach (PluginConfig p in config.plugins)
            {
                
                //Pode haver varios plugins com o mesmo nome, porém estarão em "resources" diferente
                if (p.uri.ToLower() == plugin.GetPluginId().AbsoluteUri.ToLower())
                {
                    Schedule schedule = null;
                    String nextExFile = null;
                    try
                    {

                        DateTime nextExecute = new DateTime(1970, 01, 01);
                        
                        nextExFile = Path.GetFullPath(asm.Location) + "-" + p.resource_plugin.ToString() + ".nextex";

                        if (File.Exists(nextExFile))
                        {
                            String tmp = File.ReadAllText(nextExFile, Encoding.UTF8);
                            DateTime.TryParseExact(tmp, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out nextExecute);
                        }

                        DateTime date = DateTime.Now;
                        TimeSpan ts = date - new DateTime(1970, 01, 01);

                        schedule = new Schedule();
                        try
                        {
                            schedule.FromJsonString(p.schedule);
                        }
                        catch
                        {
                            schedule = null;
                        }

                        Boolean checkDeployOnly = false;
                        if (schedule != null)
                        {

                            //Check Start date
                            if (nextExecute.Year == 1970)
                            {
                                nextExecute = new DateTime(schedule.StartDate.Year, schedule.StartDate.Month, schedule.StartDate.Day, schedule.TriggerTime.Hour, schedule.TriggerTime.Minute, 0);
                                File.WriteAllText(nextExFile, nextExecute.ToString("yyyy-MM-dd HH:mm:ss"), Encoding.UTF8);
                            }

                            TimeSpan stDateTs = nextExecute - new DateTime(1970, 01, 01);
                            DebugLog("TimerExecution> Executa agora? " + (ts.TotalSeconds >= stDateTs.TotalSeconds));
                            TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] TimerExecution> nextExFile " + nextExecute.ToString("yyyy-MM-dd HH:mm:ss"));
                            TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] TimerExecution> Executa agora? " + (ts.TotalSeconds >= stDateTs.TotalSeconds));
                            if (ts.TotalSeconds >= stDateTs.TotalSeconds) //Data e hora atual maior ou igual a data que se deve iniciar
                            {
                                TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Starting execution");

                                try
                                {
                                    switch (plugin.GetPluginId().Scheme.ToLower())
                                    {
                                        case "connector":
                                            DebugLog("TimerExecution> ExecuteConnector");
                                            ExecuteConnector();
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Error on execution " + ex.Message);
                                }
                                finally
                                {
                                    TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Execution completed");

                                    //Agenda a próxima execução
                                    nextExecute = schedule.CalcNext();

                                    TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Next execution scheduled to " + nextExecute.ToString("yyyy-MM-dd HH:mm:ss"));
                                    File.WriteAllText(nextExFile, nextExecute.ToString("yyyy-MM-dd HH:mm:ss"), Encoding.UTF8);
                                }
                            }
                            else
                            {
                                checkDeployOnly = true;
                            }
                        }
                        else
                        {
                            checkDeployOnly = true;
                        }

                        if (checkDeployOnly)
                        {
                            DebugLog("TimerExecution> DeployOnly> Start");
                            //Não está na hora da execução programada, mas verifica se há deploy a se fazer
                            try
                            {
                                DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(basePath, "In\\" + Path.GetFileNameWithoutExtension(asm.Location)));

                                DebugLog("TimerExecution> DeployOnly> dirFrom.Exists (" + dirFrom.FullName + ") " + dirFrom.Exists);
                                if (!dirFrom.Exists) //Diretório inexistente
                                    return;

                                if (dirFrom.GetFiles("*.iamdat", SearchOption.AllDirectories).Length > 0)
                                {
                                    TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Deploy files identified, starting deploy only execution");
                                    try
                                    {
                                        ExecuteConnector(true);
                                    }
                                    catch (Exception ex)
                                    {
                                        TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Error on execution " + ex.Message);
                                    }
                                    finally
                                    {
                                        TextLog.Log("PluginStarter", "[" + p.resource_plugin + "] Execution completed");
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLog("TimerExecution> DeployOnly: " + ex.Message);
                            }

                        }

                    }
                    finally
                    {
                        schedule.Dispose();
                        schedule = null;

                        nextExFile = null;
                    }

                }//End if
            }

            asm = null;

            DebugLog("TimerExecution> End");
        }

        private void DebugLog(String text)
        {
#if DEBUG
            TextLog.Log("PluginStarter", text);
#endif
        }

        private Boolean CheckPasswordComplexity(String password, Boolean uppercase, Boolean lowercase, Boolean numeric, Boolean special)
        {
            if (password.Length < 8)
                return false;

            Boolean contain = false;
            if (uppercase)
            {
                for (Int32 i = 65; i <= 90; i++)
                {
                    String tmp = Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
                    if (password.IndexOf(tmp) > -1)
                    {
                        contain = true;
                        break;
                    }
                }

                if (!contain)
                    return false;
            }

            if (lowercase)
            {
                contain = false;
                for (Int32 i = 97; i <= 122; i++)
                {
                    String tmp = Encoding.ASCII.GetString(new Byte[] { Byte.Parse(i.ToString("X"), System.Globalization.NumberStyles.HexNumber) });
                    if (password.IndexOf(tmp) > -1)
                    {
                        contain = true;
                        break;
                    }
                }

                if (!contain)
                    return false;
            }

            if (numeric)
            {
                contain = false;
                for (Int32 i = 0; i <= 9; i++)
                {
                    String tmp = i.ToString();
                    if (password.IndexOf(tmp) > -1)
                    {
                        contain = true;
                        break;
                    }
                }

                if (!contain)
                    return false;
            }

            if (special)
            {
                String tmp2 = "\"'!@#$%¨&*()-=_+<>;:{}[]";
                contain = false;
                foreach (Char c in tmp2)
                    if (password.IndexOf(c.ToString()) > -1)
                    {
                        contain = true;
                        break;
                    }

                if (!contain)
                    return false;
            }

            return true;
        }

    }
}
