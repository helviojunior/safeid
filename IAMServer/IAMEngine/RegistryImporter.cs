using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

using IAM.Config;
using IAM.PluginManager;
using IAM.PluginInterface;
using IAM.Log;
using IAM.CA;
//using IAM.SQLDB;
using IAM.LocalConfig;
using IAM.License;
using IAM.GlobalDefs;
using IAM.Queue;
using IAM.UserProcess;
using SafeTrend.Json;
using IAM.GlobalDefs;

namespace IAM.Engine
{
    class RegistryImporter
    {
        private ServerLocalConfig localConfig;
        private String basePath;
        private Boolean executing;
        private Int64 totalReg;
        private Int64 atualReg;
        private Double percent = 0;
        private Int32 iPercent = 0;
        private Int32 newUsers = 0;
        private Int32 ignored = 0;
        private Int32 errors = 0;
        private String last_status = "";
        private DateTime startTime = new DateTime(1970, 1, 1);
        private Int32 maxThreads = 30;

        Timer tmpStatus = null;
        Timer procTimer = null;

        private QueueManager<RegistryProcessStarter> queueManager = null;
        //private RegistryQueue[] _queue;
        private Dictionary<Int64, LicenseControl> licControl = new Dictionary<Int64, LicenseControl>();
        private Dictionary<Int64, EnterpriseKeyConfig> entKeys = new Dictionary<Int64, EnterpriseKeyConfig>();

        public RegistryImporter(ServerLocalConfig config)
        {
            this.localConfig = config;
            
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            Taskbar.TaskbarProgress.SetProgressState(Taskbar.TaskbarProgressState.NoProgress);

            if (config.EngineMaxThreads > 0)
                this.maxThreads = config.EngineMaxThreads;

        }

        public void Start()
        {
            tmpStatus = new Timer(new TimerCallback(TmrServiceStatusCallback), null, 100, 10000);
            procTimer = new Timer(new TimerCallback(TmrCallback), null, 1000, 60000);
        }

        private void TmrServiceStatusCallback(Object o)
        {
            IAMDatabase db = null;
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                db.ServiceStatus("Engine", JSON.Serialize2(new { host = Environment.MachineName, executing = executing, start_time = startTime.ToString("o"), total_registers = totalReg, atual_register = atualReg, percent = iPercent, errors = errors, new_users = newUsers, ignored = ignored, thread_count = (queueManager != null ? queueManager.ThreadCount : 0), queue_count = (queueManager != null ? queueManager.QueueCount : 0), last_status = last_status, queue_description = (queueManager != null ? queueManager.QueueCount2 : "") }), null);

                db.closeDB();
            }
            catch { }
            finally
            {
                if (db != null)
                    db.Dispose();

                db = null;
            }
        }

        private void TmrCallback(Object o)
        {
            if (executing)
                return;

            executing = true;

            TextLog.Log("Engine", "Importer", "Starting registry processor timer");
            Console.WriteLine("Starting registry processor timer");
            IAMDatabase db = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            
            
            Dictionary<Int64, PluginConfig> resourcePluginCache = new Dictionary<Int64, PluginConfig>();

            StringBuilder procLog = new StringBuilder();
            Boolean writeLog = false;

            last_status = "Iniciando...";
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;
                //db.Debug = true;

                Console.WriteLine("Select data...");

                Taskbar.TaskbarProgress.SetProgressState(Taskbar.TaskbarProgressState.Indeterminate);
                startTime = DateTime.Now;
                newUsers = 0;
                errors = 0;
                totalReg = 0;
                ignored = 0;
                atualReg = 0;

                //Seleciona os registros prontos para serem importados
                //Não colocar order neste select, fica extremamente lento
                //Coloca um limite de 500.000 somente p/ não estourar memória
                last_status = "Selecionando registros a serem processados";
                DataTable dtRegs = db.Select("select top 5000 * from vw_collector_imports_regs with(nolock) order by priority desc");

                if (dtRegs == null)
                {
                    TextLog.Log("Engine", "Importer", "\tError on select registries: " + db.LastDBError);
                    db.AddUserLog(LogKey.Engine, null, "Engine", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Error on select registries: " + db.LastDBError);
                    executing = false;
                    return;
                }

                if (dtRegs.Rows.Count == 0)
                {
                    TextLog.Log("Engine", "Importer", "\t0 registers to process");
                    Console.WriteLine("0 registers to process");
                    executing = false;
                    return;
                }

                totalReg = dtRegs.Rows.Count;
                                
                TextLog.Log("Engine", "Importer", "\t" + dtRegs.Rows.Count + " registers to process");
                procLog.AppendLine("[" + DateTime.Now.ToString("o") + "] " + dtRegs.Rows.Count + " registers to process");
                Console.WriteLine(dtRegs.Rows.Count + " registers to process");

                //Carrega todos os logins do sistema
                Console.WriteLine("Fetch logins...");
                last_status = "Listando login do sistema";
                DataTable dtLogins = db.Select("select context_id,id,login from vw_entity_logins2 with(nolock)");
                if ((dtLogins != null) || (dtLogins.Rows.Count > 0))
                {
                    foreach (DataRow dr in dtLogins.Rows)
                        LoginCache.AddItem((Int64)dr["context_id"], (Int64)dr["id"], dr["login"].ToString());
                }

                //Carrega todos os e-mails do sistema
                Console.WriteLine("Fetch e-mails...");
                last_status = "Listando e-mails do sistema";
                DataTable dtEmails = db.Select("select context_id, entity_id, mail from vw_entity_mails with(nolock)");
                if ((dtEmails != null) || (dtEmails.Rows.Count > 0))
                {
                    foreach (DataRow dr in dtEmails.Rows)
                        EmailCache.AddItem((Int64)dr["context_id"], (Int64)dr["entity_id"], dr["mail"].ToString());
                }


                //Calcula a quantidade de threads com base na quantidade de registros
                Int32 tCount = dtRegs.Rows.Count / 10;
                
                if (tCount < 1)
                    tCount = 1;
                else if (tCount > this.maxThreads)
                    tCount = this.maxThreads;

#if DEBUG
                tCount = 1;
#endif

                Console.WriteLine("Starting...");
                queueManager = new QueueManager<RegistryProcessStarter>(tCount, ProcQueue);
                queueManager.OnThreadStart += new QueueManager<RegistryProcessStarter>.StartThread(delegate(Int32 threadIndex)
                {

                    LocalTheadObjects obj = new LocalTheadObjects();
                    for (Int32 t = 0; t <= 10; t++)
                    {
                        try
                        {
                            obj.db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                            obj.db.openDB();
                            obj.db.Timeout = 600;

#if DEBUG
                            //obj.db.Debug = true;
#endif

                            obj.lockRules = new LockRules();
                            obj.ignoreRules = new IgnoreRules();
                            obj.roleRules = new RoleRules();
                            obj.lockRules.GetDBConfig(obj.db.Connection);
                            obj.ignoreRules.GetDBConfig(obj.db.Connection);
                            obj.roleRules.GetDBConfig(obj.db.Connection);
                            break;
                        }
                        catch(Exception ex) {
                            if (t >= 10)
                                throw ex;
                        }
                    }

                    return obj;
                });

                queueManager.OnThreadStop += new QueueManager<RegistryProcessStarter>.ThreadStop(delegate(Int32 threadIndex, Object state)
                {
                    if ((state != null) && (state is LocalTheadObjects))
                        ((LocalTheadObjects)state).Dispose();

                    state = null;
                });


                Console.WriteLine("Starting treads...");
                last_status = "Iniciando treads";
                queueManager.Start();

                if (queueManager.ExecutingCount == 0)
                    throw new Exception("Erro on start queue manager");

                /*
                _queue = new RegistryQueue[tCount];
                Int32 qIndex = 0;

                for (Int32 i = 0; i < _queue.Length; i++)
                    _queue[i] = new RegistryQueue();
                */

                Taskbar.TaskbarProgress.SetProgressState(Taskbar.TaskbarProgressState.Normal);
                Taskbar.TaskbarProgress.SetProgressValue(0, (Int32)totalReg, System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);

                Int32 addCount = 0;
                last_status = "Processando registros";
                foreach (DataRow dr in dtRegs.Rows)
                {

                    Int64 enterpriseId = (Int64)dr["enterprise_id"];
                    Int64 contextId = (Int64)dr["context_id"];

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
                            db.AddUserLog(LogKey.Licence_error, null, "Engine", UserLogLevel.Error, 0, enterpriseId, 0, (Int64)dr["resource_id"], (Int64)dr["plugin_id"], 0, 0, "License error: " + lic.Error);
                        lic.Notified = true;

                        db.ExecuteNonQuery("update collector_imports set status = 'LE' where status = 'F' and resource_plugin_id = '" + dr["resource_id"] + "' and  import_id = '" + dr["import_id"] + "' and package_id = '" + dr["package_id"] + "'", CommandType.Text, null);

                        continue;
                    }

                    if ((lic.Entities > 0) && (lic.Count > lic.Entities))
                    {
                        if (!lic.Notified)
                            db.AddUserLog(LogKey.Licence_error, null, "Engine", UserLogLevel.Error, 0, enterpriseId, 0, (Int64)dr["resource_id"], (Int64)dr["plugin_id"], 0, 0, "License error: License limit (" + lic.Entities + " entities) exceeded");
                        lic.Notified = true;

                        db.ExecuteNonQuery("update collector_imports set status = 'LE' where status = 'F' and resource_plugin_id = '" + dr["resource_id"] + "' and  import_id = '" + dr["import_id"] + "' and package_id = '" + dr["package_id"] + "'", CommandType.Text, null);

                        continue;
                    }


                    if (!entKeys.ContainsKey(enterpriseId))
                        entKeys.Add(enterpriseId, new EnterpriseKeyConfig(db.Connection, enterpriseId));

                    if (entKeys[enterpriseId] == null)
                        entKeys[enterpriseId] = new EnterpriseKeyConfig(db.Connection, enterpriseId);

                    addCount++;
                    queueManager.AddItem(new RegistryProcessStarter(enterpriseId, contextId, new Uri(dr["plugin_uri"].ToString()), Int64.Parse(dr["resource_id"].ToString()), Int64.Parse(dr["plugin_id"].ToString()), Int64.Parse(dr["resource_plugin_id"].ToString()), (String)dr["import_id"], (String)dr["package_id"], (String)dr["package"]));

                    //A cada 100 registros monitora a CPU para adicionar mais registros
                    //O Objetivo deste processo é controlar a carga de processamento
                    if (addCount >= 100)
                    {
                        addCount = 0;
                        Int32 c = 0;
                        while (((c = queueManager.QueueCount) > 500) || ((getCPUCounter() >= 70) && (c > 0)))
                            Thread.Sleep(500);
                    }

                    
                    /*
                    _queue[qIndex].Add(enterpriseId, contextId, Int64.Parse(dr["plugin_id"].ToString()), (String)dr["plugin_uri"], Int64.Parse(dr["resource_id"].ToString()), (String)dr["import_id"], (String)dr["registry_id"]);

                    qIndex++;
                    if (qIndex > _queue.Length - 1) qIndex = 0;
                    */
                }



                /*
                for (Int32 i = 0; i < _queue.Length; i++)
                {
                    Thread procQueue = new Thread(new ParameterizedThreadStart(ProcQueue));
                    procQueue.Start(i);
                    //Thread.Sleep(1000);
                }*/

                Console.WriteLine("Waiting treads execution...");

                /*
                Int64 rest = 0;
                Double percent = 0;
                Int32 iPercent = 0;
                do
                {
                    rest = 0;

                    rest = queueManager.QueueCount;
                    
                    //for (Int32 i = 0; i < _queue.Length; i++)
                    //    rest += _queue[i].Count;

                    percent = ((Double)(totalReg - rest) / (Double)totalReg) * 100F;

                    if (iPercent != (Int32)percent)
                    {
                        iPercent = (Int32)percent;
                        procLog.AppendLine("[" + DateTime.Now.ToString("o") + "] " + iPercent + "%");
                        TextLog.Log("Engine", "Importer", "\t" + iPercent + "%");
                        Console.Write(" " + iPercent + "% ");

                        Taskbar.TaskbarProgress.SetProgressValue((Int32)(totalReg - rest), (Int32)totalReg, System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
                        
                    }
                    
                    Thread.Sleep(1000);

                } while (rest > 0);*/

                
                //Envia comando para finalizar a execução e aguarda a finalização
                last_status = "Processando registros";
                queueManager.StopAndWait();


                Taskbar.TaskbarProgress.SetProgressState(Taskbar.TaskbarProgressState.Indeterminate);

                last_status = "Finalizando";
                Console.WriteLine("Finishing...");
                
                if (dtRegs.Rows.Count > 0)
                    writeLog = true;

                procLog.AppendLine("New users: " + newUsers);
                procLog.AppendLine("Errors: " + errors);
                procLog.AppendLine("Ignored: " + ignored);
                procLog.AppendLine("Updated: " + (totalReg - errors - ignored -newUsers));

                procLog.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] Import registry processed with " + dtRegs.Rows.Count + " registers");
                
                //Joga todos os registros para a tabela de importados
                //e exclui da atual
                db.ExecuteNonQuery("sp_migrate_imported", CommandType.StoredProcedure, null);


                //Reconstroi os índices das tabelas de entidades e identidades
                try
                {
                    db.ExecuteNonQuery("sp_reindex_entity", CommandType.StoredProcedure, null);
                    db.ExecuteNonQuery("sp_rebuild_entity_keys", CommandType.StoredProcedure, null);
                }
                catch { }

                Console.WriteLine("");
                
            }
            catch (SqlException e)
            {
                procLog.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] DB Error on registry processor: " + e.Message);
                procLog.AppendLine(db.LastDBError);

                db.AddUserLog(LogKey.Import, null, "Engine", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "DB Error on registry processor", procLog.ToString());
                TextLog.Log("Engine", "Importer", "\tError on registry processor timer " + e.Message + " " + db.LastDBError);
            }
            catch (Exception ex)
            {
                procLog.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] Error on registry processor: " + ex.Message);

                db.AddUserLog(LogKey.Import, null, "Engine", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Error on registry processor", procLog.ToString());
                TextLog.Log("Engine", "Importer", "\tError on registry processor timer " + ex.Message);
            }
            finally
            {

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                executing = false;
                last_status = "";

                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:0000}", ts.TotalHours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                TextLog.Log("Engine", "Importer", "\tElapsed time: " + elapsedTime);

                TextLog.Log("Engine", "Importer", "\tScheduled for new registry processor in 60 seconds");
                TextLog.Log("Engine", "Importer", "Finishing registry processor timer");

                procLog.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] Elapsed time: " + elapsedTime);

                Console.WriteLine("Import registry processed " + procLog.ToString());
                Console.WriteLine("Elapsed time: " + elapsedTime);

                if (writeLog)
                    db.AddUserLog(LogKey.Import, null, "Engine", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, "Import registry processed", procLog.ToString());

                Taskbar.TaskbarProgress.SetProgressState(Taskbar.TaskbarProgressState.NoProgress);

                startTime = new DateTime(1970, 1, 1);

                try
                {
                    List<Int64> keys = new List<Int64>();
                    if ((entKeys != null) && (entKeys.Count > 0))
                    {
                        keys.AddRange(entKeys.Keys);
                        foreach (Int64 k in keys)
                        {
                            try
                            {
                                if (entKeys[k] != null)
                                {
                                    entKeys[k].Dispose();
                                    entKeys[k] = null;

                                }
                            }
                            catch { }
                            try
                            {
                                entKeys.Remove(k);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    licControl.Clear();
                }
                catch { }

                try
                {
                    LoginCache.Clear();
                }
                catch { }

                if (db != null)
                    db.Dispose();

                db = null;

                Thread.CurrentThread.Abort();
            }
        }


        private void ProcQueue(RegistryProcessStarter queueItem, Object oStarter)
        {
            LocalTheadObjects starter = null;

            StringBuilder tLog = new StringBuilder();
            try
            {


                if ((oStarter != null) && (oStarter is LocalTheadObjects))
                    starter = (LocalTheadObjects)oStarter;

                if (starter == null)
                    throw new Exception("Thread object starts is null");

                if (starter.db == null)
                    throw new Exception("Thread object starts database is null");

                if (queueItem == null)
                    throw new Exception("Queue item is null");

                if (starter.lockRules == null)
                    throw new Exception("Lock rules is null");

                if (starter.ignoreRules == null)
                    throw new Exception("Ignore rules is null");

                if (starter.roleRules == null)
                    throw new Exception("Role rules is null");

                if (licControl == null)
                    throw new Exception("Licence control is null");

                if (entKeys == null)
                    throw new Exception("Enterprise keys is null");

                if (entKeys[queueItem.enterpriseId] == null)
                    throw new Exception("Enterprise key of enterprise " + queueItem.enterpriseId + " is null");

                if (licControl[queueItem.enterpriseId] == null)
                    throw new Exception("Licence control of enterprise " + queueItem.enterpriseId + " is null");

                PluginConfig pluginConfig = null;

                if ((pluginConfig == null) || (pluginConfig.resource_plugin != queueItem.resourcePluginId))
                {
                    if (pluginConfig != null)
                    {
                        pluginConfig.Dispose();
                        pluginConfig = null;
                    }

                    using (DataTable dtContext = starter.db.Select("select p.scheme, rp.* from resource_plugin rp with(nolock) inner join plugin p with(nolock) on rp.plugin_id = p.id where rp.id = " + queueItem.resourcePluginId))
                    {
                        if ((dtContext != null) && (dtContext.Rows.Count > 0))
                        {
                            pluginConfig = new PluginConfig(starter.db.Connection, dtContext.Rows[0]["scheme"].ToString(), (Int64)dtContext.Rows[0]["plugin_id"], (Int64)dtContext.Rows[0]["id"]);
                        }
                    }
                }

                if (pluginConfig == null)
                    throw new Exception("Resource x plugin not found");

                

                //Realiza todo o processamento deste registro
                
                using (RegistryProcess proc = new RegistryProcess(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword, pluginConfig, queueItem))
                {

                    RegistryProcess.ProccessLog log = new RegistryProcess.ProccessLog(delegate(String text)
                    {
                        tLog.AppendLine(text);
                    });


                    proc.OnLog += log;
                    RegistryProcessStatus status = proc.Process((EnterpriseKeyConfig)entKeys[queueItem.enterpriseId].Clone(), starter.lockRules, starter.ignoreRules, starter.roleRules, licControl[queueItem.enterpriseId]);
                    proc.OnLog -= log;

                    starter.db.AddUserLog(LogKey.Import, null, "Engine", (status == RegistryProcessStatus.Error ? UserLogLevel.Error : UserLogLevel.Info), 0, 0, 0, queueItem.resourceId, queueItem.pluginId, proc.EntityId, proc.IdentityId, "Import processed", tLog.ToString());

                    if (status == RegistryProcessStatus.OK)
                    {
                        Console.Write(".");

                        if (proc.NewUser)
                            newUsers++;
                    }
                    else if (status == RegistryProcessStatus.Ignored)
                    {
                        ignored++;
                    }
                    else
                    {
                        Console.Write("!");
                        errors++;
                    }
                }

                

            }
            catch (Exception ex)
            {
                Console.Write("!");
                errors++;


//#if !DEBUG
                try
                {
                    tLog.AppendLine("Package: " + queueItem.package);
                }
                catch { }

                tLog.AppendLine("StackTrace: " + ex.StackTrace);
//#endif


                starter.db.AddUserLog(LogKey.Import, null, "Engine", UserLogLevel.Error, 0, 0, 0, queueItem.resourceId, queueItem.pluginId, 0, 0, ex.Message, tLog.ToString());
                starter.db.ExecuteNonQuery("update collector_imports set status = 'E' where status = 'F' and resource_plugin_id = '" + queueItem.resourcePluginId + "' and  import_id = '" + queueItem.importId + "' and package_id = '" + queueItem.packageId + "'", CommandType.Text, null);
            }
            finally
            {
                tLog = null;
                
                atualReg++;

                percent = ((Double)(atualReg) / (Double)totalReg) * 100F;

                if (iPercent != (Int32)percent)
                {
                    iPercent = (Int32)percent;
                    TextLog.Log("Engine", "Importer", "\t" + iPercent + "% -> New users: " + newUsers + ", Ignored: " + ignored + ", Errors: " + errors + ", Updated: " + (atualReg - errors - ignored - newUsers));

                    Console.Write(" " + iPercent + "% ");

                    Taskbar.TaskbarProgress.SetProgressValue((Int32)atualReg, (Int32)totalReg, System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);

                }

            }

        }

        private static float getCPUCounter()
        {

            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            // will always start at 0
            float firstValue = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(300);
            // now matches task manager reading
            float secondValue = cpuCounter.NextValue();

            return secondValue;

        }

        /*
        private void ProcQueueOld(Object oIndex)
        {
            Int32 index = (Int32)oIndex;

            RegistryQueueItem queueItem = null;

            IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
            db.openDB();
            db.Timeout = 600;

            Console.WriteLine("Starting thread " + index);

            try
            {

                using (LockRules lockRules = new LockRules())
                using (RoleRules roleRules = new RoleRules())
                {
                    lockRules.GetDBConfig(db.Connection);
                    roleRules.GetDBConfig(db.Connection);
                    
                    while ((queueItem = _queue[index].nextItem) != null)
                    {

                        StringBuilder tLog = new StringBuilder();
                        try
                        {

                            PluginConfig pluginConfig = null;

                            if ((pluginConfig == null) || (pluginConfig.resource != queueItem.resource_id) || (pluginConfig.plugin_id != queueItem.plugin_id))
                            {
                                if (pluginConfig != null)
                                {
                                    pluginConfig.Dispose();
                                    pluginConfig = null;
                                }

                                using (DataTable dtContext = db.Select("select p.scheme, rp.* from resource_plugin rp with(nolock) inner join plugin p with(nolock) on rp.plugin_id = p.id where rp.resource_id = " + queueItem.resource_id + " and rp.plugin_id = " + queueItem.plugin_id))
                                {
                                    if ((dtContext != null) && (dtContext.Rows.Count > 0))
                                    {
                                        pluginConfig = new PluginConfig(db.Connection, dtContext.Rows[0]["scheme"].ToString(), (Int64)dtContext.Rows[0]["plugin_id"], (Int64)dtContext.Rows[0]["id"]);
                                    }
                                }
                            }

                            if (pluginConfig == null)
                                throw new Exception("Resource x plugin not found");

                            //Realiza todo o processamento deste registro
                            using (RegistryProcess proc = new RegistryProcess(db, pluginConfig, queueItem.context_id, queueItem.enterprise_id, new Uri(queueItem.plugin_uri), queueItem.plugin_id, queueItem.resource_id, queueItem.import_id, queueItem.registry_id))
                            {

                                RegistryProcess.ProccessLog log = new RegistryProcess.ProccessLog(delegate(String text)
                                {
                                    tLog.AppendLine(text);
                                });

                                proc.OnLog += log;
                                Boolean ok = proc.Process(keys[queueItem.enterprise_id], lockRules, roleRules, licControl[queueItem.enterprise_id]);
                                proc.OnLog -= log;

                                db.AddUserLog(LogKey.Import, null, "Engine", (ok ? UserLogLevel.Info : UserLogLevel.Error), 0, 0, 0, queueItem.resource_id, queueItem.plugin_id, proc.EntityId, proc.IdentityId, "Import processed", tLog.ToString());

                            }

                            Console.Write(".");

                        }
                        catch (Exception ex)
                        {
                            Console.Write("!");

                            db.AddUserLog(LogKey.Import, null, "Engine", UserLogLevel.Error, 0, 0, 0, queueItem.resource_id, queueItem.plugin_id, 0, 0, ex.Message, tLog.ToString());

                            //TextLog.Log("Engine", "Importer", "\tError on process registry (plugin_uri=" + dr["plugin_uri"] + ", resource_id=" + dr["resource_id"] + ", import_id=" + dr["import_id"] + ", registry_id=" + dr["registry_id"] + ") " + ex.Message);
                            db.ExecuteNonQuery("update collector_imports set status = 'E' where status = 'F' and plugin_uri = '" + queueItem.plugin_uri + "' and resource_id = '" + queueItem.resource_id + "' and  import_id = '" + queueItem.import_id + "' and registry_id = '" + queueItem.registry_id + "'", CommandType.Text, null);
                        }
                        finally
                        {
                            tLog = null;
                        }

                    }
                }
            }
            finally
            {
                Console.WriteLine("Thread ended " + index);
            }

            db.Dispose();



        }*/

    }
}
