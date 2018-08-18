using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.WebAPI;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;
using System.Globalization;
using System.Threading;
using SafeTrend.Data;
using System.Runtime.Serialization;

namespace IAMWebServer._admin.action
{
    public partial class service_status : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            String serviceName = "";
            try
            {
                serviceName = (String)RouteData.Values["id"];
            }
            catch { }


            if (String.IsNullOrEmpty(serviceName))
                return;

            String rData = "";
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";

            try
            {

                switch (action)
                {
                    case "restart":

                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            DbParameterCollection par = new DbParameterCollection();
                            par.Add("@service_name", typeof(String)).Value = serviceName;


                            DataTable dtServices = db.ExecuteDataTable("select * from service_status where service_name = @service_name", CommandType.Text, par);
                            if ((dtServices != null) && (dtServices.Rows.Count > 0))
                            {
                                if (serviceName.ToLower() != "watchdog")
                                {
                                    db.ExecuteNonQuery("update service_status set started_at = null where service_name = @service_name", CommandType.Text, par);
                                }

                                contentRet = new WebJsonResponse();
                            }
                            else
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("service_not_found"), 3000, true);
                            }
                        }

                        break;

                }

            }
            catch (Exception ex)
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
            }
            finally
            {
            }

            if (contentRet != null)
            {
                if (!String.IsNullOrWhiteSpace((String)Request["cid"]))
                    contentRet.callId = (String)Request["cid"];

            
                Retorno.Controls.Add(new LiteralControl(contentRet.ToJSON()));
            }

        }
    }
}