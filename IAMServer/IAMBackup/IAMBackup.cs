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
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail;

using IAM.Config;
using IAM.Log;
//using IAM.SQLDB;
using IAM.Scheduler;
using IAM.LocalConfig;
using IAM.GlobalDefs;
using SafeTrend.Json;
using SafeTrend.Report;
using SafeTrend.Data.SQLite;
using IAM.CA;

namespace IAM.Backup
{
    public partial class IAMBackup : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer backupTimer;
        Timer statusTimer;
        Boolean executing = false;
        //Boolean executing2 = false;

        public IAMBackup()
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
                        TextLog.Log("Backup", "Falha ao acessar o banco de dados: " + ex.Message);
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

            backupTimer = new Timer(new TimerCallback(BackupTimer), null, 1000, 60000);
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

                db.ServiceStatus("Backup", JSON.Serialize2(new { host = Environment.MachineName, executing = executing }), null);

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


        private void BuildBackup()
        {
            StringBuilder bkpLog = new StringBuilder();

            IAMDatabase db = null;
            try
            {

                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();


                bkpLog.AppendLine("Listando tabelas da base de dados...");

                DataTable dtS = db.Select("select TABLE_NAME from information_schema.tables where TABLE_TYPE = 'BASE TABLE' order by TABLE_NAME");

                if ((dtS == null) || (dtS.Rows.Count == 0))
                {
                    bkpLog.AppendLine("Listagem de tabelas vazia ou nula");
                    throw new Exception("Table list is null or empty");
                }

                bkpLog.AppendLine(dtS.Rows.Count + " tabelas");

                
                FileInfo bkpFile = new FileInfo(Path.Combine(Path.Combine(basePath,"Backup"),"bkp-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".iambkp"));
                if (!bkpFile.Directory.Exists)
                    bkpFile.Directory.Create();

                bkpLog.AppendLine("Criando arquivo de backup: " + bkpFile.FullName);

                using (SqliteBase exportDB = new SqliteBase(bkpFile))
                {
                    foreach (DataRow drSrc in dtS.Rows)
                    {
                        
                        String tableName = drSrc["TABLE_NAME"].ToString();

                        bkpLog.AppendLine("Exportando tabela: " + tableName);
                        Console.WriteLine(tableName);


                        DataTable dtSchema = db.GetSchema(tableName);

                        StringBuilder createCmd = new StringBuilder();

                        createCmd.AppendLine("DROP TABLE IF EXISTS [" + tableName.ToLower() + "];");

                        /*
                    CREATE TABLE [Events] (
                        id INTEGER PRIMARY KEY AUTOINCREMENT, 
                        test_id TEXT NOT NULL, 
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        event_text TEXT NULL
                    );*/
                        List<String> columns = new List<string>();

                        bkpLog.AppendLine("Criando estrutura da tabela");
                        try
                        {


                            foreach (DataColumn dc in dtSchema.Columns)
                            {
                                if (dc.DataType.Equals(typeof(Int32)) || dc.DataType.Equals(typeof(Int64)))
                                {
                                    columns.Add("[" + dc.ColumnName + "] INTEGER NULL");
                                }
                                else if (dc.DataType.Equals(typeof(DateTime)))
                                {
                                    columns.Add("[" + dc.ColumnName + "] datetime NULL");
                                }
                                else
                                {
                                    columns.Add("[" + dc.ColumnName + "] TEXT NULL");
                                }

                            }

                        }
                        catch (Exception ex)
                        {
                            bkpLog.AppendLine("Erro ao listar as colunas da tabela '" + tableName + "': " + ex.Message);
                            TextLog.Log("Backup", "\tErro ao listar as colunas da tabela '" + tableName + "': " + ex.Message);
                            throw ex;
                        }

                        
                        try
                        {

                            createCmd.AppendLine("CREATE TABLE [" + tableName.ToLower() + "] (");

                            createCmd.AppendLine(String.Join(", " + Environment.NewLine, columns));

                            createCmd.AppendLine(");");

                            exportDB.ExecuteNonQuery(createCmd.ToString());
                        }
                        catch (Exception ex)
                        {
                            bkpLog.AppendLine("Erro ao criando tabela '" + tableName + "': " + ex.Message);
                            TextLog.Log("Backup", "\tErro ao criando tabela '" + tableName + "': " + ex.Message);
                            throw ex;
                        }

                        //Copiando dados das tabelas
                        try
                        {
                            bkpLog.AppendLine("Copiando dados");

                            if (tableName.ToLower() == "logs")
                            {

                                DataTable dtSrcData = db.ExecuteDataTable("select l.* from [logs] l with(nolock) inner join [entity_timeline] et with(nolock) on et.log_id = l.id");

                                exportDB.BulkCopy(dtSrcData, tableName.ToLower());

                            }
                            else if (tableName.ToLower() == "entity")
                            {

                                DataTable dtSrcData = db.ExecuteDataTable("select * from [" + tableName + "] with(nolock)");

                                exportDB.BulkCopy(dtSrcData, tableName.ToLower());

                            }
                            else
                            {

                                DataTable dtSrcData = db.ExecuteDataTable("select * from [" + tableName + "] with(nolock)");

                                exportDB.BulkCopy(dtSrcData, tableName.ToLower());

                            }
                        }
                        catch (Exception ex)
                        {
                            bkpLog.AppendLine("Erro copiando dados da tabela '" + tableName + "': " + ex.Message);
                            TextLog.Log("Backup", "\tErro copiando dados da tabela '" + tableName + "': " + ex.Message);
                            //throw ex;
                        }

                    }

                    //No final de todo o processo atualiza as senhas como cleartext
                    try
                    {
                        bkpLog.AppendLine("Atualizando as senhas das entidades");
                        DataTable dtEnt = db.ExecuteDataTable("select id from [enterprise] with(nolock)");

                        foreach (DataRow drEnt in dtEnt.Rows)
                        {

                            using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, (Int64)drEnt["id"]))
                            {
                                DataTable dtSrcData = db.ExecuteDataTable("select e.id, e.password, c.enterprise_id from [entity] e with(nolock) inner join [context] c with(nolock) on e.context_id = c.id where c.enterprise_id = " + drEnt["id"]);

                                //Atualiza senha em clear text de cada usu[ario
                                foreach (DataRow drUser in dtSrcData.Rows)
                                {
                                    try
                                    {
                                        using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(drUser["password"].ToString())))
                                        {
                                            exportDB.ExecuteNonQuery("update entity set password = '" + Encoding.UTF8.GetString(cApi.clearData) + "' where id = " + drUser["id"]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        bkpLog.AppendLine("Erro decriptografando a senha da entidade '" + drUser["id"] + "': " + ex.Message);
                                        TextLog.Log("Backup", "\tErro decriptografando a senha da entidade '" + drUser["id"] + "': " + ex.Message);
                                        //throw ex;
                                    }
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        bkpLog.AppendLine("Erro atualizando as senhas para cleartext: " + ex.Message);
                        TextLog.Log("Backup", "\tErro atualizando as senhas para cleartext: " + ex.Message);
                        //throw ex;
                    }

                }




                db.AddUserLog(LogKey.Backup, DateTime.Now, "Backup", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, "Backup realizado com sucesso", bkpLog.ToString());

            }
            catch (Exception ex)
            {
                TextLog.Log("Backup", "\tError building backup: " + ex.Message);
                bkpLog.AppendLine("Error building backup: " + ex.Message);
                try
                {
                    db.AddUserLog(LogKey.Backup, DateTime.Now, "Backup", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Backup finalizado com erro", bkpLog.ToString());
                }
                catch { }
            }
            finally
            {
                if (bkpLog != null)
                    bkpLog = null;

                if (db != null)
                    db.Dispose();
            }
                        
        }

        static public void sendEmail(IAMDatabase db, String Subject, List<MailAddress> to, String body, Boolean isHTML, List<Attachment> atts)
        {
            sendEmail(db, Subject, to, null, body, isHTML, atts);
        }

        static public void sendEmail(IAMDatabase db, String Subject, List<MailAddress> to, String replyTo, String body, Boolean isHTML, List<Attachment> atts)
        {

            using (ServerDBConfig conf = new ServerDBConfig(db.Connection))
            {

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(conf.GetItem("mailFrom"));
                foreach (MailAddress t in to)
                    mail.To.Add(t);

                mail.Subject = Subject;

                mail.IsBodyHtml = isHTML;
                mail.Body = body;

                if (replyTo != null)
                {
                    try
                    {
                        mail.ReplyTo = new MailAddress(replyTo);
                    }
                    catch { }
                }

                if ((atts != null) && (atts.Count > 0))
                    foreach (Attachment a in atts)
                        mail.Attachments.Add(a);

                /*Non-Encrypted	AUTH		25 (or 587)
                Secure (TLS)	StartTLS	587
                Secure (SSL)	SSL			465*/

                SmtpClient client = new SmtpClient();
                client.Host = conf.GetItem("smtpServer");
                client.Port = 25;
                client.EnableSsl = false;

                try
                {
                    Int32 port = Int32.Parse( conf.GetItem("smtpPort"));
                    switch (port)
                    {
                        case 587:
                            client.EnableSsl = true;
                            break;

                        case 465:
                            client.EnableSsl = true;
                            break;
                    }
                }
                catch { }

                client.Credentials = new System.Net.NetworkCredential(conf.GetItem("username"), conf.GetItem("password"));

                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertificateValidationCallback);

                client.Send(mail);

                client = null;
                mail = null;
            }
        }


        static private bool RemoteCertificateValidationCallback(Object sender,
                                                               X509Certificate certificate,
                                                               X509Chain chain,
                                                               SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        private void BackupTimer(Object state)
        {

            if (executing)
                return;

            executing = true;

            //TextLog.Log("Backup", "Starting report timer");
            try
            {
                //IAMDeploy deploy = new IAMDeploy("Backup", localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                //deploy.DeployAll();

                IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();

                DataTable dtS = db.Select("select * from backup_schedule");

                try
                {

                    //Processa um a um dos agendamentos
                    foreach (DataRow dr in dtS.Rows)
                        CheckSchedule(db, (Int64)dr["id"], dr["schedule"].ToString(), (DateTime)dr["next"]);

                }
                catch (Exception ex)
                {
                    TextLog.Log("Backup", "\tError on report timer schedule: " + ex.Message);
                    db.AddUserLog(LogKey.Backup, null, "Backup", UserLogLevel.Error, 0, 0, 0, 0, 0, 0, 0, "Error on report scheduler", ex.Message);
                }

                db.closeDB();
            }
            catch (Exception ex1)
            {
                TextLog.Log("Backup", "\tError on report timer: " + ex1.Message);
                
            }
            finally
            {
                //TextLog.Log("Backup", "\tScheduled for new report process in 60 seconds");
                //TextLog.Log("Backup", "Finishing report timer");
                executing = false;
            }
        }

        private void CheckSchedule(IAMDatabase db, Int64 scheduleId, String jSonSchedule, DateTime next)
        {

            DateTime date = DateTime.Now;
            TimeSpan ts = date - new DateTime(1970, 01, 01);

            Schedule schedule = new Schedule();
            try
            {
                schedule.FromJsonString(jSonSchedule);
            }
            catch
            {
                schedule = null;
            }

            if (schedule == null)
                return;

            //Check Start date

            TimeSpan stDateTs = next - new DateTime(1970, 01, 01);
            if (ts.TotalSeconds >= stDateTs.TotalSeconds) //Data e hora atual maior ou igual a data que se deve iniciar
            {
                TextLog.Log("Backup", "Starting execution");

                try
                {
                    BuildBackup();
                }
                catch (Exception ex)
                {
                    TextLog.Log("Backup", "Error on execution " + ex.Message);
                }
                finally
                {
                    TextLog.Log("Backup", "Execution completed");

                    //Agenda a próxima execução
                    DateTime calcNext = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, schedule.TriggerTime.Hour, schedule.TriggerTime.Minute, 0);
                    DateTime nextExecute = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
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


                    db.ExecuteNonQuery("update backup_schedule set [next] = '" + nextExecute.ToString("yyyy-MM-dd HH:mm:ss") + "' where id = " + scheduleId, CommandType.Text, null);
                }
            }
        }



        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("Backup", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("Backup", text);
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
