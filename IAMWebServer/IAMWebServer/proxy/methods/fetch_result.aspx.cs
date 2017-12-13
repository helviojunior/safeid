using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.Config;
using SafeTrend.Json;
using System.Data.SqlClient;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;
using IAM.GlobalDefs;
using System.Text;
using IAM.CA;

namespace IAMWebServer.proxy.methods
{
    public partial class fetch_result : System.Web.UI.Page
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
                        try
                        {
                            Byte[] bData = Convert.FromBase64String(req.data);
                            List<Dictionary<String, Object>> proccessData = new List<Dictionary<string, object>>();


                            String certPass = CATools.SHA1Checksum(Encoding.UTF8.GetBytes(config.fqdn));
                            using (CryptApi cApi = CryptApi.ParsePackage(CATools.LoadCert(Convert.FromBase64String(config.client_cert), certPass), bData))
                                proccessData = SafeTrend.Json.JSON.Deserialize<List<Dictionary<String, Object>>>(Encoding.UTF8.GetString(cApi.clearData));

                            foreach (Dictionary<String, Object> p in proccessData)
                            {
                                if (p.ContainsKey("fetch_id"))
                                {

                                    String jData = SafeTrend.Json.JSON.Serialize2(p);

                                    Int64 fetch_id = 0;

                                    try
                                    {
                                        fetch_id = Int64.Parse(p["fetch_id"].ToString());
                                    }
                                    catch { }

                                    if (fetch_id > 0)
                                    {
                                        DbParameterCollection par = new DbParameterCollection();
                                        par.Add("@fetch_id", typeof(Int64)).Value = fetch_id;
                                        par.Add("@json_data", typeof(String)).Value = jData;
                                        par.Add("@success", typeof(Boolean)).Value = (p.ContainsKey("result") && (p["result"] is Boolean) && (Boolean)p["result"]);

                                        db.ExecuteNonQuery("update resource_plugin_fetch set response_date = getdate(), [success] = @success, json_data = @json_data WHERE id = @fetch_id", System.Data.CommandType.Text, par);
                                    }
                                }
                            }

                            ReturnHolder.Controls.Add(new LiteralControl("{ \"response\":\"success\" }"));
                        }
                        catch
                        {
                            ReturnHolder.Controls.Add(new LiteralControl("{ \"response\":\"error\" }"));
                        }
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