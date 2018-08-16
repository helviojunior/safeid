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
using SafeTrend.Json;
using IAM.GlobalDefs;
using System.Reflection;


namespace IAM.Engine
{
    public partial class IAMEngine : ServiceBase
    {

        private ServerLocalConfig localConfig;
        private Timer licTimer;
        private Timer intoTimer;
        private DateTime lastInfo = new DateTime(1970, 1, 1);
        private Boolean infoExec = false;

        public IAMEngine()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            /*************
             * Carrega configurações
             */

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

                    db.ServiceStart("Engine", null);

                    db.closeDB();

                    break;
                }
                catch (Exception ex)
                {
                    if (cnt < 10)
                    {
                        TextLog.Log("Engine", "Falha ao acessar o banco de dados: " + ex.Message);
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
             * Gera os certificados do servidor
             * Verifica se o certificade está próximo de vencer
             */
            IAMDatabase db2 = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
            db2.openDB();
            ServerKey2 sk = new ServerKey2(db2.Connection);

            TimeSpan ts = sk.ServerCert.NotAfter - DateTime.Now;

            if (ts.TotalDays < 360)
            {
                //Inicia o timer que ficará gerando evento
                licTimer = new Timer(new TimerCallback(LicTimer), null, 200, 86400000); //a cada 24 Horas caso este processo não for reiniciado

            }

            db2.closeDB();


            intoTimer = new Timer(new TimerCallback(InfoTimer), null, 200, 900); //a cada 15 minutos

            /*************
             * Inicia as classes de processamento
             */

            RegistryImporter imp = new RegistryImporter(localConfig);
            imp.Start();

            TimeAccessControl acl = new TimeAccessControl(localConfig);
            acl.Start();
            
        }

        private void InfoTimer(Object oData)
        {

            TimeSpan tsInfo = lastInfo - new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0);

            if (infoExec || ((Int32)tsInfo.TotalDays == 0))
                return;

            infoExec = true;

            using (IAMDatabase db2 = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
            using (ServerKey2 sk = new ServerKey2(db2.Connection))
                try
                {

                    db2.openDB();

                    List<Object> lics = new List<Object>();
                    Version version = new Version("0.0");

                    try
                    {
                        DataTable dtLicCount = db2.ExecuteDataTable("select count(e.id) qty, c.enterprise_id from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 group by c.enterprise_id");
                        if (dtLicCount != null)
                            foreach (DataRow dr in dtLicCount.Rows)
                            {
                                String message = "";
                                List<String> data = new List<string>();

                                DataTable dtLic = db2.ExecuteDataTable("select * from license where enterprise_id in (0, " + dr["enterprise_id"] + ")");
                                if (dtLic == null)
                                    message = "Error on get licenses on server";

                                if (dtLic.Rows.Count == 0)
                                    message = "License list is empty";

                                foreach (DataRow dr2 in dtLic.Rows)
                                    data.Add(dr2["license_data"].ToString());

                                //Resgata do banco a contagem atual de entidades

                                var eItem = new {
                                    id = dr["enterprise_id"].ToString(),
                                    message = message,
                                    entity = dr["qty"],
                                    data = data
                                };

                                lics.Add(eItem);
                            }
                        

                        try
                        {
                            version = Assembly.GetEntryAssembly().GetName().Version;
                        }
                        catch { }

                        var info = new
                        {
                            date = DateTime.UtcNow.ToString("o"),
                            product = "SafeId",
                            version = version.ToString(),
                            installation_key = sk.ServerInstallationKey.AbsoluteUri,
                            server_cert = sk.ServerCertString,
                            licences = lics
                        };

                        String jData = JSON.Serialize2(info);

                        WebClient client = new WebClient();
                        client.UploadData("https://licencing.safetrend.com.br/si/", Encoding.UTF8.GetBytes(jData));

                        lastInfo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0);
                    }
                    catch { }
                    finally
                    {
                        db2.closeDB();
                    }

                }
                catch { }

            infoExec = false;
        }

        private void LicTimer(Object oData)
        {

            using (IAMDatabase db2 = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
            using (ServerKey2 sk = new ServerKey2(db2.Connection))
                try
                {

                    db2.openDB();

                    TimeSpan ts = sk.ServerCert.NotAfter - DateTime.Now;

                    if (ts.TotalDays < 60)
                        db2.AddUserLog(LogKey.Certificate_Error, null, "Engine", UserLogLevel.Fatal, 0, 0, 0, 0, 0, 0, 0, "Server certificate will expire in " + sk.ServerCert.NotAfter.ToString("yyyy-MM-dd") + ", please renew", sk.ServerInstallationKey.AbsoluteUri);
                    else if (ts.TotalDays < 180)
                        db2.AddUserLog(LogKey.Certificate_Error, null, "Engine", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Server certificate will expire in " + sk.ServerCert.NotAfter.ToString("yyyy-MM-dd") + ", please renew", sk.ServerInstallationKey.AbsoluteUri);
                    else if (ts.TotalDays < 360)
                        db2.AddUserLog(LogKey.Certificate_Error, null, "Engine", UserLogLevel.Warning, 0, 0, 0, 0, 0, 0, 0, "Server certificate will expire in " + sk.ServerCert.NotAfter.ToString("yyyy-MM-dd") + ", please renew", sk.ServerInstallationKey.AbsoluteUri);

                    db2.closeDB();
                }
                catch { }

        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Engine", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Engine", text);
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
