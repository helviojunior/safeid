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
    public partial class users : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;

            Int64 userId = 0;
            try
            {
                userId = Int64.Parse((String)RouteData.Values["id"]);

                if (userId < 0)
                    userId = 0;
            }
            catch { }

            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            if ((userId == 0) && (action != "add_user"))
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                action = "";
            }

            String rData = "";
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";

            try
            {

                switch (action)
                {
                    case "add_user":

                        Int64 rpId = 0;
                        try
                        {
                            rpId = Int64.Parse((String)Request.Form["resource_plugin"]);

                            if (rpId < 0)
                                rpId = 0;
                        }
                        catch { }


                        String[] fItems = (String.IsNullOrEmpty(Request.Form["field_id"]) ? new String[0] : Request.Form["field_id"].Split(",".ToCharArray()));
                        if (fItems.Length == 0)
                        {
                            contentRet = new WebJsonResponse("", "Nenhum campo mapeado", 3000, false);
                            break;
                        }


                        List<Dictionary<String, String>> properties = new List<Dictionary<String, String>>();

                        WebJsonResponse iError = null;
                        foreach (String sfId in fItems)
                        {
                            Int64 fId = 0;
                            try
                            {
                                fId = Int64.Parse(sfId);
                                String[] values = (String.IsNullOrEmpty(Request.Form[sfId]) ? new String[0] : Request.Form[sfId].Split(",".ToCharArray()));

                                foreach (String v in values)
                                {
                                    if (!String.IsNullOrWhiteSpace(v))
                                    {
                                        Dictionary<String, String> newItem = new Dictionary<string, string>();
                                        newItem.Add("field_id", fId.ToString());
                                        newItem.Add("value", v.Trim());

                                        properties.Add(newItem);
                                    }
                                }

                            }
                            catch
                            {
                                iError = new WebJsonResponse("", "Campo '" + fId + "' inválido", 3000, false);
                                break;
                            }
                        }

                        if (iError != null)
                        {
                            contentRet = iError;
                            break;
                        }


                        if (properties.Count == 0)
                        {
                            contentRet = new WebJsonResponse("", "Nenhum campo mapeado", 3000, false);
                            break;
                        }

                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "user.new",
                            parameters = new
                            {
                                resourcepluginid = rpId,
                                properties = properties
                            },
                            id = 1
                        });


                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) 
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        GetResult retNew = JSON.Deserialize<GetResult>(jData);
                        if (retNew == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (retNew.error != null)
                        {
                            contentRet = new WebJsonResponse("", retNew.error.data, 3000, true);
                        }
                        else if (retNew.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/users/" + retNew.result.info.userid + "/");
                        }
                        break;


                    case "deploy":

                        var reqD = new
                        {
                            jsonrpc = "1.0",
                            method = "user.deploy",
                            parameters = new
                            {
                                userid = userId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqD);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        Logs retD = JSON.Deserialize<Logs>(jData);
                        if (retD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else
                        {

                            contentRet = new WebJsonResponse("", "Dados do usuário enviados para replicação", 3000, false);
                        }
                        break;

                    case "change_container":

                        String containerId = Request.Form["container"];
                        if (String.IsNullOrEmpty(containerId))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_container"), 3000, true);
                            break;
                        }

                        var reqAdd = new
                        {
                            jsonrpc = "1.0",
                            method = "user.changecontainer",
                            parameters = new
                            {
                                containerid = containerId,
                                userid = userId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAdd);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        BooleanResult retCCont = JSON.Deserialize<BooleanResult>(jData);
                        if (retCCont == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (retCCont.error != null)
                        {
                            contentRet = new WebJsonResponse("", retCCont.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if (!retCCont.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/users/" + userId);
                        }

                        break;

                    case "change_property":

                        List<Dictionary<String, String>> prop = new List<Dictionary<String, String>>();
                        String[] findex = (String.IsNullOrEmpty(Request.Form["field_index"]) ? new String[0] : Request.Form["field_index"].Split(",".ToCharArray()));
                        foreach (String sfId in findex)
                        {
                            if ((!String.IsNullOrEmpty(Request.Form["field_id_" + sfId])) && (!String.IsNullOrEmpty(Request.Form["field_value_" + sfId])))
                            {
                                Int64 fieldId = Int64.Parse(Request.Form["field_id_" + sfId]);

                                Dictionary<String, String> newItem = new Dictionary<string, string>();
                                newItem.Add("field_id", fieldId.ToString());
                                newItem.Add("value", Request.Form["field_value_" + sfId]);

                                prop.Add(newItem);
                            }
                        }

                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "user.changeproperty",
                            parameters = new
                            {
                                userid = userId,
                                properties = prop
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        GetResult retChange = JSON.Deserialize<GetResult>(jData);
                        if (retChange == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (retChange.error != null)
                        {
                            contentRet = new WebJsonResponse("", retChange.error.data, 3000, true);
                        }
                        else if (retChange.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/users/" + retChange.result.info.userid + "/property/");
                        }
                        break;

                    case "resetpwd":

                        var tmpReq = new
                        {
                            jsonrpc = "1.0",
                            method = "user.resetpassword",
                            parameters = new
                            {
                                userid = userId,
                                must_change = true
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(tmpReq);
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        Logs ret = JSON.Deserialize<Logs>(jData);
                        if (ret == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (ret.error != null)
                        {
                            contentRet = new WebJsonResponse("", ret.error.data, 3000, true);
                        }
                        else if (ret.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else
                        {

                            contentRet = new WebJsonResponse("", "Senha do usuário redefinida para o padrão do sistema", 3000, false);
                        }

                        break;

                    case "lock":
                    case "unlock":

                        var unReq = new
                        {
                            jsonrpc = "1.0",
                            method = "user." + (action == "lock" ? "lock" : "unlock"),
                            parameters = new
                            {
                                userid = userId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(unReq);

                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                        }

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        Logs unRet = JSON.Deserialize<Logs>(jData);
                        if (unRet == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (unRet.error != null)
                        {
                            contentRet = new WebJsonResponse("", unRet.error.data, 3000, true);
                        }
                        else if (unRet.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", "Usuário " + (action == "lock" ? "Bloqueado" : "Desbloqueado") + " com sucesso", 5000, false);
                        }

                        break;

                    case "delete_identity":

                        var reqDel = new
                        {
                            jsonrpc = "1.0",
                            method = "user.deleteidentity",
                            parameters = new
                            {
                                userid = userId,
                                identityid = (String)RouteData.Values["filter"]
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("identity_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("identity_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "unlock_identity":

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "user.unlockidentity",
                            parameters = new
                            {
                                userid = userId,
                                identityid = (String)RouteData.Values["filter"]
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleDeleteResult retUnlockIdentity = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retUnlockIdentity == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("identity_not_found"), 3000, true);
                        }
                        else if (retUnlockIdentity.error != null)
                        {
                            contentRet = new WebJsonResponse("", retUnlockIdentity.error.data, 3000, true);
                        }
                        else if (!retUnlockIdentity.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("identity_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
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