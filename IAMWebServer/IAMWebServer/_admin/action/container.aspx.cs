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
    public partial class container : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 containerId = 0;
            if (action != "add_container")
            {
                try
                {
                    containerId = Int64.Parse((String)RouteData.Values["id"]);

                    if (containerId < 0)
                        containerId = 0;
                }
                catch { }

                if (containerId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
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
                            method = "container.delete",
                            parameters = new
                            {
                                containerid = containerId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDel);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerDeleteResult retDel = JSON.Deserialize<ContainerDeleteResult>(jData);
                        if (retDel == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
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
                            method = "container.deleteallusers",
                            parameters = new
                            {
                                containerid = containerId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDelUsr);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerDeleteResult retDelUsr = JSON.Deserialize<ContainerDeleteResult>(jData);
                        if (retDelUsr == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retDelUsr.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUsr.error.data, 3000, true);
                        }
                        else if (!retDelUsr.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "delete_user_inactive":
                        var reqDelUsr2 = new
                        {
                            jsonrpc = "1.0",
                            method = "container.deleteuser",
                            parameters = new
                            {
                                containerid = containerId,
                                userid = (String)RouteData.Values["filter"]
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDelUsr2);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerDeleteResult retDelUsr2 = JSON.Deserialize<ContainerDeleteResult>(jData);
                        if (retDelUsr2 == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retDelUsr2.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUsr2.error.data, 3000, true);
                        }
                        else if (!retDelUsr2.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;


                    case "change_name":

                        String name = Request.Form["name"];
                        if (String.IsNullOrEmpty(name))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "container.change",
                            parameters = new
                            {
                                containerid = containerId,
                                name = name
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerGetResult retD = JSON.Deserialize<ContainerGetResult>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#container_name_" + containerId, retD.result.info.name);
                        }
                        break;

                    case "change":

                        String name1 = Request.Form["container_name"];
                        if (String.IsNullOrEmpty(name1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        
                        String parent_id1 = Request.Form["parent_container"];
                        if (String.IsNullOrEmpty(parent_id1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_parent_container"), 3000, true);
                            break;
                        }

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "container.change",
                            parameters = new
                            {
                                containerid = containerId,
                                parentid = parent_id1,
                                name = name1
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerGetResult retC1 = JSON.Deserialize<ContainerGetResult>(jData);
                        if (retC1 == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retC1.error != null)
                        {
                            contentRet = new WebJsonResponse("", retC1.error.data, 3000, true);
                        }
                        else if (retC1.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retC1.result == null || retC1.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", "Atualização realizada com sucesso", 3000, false);
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
                            method = "container.adduser",
                            parameters = new
                            {
                                containerid = containerId,
                                userid = user_id
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAdd);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerDeleteResult retAdd = JSON.Deserialize<ContainerDeleteResult>(jData);
                        if (retAdd == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retAdd.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAdd.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if (!retAdd.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
                        }

                        //

                        break;


                    case "add_container":
                        String containerName = Request.Form["container_name"];
                        if (String.IsNullOrEmpty(containerName))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_role_name"), 3000, true);
                            break;
                        }

                        String context_id = Request.Form["container_context"];
                        if (String.IsNullOrEmpty(context_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_context"), 3000, true);
                            break;
                        }

                        
                        String parent_id = Request.Form["parent_container"];
                        if (String.IsNullOrEmpty(parent_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_parent_container"), 3000, true);
                            break;
                        }

                        if (parent_id != "0")
                        {

                            rData = SafeTrend.Json.JSON.Serialize2(new
                            {
                                jsonrpc = "1.0",
                                method = "container.list",
                                parameters = new
                                {
                                    page_size = Int32.MaxValue,
                                    page = 1
                                },
                                id = 1
                            });


                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                            List<ContainerData> conteinerList = new List<ContainerData>();
                            ContainerListResult cl = JSON.Deserialize<ContainerListResult>(jData);
                            if ((cl != null) && (cl.error == null) && (cl.result != null) && (cl.result.Count > 0))
                                conteinerList.AddRange(cl.result);

                            ContainerData cd = null;

                            foreach (ContainerData c in conteinerList)
                                if (c.container_id.ToString() == parent_id)
                                    cd = c;

                            if (cd == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_parent_container"), 3000, true);
                                break;
                            }

                            if (cd.context_id.ToString() != context_id)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_context_not_same"), 3000, true);
                                break;
                            }
                        }

                        var reqAddR = new
                        {
                            jsonrpc = "1.0",
                            method = "container.new",
                            parameters = new
                            {
                                name = containerName,
                                contextid = context_id,
                                parentid = parent_id
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAddR);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContainerGetResult retAddR = JSON.Deserialize<ContainerGetResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("container_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/container/" + retAddR.result.info.container_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
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