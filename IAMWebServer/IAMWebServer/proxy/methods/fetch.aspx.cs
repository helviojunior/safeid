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

namespace IAMWebServer.proxy.methods
{
    public partial class fetch : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Request.InputStream.Position = 0;

            try
            {

                JSONRequest req = JSON.GetRequest(Request.InputStream);

                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                {
                    ProxyFetchData fetchData = new ProxyFetchData();
                    fetchData.GetDBData(database.Connection, ((EnterpriseData)Page.Session["enterprise_data"]).Id, req.host);

                    if (fetchData.proxy_id > 0) //Encontrou o proxy
                    {
                        ReturnHolder.Controls.Add(new LiteralControl("{ \"response\":\"success\", \"data\":\"" + Convert.ToBase64String(fetchData.ToBytes()) + "\"}"));
                    }
                }
            }
            catch(Exception ex) {
                Tools.Tool.notifyException(ex, this);
                throw ex;
            }
        }
    }
}