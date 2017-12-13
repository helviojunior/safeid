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

namespace IAMWebServer._admin.action
{
    public partial class access_request : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;

            Int64 requestId = 0;
            try
            {
                requestId = Int64.Parse((String)RouteData.Values["id"]);

                if (requestId < 0)
                    requestId = 0;
            }
            catch { }

            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            if (requestId == 0)
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                action = "";
            }

            String rData = "";
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";

            try
            {

                switch (action)
                {
                    
                    case "allow":

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "workflow.accessrequestallow",
                            parameters = new
                            {
                                requestid = requestId
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) 
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");
                        
                        BooleanResult retA = JSON.Deserialize<BooleanResult>(jData);
                        if (retA == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                        }
                        else if (retA.error != null)
                        {
                            contentRet = new WebJsonResponse("", retA.error.data, 3000, true);
                        }
                        else if (!retA.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", "Requisição aprovada com sucesso", 3000, false);
                            contentRet.containerId = "#request_" + requestId;
                            contentRet.html = " ";
                        }
                        break;

                    case "deny":

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "workflow.accessrequestdeny",
                            parameters = new
                            {
                                requestid = requestId
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        BooleanResult retD = JSON.Deserialize<BooleanResult>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (!retD.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", "Requisição negada com sucesso", 3000, false);
                            contentRet.containerId = "#request_" + requestId;
                            contentRet.html = " ";
                        }
                        break;

                    case "revoke":

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "workflow.accessrequestrevoke",
                            parameters = new
                            {
                                requestid = requestId
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        BooleanResult retR = JSON.Deserialize<BooleanResult>(jData);
                        if (retR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                        }
                        else if (retR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retR.error.data, 3000, true);
                        }
                        else if (!retR.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("access_request_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", "Acesso revogada com sucesso", 3000, false);
                            contentRet.containerId = "#request_" + requestId;
                            contentRet.html = " ";
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