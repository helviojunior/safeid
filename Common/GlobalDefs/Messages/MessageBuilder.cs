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
using HtmlAgilityPack;

namespace IAM.GlobalDefs.Messages
{
    public class MessageBuilder : MessageBase, IDisposable
    {
        internal Uri serverUri;
        internal Dictionary<String, String> variables;

        public Uri ServerUri { get { return serverUri; } }
        public Dictionary<String, String> Variables { get { return variables; } }


        public MessageBuilder(Int64 enterpriseId, Boolean isHtml, String subject, String body, String recipients, Uri serverUri)
            : base(enterpriseId, isHtml, subject, body, recipients)
        {
            this.serverUri = serverUri;
            this.variables = null;
        }

        public MessageBuilder(Int64 enterpriseId, Boolean isHtml, String subject, String body, String recipients, Uri serverUri, Dictionary<String, String> variables)
            : base(enterpriseId, isHtml, subject, body, recipients)
        {
            this.serverUri = serverUri;
            this.variables = variables;
        }

        public static MessageBuilder BuildFromTemplate(DbBase database, Int64 enterpriseId, String templateKey, String recipients, Dictionary<String, String> variable, Object transaction)
        {

            using (DbParameterCollection par = new DbParameterCollection())
            {
                par.Add("@enterprise_id", typeof(Int64)).Value = enterpriseId;
                par.Add("@message_key", typeof(String)).Value = templateKey;

                DataTable dtTemplate = database.ExecuteDataTable("sp_st_get_message_template", CommandType.StoredProcedure, par, transaction);
                if ((dtTemplate == null) || (dtTemplate.Rows.Count == 0))
                    throw new Exception("Message template not found");

                return new MessageBuilder(enterpriseId, (Boolean)dtTemplate.Rows[0]["html"], dtTemplate.Rows[0]["subject"].ToString(), dtTemplate.Rows[0]["body"].ToString(), recipients, new Uri(dtTemplate.Rows[0]["last_uri"].ToString()), variable);
                //database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'E' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);
            }
        }

        public void SaveToDb(DbBase database, Object transaction)
        {
            //Salva no banco, resgata o message key e depois atualiza o body no db

            Dictionary<String, String> vars = new Dictionary<String, String>();
            try
            {

                MatchCollection ms = Regex.Matches(this.mailBody, @"%(.*?)%", RegexOptions.IgnoreCase);
                foreach (Match m in ms)
                    if (!vars.ContainsKey(m.Groups[1].Value.ToLower()))
                        vars.Add(m.Groups[1].Value.ToLower(), "");

                ms = Regex.Matches(this.mailSubject, @"%(.*?)%", RegexOptions.IgnoreCase);
                foreach (Match m in ms)
                    if (!vars.ContainsKey(m.Groups[1].Value.ToLower()))
                        vars.Add(m.Groups[1].Value.ToLower(), "");

                if (this.variables != null)
                {
                    List<String> ks = new List<string>();
                    ks.AddRange(vars.Keys);

                    foreach (String k in ks)
                        foreach (String k1 in this.variables.Keys)
                            if (k1.ToLower() == k)
                            {
                                vars[k] = this.variables[k1];
                                break;
                            }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Erro on build enviroment variables", ex);
            }

            foreach (MailAddress mail in this.mailTo)
            {

                String newBody = this.mailBody;
                String newSubject = this.mailSubject;
                try
                {
                    try
                    {

                        //Imagem para marcação de leitura da mensagem
                        if (this.isHtml) //A tag |message_key| será substituida automaticamente na procedure pela key da mensagem
                            newBody += "<img src=\"%enterprise_uri%/m/v/|message_key|\" width=\"1\" heigh=\"1\" />";

                        if (!vars.ContainsKey("enterprise_uri"))
                            vars.Add("enterprise_uri", "");

                        vars["enterprise_uri"] = this.serverUri.Scheme + "://" + serverUri.Host + (serverUri.IsDefaultPort ? "" : ":" + serverUri.Port);

                        if (vars.ContainsKey("mail"))
                            vars["mail"] = mail.Address;

                        foreach (String k in vars.Keys)
                        {
                            newBody = Regex.Replace(newBody, "%" + k + "%", vars[k], RegexOptions.IgnoreCase);
                            newSubject = Regex.Replace(newSubject, "%" + k + "%", vars[k], RegexOptions.IgnoreCase);
                        }

                        
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erro on build enviroment variables", ex);
                    }


                    using (DbParameterCollection par = new DbParameterCollection())
                    {
                        par.Add("@enterprise_id", typeof(Int64)).Value = this.enterpriseId;
                        par.Add("@send_to", typeof(String)).Value = mail.Address;
                        par.Add("@is_html", typeof(Boolean)).Value = this.isHtml;
                        par.Add("@subject", typeof(String)).Value = newSubject;
                        par.Add("@body", typeof(String)).Value = newBody;

                        DataTable dtMessage = database.ExecuteDataTable("sp_st_new_message", CommandType.StoredProcedure, par, transaction);
                        if ((dtMessage != null) && (dtMessage.Rows.Count > 0))
                        {

                            try
                            {
                                newBody = dtMessage.Rows[0]["body"].ToString();//Pega o body atualizado pois há tags nele que a procedure atualiza

                                HtmlDocument doc = new HtmlDocument();
                                doc.LoadHtml(newBody);
            

                                Boolean renew = false;
                                //Substitui os links

                                HtmlNodeCollection aLinks = doc.DocumentNode.SelectNodes("//a[@href]");
                                if (aLinks != null)
                                    foreach (HtmlNode link in aLinks)
                                    {
                                        HtmlAttribute att = link.Attributes["href"];
                                        using (DbParameterCollection par2 = new DbParameterCollection())
                                        {
                                            par2.Add("@message_id", typeof(Int64)).Value = dtMessage.Rows[0]["id"];
                                            par2.Add("@link", typeof(String)).Value = att.Value;

                                            String linkKey = database.ExecuteScalar<String>("sp_st_new_message_link", CommandType.StoredProcedure, par2, transaction);

                                            newBody = newBody.Replace(att.Value, this.serverUri.Scheme + "://" + serverUri.Host + (serverUri.IsDefaultPort ? "" : ":" + serverUri.Port) + "/m/l/" + linkKey);
                                            renew = true;
                                            //
                                        }
                                    }


                                //Se houver links atualiza o body
                                if (renew)
                                    using (DbParameterCollection par2 = new DbParameterCollection())
                                        {
                                            par2.Add("@message_id", typeof(Int64)).Value = dtMessage.Rows[0]["id"];
                                            par2.Add("@body", typeof(String)).Value = newBody;

                                            database.ExecuteNonQuery("update [st_messages] set body = @body where id = @message_id", CommandType.Text, par2, transaction);
                                    }
                            }
                            catch { }

                        }
                        
                        //database.ExecuteNonQuery("UPDATE st_messages SET [status] = 'E' WHERE id = @message_id; INSERT INTO st_messages_status (message_id,date,error,status,description) VALUES(@message_id,getdate(),1,@status,@description);", par);
                    }

                }
                finally
                {
                    newBody = null;
                    newSubject = null;
                }
            }
        }


        public void Dispose()
        {
            base.Dispose();

            this.serverUri = null;
            this.variables = null;
        }

    }
}
