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


            Int32 cnt = 0;
            Int32 stepWait = 15000;
            while (cnt <= 10)
            {
                try
                {
                    IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();

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

        private void WatchdogTimerCallback(Object o)
        {
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


                                            IAMDatabase db = null;
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
