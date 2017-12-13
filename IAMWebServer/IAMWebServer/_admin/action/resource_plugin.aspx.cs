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
using System.IO;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;

namespace IAMWebServer._admin.action
{
    public partial class resource_plugin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 resourcePluginId = 0;
            if (action != "add_resource_plugin")
            {
                try
                {
                    resourcePluginId = Int64.Parse((String)RouteData.Values["id"]);

                    if (resourcePluginId < 0)
                        resourcePluginId = 0;
                }
                catch { }

                if (resourcePluginId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                    action = "";
                }
            }

            String rData = "";
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";
            String filter = "";


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];


            try
            {

                switch (action)
                {
                    
                    case "change":

                        Boolean exit = false;

                        switch (filter)
                        {
                            case "":
                            case "config_step1":
                                String resource = Request.Form["resource"];
                                if (String.IsNullOrEmpty(resource))
                                {
                                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_resource"), 3000, true);
                                    exit = true;
                                    break;
                                }

                                String plugin = Request.Form["plugin"];
                                if (String.IsNullOrEmpty(plugin))
                                {
                                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_plugin"), 3000, true);
                                    exit = true;
                                    break;
                                }

                                String mail_domain = Request.Form["mail_domain"];
                                if (String.IsNullOrEmpty(mail_domain))
                                {
                                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_mail_domain"), 3000, true);
                                    exit = true;
                                    break;
                                }

                                var reqC = new
                                {
                                    jsonrpc = "1.0",
                                    method = "resourceplugin.change",
                                    parameters = new
                                    {
                                        resourcepluginid = resourcePluginId,
                                        resourceid = resource,
                                        pluginid = plugin,
                                        mail_domain = mail_domain
                                    },
                                    id = 1
                                };

                                rData = JSON.Serialize2(reqC);
                                break;

                            case "config_step2":
                                var reqC2 = new
                                {
                                    jsonrpc = "1.0",
                                    method = "resourceplugin.change",
                                    parameters = new
                                    {
                                        resourcepluginid = resourcePluginId,
                                        permit_add_entity = (!String.IsNullOrEmpty(Request.Form["permit_add_entity"]) ? true : false),
                                        build_login = (!String.IsNullOrEmpty(Request.Form["build_login"]) ? true : false),
                                        build_mail = (!String.IsNullOrEmpty(Request.Form["build_mail"]) ? true : false),
                                        enable_import = (!String.IsNullOrEmpty(Request.Form["enable_import"]) ? true : false),
                                        import_groups = (!String.IsNullOrEmpty(Request.Form["import_groups"]) ? true : false),
                                        import_containers = (!String.IsNullOrEmpty(Request.Form["import_containers"]) ? true : false)
                                    },
                                    id = 1
                                };

                                rData = JSON.Serialize2(reqC2);
                                break;

                            case "config_step3":
                                var reqC3 = new
                                {
                                    jsonrpc = "1.0",
                                    method = "resourceplugin.change",
                                    parameters = new
                                    {
                                        resourcepluginid = resourcePluginId,
                                        enable_deploy = (!String.IsNullOrEmpty(Request.Form["enable_deploy"]) ? true : false),
                                        deploy_all = (!String.IsNullOrEmpty(Request.Form["deploy_all"]) ? true : false),
                                        deploy_after_login = (!String.IsNullOrEmpty(Request.Form["deploy_after_login"]) ? true : false),
                                        password_after_login = (!String.IsNullOrEmpty(Request.Form["password_after_login"]) ? true : false),
                                        deploy_password_hash = (!String.IsNullOrEmpty(Request.Form["password_hash"]) ? Request.Form["password_hash"] : "none"),
                                        use_password_salt = (!String.IsNullOrEmpty(Request.Form["use_password_salt"]) ? true : false),
                                        password_salt_end = (!String.IsNullOrEmpty(Request.Form["password_salt_end"]) ? (Request.Form["password_salt_end"] == "1") : false),
                                        password_salt = (!String.IsNullOrEmpty(Request.Form["password_salt"]) ? Request.Form["password_salt"] : "")
                                    },
                                    id = 1
                                };

                                rData = JSON.Serialize2(reqC3);
                                break;

                            case "config_step4":

                                String name_field_id = Request.Form["name_field_id"];
                                if (String.IsNullOrEmpty(name_field_id))
                                {
                                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_name_field"), 3000, true);
                                    exit = true;
                                    break;
                                }

                                String mail_field_id = Request.Form["mail_field_id"];
                                if (String.IsNullOrEmpty(mail_field_id))
                                {
                                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_mail_field"), 3000, true);
                                    exit = true;
                                    break;
                                }

                                String login_field_id = Request.Form["login_field_id"];
                                if (String.IsNullOrEmpty(login_field_id))
                                {
                                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_login_field"), 3000, true);
                                    exit = true;
                                    break;
                                }


                                var reqC4 = new
                                {
                                    jsonrpc = "1.0",
                                    method = "resourceplugin.change",
                                    parameters = new
                                    {
                                        resourcepluginid = resourcePluginId,
                                        name_field_id = name_field_id,
                                        mail_field_id = mail_field_id,
                                        login_field_id = login_field_id
                                    },
                                    id = 1
                                };

                                rData = JSON.Serialize2(reqC4);
                                break;

                        }

                        if (exit)
                            break;

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourcePluginGetResult retC = JSON.Deserialize<ResourcePluginGetResult>(jData);
                        if (retC == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retC.error != null)
                        {
                            contentRet = new WebJsonResponse("", retC.error.data, 3000, true);
                        }
                        else if (retC.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retC.result == null || retC.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else
                        {
                            String ns = filter;
                            if (!String.IsNullOrEmpty(Request.Form["next_step"]))
                                ns = Request.Form["next_step"];

                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + retC.result.info.resource_plugin_id + "/" + (ns != "" ? ns + "/" : ""));
                        }
                        break;

                    case "add_resource_plugin":
                        String resource1 = Request.Form["resource"];
                        if (String.IsNullOrEmpty(resource1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_resource"), 3000, true);
                            break;
                        }

                        String plugin1 = Request.Form["plugin"];
                        if (String.IsNullOrEmpty(plugin1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_plugin"), 3000, true);
                            break;
                        }

                        String mail_domain1 = Request.Form["mail_domain"];
                        if (String.IsNullOrEmpty(mail_domain1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_mail_domain"), 3000, true);
                            break;
                        }

                        var reqAddR = new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.new",
                            parameters = new
                            {
                                resourceid = resource1,
                                pluginid = plugin1,
                                mail_domain = mail_domain1
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAddR);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourcePluginGetResult retAddR = JSON.Deserialize<ResourcePluginGetResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + retAddR.result.info.resource_plugin_id + "/config_step2/#edit/1");
                        }

                        //

                        break;

                    case "change_par":
                        var tmpReq = new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.get",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(tmpReq);
                        ////conn = DB.GetConnection();
                        jData = "";
                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                        }

                        ResourcePluginGetResult selectedResourcePlugin = JSON.Deserialize<ResourcePluginGetResult>(jData);
                        if (selectedResourcePlugin == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (selectedResourcePlugin.error != null)
                        {
                            contentRet = new WebJsonResponse("", selectedResourcePlugin.error.data, 3000, true);
                            selectedResourcePlugin = null;
                        }
                        else if (selectedResourcePlugin.result == null || selectedResourcePlugin.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            selectedResourcePlugin = null;
                        }
                        else
                        {

                            var tmpReq3 = new
                                {
                                    jsonrpc = "1.0",
                                    method = "plugin.get",
                                    parameters = new
                                    {
                                        pluginid = selectedResourcePlugin.result.info.plugin_id,
                                        parameters = true
                                    },
                                    id = 1
                                };

                            rData = JSON.Serialize2(tmpReq3);
                            ////conn = DB.GetConnection();
                            jData = "";
                            try
                            {
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                            }
                            finally
                            {
                            }

                            PluginGetResult pluginData = JSON.Deserialize<PluginGetResult>(jData);
                            if (pluginData == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                            }
                            else if (pluginData.error != null)
                            {
                                contentRet = new WebJsonResponse("", pluginData.error.data, 3000, true);
                            }
                            else if (pluginData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                            }
                            else if (pluginData.result.parameters == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("plugin_not_found"), 3000, true);
                            }
                            else
                            {
                                if (pluginData.result.parameters.Count > 0)
                                {
                                    Dictionary<String, Object> par = new Dictionary<string, object>();
                                    WebJsonResponse error = null;

                                    foreach (PluginParamterData pd in pluginData.result.parameters)
                                    {
                                        String value = Request.Form[pd.key];
                                        if ((pd.import_required || pd.deploy_required) && (String.IsNullOrEmpty(value)))
                                        {
                                            error = new WebJsonResponse("", MessageResource.GetMessage("required_field") + " " + pd.name, 3000, true);
                                            break;
                                        }
                                        else if (value != null)
                                        {
                                            Object val = null;

                                            switch (pd.type.ToLower())
                                            {

                                                
                                                case "int32":
                                                    try
                                                    {
                                                        val = Int32.Parse(value);
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, " integer"), 3000, true);
                                                    }
                                                    break;

                                                case "int64":
                                                    try
                                                    {
                                                        val = Int64.Parse(value);
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "long integer"), 3000, true);
                                                    }
                                                    break;

                                                case "datetime":
                                                    try
                                                    {
                                                        val = DateTime.Parse(value);
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "datetime"), 3000, true);
                                                    }
                                                    break;

                                                case "directory":
                                                    try
                                                    {
                                                        val = new DirectoryInfo(value);
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "directory"), 3000, true);
                                                    }
                                                    break;

                                                case "stringfixedlist":
                                                    foreach (String s in pd.list_value)
                                                        if (s == value)
                                                            val = s;

                                                    if (val == null)
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "item of list"), 3000, true);
                                                    }

                                                    break;

                                                case "base64filedata":
                                                    try
                                                    {
                                                        Byte[] tmp = Convert.FromBase64String(value);
                                                        val = value;
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "base64"), 3000, true);
                                                    }
                                                    break;

                                                case "boolean":
                                                    try
                                                    {
                                                        val = Boolean.Parse(value);
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "boolean"), 3000, true);
                                                    }
                                                    break;

                                                case "uri":
                                                    try
                                                    {
                                                        val = new Uri(value);
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "Uri"), 3000, true);
                                                    }
                                                    break;

                                                case "stringlist":
                                                    try
                                                    {
                                                        val = value.Split(",".ToCharArray());
                                                    }
                                                    catch
                                                    {
                                                        error = new WebJsonResponse("", String.Format(MessageResource.GetMessage("parameter_type_invalid"), pd.name, "String List"), 3000, true);
                                                    }
                                                    break;

                                                default:
                                                    val = value.Trim();
                                                    break;

                                            }

                                            if (error != null)
                                                break;

                                            if (val != null)
                                                par.Add(pd.key, val);
                                        }

                                        //html += String.Format(infoTemplate, pd.name, value + "<span class=\"description\">" + pd.description + "</span>");
                                    }

                                    if (error == null)
                                    {
                                        //Se passou da validação, efetiva a transação

                                        var tmpChange = new
                                        {
                                            jsonrpc = "1.0",
                                            method = "resourceplugin.changeparameters",
                                            parameters = new
                                            {
                                                resourcepluginid = resourcePluginId,
                                                configparameters = par
                                            },
                                            id = 1
                                        };


                                        rData = JSON.Serialize2(tmpChange);
                                        //conn = DB.GetConnection();
                                        jData = "";
                                        try
                                        {
                                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                                        }
                                        finally
                                        {
                                        }

                                        ResourcePluginGetResult resourcePluginParData = JSON.Deserialize<ResourcePluginGetResult>(jData);
                                        if (resourcePluginParData == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                                        }
                                        else if (resourcePluginParData.error != null)
                                        {
                                            contentRet = new WebJsonResponse("", resourcePluginParData.error.data, 3000, true);
                                        }
                                        else if (resourcePluginParData.result == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                                        }
                                        else if (resourcePluginParData.result == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                                        }
                                        else
                                        {
                                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/config_plugin/");
                                        }


                                    }
                                    else
                                    {
                                        contentRet = error;
                                    }
                                }
                            }
                        }
                        break;

                    case "change_mapping":
                        List<Dictionary<String, Object>> par1 = new List<Dictionary<string, object>>();
                        WebJsonResponse error1 = null;

                        //Valida os campos
                        //
                        String[] contentItems = (String.IsNullOrEmpty(Request.Form["content_id"]) ? new String[0] : Request.Form["content_id"].Split(",".ToCharArray()));

                        if (contentItems.Length == 0)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_field"), 3000, true);
                            break;
                        }

                        /*
                        String[] fItems = (String.IsNullOrEmpty(Request.Form["field_id"]) ? new String[0] : Request.Form["field_id"].Split(",".ToCharArray()));

                        if (fItems.Length == 0)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_field"), 3000, true);
                            break;
                        }*/

                        Boolean hasId = false;
                        Boolean hasUnique = false;

                        foreach (String c in contentItems)
                        {
                            if (String.IsNullOrEmpty(Request.Form["field_id_" + c]))
                            {
                                error1 = new WebJsonResponse("", MessageResource.GetMessage("invalid_field_id"), 3000, true);
                                break;
                            }
                            else if (String.IsNullOrEmpty(Request.Form["data_name_" + c]))
                            {
                                error1 = new WebJsonResponse("", String.Format(MessageResource.GetMessage("type_data_name"), Request.Form["field_name_" + c]), 3000, true);
                                break;
                            }
                            else
                            {
                                Int64 fieldId = 0;
                                try
                                {
                                    fieldId = Int64.Parse(Request.Form["field_id_" + c].ToString());
                                }
                                catch {
                                    error1 = new WebJsonResponse("", MessageResource.GetMessage("invalid_field_id"), 3000, true);
                                    break;
                                }

                                //Continua o processamento
                                Dictionary<string, object> newItem = new Dictionary<string, object>();

                                //Caso tenha sido adicionado mais de uma vez o campo no formulário somente o primeiro será tratado
                                Boolean isId = !String.IsNullOrEmpty(Request.Form["is_id_" + c]);
                                Boolean isPassword = !String.IsNullOrEmpty(Request.Form["is_password_" + c]);
                                Boolean isUnique = !String.IsNullOrEmpty(Request.Form["is_unique_" + c]);

                                if (isId) hasId = true;
                                if (isUnique) hasUnique = true;

                                newItem.Add("data_name", Request.Form["data_name_" + c].Split(",".ToCharArray())[0]);
                                newItem.Add("field_id", fieldId);
                                newItem.Add("is_id", isId);
                                newItem.Add("is_password", isPassword);
                                newItem.Add("is_unique_property", isUnique);

                                par1.Add(newItem);
                            }
                        }

                        if (error1 == null && (!hasId && !hasUnique))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("no_has_id_unique"), 3000, true);
                            break;
                        }

                        if (error1 == null)
                        {
                            //Se passou da validação, efetiva a transação

                            var tmpChange = new
                            {
                                jsonrpc = "1.0",
                                method = "resourceplugin.changemapping",
                                parameters = new
                                {
                                    resourcepluginid = resourcePluginId,
                                    mapping = par1
                                },
                                id = 1
                            };


                            rData = JSON.Serialize2(tmpChange);
                            //conn = DB.GetConnection();
                            jData = "";
                            try
                            {
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                            }
                            finally
                            {
                            }

                            ResourcePluginGetResult resourcePluginMappingData = JSON.Deserialize<ResourcePluginGetResult>(jData);
                            if (resourcePluginMappingData == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (resourcePluginMappingData.error != null)
                            {
                                contentRet = new WebJsonResponse("", resourcePluginMappingData.error.data, 3000, true);
                            }
                            else if (resourcePluginMappingData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (resourcePluginMappingData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else
                            {
                                contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + resourcePluginId + "/config_mapping/");
                            }

                        }
                        else
                        {
                            contentRet = error1;
                        }
                        break;

                    case "change_mapping_fetch":
                        List<Dictionary<String, Object>> par4 = new List<Dictionary<string, object>>();
                        WebJsonResponse error4 = null;

                        //Valida os campos
                        String[] fItems2 = (String.IsNullOrEmpty(Request.Form["field_key"]) ? new String[0] : Request.Form["field_key"].Split(",".ToCharArray()));

                        if (fItems2.Length == 0)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_field"), 3000, true);
                            break;
                        }

                        Boolean hasId1 = false;
                        Boolean hasUnique1 = false;

                        foreach (String f in fItems2)
                        {
                            if (String.IsNullOrEmpty(Request.Form["field_id_" + f]))
                            {
                                error4 = new WebJsonResponse("", MessageResource.GetMessage("select_field") + " " + MessageResource.GetMessage("in") + " '" + f + "'", 3000, true);
                                break;
                            }
                            else
                            {
                                Int64 fieldId = 0;
                                try
                                {
                                    fieldId = Int64.Parse(Request.Form["field_id_" + f]);
                                }
                                catch {
                                    error1 = new WebJsonResponse("", MessageResource.GetMessage("select_field") + " " + MessageResource.GetMessage("in") + " '" + f + "'", 3000, true);
                                    break;
                                }

                                //Continua o processamento
                                Dictionary<string, object> newItem = new Dictionary<string, object>();

                                //Caso tenha sido adicionado mais de uma vez o campo no formulário somente o primeiro será tratado
                                Boolean isId = !String.IsNullOrEmpty(Request.Form["is_id_" + f]);
                                Boolean isPassword = !String.IsNullOrEmpty(Request.Form["is_password_" + f]);
                                Boolean isUnique = !String.IsNullOrEmpty(Request.Form["is_unique_" + f]);

                                if (isId) hasId1 = true;
                                if (isUnique) hasUnique1 = true;

                                newItem.Add("data_name", f);
                                newItem.Add("field_id", fieldId.ToString());
                                newItem.Add("is_id", isId);
                                newItem.Add("is_password", isPassword);
                                newItem.Add("is_unique_property", isUnique);

                                par4.Add(newItem);
                            }
                        }

                        if (error4 == null && (!hasId1 && !hasUnique1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("no_has_id_unique"), 3000, true);
                            break;
                        }

                        if (error4 == null)
                        {
                            //Se passou da validação, efetiva a transação

                            var tmpChange = new
                            {
                                jsonrpc = "1.0",
                                method = "resourceplugin.changemapping",
                                parameters = new
                                {
                                    resourcepluginid = resourcePluginId,
                                    mapping = par4
                                },
                                id = 1
                            };


                            rData = JSON.Serialize2(tmpChange);
                            //conn = DB.GetConnection();
                            jData = "";
                            try
                            {
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                            }
                            finally
                            {
                                //if (conn != null)
                                    //conn.Dispose();
                            }

                            ResourcePluginGetResult resourcePluginMappingData = JSON.Deserialize<ResourcePluginGetResult>(jData);
                            if (resourcePluginMappingData == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (resourcePluginMappingData.error != null)
                            {
                                contentRet = new WebJsonResponse("", resourcePluginMappingData.error.data, 3000, true);
                            }
                            else if (resourcePluginMappingData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (resourcePluginMappingData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else
                            {
                                contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + resourcePluginId + "/config_mapping/");
                            }

                        }
                        else
                        {
                            contentRet = error4;
                        }
                        break;

                    case "change_role":
                        List<Dictionary<String, Object>> par2 = new List<Dictionary<string, object>>();
                        WebJsonResponse error2 = null;

                        //Valida os campos
                        String[] rItems = (String.IsNullOrEmpty(Request.Form["role_id"]) ? new String[0] : Request.Form["role_id"].Split(",".ToCharArray()));

                        if (rItems.Length > 0)
                        {
                            //contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_role"), 3000, true);
                            //break;


                            foreach (String r in rItems)
                            {
                                if (error2 != null)
                                    break;

                                Int32 cnt = 0;
                                foreach (String r1 in rItems)
                                    if (r1 == r)
                                        cnt++;

                                //Verifica se há role duplicada
                                if (cnt > 1)
                                {
                                    error2 = new WebJsonResponse("", String.Format(MessageResource.GetMessage("duplicated_role"), Request.Form["role_name_" + r]), 3000, true);
                                    break;
                                }

                                Dictionary<string, object> newItem = new Dictionary<string, object>();
                                newItem.Add("role_id", r);

                                String[] aItems = (String.IsNullOrEmpty(Request.Form["role_" + r + "_act"]) ? new String[0] : Request.Form["role_" + r + "_act"].Split(",".ToCharArray()));
                                List<Dictionary<string, object>> act = new List<Dictionary<string, object>>();
                                foreach (String ai in aItems)
                                {

                                    String a1 = (Request.Form["role_" + r + "_act_key_" + ai] != null ? Request.Form["role_" + r + "_act_key_" + ai] : "");
                                    if (!String.IsNullOrWhiteSpace(a1))
                                    {

                                        if (Request.Form["role_" + r + "_act_add_value_" + ai] == null || String.IsNullOrWhiteSpace(Request.Form["role_" + r + "_act_add_value_" + ai]))
                                        {
                                            error2 = new WebJsonResponse("", String.Format(MessageResource.GetMessage("incompleted_action"), Request.Form["role_name_" + r]), 3000, true);
                                            break;
                                        }
                                        else
                                        {
                                            String add = Request.Form["role_" + r + "_act_add_value_" + ai].Trim();
                                            String del = "";
                                            if (Request.Form["role_" + r + "_act_del_value_" + ai] == null || String.IsNullOrWhiteSpace(Request.Form["role_" + r + "_act_del_value_" + ai]))
                                                del = add;

                                            Dictionary<string, object> a = new Dictionary<string, object>();
                                            a.Add("key", a1);
                                            a.Add("add_value", Request.Form["role_" + r + "_act_add_value_" + ai]);
                                            a.Add("del_value", Request.Form["role_" + r + "_act_del_value_" + ai]);
                                            a.Add("additional_data", (Request.Form["role_" + r + "_act_additional_data_" + ai] != null ? Request.Form["role_" + r + "_act_additional_data_" + ai] : ""));

                                            act.Add(a);
                                        }
                                    }

                                }
                                newItem.Add("actions", act);

                                String[] rfItems = (String.IsNullOrEmpty(Request.Form["role_" + r + "_filter"]) ? new String[0] : Request.Form["role_" + r + "_filter"].Split(",".ToCharArray()));
                                List<String> fil = new List<string>();
                                foreach (String ei in rfItems)
                                {
                                    String e1 = Request.Form["role_" + r + "_filter_" + ei];
                                    if (!String.IsNullOrWhiteSpace(e1))
                                        fil.Add(e1.Trim());
                                }
                                newItem.Add("filters", fil);

                                String[] aclItems = (String.IsNullOrEmpty(Request.Form["role_" + r + "_timeacl"]) ? new String[0] : Request.Form["role_" + r + "_timeacl"].Split(",".ToCharArray()));
                                List<Dictionary<string, object>> timeAcl = new List<Dictionary<string, object>>();
                                foreach (String tAcl in aclItems)
                                {
                                    IAM.TimeACL.TimeAccess ta = new IAM.TimeACL.TimeAccess();

                                    String type = Request.Form["role_" + r + "_timeacl_type_" + tAcl];
                                    if (!String.IsNullOrWhiteSpace(type))
                                    {
                                        String start_time = Request.Form["role_" + r + "_timeacl_start_time_" + tAcl];
                                        String end_time = Request.Form["role_" + r + "_timeacl_end_time_" + tAcl];
                                        String week_day = Request.Form["role_" + r + "_timeacl_week_day_" + tAcl];

                                        ta.FromString(type, start_time, end_time, week_day);

                                        if (ta.Type == IAM.TimeACL.TimeAccessType.SpecificTime)
                                        {
                                            if (String.IsNullOrWhiteSpace(start_time))
                                                error2 = new WebJsonResponse("", MessageResource.GetMessage("type_start_time"), 3000, true);
                                            else if (String.IsNullOrWhiteSpace(end_time))
                                                error2 = new WebJsonResponse("", MessageResource.GetMessage("type_end_time"), 3000, true);
                                            else if (String.IsNullOrWhiteSpace(week_day))
                                                error2 = new WebJsonResponse("", MessageResource.GetMessage("select_week_day"), 3000, true);
                                            else
                                            {
                                                try
                                                {
                                                    DateTime tmp = DateTime.ParseExact("1970-01-01 " + start_time + ":00", "yyyy-MM-dd HH:mm:ss", null);
                                                }
                                                catch
                                                {
                                                    error2 = new WebJsonResponse("", MessageResource.GetMessage("invalid_start_time"), 3000, true);
                                                }


                                                try
                                                {
                                                    DateTime tmp = DateTime.ParseExact("1970-01-01 " + end_time + ":00", "yyyy-MM-dd HH:mm:ss", null);
                                                }
                                                catch
                                                {
                                                    error2 = new WebJsonResponse("", MessageResource.GetMessage("invalid_end_time"), 3000, true);
                                                }

                                                try
                                                {
                                                    List<DayOfWeek> wd = new List<DayOfWeek>();
                                                    if (!String.IsNullOrWhiteSpace(week_day))
                                                        foreach (String w in week_day.Split(",".ToCharArray()))
                                                        {
                                                            switch (w.ToLower())
                                                            {
                                                                case "sunday":
                                                                    wd.Add(DayOfWeek.Sunday);
                                                                    break;

                                                                case "monday":
                                                                    wd.Add(DayOfWeek.Monday);
                                                                    break;

                                                                case "tuesday":
                                                                    wd.Add(DayOfWeek.Tuesday);
                                                                    break;

                                                                case "wednesday":
                                                                    wd.Add(DayOfWeek.Wednesday);
                                                                    break;

                                                                case "thursday":
                                                                    wd.Add(DayOfWeek.Thursday);
                                                                    break;

                                                                case "friday":
                                                                    wd.Add(DayOfWeek.Friday);
                                                                    break;

                                                                case "saturday":
                                                                    wd.Add(DayOfWeek.Saturday);
                                                                    break;

                                                                case "":
                                                                    break;

                                                                default:
                                                                    throw new Exception("Invalid week day '" + w + "'");
                                                                    break;
                                                            }
                                                        }
                                                }
                                                catch
                                                {
                                                    error2 = new WebJsonResponse("", MessageResource.GetMessage("invalid_week_day"), 3000, true);
                                                }
                                            }
                                        }

                                        timeAcl.Add(ta.ToJsonObject());

                                    }
                                }
                                newItem.Add("time_acl", timeAcl);

                                //if ((act.Count > 0) || (fil.Count > 0) || (timeAcl.Count > 0))
                                par2.Add(newItem);

                            }
                        }
                        
                        if (error2 == null)
                        {
                            //Se passou da validação, efetiva a transação

                            var tmpChange = new
                            {
                                jsonrpc = "1.0",
                                method = "resourceplugin.changerole",
                                parameters = new
                                {
                                    resourcepluginid = resourcePluginId,
                                    roles = par2
                                },
                                id = 1
                            };


                            rData = JSON.Serialize2(tmpChange);
                            //conn = DB.GetConnection();
                            jData = "";
                            try
                            {
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                            }
                            finally
                            {
                                //if (conn != null)
                                    //conn.Dispose();
                            }

                            ResourcePluginGetResult resourcePluginMappingData = JSON.Deserialize<ResourcePluginGetResult>(jData);
                            if (resourcePluginMappingData == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (resourcePluginMappingData.error != null)
                            {
                                contentRet = new WebJsonResponse("", resourcePluginMappingData.error.data, 3000, true);
                            }
                            else if (resourcePluginMappingData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (resourcePluginMappingData.result == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else
                            {
                                contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + resourcePluginId + "/config_role/");
                            }

                        }
                        else
                        {
                            contentRet = error2;
                        }
                        break;

                    case "deploy_now":
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.deploy",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId
                            },
                            id = 1
                        });
                        //conn = DB.GetConnection();
                        jData = "";
                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                            //if (conn != null)
                                //conn.Dispose();
                        }

                        ResourcePluginTFResult resourcePluginDp = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (resourcePluginDp == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (resourcePluginDp.error != null)
                        {
                            contentRet = new WebJsonResponse("", resourcePluginDp.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "new_fetch":
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.newfetch",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId
                            },
                            id = 1
                        });
                        //conn = DB.GetConnection();
                        jData = "";
                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                            //if (conn != null)
                                //conn.Dispose();
                        }

                        ResourcePluginTFResult resourcePluginF = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (resourcePluginF == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (resourcePluginF.error != null)
                        {
                            contentRet = new WebJsonResponse("", resourcePluginF.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "delete_fetch":
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.deletefetch",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId,
                                fetchid = filter
                            },
                            id = 1
                        });
                        //conn = DB.GetConnection();
                        jData = "";
                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                            //if (conn != null)
                                //conn.Dispose();
                        }

                        ResourcePluginTFResult resourcePluginDf = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (resourcePluginDf == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (resourcePluginDf.error != null)
                        {
                            contentRet = new WebJsonResponse("", resourcePluginDf.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "disable":
                        var tmpDis = new
                            {
                                jsonrpc = "1.0",
                                method = "resourceplugin.disable",
                                parameters = new
                                {
                                    resourcepluginid = resourcePluginId
                                },
                                id = 1
                            };


                        rData = JSON.Serialize2(tmpDis);
                        //conn = DB.GetConnection();
                        jData = "";
                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                            //if (conn != null)
                                //conn.Dispose();
                        }

                        ResourcePluginTFResult resourcePluginE = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (resourcePluginE == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (resourcePluginE.error != null)
                        {
                            contentRet = new WebJsonResponse("", resourcePluginE.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "enable":
                        var tmpEn = new
                            {
                                jsonrpc = "1.0",
                                method = "resourceplugin.enable",
                                parameters = new
                                {
                                    resourcepluginid = resourcePluginId
                                },
                                id = 1
                            };


                        rData = JSON.Serialize2(tmpEn);
                        //conn = DB.GetConnection();
                        jData = "";
                        try
                        {
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        }
                        finally
                        {
                            //if (conn != null)
                                //conn.Dispose();
                        }

                        ResourcePluginTFResult resourcePluginD = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (resourcePluginD == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (resourcePluginD.error != null)
                        {
                            contentRet = new WebJsonResponse("", resourcePluginD.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "delete":

                        var reqDel = new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.delete",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDel);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourcePluginTFResult retDel = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (retDel == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/");
                        }
                        break;

                        
                    case "clone":
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.clone",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourcePluginGetResult retClone = JSON.Deserialize<ResourcePluginGetResult>(jData);
                        if (retClone == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retClone.error != null)
                        {
                            contentRet = new WebJsonResponse("", retClone.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + retClone.result.info.resource_plugin_id + "/");
                        }
                        break;


                    case "add_identity":
                        String user_id = Request.Form["user_id"];
                        if (String.IsNullOrEmpty(user_id))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_username"), 3000, true);
                            break;
                        }
                        
                        var reqAdd = new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.addidentity",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId,
                                userid = user_id
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqAdd);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        RoleDeleteResult retAdd = JSON.Deserialize<RoleDeleteResult>(jData);
                        if (retAdd == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retAdd.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAdd.error.data, 3000, true);
                        }
                        else if (!retAdd.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/"+ resourcePluginId +"/identity/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
                        }

                        //

                        break;

                    case "delete_identity":
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.deleteidentity",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId,
                                userid = (String)RouteData.Values["filter"]
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        ResourcePluginTFResult retDelUser = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (retDelUser == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retDelUser.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDelUser.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "change_lockrules":
                        List<String> lock_filters = new List<string>();
                        foreach (String key in Request.Form.Keys)
                            if ((key.IndexOf("filter_") == 0) && (!String.IsNullOrWhiteSpace(Request.Form[key])))
                                lock_filters.Add(Request.Form[key].Trim());

                         rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.changelockrules",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId,
                                lock_filters = lock_filters
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
                        
                        ResourcePluginTFResult retCL = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (retCL == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retCL.error != null)
                        {
                            contentRet = new WebJsonResponse("", retCL.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + resourcePluginId + "/config_lockrules/");
                        }
                        break;

                    case "change_ignore":
                        List<String> ignore_filters = new List<string>();
                        foreach (String key in Request.Form.Keys)
                            if ((key.IndexOf("filter_") == 0) && (!String.IsNullOrWhiteSpace(Request.Form[key])))
                                ignore_filters.Add(Request.Form[key].Trim());

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.changeignore",
                            parameters = new
                            {
                                resourcepluginid = resourcePluginId,
                                ignore_filters = ignore_filters
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        ResourcePluginTFResult retCL1 = JSON.Deserialize<ResourcePluginTFResult>(jData);
                        if (retCL1 == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        }
                        else if (retCL1.error != null)
                        {
                            contentRet = new WebJsonResponse("", retCL1.error.data, 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + resourcePluginId + "/config_ignore/");
                        }
                        break;

                    case "change_schedules":
                        List<Dictionary<String, Object>> schedules = new List<Dictionary<String, Object>>();
                        String[] sItems = (String.IsNullOrEmpty(Request.Form["schedule_id"]) ? new String[0] : Request.Form["schedule_id"].Split(",".ToCharArray()));
                        WebJsonResponse error3 = null;

                        if (sItems.Length == 0)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("schedule_empty"), 3000, true);
                            break;
                        }

                        foreach (String id in sItems)
                        {
                            IAM.Scheduler.Schedule newItem = new IAM.Scheduler.Schedule();

                            if (String.IsNullOrWhiteSpace(Request.Form["schedule_" + id + "_type"]))
                            {
                                error3 = new WebJsonResponse("", MessageResource.GetMessage("select_schedule_type"), 3000, true);
                                break;
                            }

                            switch (Request.Form["schedule_" + id + "_type"].ToLower())
                            {
                                case "annually":
                                    newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Annually;
                                    break;

                                case "monthly":
                                    newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Monthly;
                                    break;

                                case "weekly":
                                    newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Weekly;
                                    break;

                                default:
                                    newItem.Trigger = IAM.Scheduler.ScheduleTtiggers.Dialy;
                                    break;
                            }

                            try
                            {
                                newItem.StartDate = DateTime.Parse(Request.Form["schedule_" + id + "_date"]);
                            }catch{
                                error3 = new WebJsonResponse("", MessageResource.GetMessage("invalid_date"), 3000, true);
                                break;
                            }

                            try
                            {
                                newItem.TriggerTime = DateTime.Parse(Request.Form["schedule_" + id + "_time"]);
                            }
                            catch
                            {
                                error3 = new WebJsonResponse("", MessageResource.GetMessage("invalid_time"), 3000, true);
                                break;
                            }

                            try
                            {
                                newItem.Repeat = Int32.Parse(Request.Form["schedule_" + id + "_repeat"]);
                            }
                            catch { }

                            schedules.Add(newItem.ToJsonObject());
                        }

                        if (error3 == null)
                        {

                            rData = JSON.Serialize2(new
                            {
                                jsonrpc = "1.0",
                                method = "resourceplugin.changeschedules",
                                parameters = new
                                {
                                    resourcepluginid = resourcePluginId,
                                    schedules = schedules
                                },
                                id = 1
                            });

                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                            ResourcePluginTFResult retSchedule = JSON.Deserialize<ResourcePluginTFResult>(jData);
                            if (retSchedule == null)
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                            }
                            else if (retSchedule.error != null)
                            {
                                contentRet = new WebJsonResponse("", retSchedule.error.data, 3000, true);
                            }
                            else
                            {
                                contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/resource_plugin/" + resourcePluginId + "/config_schedule/");
                            }
                        }
                        else
                        {
                            contentRet = error3;
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
                //if (conn != null)
                    //conn.Dispose();
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