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
using IAM.Log;
using IAM.CA;
using SafeTrend.Data;
using IAM.LocalConfig;
using IAM.GlobalDefs;
using IAM.GlobalDefs.Messages;
using SafeTrend.Json;


namespace IAM.Messenger
{
    public partial class IAMMessenger : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer messengerTimer;
        Timer statusTimer;
        Boolean executing = false;
        private String last_status = "";
        private DateTime startTime = new DateTime(1970, 1, 1);

        public IAMMessenger()
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
                        TextLog.Log("Messenger", "Falha ao acessar o banco de dados: " + ex.Message);
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
#if DEBUG
            try
            {
                using (IAM.GlobalDefs.Update.IAMDbUpdate updt = new GlobalDefs.Update.IAMDbUpdate(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
                    updt.Update();
            }
            catch (Exception ex)
            {
                StopOnError("Falha ao atualizar o banco de dados", ex);
            }
#endif

            /*************
             * Inicia timer que processa as mensagens
             */

            messengerTimer = new Timer(new TimerCallback(MessengerTimer), null, 1000, 60000);
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

                db.ServiceStatus("Messenger", JSON.Serialize2(new { host = Environment.MachineName, executing = executing, start_time = startTime.ToString("o"), last_status = last_status }), null);

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


        private void MessengerTimer(Object state)
        {

            if (executing)
                return;

            executing = true;

            startTime = DateTime.Now;

            try
            {
                
                IAMDatabase db = null;
                try
                {

                    db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();
                    db.Timeout = 900;

                    //Lista todas as mensagens pendêntes de entrega
                    //Status: W = Waiting, PE = Protocol error
                    //DataTable dtMessages = db.ExecuteDataTable("select m.*, e.last_uri from [st_messages] m with(nolock) inner join enterprise e with(nolock) on e.id = m.enterprise_id where status in ('W','PE')");
                    DataTable dtMessages = db.ExecuteDataTable("select m.id from [st_messages] m with(nolock) where status in ('W','PE')");
                    if ((dtMessages != null) && (dtMessages.Rows.Count > 0))
                    {
                        try
                        {
                            TextLog.Log("Messenger", "Starting message timer");

                            foreach (DataRow dr in dtMessages.Rows)
                            {
                                try
                                {
                                    using (MessageSender sender = new MessageSender(db, (Int64)dr["id"]))
                                    using (ServerDBConfig conf = new ServerDBConfig(db.Connection, true))
                                        sender.Send(conf.GetItem("mailFrom"), conf.GetItem("smtpServer"), conf.GetItem("username"), conf.GetItem("password"));
                                }
                                catch (Exception ex)
                                {
                                    DbParameterCollection par = new DbParameterCollection();
                                    par.Add("@message_id", typeof(Int64)).Value = (Int64)dr["id"];
                                    par.Add("@status", typeof(String)).Value = "Erro no envio";
                                    par.Add("@description", typeof(String)).Value = ex.Message;

                                    db.ExecuteNonQuery("UPDATE st_messages SET [status] = 'E' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);
                                }
                            }
                        }
                        finally
                        {
                            if (db != null)
                                db.Dispose();

                            TextLog.Log("Messenger", "Finishing message timer");
                        }
                    }

                    db.closeDB();
                }
                finally
                {
                    if (db != null)
                        db.Dispose();
                }

            }
            catch (Exception ex)
            {
                TextLog.Log("Messenger", "Error on message timer " + ex.Message);
            }
            finally
            {
                executing = false;
                last_status = "";
                startTime = new DateTime(1970, 1, 1);
            }
            
        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Messenger", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Messenger", text);
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
