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
using IAM.SQLDB;
using JsonBase;


namespace IAM.Server
{
    public partial class IAMServer : ServiceBase
    {

        LocalConfig localConfig;
        String basePath = "";

        Timer inboundTimer;
        Timer outboundTimer;

        public IAMServer()
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

            localConfig = new LocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);
            
            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);
            
            try
            {
                MSSQLDB db = new MSSQLDB(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                db.closeDB();
            }
            catch (Exception ex)
            {
                StopOnError("Falha ao acessar o banco de dados", ex);
            }

            
            /*************
             * Inicia timer que processa os arquivos
             */

            inboundTimer = new Timer(new TimerCallback(InboundTimer), null, 1000, 900000);
            outboundTimer = new Timer(new TimerCallback(OutboundTimer), null, 1000, 900000);

        }

        private void InboundTimer(Object state)
        {
            TextLog.Log("Server", "Starting inbound timer");
            try
            {
                DirectoryInfo inDir = new DirectoryInfo(Path.Combine(basePath, "In"));
                if (!inDir.Exists)
                {
                    TextLog.Log("Server", "\t0 files to process");
                    return;
                }

                FileInfo[] files = inDir.GetFiles("*.iamreq");
                TextLog.Log("Server", "\t" + files.Length + " files to process");


                MSSQLDB db = new MSSQLDB(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                foreach (FileInfo f in files)
                {

                    JSONRequest req = null;
                    try
                    {
                        using (FileStream fs = f.OpenRead())
                            req = JSON.GetRequest(fs);

                        if ((req.host == null) || (req.host == ""))
                        {
                            TextLog.Log("Server", "Paramter 'host' is empty on  " + f.Name);
                            continue;
                        }

                        if ((req.enterpriseid == null) || (req.enterpriseid == ""))
                        {
                            TextLog.Log("Server", "Paramter 'enterpriseid' is empty on  " + f.Name);
                            continue;
                        }

                        try
                        {
                            Int64 tst = Int64.Parse(req.enterpriseid);
                        }
                        catch {
                            if ((req.enterpriseid == null) || (req.enterpriseid == ""))
                            {
                                TextLog.Log("Server", "Paramter 'enterpriseid' is not Int64  " + f.Name);
                                continue;
                            }
                        }

                        ProxyConfig config = new ProxyConfig(true);
                        config.GetDBCertConfig(db.conn, Int64.Parse(req.enterpriseid), req.host);

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
                                TextLog.Log("Server", "Error on decrypt package data " + f.Name + " for enterprise " + req.enterpriseid + " and proxy " + req.host + ", " + ex.Message);

                            }

                            if (jData == null)
                                continue;

                            Int32 contextCol = jData.GetKeyIndex("context");

                            Int32 uriCol = jData.GetKeyIndex("uri");
                            Int32 importidCol = jData.GetKeyIndex("importid");
                            Int32 registryidCol = jData.GetKeyIndex("registryid");
                            Int32 datanameCol = jData.GetKeyIndex("dataname");
                            Int32 datavalueCol = jData.GetKeyIndex("datavalue");
                            Int32 datatypeCol = jData.GetKeyIndex("datatype");

                            if (uriCol == -1)
                            {
                                TextLog.Log("Server", "Erro on find column 'uri' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                                continue;
                            }


                            if (importidCol == -1)
                            {
                                TextLog.Log("Server", "Erro on find column 'importid' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                                continue;
                            }


                            if (registryidCol == -1)
                            {
                                TextLog.Log("Server", "Erro on find column 'registryid' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                                continue;
                            }


                            if (datanameCol == -1)
                            {
                                TextLog.Log("Server", "Erro on find column 'dataname' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                                continue;
                            }


                            if (datavalueCol == -1)
                            {
                                TextLog.Log("Server", "Erro on find column 'datavalue' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                                continue;
                            }


                            if (datatypeCol == -1)
                            {
                                TextLog.Log("Server", "Erro on find column 'datatype' in " + f.Name + " enterprise " + req.enterpriseid + " and proxy " + req.host);
                                continue;
                            }

                            DateTime date = DateTime.Now;


                            //Realiza a importação no modelo BulkInsert por melhor desempenho do banco
                            DataTable dtBulk = new DataTable();
                            dtBulk.Columns.Add(new DataColumn("date", typeof(DateTime)));
                            dtBulk.Columns.Add(new DataColumn("plugin_uri", typeof(String)));
                            dtBulk.Columns.Add(new DataColumn("context_id", typeof(Int64)));
                            dtBulk.Columns.Add(new DataColumn("import_id", typeof(String)));
                            dtBulk.Columns.Add(new DataColumn("registry_id", typeof(String)));
                            dtBulk.Columns.Add(new DataColumn("data_name", typeof(String)));
                            dtBulk.Columns.Add(new DataColumn("data_value", typeof(String)));
                            dtBulk.Columns.Add(new DataColumn("data_type", typeof(String)));

                            foreach (String[] dr in jData.data)
                                dtBulk.Rows.Add(new Object[] { date, dr[uriCol], Int64.Parse(dr[contextCol]), dr[importidCol], dr[registryidCol], dr[datanameCol], dr[datavalueCol], dr[datatypeCol] });

                            db.BulkCopy(dtBulk, "collector_imports");

                            TextLog.Log("Server", "Imported " + dtBulk.Rows.Count + " registers for enterprise " + req.enterpriseid + " and proxy " + req.host);

                            dtBulk.Dispose();
                            dtBulk = null;

                            jData = null;

                            f.Delete();
                        }
                        else
                        {
                            TextLog.Log("Server", "Proxy config not found for enterprise " + req.enterpriseid + " and proxy " + req.host);
                        }
                        config = null;

                    }
                    finally
                    {
                        req = null;
                    }
                }
                db.closeDB();

            }
            catch (Exception ex)
            {
                TextLog.Log("Server", "Error on inbound timer " + ex.Message);
            }
            finally
            {
                TextLog.Log("Server", "Finishing inbound timer");
            }
        }

        private void OutboundTimer(Object state)
        {

        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
            }

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
