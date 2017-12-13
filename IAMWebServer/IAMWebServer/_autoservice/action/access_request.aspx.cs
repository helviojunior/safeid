using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using IAM.WebAPI;
using System.Reflection;
using System.Web.Hosting;
using System.Net;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;

namespace IAMWebServer._autoservice.action
{
    public partial class access_request : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            LoginData login = LoginUser.LogedUser(this.Page);


            Int64 requestId = 0;
            if (action != "add_request")
            {
                try
                {
                    requestId = Int64.Parse((String)RouteData.Values["id"]);

                    if (requestId < 0)
                        requestId = 0;
                }
                catch { }

                if (requestId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("workflow_request_not_found"), 3000, true);
                    action = "";
                }
            }

            String rData = "";
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";

            try
            {
                switch (action)
                {
                    case "add_request":
                        String workflow_id = Request.Form["workflow"];
                        if (String.IsNullOrEmpty(workflow_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_workflow"), 3000, true);
                            break;
                        }

                        String description = Request.Form["description"];
                        if (String.IsNullOrEmpty(description))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_description"), 3000, true);
                            break;
                        }

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "user.accessrequest",
                            parameters = new
                            {
                                workflowid = workflow_id,
                                userid = login.Id,
                                description = description
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        BooleanResult retAddR = JSON.Deserialize<BooleanResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", "Undefined erro on insert new request", 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                        }
                        else if (!retAddR.result)
                        {
                            contentRet = new WebJsonResponse("", "Undefined erro on insert new request", 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "autoservice/access_request/");
                        }

                        //

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