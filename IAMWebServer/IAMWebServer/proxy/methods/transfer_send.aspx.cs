using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.Config;
using SafeTrend.Json;
using IAM.CA;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using IAM.GlobalDefs;

namespace IAMWebServer.proxy.methods
{
    public partial class transfer_send : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Request.InputStream.Position = 0;

            try
            {
                JSONRequest req = JSON.GetRequest(Request.InputStream);

                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {
                    ProxyConfig config = new ProxyConfig();
                    config.GetDBConfig(database.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn != null) //Encontrou o proxy
                    {

                        DirectoryInfo inDir = null;

                        using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                            inDir = new DirectoryInfo(c.GetItem("inboundFiles"));

                        if (!inDir.Exists)
                            inDir.Create();

                        req.enterpriseid = ((EnterpriseData)Page.Session["enterprise_data"]).Id.ToString();

                        File.WriteAllBytes(Path.Combine(inDir.FullName, config.proxy_name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss-ffffff") + ".iamreq"), Encoding.UTF8.GetBytes(JSON.Serialize<JSONRequest>(req)));

                        /*

                        Byte[] cData = new Byte[0];

                        try
                        {

                            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                            using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.server_cert), certPass), Convert.FromBase64String(req.data)))
                                cData = cApi.clearData;

                        }
                        catch (Exception ex)
                        {
                            ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "Error on decryption " + ex.Message, "")));
                            return;
                        }

                        JsonGeneric jData = null;
                        try
                        {
                            jData = new JsonGeneric();
                            jData.FromJsonBytes(cData);
                        }
                        catch (Exception ex)
                        {
                            ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "Error parsing Json data " + ex.Message, "")));
                            return;
                        }

                        if (jData == null)
                            return;

                        //"context", "uri", "importid", "registryid", "dataname", "datavalue", "datatype"

                        Int32 contextCol = jData.GetKeyIndex("context");
                        Int32 registryidCol = jData.GetKeyIndex("registryid");
                        Int32 datanameCol = jData.GetKeyIndex("dataname");
                        Int32 datavalueCol = jData.GetKeyIndex("datavalue");
                        Int32 datatypeCol = jData.GetKeyIndex("datatype");

                        foreach (String[] d1 in jData.data)
                        {
                     
                        
                            packages[d1[registryidCol]].data.Add(new PluginBaseDeployPackageData(d1[datanameCol], d1[datavalueCol], d1[datatypeCol]));
                        }
                        */

                        ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(true, "", "Request received ans proxy finded (" + (req.data != null ? req.data.Length.ToString() : "0") + ")")));

                    }
                }
            }
            catch(Exception ex) {
                Tools.Tool.notifyException(ex);
                throw ex;
            }
        }
    }
}