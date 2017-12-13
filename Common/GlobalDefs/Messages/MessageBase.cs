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
    [Serializable()]
    public abstract class MessageBase : IDisposable
    {
        internal Int64 enterpriseId;
        internal String mailBody;
        internal String mailSubject;
        internal List<MailAddress> mailTo;
        internal Boolean isHtml;
        internal Uri serverUri;
        internal Dictionary<String, String> variables;

        public Int64 EnterpriseId { get { return enterpriseId; } }
        public String MailBody { get { return mailBody; } }
        public String MailSubject { get { return mailSubject; } }
        public List<MailAddress> MailTo { get { return mailTo; } }
        public Boolean IsHtml { get { return isHtml; } }
        
        public MessageBase() {
            mailTo = new List<MailAddress>();
        }

        public MessageBase(Int64 enterpriseId, Boolean isHtml, String subject, String body, String recipients)
        {
            this.enterpriseId = enterpriseId;
            this.isHtml = isHtml;
            this.mailSubject = subject;
            this.mailBody = body;

            mailTo = new List<MailAddress>();
            try
            {
                foreach (String m in recipients.Split(",".ToCharArray()))
                    mailTo.Add(new MailAddress(m));
            }
            catch(Exception ex) {
                throw new Exception("Erro parsing recipient", ex);
            }

        }

        public MessageBase(Int64 enterpriseId, Boolean isHtml, String subject, String body, List<MailAddress> recipients)
        {
            this.enterpriseId = enterpriseId;
            this.isHtml = isHtml;
            this.mailSubject = subject;
            this.mailBody = body;
            this.mailTo = recipients;
        }

        public void Dispose()
        {
            this.mailBody = null;
            this.mailSubject = null;

            if (this.variables != null)
                this.variables.Clear();

            this.variables = null;
        }

    }
}
