using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.Config;
using SafeTrend.Json;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;

namespace IAMWebServer.proxy.methods
{
    public partial class proxy_config : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Request.InputStream.Position = 0;

            try
            {

                JSONRequest req = JSON.GetRequest(Request.InputStream);

                using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {
                    ProxyConfig config = new ProxyConfig();
                    config.GetDBConfig(db.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn != null) //Encontrou o proxy
                    {

                        //Limpa os certificados para não enviar
                        config.server_cert = "";
                        config.server_pkcs12_cert = "";
                        config.client_cert = "";

                        db.ExecuteNonQuery("update proxy set last_sync = getdate(), address = '" + Request.ServerVariables["REMOTE_ADDR"] + "', config = 0 where id = " + config.proxyID, System.Data.CommandType.Text, null);
                        ReturnHolder.Controls.Add(new LiteralControl(config.ToJsonString()));
                    }
                    else
                    {
                        db.AddUserLog(LogKey.API_Error, DateTime.Now, "ProxyAPI", UserLogLevel.Warning, 0, ((EnterpriseData)Page.Session["enterprise_data"]).Id, 0, 0, 0, 0, 0, "Proxy not found " + req.host, req.ToString());
                    }
                }
            }
            catch(Exception ex) {
                Tools.Tool.notifyException(ex, this);
                //throw ex;
            }
        }
    }
}