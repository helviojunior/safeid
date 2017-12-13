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
using IAM.GlobalDefs;
using IAM.CA;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{

    [Serializable()]
    public class JsonGeneric
    {
        [OptionalField()]
        public String function;

        public String[] fields;
        public List<String[]> data;

        public JsonGeneric()
        {
            fields = new String[0];
            data = new List<String[]>();
        }

        public Int32 GetKeyIndex(String key)
        {
            for (Int32 c = 0; c < fields.Length; c++)
                if (key.ToLower() == fields[c].ToLower())
                    return c;

            return -1;
        }

        public void FromJsonString(String json)
        {
            JsonGeneric item = null;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {

                DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());
                item = (JsonGeneric)ser.ReadObject(ms);
            }

            if (item == null)
                return;

            this.fields = item.fields;
            this.data = item.data;
            this.function = item.function;

        }

        public String ToJsonString()
        {
            String ret = "";
            DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, this);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }

        public Byte[] ToJsonBytes()
        {
            return Encoding.UTF8.GetBytes(ToJsonString());
        }

        public void FromJsonBytes(Byte[] utf8JsonData)
        {
            FromJsonString(Encoding.UTF8.GetString(utf8JsonData));
        }
    }

    [Serializable()]
    public class ProxyConfig : IAMDatabase, IDisposable
    {
        public String fqdn;
        public String proxy_name;
        public List<PluginConfig> plugins;

        public String server_cert;
        public String client_cert;

        [NonSerialized()]
        public Int64 proxyID = 0;
        
        [NonSerialized()]
        public Boolean forceDownloadConfig = false;

        [NonSerialized()]
        private Boolean withPrivateServerKey = false;
        [NonSerialized()]
        public String server_pkcs12_cert;

        public ProxyConfig() : this(false) { }

        public ProxyConfig(Boolean withPrivateServerKey)
        {
            this.withPrivateServerKey = withPrivateServerKey;
        }


        public void GetDBCertConfig(SqlConnection conn, Int64 id, String proxyName)
        {
            DataTable dt = null;
            proxyID = 0;
            this.Connection = conn;

            dt = ExecuteDataTable("select id, name, config from proxy with(nolock) where enterprise_id = " + id + " and name = '" + proxyName + "'");

            if ((dt == null) || (dt.Rows.Count == 0)) //Só retorna os dados quando o proxy é encontrado
                return;

            proxyID = (Int64)dt.Rows[0]["id"];
            proxy_name = dt.Rows[0]["name"].ToString();
            forceDownloadConfig = (Boolean)dt.Rows[0]["config"];

            //Configurações gerais da empresa
            dt = ExecuteDataTable("select id, fqdn, server_cert, client_pkcs12_cert, server_pkcs12_cert from enterprise with(nolock) where id = " + id);
            if ((dt != null) || (dt.Rows.Count > 0))
            {
                fqdn = dt.Rows[0]["fqdn"].ToString();
                server_cert = dt.Rows[0]["server_cert"].ToString();
                client_cert = dt.Rows[0]["client_pkcs12_cert"].ToString();
                
                if (this.withPrivateServerKey)
                    server_pkcs12_cert = dt.Rows[0]["server_pkcs12_cert"].ToString();
            }
            else
            {
                throw new Exception("Error on get proxy config");
            }

        }

        public void GetDBConfig(SqlConnection conn, Int64 enterpriseId, String proxyName)
        {
            DataTable dt = null;
            this.Connection = conn;

            GetDBCertConfig(conn, enterpriseId, proxyName);

            if (this.fqdn == null) //Não encontrou o proxy
                return;

            //Plugins ativos
            plugins = new List<PluginConfig>();

            dt = ExecuteDataTable("select * from vw_proxy_plugin with(nolock) where proxy_id = " + proxyID + " and enterprise_id = " + enterpriseId);
            if ((dt != null) || (dt.Rows.Count > 0))
            {
                String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(this.fqdn));
                OpenSSL.X509.X509Certificate cert = CATools.LoadCert(Convert.FromBase64String(this.client_cert), certPass);

                foreach (DataRow dr in dt.Rows)
                {
                    PluginConfig newItem = new PluginConfig(cert, conn, dr["scheme"].ToString(), (Int64)dr["plugin_id"], (Int64)dr["resource_plugin_id"]);
                    
                    plugins.Add(newItem);
                }
                
            }
       
        }

        public void FromJsonString(String json)
        {
            ProxyConfig item = null;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {

                DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());
                item = (ProxyConfig)ser.ReadObject(ms);
            }

            if (item == null)
                return;

            this.fqdn = item.fqdn;
            this.server_cert = item.server_cert;
            this.client_cert = item.client_cert;
            this.plugins = item.plugins;
            this.proxy_name = item.proxy_name;

        }

        public String ToJsonString()
        {
            String ret = "";
            DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, this);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }

        public void Dispose()
        {
            this.fqdn = null;
            this.proxy_name = null;
            this.server_cert = null;
            this.client_cert = null;
        
            if (this.plugins != null) this.plugins.Clear();
            this.plugins = null;
        
        }
    }
}
