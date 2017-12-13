using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SafeTrend.Data;
using IAM.GlobalDefs;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SafeTrend.Json;
using System.Data;

namespace IAM.GlobalDefs.Messages
{
    

    public class MessageSender : MessageBase, IDisposable
    {
        internal Int64 messageId;
        internal String messageKey;
        internal DbBase database;

        public MessageSender(DbBase database, Int64 messageId)
            : base()
        {
            this.messageId = messageId;
            this.database = database;

            //Busca no banco a mensage

            DataTable dtMessage = database.ExecuteDataTable("select * from st_messages where id = " + messageId);
            if ((dtMessage == null) || (dtMessage.Rows.Count == 0))
                throw new Exception("Message not found");

            this.enterpriseId = (Int64)dtMessage.Rows[0]["enterprise_id"];
            this.isHtml = (Boolean)dtMessage.Rows[0]["html"];
            this.mailSubject = dtMessage.Rows[0]["subject"].ToString();
            this.mailBody = dtMessage.Rows[0]["body"].ToString();
            this.messageKey = dtMessage.Rows[0]["key"].ToString();

            this.mailTo = new List<MailAddress>();
            try
            {
                foreach (String m in dtMessage.Rows[0]["to"].ToString().Split(",".ToCharArray()))
                    mailTo.Add(new MailAddress(m));
            }
            catch (Exception ex)
            {
                throw new Exception("Erro parsing recipient", ex);
            }

        }

        public Boolean Send(String mailFom, String smtpServer)
        {
            return Send(mailFom, smtpServer, null, null);
        }

        public Boolean Send(String mailFom, String smtpServer, String username, String password)
        {

            try
            {
                foreach (MailAddress mail in this.mailTo)
                    sendEmail(this.mailSubject, mail.Address, null, this.mailBody, this.isHtml, mailFom, smtpServer, username, password);

                DbParameterCollection par = new DbParameterCollection();
                par.Add("@message_id", typeof(Int64)).Value = this.messageId;
                par.Add("@status", typeof(String)).Value = "Email enviado com sucesso";
                par.Add("@description", typeof(String)).Value = "";

                database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'OK' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);

                return true;
            }
            catch (Exception ex)
            {
                DbParameterCollection par = new DbParameterCollection();
                par.Add("@message_id", typeof(Int64)).Value = this.messageId;
                par.Add("@status", typeof(String)).Value = "Erro no tratamento das variáveis";
                par.Add("@description", typeof(String)).Value = ex.Message;

                database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'PE' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);
                return false;
            }

        }

        private void sendEmail(String Subject, String to, String replyTo, String body, Boolean isHTML, String mailFom, String smtpServer, String username, String password)
        {


            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(mailFom);
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
            client.Host = smtpServer;
            client.Port = 587;
            client.EnableSsl = true;
            if (username != null && password != null)
                client.Credentials = new System.Net.NetworkCredential(username, password);

            System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; });

            client.Send(mail);

            client = null;
            mail = null;

        }

        public void Dispose()
        {
            base.Dispose();

            this.messageKey = null;
        }

    }
}
