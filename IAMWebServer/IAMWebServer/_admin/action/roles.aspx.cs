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
    public partial class roles : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 roleId = 0;
            if (action != "add_role")
            {
                try
                {
                    roleId = Int64.Parse((String)RouteData.Values["id"]);

                    if (roleId < 0)
                        roleId = 0;
                }
                catch { }

                if (roleId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
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
                            method = "role.delete",
                            parameters = new
                            {
                                roleid = roleId
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

                    case "delete_all_users":
                        var reqDelUsr = new
                        {
                            jsonrpc = "1.0",
                            method = "role.deleteallusers",
                            parameters = new
                            {
                                roleid = roleId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDelUsr);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retDelUsr = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retDelUsr == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retDelUsr.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUsr.error.data, 3000, true);
                        }
                        else if (!retDelUsr.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "delete_user":
                        var reqDelUsr2 = new
                        {
                            jsonrpc = "1.0",
                            method = "role.deleteuser",
                            parameters = new
                            {
                                roleid = roleId,
                                userid = (String)RouteData.Values["filter"]
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDelUsr2);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retDelUsr2 = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retDelUsr2 == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retDelUsr2.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUsr2.error.data, 3000, true);
                        }
                        else if (!retDelUsr2.result)
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
                            method = "role.change",
                            parameters = new
                            {
                                roleid = roleId,
                                name = name
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqD);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleGetResult retD = JSON.Deserialize<RoleGetResult>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#role_name_" + roleId, retD.result.info.name);
                        }
                        break;


                    case "add_user":
                        String user_id = Request.Form["user_id"];
                        if (String.IsNullOrEmpty(user_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_username"), 3000, true);
                            break;
                        }

                        
                        var reqAdd = new
                        {
                            jsonrpc = "1.0",
                            method = "role.adduser",
                            parameters = new
                            {
                                roleid = roleId,
                                userid = user_id
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAdd);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retAdd = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retAdd == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retAdd.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAdd.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if (!retAdd.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
                        }

                        //

                        break;

                            
                    case "add_role":
                        String roleName = Request.Form["add_role_name"];
                        if (String.IsNullOrEmpty(roleName))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_role_name"), 3000, true);
                            break;
                        }

                        String context_id = Request.Form["add_role_context"];
                        if (String.IsNullOrEmpty(context_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_context"), 3000, true);
                            break;
                        }
                                                
                        var reqAddR = new
                        {
                            jsonrpc = "1.0",
                            method = "role.new",
                            parameters = new
                            {
                                name = roleName,
                                contextid = context_id,
                                parentid = 0
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAddR);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleGetResult retAddR = JSON.Deserialize<RoleGetResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/roles/" + retAddR.result.info.role_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
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