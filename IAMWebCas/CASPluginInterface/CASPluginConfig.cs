using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace CAS.PluginInterface
{
    [Serializable()]
    public class CASPluginConfig : CASJsonBase
    {
        public String Context { get; set; }
        public Uri Service { get; set; }
        public String PluginAssembly { get; set; }
        public Boolean PermitPasswordRecover { get; set; }
        public Boolean ExternalPasswordRecover { get; set; }
        public Uri PasswordRecoverUri { get; set; }
        public Boolean PermitChangePassword { get; set; }
        public String Admin { get; set; }
        public Dictionary<String, Object> Attributes { get; set; }

        public CASPluginConfig()
        {
            this.Attributes = new Dictionary<string, object>();
            this.Context = "Default";
        }

        public void SaveToXML(FileInfo file)
        {
            StringBuilder xml = new StringBuilder();
            xml.AppendLine("<cas:serviceConfig xmlns:cas=\"http://www.safetrend.com.br/tp/cas\">");
            xml.AppendLine(" <cas:context>" + Context + "</cas:context>");
            xml.AppendLine(" <cas:service>" + Service.AbsoluteUri + "</cas:service>");
            xml.AppendLine(" <cas:pluginAssembly>" + PluginAssembly + "</cas:pluginAssembly>");
            xml.AppendLine(" <cas:permitPasswordRecover>" + PermitPasswordRecover + "</cas:permitPasswordRecover>");
            xml.AppendLine(" <cas:externalPasswordRecover>" + ExternalPasswordRecover + "</cas:externalPasswordRecover>");
            xml.AppendLine(" <cas:passwordRecoverUri>" + PasswordRecoverUri.AbsoluteUri + "</cas:passwordRecoverUri>");
            xml.AppendLine(" <cas:permitChangePassword>" + PermitChangePassword + "</cas:permitChangePassword>");
            xml.AppendLine(" <cas:admin>" + Admin + "</cas:admin>");
            xml.AppendLine(" <cas:attributes>");
            foreach (String key in Attributes.Keys)
            {
                xml.AppendLine("    <cas:attribute>");
                xml.AppendLine("        <cas:key>" + key + "</cas:key>");
                xml.AppendLine("        <cas:value>" + Attributes[key].ToString() + "</cas:value>");
                xml.AppendLine("    </cas:attribute>");
            }
            xml.AppendLine(" </cas:attributes>");
            xml.AppendLine("</cas:serviceConfig>");

            File.WriteAllText(file.FullName, xml.ToString(), Encoding.UTF8);
        }

        public void LoadFromXML(FileInfo file)
        {
            String xml = File.ReadAllText(file.FullName, Encoding.UTF8);
            LoadFromXMLText(xml);
            xml = null;
        }

        public void LoadFromXMLText(String xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("cas", "http://www.safetrend.com.br/tp/cas");

            XmlNode tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:service", namespaceManager);
            if ((tNode == null) || (String.IsNullOrEmpty(tNode.ChildNodes[0].Value)))
                return;

            this.Service = CASPluginService.Normalize(tNode.ChildNodes[0].Value);

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:context", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.Context = tNode.ChildNodes[0].Value;
            }
            catch { }

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:pluginAssembly", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.PluginAssembly = tNode.ChildNodes[0].Value;
            }
            catch { }

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:permitPasswordRecover", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.PermitPasswordRecover = Boolean.Parse(tNode.ChildNodes[0].Value);
            }
            catch { }

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:externalPasswordRecover", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.ExternalPasswordRecover = Boolean.Parse(tNode.ChildNodes[0].Value);
            }
            catch { }

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:passwordRecoverUri", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.PasswordRecoverUri = new Uri(tNode.ChildNodes[0].Value);
            }
            catch { }

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:permitChangePassword", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.PermitChangePassword = Boolean.Parse(tNode.ChildNodes[0].Value);
            }
            catch { }

            try
            {
                tNode = doc.SelectSingleNode("/cas:serviceConfig/cas:admin", namespaceManager);
                if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) this.Admin = tNode.ChildNodes[0].Value;
            }
            catch { }

            XmlNodeList lNode = doc.SelectNodes("/cas:serviceConfig/cas:attributes/cas:attribute", namespaceManager);
            foreach (XmlNode n in lNode)
            {
                String key = null;
                Object value = null;

                try
                {
                    tNode = n.SelectSingleNode("cas:key", namespaceManager);
                    if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value))) key = tNode.ChildNodes[0].Value;

                    tNode = n.SelectSingleNode("cas:value", namespaceManager);
                    if ((tNode != null) && (!String.IsNullOrEmpty(tNode.ChildNodes[0].Value)))
                    {
                        //Testa parses
                        try
                        {
                            value = Int64.Parse(tNode.ChildNodes[0].Value);
                        }
                        catch { }

                        if (value == null)
                            try
                            {
                                value = Boolean.Parse(tNode.ChildNodes[0].Value);
                            }
                            catch { }

                        if (value == null)
                            try
                            {
                                value = DateTime.Parse(tNode.ChildNodes[0].Value);
                            }
                            catch { }

                        if (value == null)
                            try
                            {
                                value = tNode.ChildNodes[0].Value.ToString();
                            }
                            catch { }
                    }


                    if ((key != null && value != null) && (!Attributes.ContainsKey(key)))
                        Attributes.Add(key, value);
                }
                catch { }
            }
        }

    }

    public class CASPluginService
    {
        public CASPluginConfig Config { get; set; }
        public Type Plugin { get; set; }

        public Boolean Equal(Uri service)
        {
            if (Config == null)
                return false;

            String comp = Normalize(service).AbsoluteUri;
            String svc = Normalize(Config.Service).AbsoluteUri;

            try
            {
                return (comp == svc);
            }
            finally {
                comp = null;
                svc = null;
            }
            
        }

        public override string ToString()
        {
            if (Config == null)
                return "CASPluginService: config is null";
            else
                return "CASPluginService: config = " + Config.Service.AbsoluteUri;
        }

        public static Uri Normalize(String service)
        {
            return Normalize(new Uri(service));
        }

        public static Uri Normalize(Uri service)
        {
            return new Uri(service.Scheme + ":" + service.AbsoluteUri.Replace(service.Scheme, "").TrimStart(": ".ToCharArray()).TrimEnd("/".ToCharArray()).ToLower());
        }

    }

    
}
