using System;
using System.Collections.Generic;
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
using JsonBase;
using IAM.GlobalDefs;

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

    class Program
    {
        static String basePath;
        static ProxyConfig config;
        static Timer pluginsTimer;
        static PluginBase plugin = null;
        static Boolean executing = false;
        static LogProxy logProxy;

        static Int32 Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            if (args.Length == 0)
                return 1;
            
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(Program));
            basePath = Path.GetDirectoryName(asm.Location);

            if (!File.Exists(Path.Combine(basePath, "config.json")))
            {
                TextLog.Log("PluginStarter", "Json config file not found");
                return 2;
            }

            String ConfigJson =  Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(basePath, "config.json")));

            try
            {
                List<PluginBase> plugins = Plugins.GetPlugins<PluginBase>(Path.Combine(basePath, "plugins"));

                foreach (PluginBase p in plugins)
                    if (p.GetPluginId().AbsoluteUri.ToLower() == args[0].ToLower())
                        plugin = p;

            }
            catch (Exception ex)
            {
                TextLog.Log("PluginStarter", "Error loading plugins");
                return 3;
            }


            if (plugin == null)
            {
                TextLog.Log("PluginStarter", "Error startin with plugin " + args[0]);
                return 4;
            }

            config = new ProxyConfig();
            config.FromJsonString(ConfigJson);
            ConfigJson = null;

            logProxy = new LogProxy(basePath, config.server_cert);

            pluginsTimer = new Timer(new TimerCallback(TimerCallback), null, 1000, 60000);

            TextLog.Log("PluginStarter", "Successfully started with plugin " + plugin.GetPluginId().AbsoluteUri);
            Console.WriteLine("Successfully started with plugin " + plugin.GetPluginId().AbsoluteUri);

            while(true)
                Console.ReadLine();

            return 0;
        }

        static void ExecuteConnector()
        {
            ExecuteConnector(false);
        }

        static void ExecuteConnector(Boolean deployOnly)
        {

            List<Int64> resource = new List<Int64>();

            //Separa os contextos
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
            OpenSSL.X509.X509Certificate cert = CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass);
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

                    if (!resource.Contains(p.resource))
                        resource.Add(p.resource);

                }

            }
            

            foreach (Int64 r in resource)
            {
                Dictionary<String, Object> connectorConf = new Dictionary<String, Object>();
                Dictionary<String, String> mapping = new Dictionary<String, String>();

                Boolean enableDeploy = false;

                try
                {

                    foreach (PluginConfig p in config.plugins)
                    {
                        if ((p.uri.ToLower() == plugin.GetPluginId().AbsoluteUri.ToLower()) && (p.resource == r))
                        {

                            mapping = p.mappingDataTypeDic;
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
                                connectorConf.Add("iam_mail_domain", p.mail_domain);

                            foreach (String[] d1 in pgConf.data)
                                if (!connectorConf.ContainsKey(d1[kCol]))
                                    connectorConf.Add(d1[kCol], d1[vCol].ToString());
                        }

                    }

                    //Deploy ocorre antes da importação
                    //Para que na importação ja apareça os registros que foram publicados pelo deploy
                    try
                    {
                        if (enableDeploy)
                            ProcessDeploy(r, connectorConf, mapping);
                        else
                        {
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Deploy disabled");

                            //Exclui os arquivos
                            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());
                            DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(basePath, "In\\" + Path.GetFileNameWithoutExtension(asm.Location) + "\\" + resource));
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
                            ProcessImport(r, connectorConf, mapping);
                        }
                        catch (Exception ex)
                        {
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error on import: " + ex.Message);
                        }
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

            cert = null;
            certPass = null;

        }

        static void ProcessDeploy(Int64 resource, Dictionary<String, Object> connectorConf, Dictionary<String, String> mapping)
        {
            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Starting deploy thread...");

            
            JsonGeneric notify = new JsonGeneric();
            notify.function = "notify";
            notify.fields = new String[] { "source", "resource", "uri", "entityid" };


            try
            {

                System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());
                DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(basePath, "In\\" + Path.GetFileNameWithoutExtension(asm.Location) + "\\" + resource));
                if (!dirFrom.Exists) //Diretório inexistente
                    return;

                //Ordena os arquivos, do mais antigo para o mais novo
                sortOndate sod = new sortOndate();
                List<FileInfo> files = new List<FileInfo>();
                files.AddRange(dirFrom.GetFiles("*.iamdat"));
                files.Sort(sod);

                foreach (FileInfo f in files)
                {
                    try
                    {

                        List<PluginBaseDeployPackage> fData = null;
                        try
                        {
                            fData = LoadFile(f);
                        }
                        catch (Exception ex)
                        {
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error reading file " + f.FullName.Replace(basePath, "") + ", " + ex.Message);
                            logProxy.AddLog("Proxy", resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Error reading file " + f.FullName.Replace(basePath, "") + ", " + ex.Message, "");
                        }

                        if (fData == null)
                            continue;

                        if (fData.Count == 0)
                            throw new Exception("Package is empty");

                        TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} [" + resource + "]" + fData.Count + " packages in " + f.Name);

                        LogEvent log = new LogEvent(delegate(PluginBase sender, PluginLogType type, String text)
                        {
                            TextLog.Log("PluginStarter", "{" + sender.GetPluginId().AbsoluteUri + "} " + type + ", " + text);
                        });

                        LogEvent2 log2 = new LogEvent2(delegate(PluginBase sender, PluginLogType type, Int64 entityId, Int64 identityId, String text, String additionalData)
                        {
                            logProxy.AddLog("Proxy", resource.ToString(), sender.GetPluginId().AbsoluteUri, (UserLogLevel)((Int32)type), entityId, identityId, text, additionalData);
                        });

                        NotityChangeUserEvent log3 = new NotityChangeUserEvent(delegate(PluginBase sender, Int64 entityId)
                        {
                            notify.data.Add(new String[] { "Proxy", resource.ToString(), sender.GetPluginId().AbsoluteUri, entityId.ToString() });
                        });

                        plugin.Log += log;
                        plugin.Log2 += log2;
                        plugin.NotityChangeUser += log3;

                        try
                        {
                            foreach (PluginBaseDeployPackage pkg in fData)
                                try
                                {
                                    plugin.ProcessDeploy(pkg, connectorConf, mapping);
                                }
                                catch (Exception ex)
                                {
                                    logProxy.AddLog("Proxy", resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, pkg.entityId, 0, "error on ProcessDeploy thread of file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""), "");
                                }

                        }
                        finally
                        {
                            plugin.Log -= log;
                            plugin.Log2 -= log2;
                            plugin.NotityChangeUser -= log3;

                            log = null;
                            log2 = null;
                            log3 = null;
                        }


                        //Salva as notificações
                        if (notify.data.Count > 0)
                            SaveToSend(notify, resource.ToString() + "notify");

                        //Salva os logs para envio
                        logProxy.SaveToSend(resource.ToString() + "log");

                        try
                        {
                            f.Delete();

                            if (dirFrom.GetFiles("*.iamdat").Length == 0)
                                dirFrom.Delete();

                            if (dirFrom.Parent.GetFiles("*.iamdat").Length == 0)
                                dirFrom.Parent.Delete();
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        logProxy.AddLog("Proxy", resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Erro on deploy thread of file " + f.FullName.Replace(basePath, "") + ", " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""), "");
                    }
                }

                files.Clear();
            }
            catch(Exception ex)
            {
                logProxy.AddLog("Proxy", resource.ToString(), plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Erro on deploy thread: " + ex.Message, "");
                throw ex;
            }
            finally
            {

                //Salva as notificações
                if (notify.data.Count > 0)
                    SaveToSend(notify, resource.ToString() + "notify");

                //Salva os logs para envio
                logProxy.SaveToSend(resource.ToString() + "log");

                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Finishing deploy thread...");
            }

        }


        static void ProcessImport(Int64 resource, Dictionary<String, Object> connectorConf, Dictionary<String, String> mapping)
        {
            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Starting import thread...");

            try
            {

                if (connectorConf == null)
                    throw new Exception("connectorConf is null");

                if (mapping == null)
                    throw new Exception("mapping is null");

                String id = Guid.NewGuid().ToString();

                JsonGeneric records = new JsonGeneric();
                records.function = "ProcessImport";
                records.fields = new String[] { "resource", "uri", "importid", "registryid", "dataname", "datavalue", "datatype" };

                String uri = plugin.GetPluginId().AbsoluteUri.ToLower();

                String lastRegistryId = "";

                RegistryEvent reg = new RegistryEvent(delegate(String importId, String registryId, String dataName, String dataValue, String dataType)
                {
                    records.data.Add(new String[] { resource.ToString(), uri, importId, registryId, dataName, dataValue, dataType });

                    //Contabiliza a quantidade de registros para separar em vários arquivos
                    if (records.data.Count >= 2000)
                    {
                        //Após 2000 registros monitora a troca de registryId para salvar o arquivo
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
                });

                LogEvent log = new LogEvent(delegate(PluginBase sender, PluginLogType type, string text)
                {
                    TextLog.Log("PluginStarter", "{" + sender.GetPluginId().AbsoluteUri + "} " + type + ", " + text);
                });


                LogEvent2 log2 = new LogEvent2(delegate(PluginBase sender, PluginLogType type, Int64 entityId, Int64 identityId, String text, String additionalData)
                {
                    logProxy.AddLog("Proxy", resource.ToString(), sender.GetPluginId().AbsoluteUri, (UserLogLevel)((Int32)type), entityId, identityId, text, additionalData);
                });

                
                plugin.Registry += reg;
                plugin.Log += log;
                plugin.Log2 += log2;

                plugin.ProcessImport(id, connectorConf, mapping);

                plugin.Registry -= reg;
                plugin.Log -= log;
                plugin.Log2 -= log2;

                reg = null;
                log = null;
                uri = null;

                //Salva os registros remanescentes
                if (records.data.Count > 0)
                    SaveToSend(records, id);

                //Salva os logs para envio
                logProxy.SaveToSend(resource.ToString() + "log");

            }
            finally
            {
                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Finishing import thread");
            }
        }

        static List<PluginBaseDeployPackage> LoadFile(FileInfo file)
        {
            Byte[] fData = File.ReadAllBytes(file.FullName);
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
            try
            {
                using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), fData))
                {
                    List<PluginBaseDeployPackage> data = null;
                    data = JSON.Deserialize<List<PluginBaseDeployPackage>>(Encoding.UTF8.GetString(cApi.clearData));
                    return data;
                }
            }
            finally
            {
                certPass = null;
                fData = new Byte[0];
            }
        }

        static void SaveToSend(JsonGeneric data, String prefix)
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

        static void TimerCallback(Object state)
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

        static void TimerExecution()
        {

            //Atualiza a config
            try
            {
                String ConfigJson = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(basePath, "config.json")));
                ProxyConfig tmpConfig = new ProxyConfig();
                tmpConfig.FromJsonString(ConfigJson);
                ConfigJson = null;

                config = tmpConfig;
            }
            catch { }

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(plugin.GetType());
            
            foreach (PluginConfig p in config.plugins)
            {
                //Pode haver varios plugins com o mesmo nome, porém estarão em "resources" diferente
                if (p.uri.ToLower() == plugin.GetPluginId().AbsoluteUri.ToLower())
                {
                    DateTime nextExecute = new DateTime(1970, 01, 01);

                    String nextExFile = Path.GetFullPath(asm.Location) + "-" + p.resource.ToString() + ".nextex";

                    if (File.Exists(nextExFile))
                    {
                        String tmp = File.ReadAllText(nextExFile, Encoding.UTF8);
                        DateTime.TryParseExact(tmp, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out nextExecute);
                    }

                    DateTime date = DateTime.Now;
                    TimeSpan ts = date - new DateTime(1970, 01, 01);

                    Schedule schedule = new Schedule();
                    try
                    {
                        schedule.FromJsonString(p.schedule);
                    }
                    catch {
                        schedule = null;
                    }

                    if (schedule == null)
                        continue;

                    //Check Start date
                    if (nextExecute.Year == 1970)
                    {
                        nextExecute = new DateTime(schedule.StartDate.Year, schedule.StartDate.Month, schedule.StartDate.Day, schedule.TriggerTime.Hour, schedule.TriggerTime.Minute, 0);
                        File.WriteAllText(nextExFile, nextExecute.ToString("yyyy-MM-dd HH:mm:ss"), Encoding.UTF8);
                    }

                    TimeSpan stDateTs = nextExecute - new DateTime(1970, 01, 01);
                    if (ts.TotalSeconds >= stDateTs.TotalSeconds) //Data e hora atual maior ou igual a data que se deve iniciar
                    {
                        TextLog.Log("PluginStarter", "[" + p.resource + "] Starting execution");

                        try
                        {
                            switch (plugin.GetPluginId().Scheme)
                            {
                                case "connector":
                                    ExecuteConnector();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            TextLog.Log("PluginStarter", "[" + p.resource + "] Error on execution " + ex.Message);
                        }
                        finally
                        {
                            TextLog.Log("PluginStarter", "[" + p.resource + "] Execution completed");

                            //Agenda a próxima execução
                            DateTime calcNext = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, schedule.TriggerTime.Hour, schedule.TriggerTime.Minute, 0);
                            nextExecute = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
                            switch (schedule.Trigger)
                            {
                                case ScheduleTtiggers.Dialy:
                                    calcNext = calcNext.AddDays(1);
                                    break;

                                case ScheduleTtiggers.Monthly:
                                    calcNext = calcNext.AddMonths(1);
                                    break;

                                case ScheduleTtiggers.Annually:
                                    calcNext = calcNext.AddYears(1);
                                    break;
                            }

                            //TextLog.Log("PluginStarter", "Calc 1 " + calcNext.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (schedule.Repeat > 0)
                            {
                                if (nextExecute.AddMinutes(schedule.Repeat).CompareTo(calcNext) < 0)
                                {
                                    nextExecute = nextExecute.AddMinutes(schedule.Repeat);
                                    //TextLog.Log("PluginStarter", "Calc 2 " + nextExecute.ToString("yyyy-MM-dd HH:mm:ss"));
                                }
                                else
                                {
                                    nextExecute = calcNext;
                                }
                            }
                            else
                                nextExecute = calcNext;

                            TextLog.Log("PluginStarter", "[" + p.resource + "] Next execution scheduled to " + nextExecute.ToString("yyyy-MM-dd HH:mm:ss"));
                            File.WriteAllText(nextExFile, nextExecute.ToString("yyyy-MM-dd HH:mm:ss"), Encoding.UTF8);
                        }
                    }
                    else
                    {
                        //Não está na hora da execução programada, mas verifica se há deploy a se fazer
                        try
                        {
                            DirectoryInfo dirFrom = new DirectoryInfo(Path.Combine(basePath, "In\\" + Path.GetFileNameWithoutExtension(asm.Location)));
                            if (!dirFrom.Exists) //Diretório inexistente
                                return;

                            if (dirFrom.GetFiles("*.iamdat", SearchOption.AllDirectories).Length > 0)
                            {
                                TextLog.Log("PluginStarter", "[" + p.resource + "] Deploy files identified, starting deploy only execution");
                                try
                                {
                                    ExecuteConnector(true);
                                }
                                catch (Exception ex)
                                {
                                    TextLog.Log("PluginStarter", "[" + p.resource + "] Error on execution " + ex.Message);
                                }
                                finally
                                {
                                    TextLog.Log("PluginStarter", "[" + p.resource + "] Execution completed");
                                }

                            }
                        }
                        catch { }

                    }
                }
            }

        }


        private static Boolean CheckPasswordComplexity(String password, Boolean uppercase, Boolean lowercase, Boolean numeric, Boolean special)
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


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }
    }
}
