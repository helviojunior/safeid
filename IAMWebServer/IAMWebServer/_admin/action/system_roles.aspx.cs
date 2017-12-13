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
    public partial class system_roles : System.Web.UI.Page
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
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
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
                            method = "systemrole.delete",
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
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
                            method = "systemrole.deleteallusers",
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retDelUsr.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUsr.error.data, 3000, true);
                        }
                        else if (!retDelUsr.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "delete_user":
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "systemrole.deleteuser",
                            parameters = new
                            {
                                roleid = roleId,
                                userid = (String)RouteData.Values["filter"]
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retDelUsr2 = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retDelUsr2 == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retDelUsr2.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUsr2.error.data, 3000, true);
                        }
                        else if (!retDelUsr2.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "change_role":
                    case "change_name":

                        String name = Request.Form["name"];
                        if (String.IsNullOrEmpty(name)){
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        if (action == "change_role")
                        {
                            rData = JSON.Serialize2(new
                            {
                                jsonrpc = "1.0",
                                method = "systemrole.change",
                                parameters = new
                                {
                                    roleid = roleId,
                                    name = name,
                                    enterprise_admin = (!String.IsNullOrEmpty(Request.Form["enterprise_admin"]))
                                },
                                id = 1
                            });
                        }
                        else
                        {
                            rData = JSON.Serialize2(new
                            {
                                jsonrpc = "1.0",
                                method = "systemrole.change",
                                parameters = new
                                {
                                    roleid = roleId,
                                    name = name
                                },
                                id = 1
                            });
                        }

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleGetResult retD = JSON.Deserialize<RoleGetResult>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#role_name_" + roleId, retD.result.info.name);

                            if (action == "change_role")
                                contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/system_roles/" + roleId + "/");
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
                            method = "systemrole.adduser",
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retAdd.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAdd.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if (!retAdd.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/system_roles/" + roleId + "/users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
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
                   
                        var reqAddR = new
                        {
                            jsonrpc = "1.0",
                            method = "systemrole.new",
                            parameters = new
                            {
                                name = roleName,
                                parentid = 0,
                                enterprise_admin = (!String.IsNullOrEmpty(Request.Form["enterprise_admin"]))
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/system_roles/" + retAddR.result.info.role_id + "/permissions/");
                        }

                        //

                        break;

                    case "change_permissions":
                        String[] pItems = (String.IsNullOrEmpty(Request.Form["permission_id"]) ? new String[0] : Request.Form["permission_id"].Split(",".ToCharArray()));

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "systemrole.changepermissions",
                            parameters = new
                            {
                                roleid = roleId,
                                permissions = pItems
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        SystemRoleGetResult retChangeP = JSON.Deserialize<SystemRoleGetResult>(jData);
                        if (retChangeP == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else if (retChangeP.error != null)
                        {
                            contentRet = new WebJsonResponse("", retChangeP.error.data, 3000, true);
                        }
                        else if (retChangeP.result.info == null) 
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("system_role_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/system_roles/" + retChangeP.result.info.role_id + "/permissions/");
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