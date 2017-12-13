using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;

using IAM.Config;
using IAM.Log;
using IAM.SQLDB;
using IAM.LocalConfig;
using IAM.Report;


namespace IAMReportGenerator
{
    class Program
    {
        private static ServerLocalConfig localConfig;

        static void Main(string[] args)
        {


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
                    MSSQLDB db = new MSSQLDB(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();

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
            MSSQLDB db2 = new MSSQLDB(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
            db2.openDB();


            DataTable created = db2.Select("select * from vw_entity_mails where create_date between CONVERT(datetime, convert(varchar(10),DATEADD(DAY, -1, GETDATE()),120) + ' 00:00:00', 120) and CONVERT(datetime, convert(varchar(10),getdate(),120) + ' 23:59:59', 120) order by context_name, full_name");
            DataTable all = db2.Select("select * from vw_entity_mails order by context_name, full_name");
            Dictionary<String, String> title = new Dictionary<string, string>();
            title.Add("context_name", "Contexto");
            title.Add("full_name", "Nome completo");
            title.Add("login", "Login");
            title.Add("create_date", "Data de criação");
            title.Add("last_login", "Ultimo login");
            title.Add("mail", "E-mail");
            title.Add("locked", "Bloqueado");

            ReportBase rep1 = new ReportBase(created, title);
            ReportBase rep2 = new ReportBase(all, title);

            List<Attachment> atts = new List<Attachment>();

            using (MemoryStream ms1 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetTXT())))
            using (MemoryStream ms2 = new MemoryStream(Encoding.UTF8.GetBytes(rep1.GetXML("Usuários", ""))))
            using (MemoryStream ms3 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetTXT())))
            using (MemoryStream ms4 = new MemoryStream(Encoding.UTF8.GetBytes(rep2.GetXML("Usuários", ""))))
            {
                atts.Add(new Attachment(ms1, "created.txt"));
                atts.Add(new Attachment(ms2, "created.xls"));
                atts.Add(new Attachment(ms3, "all.txt"));
                atts.Add(new Attachment(ms4, "all.xls"));

                List<String> to = new List<string>();
                to.Add("helvio.junior@fael.edu.br");
                to.Add("camile.mattar@fael.edu.br");
                to.Add("cleidy.santos@fael.edu.br");

                sendEmail(db2, "Listagem de usuários em " + DateTime.Now.ToString("dd/MM/yyyy"), to, created.Rows.Count + " usuários criados de " + DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy") + " até " + DateTime.Now.ToString("dd/MM/yyyy"), false, atts);
            }
            db2.closeDB();


        }


        static public void sendEmail(MSSQLDB db, String Subject, List<String> to, String body, Boolean isHTML, List<Attachment> atts)
        {
            sendEmail(db, Subject, to, null, body, isHTML, atts);
        }

        static public void sendEmail(MSSQLDB db, String Subject, List<String> to, String replyTo, String body, Boolean isHTML, List<Attachment> atts)
        {

            using (ServerDBConfig conf = new ServerDBConfig(db.conn))
            {

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(conf.GetItem("mailFrom"));
                foreach(String t in to)
                    mail.To.Add(new MailAddress(t));

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

                SmtpClient client = new SmtpClient();
                client.Host = conf.GetItem("smtpServer");
                client.Port = 587;
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(conf.GetItem("username"),
                    conf.GetItem("password"));

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

        private static void StopOnError(String text, Exception ex)
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
    }
}
