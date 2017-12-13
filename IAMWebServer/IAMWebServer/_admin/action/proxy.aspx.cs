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
    public partial class proxy : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 proxyId = 0;
            if (action != "add_proxy")
            {
                try
                {
                    proxyId = Int64.Parse((String)RouteData.Values["id"]);

                    if (proxyId < 0)
                        proxyId = 0;
                }
                catch { }

                if (proxyId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
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
                            method = "proxy.delete",
                            parameters = new
                            {
                                proxyid = proxyId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDel);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retDel = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retDel == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
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
                            method = "proxy.change",
                            parameters = new
                            {
                                proxyid = proxyId,
                                name = name
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqD);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ProxyGetResult retD = JSON.Deserialize<ProxyGetResult>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#proxy_name_" + proxyId, retD.result.info.name);
                        }
                        break;
                            
                    case "add_proxy":
                        String proxyName = Request.Form["proxy_name"];
                        if (String.IsNullOrEmpty(proxyName))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_proxy_name"), 3000, true);
                            break;
                        }
             
                        var reqAddR = new
                        {
                            jsonrpc = "1.0",
                            method = "proxy.new",
                            parameters = new
                            {
                                name = proxyName
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAddR);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ProxyGetResult retAddR = JSON.Deserialize<ProxyGetResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("proxy_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/proxy/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
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