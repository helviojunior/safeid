using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

using IAM.CodeManager;
using libphonenumber;


namespace cSIPCall
{
    public class SIPCall : CodeManagerPluginBase
    {

        public override String GetPluginName() { return "SIP CALL plugin"; }
        public override String GetPluginDescription() { return "Plugin para efetuar ligação SIP e narrar o código"; }
        public override String GetPluginPrefix() { return "Ligação"; }

        public override Uri GetPluginId()
        {
            return new Uri("codesender://iam/codeplugins/SIPCall");
        }

        public SIPCall()
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
            conf.Add(new CodePluginConfigFields("Servidor SIP", "host", "Host/IP do servidor SIP", CodePluginConfigTypes.String, true, @""));
            conf.Add(new CodePluginConfigFields("Usuário", "username", "Usuário SIP", CodePluginConfigTypes.String, true, @""));
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

                        if (number.IsValidNumber)
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


                List<String> cmds = new List<string>();
                //cmds.Add("wait(1)");
                cmds.Add("playback(audio\\seu_codigo)");
                cmds.Add("wait(1)");

                Int32 cnt = 0;
                foreach (Char c in code)
                {
                    if (cnt == 3)
                    {
                        cmds.Add("wait(1)");
                        cnt = 0;
                    }
                    cnt++;

                    cmds.Add("playback(audio\\"+ c.ToString().ToLower() +")");
                }

                cmds.Add("playback(audio\\repetindo)");

                cnt = 0;
                foreach (Char c in code)
                {
                    if (cnt == 3)
                    {
                        cmds.Add("wait(1)");
                        cnt = 0;
                    }
                    cnt++;

                    cmds.Add("playback(audio\\" + c.ToString().ToLower() + ")");
                }

                cmds.Add("hangup()");



                DirectoryInfo tempFile = new DirectoryInfo(Path.Combine(Path.GetTempPath(),Path.GetRandomFileName() + ".xml"));

                XmlDocument xmlDoc = new XmlDocument();
                XmlNode rootNode = xmlDoc.CreateElement("call");
                xmlDoc.AppendChild(rootNode);

                XmlNode configNode = xmlDoc.CreateElement("config");
                
                if(config.Keys.Contains("target"))
                    config.Remove("target");

                config.Add("target", target.ClearData);

                foreach (String key in config.Keys)
                {
                    XmlNode c1 = xmlDoc.CreateElement("item");

                    XmlNode k = xmlDoc.CreateElement("key");
                    k.InnerText = key;

                    XmlNode v = xmlDoc.CreateElement("value");
                    v.InnerText = config[key].ToString();

                    c1.AppendChild(k);
                    c1.AppendChild(v);
                    configNode.AppendChild(c1);
                }

                rootNode.AppendChild(configNode);

                XmlNode cmdNode = xmlDoc.CreateElement("commands");

                foreach (String c in cmds)
                {
                    XmlNode cmd = xmlDoc.CreateElement("cmd");
                    cmd.InnerText = c;
                    cmdNode.AppendChild(cmd);
                }
                rootNode.AppendChild(cmdNode);

                xmlDoc.Save(tempFile.FullName);
                xmlDoc = null;

#if DEBUG
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                File.Copy(tempFile.FullName, Path.Combine(Path.GetDirectoryName(asm.Location), "config.xml"));
#endif

                StartCall(tempFile.FullName);

                return true;
            }
            catch (Exception ex)
            {
                Log(CodePluginLogType.Error, "Error calling: " + ex.Message);
                return false;
            }
        }


        private void StartCall(String configFile)
        {

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            
            ProcessStartInfo psi = new ProcessStartInfo();
            //psi.Verb = "runas";
            psi.Arguments = configFile;
            psi.FileName = Path.Combine(Path.GetDirectoryName(asm.Location), "SIPCall2.exe");
            
            Process p = new Process();
            p.StartInfo = psi;
            p.Start();

        }


    }
}
