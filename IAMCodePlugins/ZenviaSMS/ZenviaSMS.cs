using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using IAM.CodeManager;
using libphonenumber;
using HumanAPIClient.Enum;
using HumanAPIClient.Model;
using HumanAPIClient.Service;

namespace cZenviaSMS
{
    public class ZenviaSMS : CodeManagerPluginBase
    {

        public override String GetPluginName() { return "SMS Sender plugin by Zenvia"; }
        public override String GetPluginDescription() { return "Plugin para enviar código de recuperação através de SMS utilizando Zenvia"; }
        public override String GetPluginPrefix() { return "SMS"; }

        public override Uri GetPluginId()
        {
            return new Uri("codesender://iam/codeplugins/ZenviaSMS");
        }

        public ZenviaSMS()
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
            conf.Add(new CodePluginConfigFields("Conta Zenvia", "account", "ID da conta de envio Zenvia", CodePluginConfigTypes.String, true, @""));
            conf.Add(new CodePluginConfigFields("Senha", "password", "Senha para autenticação", CodePluginConfigTypes.Password, true, @""));

            return conf.ToArray();
        }

        public override List<CodeData> ParseData(List<String> inputData)
        {
            List<CodeData> list = new List<CodeData>();

            foreach (String s in inputData)
                try
                {
                    foreach (String s1 in s.Split("(".ToCharArray()))
                    {
                        if (String.IsNullOrEmpty(s1))
                            continue;

                        String phone = s1.ToLower();
                        Regex regexObj = new Regex(@"[^\d]");
                        phone = regexObj.Replace(phone, "");
                        phone = phone.Trim();

                        PhoneNumber number = PhoneNumberUtil.Instance.Parse(phone, "BR");

                        if ((number.IsValidNumber) && (number.NumberType == PhoneNumberUtil.PhoneNumberType.MOBILE))
                        {
                            String clearData = number.FormatInOriginalFormat("BR");

                            if (list.Exists(p => (p.ClearData == clearData)))
                                continue;

                            //(xx) xxxx-xxxx
                            String maskedData = "";

                            Int32 start = clearData.IndexOf(" ") + 2;
                            Int32 end = clearData.IndexOf("-", start);

                            for (Int32 p = 0; p < clearData.Length; p++)
                                if ((p >= start) && (p < end))
                                    maskedData += "*";
                                else
                                    maskedData += clearData[p].ToString();

                            list.Add(new CodeData(this.GetPluginPrefix(), number.CountryCode.ToString() + number.NationalNumber.Value.ToString(), maskedData));
                        }

                    }
                }
                catch { }

            return list;
        }

        public override Boolean iSendCode(Dictionary<String, Object> config, CodeData target, String code)
        {

            try
            {

                SimpleSending sms = new SimpleSending(config["account"].ToString(), config["password"].ToString());

                SimpleMessage message = new SimpleMessage();

                message.To = target.ClearData;
                message.Message = "Use " + code + " como codigo de recuperacao da sua conta";
                message.Id = DateTime.Now.ToString("HHmmss");

                // Whit Proxy 
                // sms.Proxy = new WebProxy("host_proxy", 3128);

                List<String> response = sms.send(message);

                //000 - Message Sent
                foreach (String resp in response)
                    if (resp.ToLower().IndexOf("message sent") != -1)
                        return true;

                return false;
            }
            catch (Exception ex)
            {
                Log(CodePluginLogType.Error, "Error sending SMS: " + ex.Message);
                return false;
            }
        }

    }
}
