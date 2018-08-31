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
using SafeTrend.Data;

namespace IAMWebServer.proxy.methods
{
    public partial class send_logs : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Request.InputStream.Position = 0;

            try
            {
                JSONRequest req = JSON.GetRequest(Request.InputStream);

                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                using (ServerDBConfig conf = new ServerDBConfig(database.Connection))
                {
                    ProxyConfig config = new ProxyConfig();
                    config.GetDBConfig(database.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (config.fqdn != null) //Encontrou o proxy
                    {

                        if ((req.data != null) && (req.data != ""))
                        {

                            String dData = req.data;

                            try
                            {
                                dData = Encoding.UTF8.GetString(Convert.FromBase64String(dData));
                            }
                            catch { }

                            String header = "Proxy: " + req.host + Environment.NewLine;
                            header += "IP: " + Tools.Tool.GetIPAddress() + Environment.NewLine;
                            header += "Data: " + Environment.NewLine + Environment.NewLine;

                            Tools.Tool.sendEmail("Proxy log received from " + req.host + " " + DateTime.Now.ToString("yyyy-MM-dd"), conf.GetItem("to"), header + dData, false);

                        }

                        ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(true, "", "Request received with " + (req.data != null ? req.data.Length.ToString() : "0") + " bytes and proxy found")));

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