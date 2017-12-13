using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using IAM.CodeManager;

namespace cMailSender
{
    public class MailSender : CodeManagerPluginBase
    {

        public override String GetPluginName() { return "Mail Sender plugin"; }
        public override String GetPluginDescription() { return "Plugin para enviar código de recuperação através de E-mail"; }
        public override String GetPluginPrefix() { return "E-mail"; }

        public override Uri GetPluginId()
        {
            return new Uri("codesender://iam/codeplugins/MailSender");
        }

        public MailSender()
        {
        }

        public override Boolean ValidateConfigFields(Dictionary<String, Object> config)
        {
            if (!CheckConfig(config))
                return false;

            //Verifica as informações próprias deste plugin
            return true;
        }

        public override CodePluginConfigFields[] GetConfigFields()
        {

            List<CodePluginConfigFields> conf = new List<CodePluginConfigFields>();
            conf.Add(new CodePluginConfigFields("Servidor de e-mail", "mail_server", "IP ou nome do servidor de e-mail", CodePluginConfigTypes.String, true, @""));
            conf.Add(new CodePluginConfigFields("De", "mail_from", "E-mail de origem", CodePluginConfigTypes.String, true, @""));
            conf.Add(new CodePluginConfigFields("Usuário", "username", "Usuário para autenticação de envio", CodePluginConfigTypes.String, false, @""));
            conf.Add(new CodePluginConfigFields("Senha", "password", "Senha para autenticação", CodePluginConfigTypes.Password, false, @""));
            conf.Add(new CodePluginConfigFields("Usar SSL", "use_ssl", "Utilizar conexão segura com SSL/TLS", CodePluginConfigTypes.Boolean, false, "true"));
            conf.Add(new CodePluginConfigFields("Porta de cnexão", "port", "Porta de conexão", CodePluginConfigTypes.Int32, false, "25"));

            return conf.ToArray();
        }

        public override List<CodeData> ParseData(List<String> inputData)
        {
            List<CodeData> list = new List<CodeData>();

            foreach (String s in inputData)
                try
                {
                    MailAddress mailData = new MailAddress(s.ToLower());
                    String clearData = mailData.Address.ToString().ToLower();
                    String maskedData = "";

                    Int32 start = 3;
                    if (mailData.User.Length < 3)
                        start = 1;

                    for (Int32 p = 0; p < mailData.User.Length; p++)
                        if (p >= start)
                            maskedData += "*";
                        else
                            maskedData += mailData.User[p].ToString();

                    maskedData += "@" + mailData.Host;

                    list.Add(new CodeData(this.GetPluginPrefix(), clearData, maskedData));
                }
                catch { }

            return list;
        }

        public override Boolean iSendCode(Dictionary<String, Object> config, CodeData target, String code)
        {

            try
            {

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(config["mail_from"].ToString());
                mail.To.Add(new MailAddress(target.ClearData));

                mail.Subject = "Password recover code";

                mail.IsBodyHtml = false;
                mail.Body = "Code: " + code;

                SmtpClient client = new SmtpClient();
                client.Host = config["mail_server"].ToString();
                client.Port = ((config.ContainsKey("port") && config["port"].ToString() != "") ? Int32.Parse(config["port"].ToString()) : 25);
                client.EnableSsl = (config.ContainsKey("use_ssl") && (Boolean)config["use_ssl"] == true);
                if (config.ContainsKey("username") && config.ContainsKey("password"))
                    client.Credentials = new System.Net.NetworkCredential(config["username"].ToString(), config["password"].ToString());

                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; });

                client.Send(mail);

                client = null;
                mail = null;

                return true;
            }
            catch(Exception ex) {
                Log(CodePluginLogType.Error, "Error sending e-mail: " + ex.Message);
                return false;
            }
        }

    }
}
