using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SafeTrend.Json;
using IAM.GlobalDefs;
using IAM.Config;
using System.IO;
using IAM.CA;

namespace IAM.Log
{
    public class LogProxy
    {
        private JsonGeneric logRecords = new JsonGeneric();
        private String serverCert;
        private String basePath;

        public LogProxy(String basePath, String serverCert)
        {
            this.serverCert = serverCert;
            this.basePath = basePath;

            logRecords.function = "logRecords";
            logRecords.fields = new String[] { "date", "key", "source", "resource_plugin", "resource", "uri", "type", "entityid", "identityid", "text", "additionaldata" };
        }

        public void AddLog(LogKey key, String source, Int64 resource_plugin, String resource, String uri, UserLogLevel type, Int64 entityid, Int64 identityid, String text, String additionalData)
        {
            logRecords.data.Add(new String[] { DateTime.Now.ToString("o"), ((Int32)key).ToString(), source, resource_plugin.ToString(), resource, uri, ((Int32)type).ToString(), entityid.ToString(), identityid.ToString(), text, additionalData });

            if (logRecords.data.Count > 500)
                SaveToSend(resource.ToString() + "log");
        }

        public void SaveToSend(String sufix)
        {
            if ((logRecords.data == null) || (logRecords.data.Count == 0))
                return;

            Byte[] jData = logRecords.ToJsonBytes();

            using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(this.serverCert)), jData))
            {
                DirectoryInfo dirTo = new DirectoryInfo(Path.Combine(this.basePath, "Out"));
                if (!dirTo.Exists)
                    dirTo.Create();

                FileInfo f = new FileInfo(Path.Combine(dirTo.FullName, DateTime.Now.ToString("yyyyMMddHHmss-ffffff") + "-" + sufix) + ".iamdat");

                File.WriteAllBytes(f.FullName, cApi.ToBytes());

#if debug
                TextLog.Log("PluginStarter", "File to send created " + f.Name + " (" + logRecords.data.Count + ")");
#endif
                logRecords.data.Clear();
            }

        }
    }
}
