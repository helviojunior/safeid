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
using IAM.Deploy;
using IAM.Scheduler;
using IAM.LocalConfig;
using SafeTrend.Json;
using IAM.GlobalDefs;
using IAM.GlobalDefs;

namespace IAM.Dispatcher
{
    public partial class IAMDispatcher : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer dispatcherTimer;
        Timer deployNowTimer;
        Timer statusTimer;
        Boolean executing = false;
        Boolean executing2 = false;

        public IAMDispatcher()
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
                    db.Dispose();

                    break;
                }
                catch (Exception ex)
                {
                    if (cnt < 10)
                    {
                        TextLog.Log("Dispatcher", "Falha ao acessar o banco de dados: " + ex.Message);
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

            dispatcherTimer = new Timer(new TimerCallback(DispatcherTimer), null, 1000, 60000);
            deployNowTimer = new Timer(new TimerCallback(DeployNowTimer), null, 1000, 60000);
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

                db.ServiceStatus("Dispatcher", JSON.Serialize2(new { host = Environment.MachineName, executing_deploy_now = executing2, executing = executing }), null);

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


        private void DeployNowTimer(Object state)
        {

            if (executing2)
                return;

            executing2 = true;

            
            try
            {

                IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                DataTable dtS = db.Select("select entity_id, MAX(date) [date] from deploy_now with(nolock) where date < GETDATE() group by entity_id order by MAX(date)");

                if ((dtS == null) || (dtS.Rows.Count == 0))
                    return;

                TextLog.Log("Dispatcher", "Starting deploy now timer");

                //Processa um a um dos agendamentos
                foreach (DataRow dr in dtS.Rows)
                {
                    try
                    {
                        Int32 count = 0;
                        using (IAMDeploy deploy = new IAMDeploy("Dispatcher", localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
                        {
                            count = deploy.DeployOne((Int64)dr["entity_id"]);


                            if (count == 0)
                                db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Error, 0, 0, 0, 0, 0, (Int64)dr["entity_id"], 0, "Erro on deploy now user: no package sent", deploy.ExecutionLog);
                        }


                        db.ExecuteNonQuery("delete from deploy_now where entity_id = " + dr["entity_id"], CommandType.Text, null);
                    }
                    catch(Exception ex2) {
                        db.AddUserLog(LogKey.User_Deploy, null, "Deploy", UserLogLevel.Error, 0, 0, 0, 0, 0, (Int64)dr["entity_id"], 0, "Erro on deploy now user: " + ex2.Message, ex2.StackTrace);
                    }
                }

                db.closeDB();
                db.Dispose();
                db = null;

                dtS.Clear();
                dtS = null;

            }
            catch (Exception ex)
            {
                TextLog.Log("Dispatcher", "\tError on deploy now timer " + ex.Message + ex.StackTrace);
            }
            finally
            {
                TextLog.Log("Dispatcher", "Finishing deploy now timer");
                executing2 = false;
            }
        }

        private void DispatcherTimer(Object state)
        {

            if (executing)
                return;

            executing = true;

            TextLog.Log("Dispatcher", "Starting dispatcher timer");
            try
            {

                IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                DataTable dtS = db.Select("select * from vw_schedules order by context_id, [order]");
                
                //Processa um a um dos agendamentos
                foreach (DataRow dr in dtS.Rows)
                    CheckSchedule(db, (Int64)dr["schedule_id"], (Int64)dr["resource_plugin_id"], (Int64)dr["resource_id"], dr["schedule"].ToString(), (DateTime)dr["next"]);

                dtS.Clear();
                dtS = null;

                db.closeDB();
                db.Dispose();
                db = null;

            }
            catch (Exception ex)
            {
                TextLog.Log("Dispatcher", "\tError on dispatcher timer " + ex.Message);
            }
            finally
            {
                TextLog.Log("Dispatcher", "Finishing dispatcher timer");
                executing = false;
            }
        }

        private void CheckSchedule(IAMDatabase db, Int64 scheduleId, Int64 resourcePluginId, Int64 resourceId, String jSonSchedule, DateTime next)
        {

            DateTime date = DateTime.Now;
            TimeSpan ts = date - new DateTime(1970, 01, 01);

            Schedule schedule = new Schedule();
            try
            {
                schedule.FromJsonString(jSonSchedule);
                jSonSchedule = null;
            }
            catch
            {
                schedule.Dispose();
                schedule = null;
            }

            if (schedule == null)
                return;

            //Check Start date

            TimeSpan stDateTs = next - new DateTime(1970, 01, 01);
            TextLog.Log("Dispatcher", "[" + resourceId + "] CheckSchedule> next " + next.ToString("yyyy-MM-dd HH:mm:ss"));
            TextLog.Log("Dispatcher", "[" + resourceId + "] CheckSchedule> Executa agora? " + (ts.TotalSeconds >= stDateTs.TotalSeconds));
            if (ts.TotalSeconds >= stDateTs.TotalSeconds) //Data e hora atual maior ou igual a data que se deve iniciar
            {
                TextLog.Log("Dispatcher", "[" + resourceId + "] Starting execution");

                try
                {
                    using(IAMDeploy deploy = new IAMDeploy("Dispatcher", localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
                        deploy.DeployResourcePlugin(resourcePluginId);
                }
                catch (Exception ex)
                {
                    TextLog.Log("Dispatcher", "[" + resourceId + "] Error on execution " + ex.Message);
                }
                finally
                {
                    TextLog.Log("Dispatcher", "[" + resourceId + "] Execution completed");

                    //Agenda a próxima execução
                    DateTime nextExecute = schedule.CalcNext();

                    db.ExecuteNonQuery("update resource_plugin_schedule set [next] = '" + nextExecute.ToString("yyyy-MM-dd HH:mm:ss") + "' where id = " + scheduleId, CommandType.Text, null);
                }
            }

            schedule.Dispose();
            schedule = null;
        }
        
        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Dispatcher", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Dispatcher", text);
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
