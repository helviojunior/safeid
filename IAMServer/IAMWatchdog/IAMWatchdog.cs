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
using System.Management;

using IAM.Config;
using IAM.Log;
//using IAM.SQLDB;
using IAM.Scheduler;
using IAM.LocalConfig;
using IAM.GlobalDefs;
using SafeTrend.Json;


namespace IAM.Watchdog
{

    public enum StartupState
    {
        Unknown,
        Disabled,
        Automatic,
        Manual,
        Refused,
        Error
    }


    public partial class IAMWatchdog : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer watchdogTimer;
        Timer statusTimer;

        public IAMWatchdog()
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


            /*************
             * Inicia processo de verificação/atualização da base de dados
             */
            try
            {
                using (IAM.GlobalDefs.Update.IAMDbUpdate updt = new GlobalDefs.Update.IAMDbUpdate(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
                    updt.Update();
            }
            catch (Exception ex)
            {
                StopOnError("Falha ao atualizar o banco de dados", ex);
            }


            Int32 cnt = 0;
            Int32 stepWait = 15000;
            while (cnt <= 10)
            {
                try
                {
                    IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();

                    db.ServiceStart("Watchdog", null);

                    db.closeDB();

                    break;
                }
                catch (Exception ex)
                {
                    if (cnt < 10)
                    {
                        TextLog.Log("Watchdog", "Falha ao acessar o banco de dados: " + ex.Message);
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
             * Inicia timer que processa os arquivos
             */

            watchdogTimer = new Timer(new TimerCallback(WatchdogTimerCallback), null, 1000, 30000);
            statusTimer = new Timer(new TimerCallback(TmrServiceStatusCallback), null, 100, 30000);
        }


        private void TmrServiceStatusCallback(Object o)
        {
            IAMDatabase db = null;
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                db.ServiceStatus("Watchdog", JSON.Serialize2(new { host = Environment.MachineName }), null);

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

        public void Killall(String name)
        {
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.ToLower() == name.ToLower())
                        p.Kill();
                }
                catch { }
            }
        }

        private void WatchdogTimerCallback(Object o)
        {
            IAMDatabase db = null;
            try
            {
                //check if we need to stop any service
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                //Limpa status lixo
                db.ExecuteNonQuery("delete from service_status where last_status < DATEADD(day,-15,getdate())");

                //seleciona os servicos comproblema ou parados
                DataTable dtServices = db.Select("select * from service_status where started_at is null or last_status < DATEADD(hour,-1,getdate()) or case when started_at is null then cast(getdate() as date) else cast(started_at as date) end <> cast(getdate() as date)");
                if (dtServices != null && dtServices.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtServices.Rows)
                    {
                        String svcName = dr["service_name"].ToString();

                        if (svcName.ToLower().IndexOf("watchdog") >= 0)
                            continue;

                        TextLog.Log("Watchdog", "Killing service '" + svcName + "'");
                        Killall(svcName);
                        Killall("IAM" + svcName);
                    }
                }

                db.closeDB();
            }
            catch { }
            finally
            {
                if (db != null)
                    db.Dispose();

                db = null;
            }

            try
            {
                ServiceController[] services = ServiceController.GetServices();

                foreach(ServiceController service in ServiceController.GetServices())
                    try
                    {
                        
                        switch (service.ServiceName.ToLower())
                        {
                            case "iambackup":
                            case "iamdispatcher":
                            case "iamengine":
                            case "iaminbound":
                            case "iamreport":
                            case "iamproxy":
                            case "iammultiproxy":
                            case "iammessenger":
                            case "iamworkflowprocessor":
                                StartupState stMode = StartMode(service.ServiceName);

                                switch (stMode)
                                {
                                    case StartupState.Automatic:
                                        if ((service.Status.Equals(ServiceControllerStatus.Stopped)) || (service.Status.Equals(ServiceControllerStatus.StopPending)))
                                        {
                                            TextLog.Log("Watchdog", "Starting service '" + service.DisplayName + "'");
                                            service.Start();

                                            try
                                            {
                                                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                                                db.openDB();
                                                db.Timeout = 600;

                                                db.AddUserLog(LogKey.Watchdog, null, "Watchdog", UserLogLevel.Warning, 0, 0, 0, 0, 0, 0, 0, "Starting service '" + service.DisplayName + "'");

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
                                        break;

                                    default:
                                        TextLog.Log("Watchdog", "Unknow action for service start mode '" + stMode.ToString() + "' for service '" + service.DisplayName + "'");
                                        break;
                                }

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        TextLog.Log("Watchdog", "Erro ao processar o controle do serviço '" + service.DisplayName + "': " + ex.Message);
                    }

            }
            catch (Exception ex)
            {
                TextLog.Log("Watchdog", "Erro ao processar o controle dos serviços: " + ex.Message);
            }
        }

        private StartupState StartMode(String serviceName)
        {
            StartupState state = StartupState.Unknown;
            try
            {
                
                string wmiQuery = @"SELECT * FROM Win32_Service WHERE Name='" + serviceName + @"'";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
                ManagementObjectCollection results = searcher.Get();
                foreach (ManagementObject service in results)
                {
#if DEBUG
                    TextLog.Log("Watchdog", "StartMode: " + service["StartMode"]);
#endif

                    switch (service["StartMode"].ToString().ToLower())
                    {
                        case "disabled":
                            state = StartupState.Disabled;
                            break;

                        case "auto":
                        case "automatic":
                            state = StartupState.Automatic;
                            break;

                        case "manual":
                            state = StartupState.Manual;
                            break;
                    }

                }
                return state;
            }
            catch (Exception e)
            {
                return StartupState.Error;
            }
        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Watchdog", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Watchdog", text);
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
