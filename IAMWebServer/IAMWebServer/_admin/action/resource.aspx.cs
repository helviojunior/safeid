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
    public partial class resource : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 resourceId = 0;
            if (action != "add_resource")
            {
                try
                {
                    resourceId = Int64.Parse((String)RouteData.Values["id"]);

                    if (resourceId < 0)
                        resourceId = 0;
                }
                catch { }

                if (resourceId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
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
                    case "delete":
                        
                        var reqDel = new
                        {
                            jsonrpc = "1.0",
                            method = "resource.delete",
                            parameters = new
                            {
                                resourceid = resourceId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDel);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourceDeleteResult retDel = JSON.Deserialize<ResourceDeleteResult>(jData);
                        if (retDel == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "change_name":

                        String name = Request.Form["name"];
                        if (String.IsNullOrEmpty(name)){
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        var reqD = new
                        {
                            jsonrpc = "1.0",
                            method = "resource.change",
                            parameters = new
                            {
                                resourceid = resourceId,
                                name = name
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqD);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourceGetResult retD = JSON.Deserialize<ResourceGetResult>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#resource_name_" + resourceId, retD.result.info.name);
                        }
                        break;

                    case "change":

                        String name1 = Request.Form["name"];
                        if (String.IsNullOrEmpty(name1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        
                        String context_id1 = Request.Form["resource_context"];
                        if (String.IsNullOrEmpty(context_id1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_context"), 3000, true);
                            break;
                        }

                        String proxy_id1 = Request.Form["resource_proxy"];
                        if (String.IsNullOrEmpty(proxy_id1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_proxy"), 3000, true);
                            break;
                        }


                        var reqC = new
                        {
                            jsonrpc = "1.0",
                            method = "resource.change",
                            parameters = new
                            {
                                resourceid = resourceId,
                                name = name1,
                                contextid = context_id1,
                                proxyid = proxy_id1,
                                enabled = (!String.IsNullOrEmpty(Request.Form["disabled"]) ? false : true)
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqC);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourceGetResult retC = JSON.Deserialize<ResourceGetResult>(jData);
                        if (retC == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else if (retC.error != null)
                        {
                            contentRet = new WebJsonResponse("", retC.error.data, 3000, true);
                        }
                        else if (retC.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else if (retC.result == null || retC.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource/" + resourceId + "/");
                        }
                        break;

                            
                    case "add_resource":
                        String resourceName = Request.Form["resource_name"];
                        if (String.IsNullOrEmpty(resourceName))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_resource_name"), 3000, true);
                            break;
                        }

                        String context_id = Request.Form["resource_context"];
                        if (String.IsNullOrEmpty(context_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_context"), 3000, true);
                            break;
                        }

                        String proxy_id = Request.Form["resource_proxy"];
                        if (String.IsNullOrEmpty(proxy_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_proxy"), 3000, true);
                            break;
                        }
                                               
                        var reqAddR = new
                        {
                            jsonrpc = "1.0",
                            method = "resource.new",
                            parameters = new
                            {
                                name = resourceName,
                                contextid = context_id,
                                proxyid = proxy_id
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAddR);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourceGetResult retAddR = JSON.Deserialize<ResourceGetResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
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