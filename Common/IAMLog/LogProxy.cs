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
        private JsonGeneric logRecords1 = new JsonGeneric();
        private JsonGeneric logRecords2 = new JsonGeneric();
        private String serverCert;
        private String basePath;

        public LogProxy(String basePath, String serverCert)
        {
            this.serverCert = serverCert;
            this.basePath = basePath;

            logRecords1.function = "logRecords";
            logRecords1.fields = new String[] { "date", "key", "source", "resource_plugin", "resource", "uri", "type", "entityid", "identityid", "text", "additionaldata" };

            logRecords2.function = "packageTrack";
            logRecords2.fields = new String[] { "date", "source", "resource", "filename", "packageid", "flow", "text" };
        }

        public void AddLog(LogKey key, String source, Int64 resource_plugin, String resource, String uri, UserLogLevel type, Int64 entityid, Int64 identityid, String text, String additionalData)
        {
            logRecords1.data.Add(new String[] { DateTime.Now.ToString("o"), ((Int32)key).ToString(), source, resource_plugin.ToString(), resource, uri, ((Int32)type).ToString(), entityid.ToString(), identityid.ToString(), text, additionalData });

            if (logRecords1.data.Count > 500)
                SaveToSend(resource.ToString() + "log");
        }

        public void AddPackageTrack(String source, String resource, String filename, String packageId, String flow, String text)
        {
            logRecords2.data.Add(new String[] { DateTime.Now.ToString("o"), resource, filename, packageId, flow, text });

            if (logRecords2.data.Count > 500)
                SaveToSend(resource.ToString() + "log");
        }

        public void SaveToSend(String sufix)
        {
            if ((logRecords1.data != null) && (logRecords1.data.Count > 0))
            {

                Byte[] jData = logRecords1.ToJsonBytes();

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
                    logRecords1.data.Clear();
                }
            }

            if ((logRecords2.data != null) && (logRecords2.data.Count > 0))
            {

                Byte[] jData = logRecords2.ToJsonBytes();

                using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(this.serverCert)), jData))
                {
                    DirectoryInfo dirTo = new DirectoryInfo(Path.Combine(this.basePath, "Out"));
                    if (!dirTo.Exists)
                        dirTo.Create();

                    FileInfo f = new FileInfo(Path.Combine(dirTo.FullName, DateTime.Now.ToString("yyyyMMddHHmss-ffffff") + "-pl-" + sufix) + ".iamdat");

                    File.WriteAllBytes(f.FullName, cApi.ToBytes());

#if debug
                TextLog.Log("PluginStarter", "File to send created " + f.Name + " (" + logRecords.data.Count + ")");
#endif
                    logRecords2.data.Clear();
                }
            }

        }
    }
}
