using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SafeTrend.Data;
using IAM.GlobalDefs;
using System.IO;
using IAM.Config;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SafeTrend.Json;

namespace IAM.Messenger
{
    /*
    public class IAMMailMessage : IDisposable
    {
        private IAMDatabase database;
        private Int64 messageId;
        private String mailBody;
        private String mailSubject;
        private String mailTo;
        private Boolean isHtml;
        private Uri serverUri;
        private Dictionary<String, String> variables;

        public IAMMailMessage(IAMDatabase database, Int64 messageId, String messageKey, Boolean isHtml, String subject, String body, String variables, String to, Uri serverUri)
        {
            this.database = database;
            this.messageId = messageId;
            this.isHtml = isHtml;
            this.mailSubject = subject;
            this.mailBody = body;
            this.mailTo = to;
            this.serverUri = serverUri;

            //Imagem para marcação de leitura da mensagem
            if (this.isHtml)
                this.mailBody += "<img src=\"%enterprise_uri%/m/v/" + messageKey + "\" width=\"1\" heigh=\"1\" />";

            try
            {
                this.variables = JSON.Deserialize<Dictionary<String, String>>(variables);
            }
            catch { }
        }

        public Boolean Send()
        {
            String newBody = this.mailBody;
            String newSubject = this.mailSubject;

            try
            {
                try
                {
                    Dictionary<String, String> vars = new Dictionary<String, String>();

                    MatchCollection ms = Regex.Matches(this.mailBody, @"%(.*?)%", RegexOptions.IgnoreCase);
                    foreach (Match m in ms)
                        if (!vars.ContainsKey(m.Groups[1].Value.ToLower()))
                            vars.Add(m.Groups[1].Value.ToLower(), "");

                    ms = Regex.Matches(this.mailSubject, @"%(.*?)%", RegexOptions.IgnoreCase);
                    foreach (Match m in ms)
                        if (!vars.ContainsKey(m.Groups[1].Value.ToLower()))
                            vars.Add(m.Groups[1].Value.ToLower(), "");

                    if (vars.ContainsKey("enterprise_uri"))
                        vars["enterprise_uri"] = this.serverUri.Scheme + "://" + serverUri.Host + (serverUri.IsDefaultPort ? "" : ":" + serverUri.Port);

                    if (this.variables != null)
                        foreach (String k in vars.Keys)
                            foreach (String k1 in this.variables.Keys)
                                if (k1.ToLower() == k)
                                {
                                    vars[k] = this.variables[k1];
                                    break;
                                }

                    foreach (String k in vars.Keys){
                        newBody = Regex.Replace(newBody, "%" + k + "%", vars[k], RegexOptions.IgnoreCase);
                        newSubject = Regex.Replace(newSubject, "%" + k + "%", vars[k], RegexOptions.IgnoreCase);
                    }

                }
                catch (Exception ex)
                {
                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@message_id", typeof(Int64)).Value = this.messageId;
                    par.Add("@status", typeof(String)).Value = "Erro no tratamento das variáveis";
                    par.Add("@description", typeof(String)).Value = ex.Message;

                    database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'E' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);
                    return false;
                }

                try
                {
                    sendEmail(newSubject, this.mailTo, newBody, this.isHtml);

                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@message_id", typeof(Int64)).Value = this.messageId;
                    par.Add("@status", typeof(String)).Value = "Email enviado com sucesso";
                    par.Add("@description", typeof(String)).Value = "";

                    database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'OK' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);

                    return true;
                }
                catch(Exception ex) {
                    DbParameterCollection par = new DbParameterCollection();
                    par.Add("@message_id", typeof(Int64)).Value = this.messageId;
                    par.Add("@status", typeof(String)).Value = "Erro no tratamento das variáveis";
                    par.Add("@description", typeof(String)).Value = ex.Message;

                    database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'PE' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);
                    return false;
                }

            }
            finally
            {
                newBody = null;
                newSubject = null;
            }

            return false;
        }

        private void sendEmail(String Subject, String to, String body, Boolean isHTML)
        {
            sendEmail(Subject, to, null, body, isHTML);
        }

        private void sendEmail(String Subject, String to, String replyTo, String body, Boolean isHTML)
        {

            using (ServerDBConfig conf = new ServerDBConfig(database.Connection, true))
            {

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(conf.GetItem("mailFrom"));
                mail.To.Add(new MailAddress(to));

                mail.Subject = Subject;

                mail.IsBodyHtml = isHTML;
                mail.Body = body;

                if (replyTo != null)
                {
                    try
                    {
                        mail.ReplyToList.Add(new MailAddress(replyTo));
                    }
                    catch { }
                }

                SmtpClient client = new SmtpClient();
                client.Host = conf.GetItem("smtpServer");
                client.Port = 587;
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(conf.GetItem("username"),
                    conf.GetItem("password"));

                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; });

                client.Send(mail);

                client = null;
                mail = null;

            }
        }

        public void Dispose()
        {
            this.mailBody = null;
            this.mailSubject = null;

            if (this.variables != null)
                this.variables.Clear();

            this.variables = null;
        }

    }*/
}
