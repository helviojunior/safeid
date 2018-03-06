using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using SafeTrend.Data;

namespace IAMWebServer.proxy
{
    public partial class sync : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(Page, false, true)) //Se houver falha na identificação da empresa finaliza a resposta
            {
                Page.Response.Status = "403 Access denied";
                Page.Response.StatusCode = 403;
                Page.Response.End();
                return;
            }
            else
            {
                String proxyName = "";
                String version = "";
                try
                {
                    proxyName = Request.Headers["X-SAFEID-PROXY"];
                }
                catch { }

                try
                {
                    version = Request.Headers["X-SAFEID-VERSION"];
                }
                catch { }

                if (String.IsNullOrEmpty(proxyName))
                {
                    Page.Response.Status = "403 Access denied";
                    Page.Response.StatusCode = 403;
                    Page.Response.End();
                    return;
                }

                Int32 files = 0;
                Int32 rConfig = 0;
                Int32 fetch = 0;
                try
                {
                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {

                        ProxyConfig config = new ProxyConfig();
                        config.GetDBConfig(db.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, proxyName);

                        if (config.fqdn != null) //Encontrou o proxy
                        {

                            DirectoryInfo outDir = null;

                            using (ServerDBConfig c = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                                outDir = new DirectoryInfo(Path.Combine(c.GetItem("outboundFiles"), config.proxyID + "_" + config.proxy_name));

                            if (!outDir.Exists)
                                outDir.Create();

                            files = outDir.GetDirectories().Length;

                            if (config.forceDownloadConfig)
                                rConfig++;

                            //Verifica fetch
                            try
                            {
                                fetch = db.ExecuteScalar<Int32>("select COUNT(*) from resource_plugin_fetch f with(nolock) inner join resource_plugin rp  with(nolock) on rp.id = f.resource_plugin_id inner join resource r  with(nolock) on r.id = rp.resource_id where f.response_date is null and proxy_id = " + config.proxyID, System.Data.CommandType.Text, null);
                            }
                            catch { }

                            

                            db.ExecuteNonQuery("update proxy set last_sync = getdate(), address = '" + Tools.Tool.GetIPAddress() + "', config = 0, version = '" + version + "' where id = " + config.proxyID, System.Data.CommandType.Text, null);
                        }
                        else
                        {
                            db.AddUserLog(LogKey.API_Error, DateTime.Now, "ProxyAPI", UserLogLevel.Warning, 0, ((EnterpriseData)Page.Session["enterprise_data"]).Id, 0, 0, 0, 0, 0, "Proxy not found " + proxyName);
                            Page.Response.Status = "403 Access denied";
                            Page.Response.StatusCode = 403;
                            return;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Tools.Tool.notifyException(ex, this);
                    //throw ex;
                }

                Page.Response.HeaderEncoding = Encoding.UTF8;
                ReturnHolder.Controls.Add(new LiteralControl("{\"config\":" + rConfig + ",\"files\":" + files + ",\"fetch\":" + fetch + "}"));
            }

        }
    }
}