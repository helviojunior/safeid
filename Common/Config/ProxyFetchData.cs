using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Data;
using System.Data.SqlClient;

using IAM.CA;
using IAM.Config;
using IAM.PluginInterface;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{

    [Serializable()]
    public class ProxyFetchData : IAMDatabase, IDisposable
    {
        public String proxy_name;
        public Int64 proxy_id = 0;

        public String fqdn;
        public String client_cert;

        List<PluginConnectorBaseFetchPackage> fetch_packages;

        public ProxyFetchData()  { }

        public void FindProxy(SqlConnection conn, Int64 id, String proxyName)
        {
            this.Connection = conn;

            DataTable dt = null;
            proxy_id = 0;

            dt = ExecuteDataTable("select id, name, config from proxy with(nolock) where enterprise_id = " + id + " and name = '" + proxyName + "'");

            if ((dt == null) || (dt.Rows.Count == 0)) //Só retorna os dados quando o proxy é encontrado
                return;

            this.proxy_id = (Int64)dt.Rows[0]["id"];
            this.proxy_name = dt.Rows[0]["name"].ToString();
            
        }

        public void GetDBData(SqlConnection conn, Int64 id, String proxyName)
        {
            this.Connection = conn;

            FindProxy(conn, id, proxyName);

            if (this.proxy_id == 0) //Não encontrou o proxy
                return;

            this.fetch_packages = new List<PluginConnectorBaseFetchPackage>();

            //Configurações gerais da empresa
            DataTable dt = ExecuteDataTable("select id, fqdn, server_cert, client_pkcs12_cert, server_pkcs12_cert from enterprise with(nolock) where id = " + id);
            if ((dt != null) || (dt.Rows.Count > 0))
            {
                fqdn = dt.Rows[0]["fqdn"].ToString();
                client_cert = dt.Rows[0]["client_pkcs12_cert"].ToString();
            }
            else
            {
                throw new Exception("Error on get proxy config");
            }

            //Monta os pacotes

            dt = ExecuteDataTable("select f.id fetch_id,  rp.id resource_plugin_id, p.assembly, p.uri from resource_plugin_fetch f with(nolock) inner join resource_plugin rp  with(nolock) on rp.id = f.resource_plugin_id inner join resource r  with(nolock) on r.id = rp.resource_id inner join plugin p with(nolock) on p.id = rp.plugin_id where f.response_date is null and r.proxy_id = " + this.proxy_id);
            if ((dt != null) || (dt.Rows.Count > 0))
            {
                foreach (DataRow dr in dt.Rows)
                {

                    FileInfo pluginsFile = null;

                    using (ServerDBConfig c = new ServerDBConfig(conn, true))
                        pluginsFile = new FileInfo(Path.Combine(c.GetItem("pluginFolder"), dr["assembly"].ToString()));

                    if (pluginsFile == null)
                        continue;

                    if (!pluginsFile.Exists)
                        continue;

                    PluginConnectorBaseFetchPackage newItem = new PluginConnectorBaseFetchPackage();
                    newItem.pluginRawData = File.ReadAllBytes(pluginsFile.FullName);
                    newItem.resourcePluginId = (Int64)dr["resource_plugin_id"];
                    newItem.pluginUri = new Uri(dr["uri"].ToString());
                    newItem.fetchId = (Int64)dr["fetch_id"];

                    DataTable dtConf = ExecuteDataTable("select * from resource_plugin_par where resource_plugin_id = " + newItem.resourcePluginId);
                    if ((dtConf != null) || (dtConf.Rows.Count > 0))
                    {
                        foreach (DataRow drC in dtConf.Rows)
                        {
                            try
                            {
                                newItem.config.Add(drC["key"].ToString(), drC["value"].ToString());
                            }
                            catch { }
                        }
                    }

                    this.fetch_packages.Add(newItem);
                }
            }

        }

        public static List<PluginConnectorBaseFetchPackage> ParsePackage(Byte[] packageData)
        {
            List<PluginConnectorBaseFetchPackage> item = null;
            using (MemoryStream ms = new MemoryStream(packageData))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<PluginConnectorBaseFetchPackage>));
                item = (List<PluginConnectorBaseFetchPackage>)ser.ReadObject(ms);
            }

            return item;
        }

        public Byte[] ToBytes()
        {
            Byte[] jData = new Byte[0];
            
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<PluginConnectorBaseFetchPackage>));

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, this.fetch_packages);
                ms.Flush();
                jData = ms.ToArray();
            }

            Byte[] retData = new Byte[0];
            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(fqdn));
            using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(client_cert), certPass), jData))
            {
                retData = cApi.ToBytes();
            }

            return retData;
        }

        public void Dispose()
        {
            this.proxy_id = 0;
            this.proxy_name = null;
        
        }
    }
}
