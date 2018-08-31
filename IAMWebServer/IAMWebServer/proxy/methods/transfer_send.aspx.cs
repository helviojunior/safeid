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
using IAM.PluginInterface;
using SafeTrend.Data;

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
                    ProxyConfig config = new ProxyConfig(true);
                    config.GetDBConfig(database.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn != null) //Encontrou o proxy
                    {

                        DirectoryInfo inDir = null;

                        using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                            inDir = new DirectoryInfo(c.GetItem("inboundFiles"));

                        if (!inDir.Exists)
                            inDir.Create();

                        req.enterpriseid = ((EnterpriseData)Page.Session["enterprise_data"]).Id.ToString();

                        String filename = config.proxy_name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss-ffffff") + ".iamreq";


                        if (String.IsNullOrEmpty(req.filename))
                            req.filename = "Empty";

                        StringBuilder trackData = new StringBuilder();
                        trackData.AppendLine("Proxy: " + req.host);
                        trackData.AppendLine("Enterprise ID: " + req.enterpriseid);
                        trackData.AppendLine("Proxy filename: " + req.filename);
                        trackData.AppendLine("Saved filename: " + filename);

                        UserLogLevel level = UserLogLevel.Info;

                        trackData.AppendLine("");
                        trackData.AppendLine("Checking package...");

                        if (String.IsNullOrEmpty(req.data))
                            throw new Exception("Request data is empty");

                        Byte[] rData = Convert.FromBase64String(req.data);

                        if (!String.IsNullOrEmpty(req.sha1hash))
                        {
                            if(!CATools.SHA1CheckHash(rData, req.sha1hash))
                                throw new Exception("SHA1 Checksum is not equal");

                        }

                        String type = "";
                        try
                        {
                            
                            JsonGeneric jData = new JsonGeneric();
                            try
                            {

                                String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                                if (String.IsNullOrEmpty(config.server_pkcs12_cert))
                                    throw new Exception("Server PKCS12 from proxy config is empty");

                                using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.server_pkcs12_cert), certPass), rData))
                                    jData.FromJsonBytes(cApi.clearData);
                            }
                            catch (Exception ex)
                            {
                                jData = null;
                                trackData.AppendLine("Error decrypting package data for enterprise " + req.enterpriseid + " and proxy " + req.host + ", " + ex.Message);

#if DEBUG
                                trackData.AppendLine(ex.StackTrace);

#endif

                            }

                            if (jData != null)
                            {

#if DEBUG
                                trackData.AppendLine("");
                                trackData.AppendLine("Request data:");
                                trackData.AppendLine(jData.ToJsonString());

                                trackData.AppendLine("");
#endif

                                type = jData.function;

                                trackData.AppendLine("Type: " + type);
                                trackData.AppendLine("Data array length: " + (jData.data == null ? "0" : jData.data.Count.ToString()));

                                if (type.ToLower() == "processimportv2")
                                {
                                    
                                    Int32 d = 1;
                                    foreach (String[] dr in jData.data)
                                    {
                                        try
                                        {
                                            Int32 resourcePluginCol = jData.GetKeyIndex("resource_plugin");
                                            Int32 pkgCol = jData.GetKeyIndex("package");

                                            if (resourcePluginCol == -1)
                                            {
                                                trackData.AppendLine("[Package data " + d + "] Erro finding column 'resource_plugin'");
                                            }

                                            if (pkgCol == -1)
                                            {
                                                trackData.AppendLine("[Package data " + d + "] Erro finding column 'package'");
                                            }

                                            if ((resourcePluginCol != -1) && (pkgCol != -1))
                                            {
                                                PluginConnectorBaseImportPackageUser pkg = JSON.DeserializeFromBase64<PluginConnectorBaseImportPackageUser>(dr[pkgCol]);
                                                trackData.AppendLine("[Package data " + d + "] Import id: " + pkg.importId);
                                                trackData.AppendLine("[Package data " + d + "] Package id: " + pkg.pkgId);

                                                Int64 trackId = 0;
                                                try
                                                {

                                                    String tpkg = JSON.Serialize2(pkg);

                                                    DbParameterCollection par = new DbParameterCollection();
                                                    par.Add("@entity_id", typeof(Int64)).Value = 0;
                                                    par.Add("@date", typeof(DateTime)).Value = pkg.GetBuildDate();
                                                    par.Add("@flow", typeof(String)).Value = "inbound";
                                                    par.Add("@package_id", typeof(String), pkg.pkgId.Length).Value = pkg.pkgId;
                                                    par.Add("@filename", typeof(String)).Value = req.filename;
                                                    par.Add("@package", typeof(String), tpkg.Length).Value = tpkg;

                                                    trackId = database.ExecuteScalar<Int64>("sp_new_package_track", System.Data.CommandType.StoredProcedure, par, null);

                                                    trackData.AppendLine("[Package data " + d + "] Package track id: " + trackId);

                                                    tpkg = null;

                                                    if (trackId > 0)
                                                        database.AddPackageTrack(trackId, "ProxyAPI", "Package received from proxy and saved at " + filename);

                                                }
                                                catch(Exception ex3) {
                                                    trackData.AppendLine("[Package data " + d + "] Erro generating package track: " + ex3.Message);
                                                }


                                                pkg.Dispose();
                                                pkg = null;
                                            }
                                        }
                                        catch (Exception ex2)
                                        {
                                            trackData.AppendLine("[Package data " + d + "] Erro parsing package data " + ex2.Message);
                                        }
                                        d++;
                                    }
                                }
                            }
                        }
                        catch(Exception ex1) {
                            trackData.AppendLine("Erro parsing package " + ex1.Message);
                            level = UserLogLevel.Error;
                        }

                        database.AddUserLog(LogKey.API_Log, DateTime.Now, "ProxyAPI", level, 0, ((EnterpriseData)Page.Session["enterprise_data"]).Id, 0, 0, 0, 0, 0, "File received from proxy " + req.host + (String.IsNullOrEmpty(type) ? "" : " (" + type + ")"), trackData.ToString());


                        File.WriteAllBytes(Path.Combine(inDir.FullName, filename), Encoding.UTF8.GetBytes(JSON.Serialize<JSONRequest>(req)));

                        ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(true, "", "Request received and proxy finded (" + (req.data != null ? req.data.Length.ToString() : "0") + ")")));

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