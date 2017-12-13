using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using IAM.Config;
using System.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Data.SqlClient;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;

namespace IAMWebServer.proxy.methods
{
    public partial class plugin_download : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {

                Request.InputStream.Position = 0;

                JSONRequest req = JSON.GetRequest(Request.InputStream);

                JsonGeneric data = new JsonGeneric();
                data.FromJsonString(req.data);

                if (data.data.Count == 0)
                    return;

                using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {
                    ProxyConfig config = new ProxyConfig();
                    config.GetDBConfig(db.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn == null) //Não encontrou o proxy
                        return;

                    String uri = Tools.Tool.TrataInjection(data.data[0][data.GetKeyIndex("uri")]);

                    DataTable dt = db.Select("select * from plugin where uri = '" + uri + "'");

                    if ((dt == null) || (dt.Rows.Count == 0))
                        return;

                    DirectoryInfo pluginsDir = null;

                    using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                        pluginsDir = new DirectoryInfo(c.GetItem("pluginFolder"));

                    if (pluginsDir == null)
                        throw new Exception("Parâmtro 'pluginFolder' não encontrado");

                    if (pluginsDir.Exists)
                    {
                        FileInfo f = new FileInfo(Path.Combine(pluginsDir.FullName, dt.Rows[0]["assembly"].ToString()));

                        if (f.Exists)
                        {

                            Byte[] fData = File.ReadAllBytes(f.FullName);
                            String fileHash = CATools.SHA1Checksum(fData);

                            Int32 ci = data.GetKeyIndex("checksum");
                            if ((ci != -1) && (data.data[0][ci] == fileHash))
                            {
                                ReturnHolder.Controls.Add(new LiteralControl("{ \"name\":\"" + f.Name + "\", \"status\":\"updated\"}"));
                            }
                            else
                            {
                                String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                                using (CryptApi cApi = new CryptApi(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), fData))
                                    ReturnHolder.Controls.Add(new LiteralControl("{ \"name\":\"" + f.Name + "\", \"status\":\"outdated\", \"date\":\"" + f.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss") + "\", \"content\":\"" + Convert.ToBase64String(cApi.ToBytes()) + "\"}"));
                            }

                            fData = new Byte[0];
                        }
                    }

                    /*
                    ProxyConfig config = new ProxyConfig();
                    config.GetDBConfig(IAMDatabase.GetWebConnection(), ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn != null)
                    {
                        ReturnHolder.Controls.Add(new LiteralControl(config.ToJsonString()));
                    }*/

                }
            }
            catch (Exception ex)
            {
                Tools.Tool.notifyException(ex);
                throw ex;
            }
        }
    }
}