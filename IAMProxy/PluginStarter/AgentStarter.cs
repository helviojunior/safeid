using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.IO.Compression;

using IAM.PluginInterface;
using IAM.PluginManager;
using IAM.Log;
using IAM.Config;
using IAM.Scheduler;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;
//using IAM.SQLDefs;


namespace IAM.PluginStarter
{

    class AgentStarter
    {
        private String basePath;
        private ProxyConfig config;
        private PluginAgentBase plugin = null;
        private Boolean executing = false;
        private LogProxy logProxy;

        public AgentStarter(String ConfigJson, PluginAgentBase plugin)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            config = new ProxyConfig();
            config.FromJsonString(ConfigJson);
            ConfigJson = null;

            this.plugin = plugin;

            logProxy = new LogProxy(basePath, config.server_cert);

            StartAgents();
        }

        public void NewConfig(String ConfigJson)
        {

            if (plugin == null)
                throw new Exception("Plugin is null");

            if (String.IsNullOrWhiteSpace(ConfigJson))
                throw new Exception("Config is null or empty");

            ProxyConfig configTmp = new ProxyConfig();
            configTmp.FromJsonString(ConfigJson);
            ConfigJson = null;

            //Se tudo ocorreu sem erro altera a config local

            config.Dispose();
            config = null;

            config = configTmp;
        }

        private void StartAgents()
        {

            List<Int64> resource = new List<Int64>();

            //Separa os contextos
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
            OpenSSL.X509.X509Certificate cert = CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass);
            
            try
            {

                foreach (PluginConfig p in config.plugins)
                {
                    if (p.uri.ToLower() == plugin.GetPluginId().AbsoluteUri.ToLower())
                    {

                        Dictionary<String, Object> connectorConf = new Dictionary<String, Object>();

                        JsonGeneric pgConf = new JsonGeneric();
                        try
                        {
                            if (cert == null)
                                throw new Exception("Certificate is null");

                            using (CryptApi cApi = CryptApi.ParsePackage(cert, Convert.FromBase64String(p.parameters)))
                                pgConf.FromJsonString(Encoding.UTF8.GetString(cApi.clearData));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Decrypt error: " + ex.Message);
                        }

                        if ((pgConf.data == null) || (pgConf.data.Count == 0))
                            continue;

                        Int32 kCol = pgConf.GetKeyIndex("key");
                        Int32 vCol = pgConf.GetKeyIndex("value");

                        foreach (String[] d1 in pgConf.data)
                            PluginBase.FillConfig(plugin, ref connectorConf, d1[kCol], d1[vCol].ToString());
                            /*if (!connectorConf.ContainsKey(d1[kCol]))
                                connectorConf.Add(d1[kCol], d1[vCol].ToString());*/
                        try
                        {
                            StartAgents(connectorConf);
                        }
                        catch (Exception ex)
                        {
                            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error on start agent: " + ex.Message);
                        }
                        finally
                        {
                            connectorConf.Clear();
                            connectorConf = null;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Error on parse config: " + ex.Message);
            }
            
            cert = null;
            certPass = null;

        }

        private void StartAgents(Dictionary<String, Object> connectorConf)
        {
            TextLog.Log("PluginStarter", "{" + plugin.GetPluginId().AbsoluteUri + "} Starting agent thread...");

            try
            {

                LogEvent log = new LogEvent(delegate(Object sender, PluginLogType type, String text)
                {
                    TextLog.Log("PluginStarter", "{" + ((PluginBase)sender).GetPluginId().AbsoluteUri + "} " + type + ", " + text);
                });

                LogEvent2 log2 = new LogEvent2(delegate(Object sender, PluginLogType type, Int64 entityId, Int64 identityId, String text, String additionalData)
                {

#if DEBUG
                    TextLog.Log("PluginStarter", "{" + ((PluginBase)sender).GetPluginId().AbsoluteUri + "} Type: " + type + ", Entity Id: " + entityId + ", Identity Id: " + identityId + "Data: "  + text + additionalData);
#endif

                    logProxy.AddLog(LogKey.Plugin_Event, "Proxy", 0, "0", ((PluginBase)sender).GetPluginId().AbsoluteUri, (UserLogLevel)((Int32)type), entityId, identityId, text, additionalData);
                    logProxy.SaveToSend("agentlog");
                });

                plugin.Log += log;
                plugin.Log2 += log2;

                plugin.Start(connectorConf);
            }
            catch (Exception ex)
            {
                logProxy.AddLog(LogKey.Proxy_Event, "Proxy", 0, "", plugin.GetPluginId().AbsoluteUri, UserLogLevel.Error, 0, 0, "Erro on agent thread: " + ex.Message, "");
                throw ex;
            }
        }

        private void SaveToSend(JsonGeneric data, String prefix)
        {
            if ((data.data == null) || (data.data.Count == 0))
                return;

            Byte[] jData = data.ToJsonBytes();

            using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(config.server_cert)), jData))
            {
                DirectoryInfo dirTo = new DirectoryInfo(Path.Combine(basePath, "Out"));
                if (!dirTo.Exists)
                    dirTo.Create();

                FileInfo f = new FileInfo(Path.Combine(dirTo.FullName, DateTime.Now.ToString("yyyyMMddHHmss-ffffff") + "-" + prefix) + ".iamdat");

                File.WriteAllBytes(f.FullName, cApi.ToBytes());

                TextLog.Log("PluginStarter", "File to send created " + f.Name + " (" + data.data.Count + ")");

                data.data.Clear();
            }

        }

    }
}
