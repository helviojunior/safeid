using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
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
using IAM.GlobalDefs;
using SafeTrend.Json;
using IAM.Queue;
using SafeTrend.Data;

namespace IAM.Inbound
{
    public partial class IAMInbound : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer inboundTimer;
        Timer statusTimer;
        Boolean executing = false;
        private String last_status = "";
        private DateTime startTime = new DateTime(1970, 1, 1);
        private Int32 filesToProcess = 0;
        private Int32 filesProcessed = 0;

        private QueueManager<FileInfo> fileQueue = null;

        public IAMInbound()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            /*************
             * Carrega configurações
             */

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            localConfig = new ServerLocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);
            
            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);


            Int32 cnt = 0;
            Int32 stepWait = 15000;
            while (cnt <= 10)
            {
                try
                {
                    IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();

                    db.ServiceStart("Inbound", null);

                    //Recria a tabela temporária
                    db.ExecuteNonQuery(@"
                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'collector_imports_temp'))
                    BEGIN
                        DROP TABLE [collector_imports_temp];
                    END", System.Data.CommandType.Text, null, null);

                    db.ExecuteNonQuery(@"
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'collector_imports_temp'))
                    BEGIN
                        select top 0 * into [collector_imports_temp] from [collector_imports];
                    END", System.Data.CommandType.Text, null, null);


                    db.closeDB();

                    break;
                }
                catch (Exception ex)
                {
                    if (cnt < 10)
                    {
                        TextLog.Log("Inbound", "Falha ao acessar o banco de dados: " + ex.Message);
                        Thread.Sleep(stepWait);
                        stepWait = stepWait * 2;
                        cnt++;
                    }
                    else
                    {
                        StopOnError("Falha ao acessar o banco de dados", ex);
                    }
                }
            }


            /*************
             * Inicia processo de verificação/atualização da base de dados
             */
            try
            {
                using(IAM.GlobalDefs.Update.IAMDbUpdate updt = new GlobalDefs.Update.IAMDbUpdate(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
                    updt.Update();
            }
            catch (Exception ex)
            {
                StopOnError("Falha ao atualizar o banco de dados", ex);
            }

            /*************
             * Inicia timer que processa os arquivos
             */

            Int32 maxThreads = 1;
            if (maxThreads > 0)
                maxThreads = localConfig.EngineMaxThreads;

            this.fileQueue = new QueueManager<FileInfo>(maxThreads, ProcQueue);
            this.fileQueue.Start();

            inboundTimer = new Timer(new TimerCallback(InboundTimer), null, 1000, 60000);
            statusTimer = new Timer(new TimerCallback(TmrServiceStatusCallback), null, 100, 10000);

        }

        private void TmrServiceStatusCallback(Object o)
        {
            IAMDatabase db = null;
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                Double percent = 0;

                percent = ((Double)(filesProcessed) / (Double)filesToProcess) * 100F;

                if (Double.IsNaN(percent) || Double.IsInfinity(percent))
                    percent = 0;

                db.ServiceStatus("Inbound", JSON.Serialize2(new { host = Environment.MachineName, executing = executing, start_time = startTime.ToString("o"), last_status = last_status, total_files = filesToProcess, processed_files = filesProcessed, percent = percent }), null);

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


        private void InboundTimer(Object state)
        {

            if (executing)
                return;

            executing = true;

            startTime = DateTime.Now;

            filesToProcess = 0;
            filesProcessed = 0;

            try
            {
                DirectoryInfo inDir = new DirectoryInfo(Path.Combine(basePath, "In"));
                if (!inDir.Exists)
                {
                    //TextLog.Log("Inbound", "\t0 files to process");
                    return;
                }

                FileInfo[] files = inDir.GetFiles("*.iamreq");
                //TextLog.Log("Inbound", "\t" + files.Length + " files to process");

                if (files.Length == 0)
                    return;

                filesToProcess = files.Length;

                TextLog.Log("Inbound", "Starting inbound timer");
                try
                {
                    foreach (FileInfo f in files)
                    {
                        this.fileQueue.AddItem(f);
                    }

                    this.fileQueue.Wait();

                }
                finally
                {
                    TextLog.Log("Inbound", "Finishing inbound timer");
                }
            }
            catch (Exception ex)
            {
                TextLog.Log("Inbound", "Error on inbound timer " + ex.Message);
            }
            finally
            {
                executing = false;
                last_status = "";
                startTime = new DateTime(1970, 1, 1);

                filesToProcess = 0;
                filesProcessed = 0;

            }
            
        }


        private void ProcQueue(FileInfo f, Object oStarter)
        {

            IAMDatabase db = null;
            try
            {

                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 900;
                Boolean rebuildIndex = false;

                String type = "";

                type = "";
                JSONRequest req = null;
                try
                {
                    using (FileStream fs = f.OpenRead())
                        req = JSON.GetRequest(fs);

                    if ((req.host == null) || (req.host == ""))
                    {
                        db.AddUserLog(LogKey.Inbound, null, "Inbound", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Paramter 'host' is empty on  " + f.Name);
                        return;
                    }

                    if ((req.enterpriseid == null) || (req.enterpriseid == ""))
                    {
                        db.AddUserLog(LogKey.Inbound, null, "Inbound", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Paramter 'enterpriseid' is empty on  " + f.Name);
                        return;
                    }

                    try
                    {
                        Int64 tst = Int64.Parse(req.enterpriseid);
                    }
                    catch
                    {
                        if ((req.enterpriseid == null) || (req.enterpriseid == ""))
                        {
                            db.AddUserLog(LogKey.Inbound, null, "Inbound", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Paramter 'enterpriseid' is not Int64  " + f.Name);
                            return;
                        }
                    }

                    ProxyConfig config = new ProxyConfig(true);
                    config.GetDBCertConfig(db.Connection, Int64.Parse(req.enterpriseid), req.host);

                    if (config.fqdn != null) //Encontrou o proxy
                    {
                        JsonGeneric jData = new JsonGeneric();
                        try
                        {

                            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                            using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.server_pkcs12_cert), certPass), Convert.FromBase64String(req.data)))
                                jData.FromJsonBytes(cApi.clearData);
                        }
                        catch (Exception ex)
                        {
                            jData = null;
                            db.AddUserLog(LogKey.Inbound, null, "Inbound", UserLogLevel.Error, config.proxyID, 0, 0, 0, 0, 0, 0, "Error on decrypt package data " + f.Name + " for enterprise " + req.enterpriseid + " and proxy " + req.host + ", " + ex.Message);
                        }

                        if (jData == null)
                            return;

                        type = jData.function.ToLower();

                        switch (type)
                        {
                            case "processimport-disabled":
                                rebuildIndex = true;
                                //ImportRegisters(config, jData, f, req, db);
                                f.Delete();
                                break;

                            case "processimportv2":
                                rebuildIndex = true;
                                last_status = "Executando importação de registros";
                                ImportRegistersV2(config, jData, f, req, db);
                                f.Delete();
                                break;

                            case "processstructimport":
                                last_status = "Executando importação de registros de estrutura";
                                ImportRegistersStruct(config, jData, f, req, db);
                                f.Delete();
                                break;

                            case "notify":
                                last_status = "Executando importação de notificações";
                                ImportNotify(config, jData, f, req, db);

                                f.Delete();
                                break;

                            case "deleted":
                                last_status = "Executando importação de exclusões";
                                ImportDelete(config, jData, f, req, db);
                                f.Delete();
                                break;

                            case "logrecords":
                                last_status = "Executando importação de logs";
                                ImportLogs(config, jData, f, req, db);
                                f.Delete();
                                //f.MoveTo(f.FullName + ".imported");
                                break;

                            case "packagetrack":
                                last_status = "Executando importação de track dos pacotes";
                                ImportPackageTrack(config, jData, f, req, db);
                                f.Delete();
                                //f.MoveTo(f.FullName + ".imported");
                                break;

                            default:
                                db.AddUserLog(LogKey.Inbound, null, "Inbound", UserLogLevel.Error, config.proxyID, 0, 0, 0, 0, 0, 0, "Invalid jData function '" + jData.function + "'");
                                break;
                        }

                    }
                    else
                    {
                        db.AddUserLog(LogKey.Inbound, null, "Inbound", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Proxy config not found for enterprise " + req.enterpriseid + " and proxy " + req.host);
                    }
                    config = null;

                }
                catch (Exception ex)
                {
                    TextLog.Log("Inbound", "Erro on process file '" + f.Name + "' (" + type + "): " + ex.Message);
                    db.AddUserLog(LogKey.Import, null, "Inbound", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, "Erro processing file '" + f.Name + "' (" + type + "): " + ex.Message);
                }
                finally
                {
                    last_status = "";
                    req = null;

                    filesProcessed++;
                }

                /*
                if (rebuildIndex)
                {
                    db.Timeout = 900;
                    last_status = "Reindexando registros";
                    db.ExecuteNonQuery("sp_reindex_imports", CommandType.StoredProcedure, null);
                }*/



            }
            catch (Exception ex)
            {
                

                TextLog.Log("Inbound", "Error importing file (" + f.Name + ")" + ex.Message);
            }
            finally
            {
                if (db != null)
                    db.closeDB();
            }
        }

        private void ImportPackageTrack(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {
            Int32 resourceCol = jData.GetKeyIndex("resource");

            Int32 dateCol = jData.GetKeyIndex("date");
            Int32 sourceCol = jData.GetKeyIndex("source");
            Int32 filenameCol = jData.GetKeyIndex("filename");
            Int32 packageIdCol = jData.GetKeyIndex("packageid");
            Int32 flowCol = jData.GetKeyIndex("flow");
            Int32 textCol = jData.GetKeyIndex("text");


            if (resourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'resource' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (sourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'source' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (textCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'text' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (flowCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'flow' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (filenameCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'filename' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (packageIdCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'packageid' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            DateTime date = DateTime.Now;

            foreach (String[] dr in jData.data)
                try
                {
                    //Console.WriteLine(f.Name + " - " + dr[entityIdCol] + " ==> " + dr[textCol]);
                    //Console.WriteLine(dr[additionaldataCol]);
                    //Console.WriteLine("");

                    Int64 packageId = 0;

                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@flow", typeof(String)).Value = dr[flowCol];
                    par.Add("@package_id", typeof(String)).Value = dr[packageIdCol];

                    try
                    {
                        Int64 tmp = db.ExecuteScalar<Int64>("select id from st_package_track where flow = @flow and package_id = @package_id", System.Data.CommandType.Text, par, null);

                        if (tmp > 0)
                            packageId = tmp;
                    }
                    catch { }

                    if (packageId == 0)
                    {
                        par = new DbParameterCollection();
                        par.Add("@entity_id", typeof(Int64)).Value = 0;
                        par.Add("@date", typeof(DateTime)).Value = (dateCol >= 0 ? DateTime.Parse(dr[dateCol]) : date);
                        par.Add("@flow", typeof(String)).Value = dr[flowCol];
                        par.Add("@package_id", typeof(String), dr[packageIdCol].Length).Value = dr[packageIdCol];
                        par.Add("@filename", typeof(String), dr[filenameCol].Length).Value = dr[filenameCol];
                        par.Add("@package", typeof(String), dr[textCol].Length).Value = dr[textCol];

                        packageId = db.ExecuteScalar<Int64>("sp_new_package_track", System.Data.CommandType.StoredProcedure, par, null);

                    }

                    db.AddPackageTrack(packageId, dr[flowCol], dr[textCol]);


                }
                catch (Exception ex)
                {
                    throw ex;
                }

            jData = null;


        }

        private void ImportLogs(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {
            Int32 resourceCol = jData.GetKeyIndex("resource");

            Int32 dateCol = jData.GetKeyIndex("date");
            Int32 sourceCol = jData.GetKeyIndex("source");
            Int32 keyCol = jData.GetKeyIndex("key");
            Int32 uriCol = jData.GetKeyIndex("uri");
            Int32 typeCol = jData.GetKeyIndex("type");
            Int32 entityIdCol = jData.GetKeyIndex("entityid");
            Int32 identityIdCol = jData.GetKeyIndex("identityid");
            Int32 textCol = jData.GetKeyIndex("text");

            Int32 additionaldataCol = jData.GetKeyIndex("additionaldata");

            if (resourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'resource' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (sourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'source' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (keyCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'key' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }



            if (uriCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'uri' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (entityIdCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'entityId' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (identityIdCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'identityId' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (textCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'text' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            DateTime date = DateTime.Now;

            //Realiza a importação no modelo BulkInsert por melhor desempenho do banco
            DataTable dtBulk = new DataTable();
            dtBulk.Columns.Add(new DataColumn("date", typeof(DateTime)));
            dtBulk.Columns.Add(new DataColumn("source", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("key", typeof(Int32)));
            dtBulk.Columns.Add(new DataColumn("enterprise_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("proxy_name", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("proxy_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("plugin_uri", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("plugin_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("resource_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("entity_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("identity_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("type", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("text", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("additional_data", typeof(String)));

            foreach (String[] dr in jData.data)
                try
                {
                    //Console.WriteLine(f.Name + " - " + dr[entityIdCol] + " ==> " + dr[textCol]);
                    //Console.WriteLine(dr[additionaldataCol]);
                    //Console.WriteLine("");
                    dtBulk.Rows.Add(new Object[] { (dateCol >= 0 ? DateTime.Parse(dr[dateCol]) : date), dr[sourceCol], dr[keyCol], req.enterpriseid, req.host, 0, dr[uriCol], 0, Int64.Parse(dr[resourceCol]), Int64.Parse(dr[entityIdCol]), Int64.Parse(dr[identityIdCol]), dr[typeCol], dr[textCol], (additionaldataCol >= 0 ? dr[additionaldataCol] : "") });
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            db.BulkCopy(dtBulk, "logs_imports");

            //Procedure que processa os logs e importa para a tabela definitiva
            db.ExecuteNonQuery("sp_process_logs", CommandType.StoredProcedure, null);

#if debug
            db.AddUserLog(LogKey.Import, null, "Inbound", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, "Imported " + dtBulk.Rows.Count + " logs for enterprise " + req.enterpriseid + " and proxy " + req.host + " from file " + f.Name);
            TextLog.Log("Inbound", "\t[ImportLogs] Imported " + dtBulk.Rows.Count + " logs for enterprise " + req.enterpriseid + " and proxy " + req.host);
#endif



            dtBulk.Dispose();
            dtBulk = null;

            jData = null;

        }


        private void ImportNotify(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {
            Int32 resourceCol = jData.GetKeyIndex("resource");

            Int32 sourceCol = jData.GetKeyIndex("source");
            Int32 uriCol = jData.GetKeyIndex("uri");
            Int32 entityIdCol = jData.GetKeyIndex("entityid");

            if (resourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportNotify] Erro on find column 'resource' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (sourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportLogs] Erro on find column 'source' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (uriCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportNotify] Erro on find column 'uri' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (entityIdCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportNotify] Erro on find column 'entityId' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            
            DateTime date = DateTime.Now;

            //Realiza a importação no modelo BulkInsert por melhor desempenho do banco
            DataTable dtBulk = new DataTable();
            dtBulk.Columns.Add(new DataColumn("date", typeof(DateTime)));
            dtBulk.Columns.Add(new DataColumn("source", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("plugin_uri", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("resource_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("entity_id", typeof(Int64)));

            foreach (String[] dr in jData.data)
                dtBulk.Rows.Add(new Object[] { date, dr[sourceCol], dr[uriCol], Int64.Parse(dr[resourceCol]), Int64.Parse(dr[entityIdCol]) });

            db.BulkCopy(dtBulk, "notify_imports");

#if DEBUG
            TextLog.Log("Inbound", "\t[ImportNotify] Imported " + dtBulk.Rows.Count + " notify for enterprise " + req.enterpriseid + " and proxy " + req.host);
#endif

            dtBulk.Dispose();
            dtBulk = null;

            jData = null;

        }


        private void ImportDelete(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {
            Int32 resourceCol = jData.GetKeyIndex("resource");

            Int32 sourceCol = jData.GetKeyIndex("source");
            Int32 uriCol = jData.GetKeyIndex("uri");
            Int32 entityIdCol = jData.GetKeyIndex("entityid");
            Int32 identityIdCol = jData.GetKeyIndex("identityid");

            if (resourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportDelete] Erro on find column 'resource' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (sourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportDelete] Erro on find column 'source' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (uriCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportDelete] Erro on find column 'uri' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (entityIdCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportDelete] Erro on find column 'entityId' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            if (identityIdCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportDelete] Erro on find column 'identityId' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            DateTime date = DateTime.Now;

            foreach (String[] dr in jData.data)
            {
                try
                {
                    db.ExecuteNonQuery("update [identity] set deleted = 1, deleted_date = '" + date.ToString("o") + "' where id = " + dr[identityIdCol], CommandType.Text, null);
                }
                catch { }
            }

#if DEBUG
            TextLog.Log("Inbound", "\t[ImportDelete] Changed " + jData.data.Count + " identities for deleted status in enterprise " + req.enterpriseid + " and proxy " + req.host);
#endif

            jData = null;

        }

        private void ImportRegistersOLD(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {

            Int32 resourceCol = jData.GetKeyIndex("resource");

            Int32 uriCol = jData.GetKeyIndex("uri");
            Int32 importidCol = jData.GetKeyIndex("importid");
            Int32 registryidCol = jData.GetKeyIndex("registryid");
            Int32 datanameCol = jData.GetKeyIndex("dataname");
            Int32 datavalueCol = jData.GetKeyIndex("datavalue");
            Int32 datatypeCol = jData.GetKeyIndex("datatype");


            if (resourceCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'resource' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (uriCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'uri' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (importidCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'importid' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (registryidCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'registryid' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (datanameCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'dataname' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (datavalueCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'datavalue' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (datatypeCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegisters] Erro on find column 'datatype' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            DateTime date = DateTime.Now;


            //Realiza a importação no modelo BulkInsert por melhor desempenho do banco
            DataTable dtBulk = new DataTable();
            dtBulk.Columns.Add(new DataColumn("date", typeof(DateTime)));
            dtBulk.Columns.Add(new DataColumn("file_name", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("plugin_uri", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("resource_id", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("import_id", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("registry_id", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("data_name", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("data_value", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("data_type", typeof(String)));

            foreach (String[] dr in jData.data)
            {
                dtBulk.Rows.Add(new Object[] { date, f.Name, dr[uriCol], Int64.Parse(dr[resourceCol]), dr[importidCol], dr[registryidCol], dr[datanameCol], dr[datavalueCol], dr[datatypeCol] });



            }

            db.BulkCopy(dtBulk, "collector_imports");

            //Atualiza os registros importados deste arquivo para liberar o processamento
            //Isso avisa o sistema que estes registros estão livres para processamento
            db.ExecuteNonQuery("update collector_imports set status = 'F' where [file_name] = '" + f.Name + "'", CommandType.Text, null);
                        
            //Realiza o rebuild do indice desta tabela para agilizar no engine
            //Este processo será executado somente uma vez pelo objeto pai
            //db.ExecuteNonQuery("sp_reindex_imports", CommandType.StoredProcedure, null);
            
#if DEBUG
            TextLog.Log("Inbound", "\t[ImportRegisters] Imported " + dtBulk.Rows.Count + " registers for enterprise " + req.enterpriseid + " and proxy " + req.host);
#endif

            dtBulk.Dispose();
            dtBulk = null;

            jData = null;
        }

        private void ImportRegistersStruct(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {

            Int32 resourcePluginCol = jData.GetKeyIndex("resource_plugin");
            Int32 pkgCol = jData.GetKeyIndex("package");


            if (resourcePluginCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportStruct] Erro on find column 'resource_plugin' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (pkgCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportStruct] Erro on find column 'package' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            //Realiza a importação no modelo BulkInsert por melhor desempenho do banco
            DataTable dtBulk = new DataTable();
            dtBulk.Columns.Add(new DataColumn("date", typeof(DateTime)));
            dtBulk.Columns.Add(new DataColumn("file_name", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("resource_plugin", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("import_id", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("package_id", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("package", typeof(String)));

            foreach (String[] dr in jData.data)
            {
                PluginConnectorBaseImportPackageStruct pkg = JSON.DeserializeFromBase64<PluginConnectorBaseImportPackageStruct>(dr[pkgCol]);
                dtBulk.Rows.Add(new Object[] { DateTime.Now, f.Name, dr[resourcePluginCol], pkg.importId, pkg.pkgId, JSON.Serialize2(pkg) });
            }

            db.BulkCopy(dtBulk, "collector_imports_struct");

            //Atualiza os registros importados deste arquivo para liberar o processamento
            //Isso avisa o sistema que estes registros estão livres para processamento
            db.ExecuteNonQuery("update collector_imports_struct set status = 'F' where [file_name] = '" + f.Name + "'", CommandType.Text, null);

#if DEBUG
            TextLog.Log("Inbound", "\t[ImportStruct] Imported " + dtBulk.Rows.Count + " registers for enterprise " + req.enterpriseid + " and proxy " + req.host);
#endif

            dtBulk.Dispose();
            dtBulk = null;

            jData = null;
        }

        private void ImportRegistersV2(ProxyConfig config, JsonGeneric jData, FileInfo f, JSONRequest req, IAMDatabase db)
        {

            Int32 resourcePluginCol = jData.GetKeyIndex("resource_plugin");
            Int32 pkgCol = jData.GetKeyIndex("package");


            if (resourcePluginCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegistersV2] Erro on find column 'resource_plugin' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }


            if (pkgCol == -1)
            {
                TextLog.Log("Inbound", "\t[ImportRegistersV2] Erro on find column 'package' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                return;
            }

            //Realiza a importação no modelo BulkInsert por melhor desempenho do banco
            DataTable dtBulk = new DataTable();
            dtBulk.Columns.Add(new DataColumn("date", typeof(DateTime)));
            dtBulk.Columns.Add(new DataColumn("file_name", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("resource_plugin", typeof(Int64)));
            dtBulk.Columns.Add(new DataColumn("import_id", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("package_id", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("package", typeof(String)));
            dtBulk.Columns.Add(new DataColumn("status", typeof(String)));

            foreach (String[] dr in jData.data)
            {
                PluginConnectorBaseImportPackageUser pkg = JSON.DeserializeFromBase64<PluginConnectorBaseImportPackageUser>(dr[pkgCol]);
                dtBulk.Rows.Add(new Object[] { DateTime.Now, f.Name, dr[resourcePluginCol], pkg.importId, pkg.pkgId, JSON.Serialize2(pkg), 'F' });

                try
                {
                    
                    DbParameterCollection par = new DbParameterCollection();
                    
                    par.Add("@date", typeof(DateTime)).Value = pkg.GetBuildDate();
                    par.Add("@package_id", typeof(String), pkg.pkgId.Length).Value = pkg.pkgId;

                    Int64 trackId = db.ExecuteScalar<Int64>("select id from st_package_track where flow = 'inbound' and date = @date and package_id = @package_id", System.Data.CommandType.Text, par, null);

                    db.AddPackageTrack(trackId, "inbound", "Package imported to process queue");

                }
                catch { }
            }

            db.BulkCopy(dtBulk, "collector_imports");

            //Apaga todos os registros da tabela temporaria
            /*
             * Procedimento desabiliato em 2018-08-29 por suspeita de problema
            db.ExecuteNonQuery("delete from collector_imports_temp", System.Data.CommandType.Text, null, null);

            db.BulkCopy(dtBulk, "collector_imports_temp");

            //Proteção contra reimportação de pacotes (loop)
            db.ExecuteNonQuery("delete from collector_imports_temp where exists (select 1 from collector_imports_old o where o.date >= dateadd(day,-1,getdate()) and o.file_name = file_name and o.resource_plugin_id = resource_plugin_id and o.import_id = import_id and o.package_id = package_id)", System.Data.CommandType.Text, null, null);
            db.ExecuteNonQuery("delete from collector_imports_temp where exists (select 1 from collector_imports o where o.date >= dateadd(day,-1,getdate()) and o.file_name = file_name and o.resource_plugin_id = resource_plugin_id and o.import_id = import_id and o.package_id = package_id)", System.Data.CommandType.Text, null, null);

            db.ExecuteNonQuery("insert into collector_imports select * from collector_imports_temp", System.Data.CommandType.Text, null, null);
            db.ExecuteNonQuery("delete from collector_imports_temp", System.Data.CommandType.Text, null, null);
             * */

            //Atualiza os registros importados deste arquivo para liberar o processamento
            //Isso avisa o sistema que estes registros estão livres para processamento
            //*** Desabilitado essa funç~~ao em 2018-03-08, e colocado o registro para ser importado diretamente com o Status 'F'
            //db.ExecuteNonQuery("update collector_imports set status = 'F' where [file_name] = '" + f.Name + "'", CommandType.Text, null);

            //Realiza o rebuild do indice desta tabela para agilizar no engine
            //Este processo será executado somente uma vez pelo objeto pai
            //db.ExecuteNonQuery("sp_reindex_imports", CommandType.StoredProcedure, null);

#if DEBUG
            TextLog.Log("Inbound", "\t[ImportRegistersV2] Imported " + dtBulk.Rows.Count + " registers for enterprise " + req.enterpriseid + " and proxy " + req.host);
#endif

            dtBulk.Dispose();
            dtBulk = null;

            jData = null;
        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Inbound", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Inbound", text);
            }

            if (this.fileQueue != null)
                this.fileQueue.StopAndWait();


            Process.GetCurrentProcess().Kill();
        }

        protected override void OnStop()
        {
            
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

    }
}
