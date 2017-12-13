using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class loginRule
    {
        public String group;
        public Int32 order;
        public String field;
    }

    public partial class context : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 contextId = 0;
            if (action != "add_context")
            {
                try
                {
                    contextId = Int64.Parse((String)RouteData.Values["id"]);

                    if (contextId < 0)
                        contextId = 0;
                }
                catch { }

                if (contextId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
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
                            method = "context.delete",
                            parameters = new
                            {
                                contextid = contextId
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
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
                            method = "context.change",
                            parameters = new
                            {
                                contextid = contextId,
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#context_name_" + contextId, retD.result.info.name);
                        }
                        break;


                    case "change_login_rules":

                        List<loginRule> items = new List<loginRule>();

                        //rule_R1410734460301_group
                        String[] rIds = Request.Form["rule_id"].Split(",".ToCharArray());
                        foreach (String rid in rIds)
                        {
                            if (String.IsNullOrEmpty(Request.Form["rule_" + rid + "_group"]))
                            {
                                contentRet = new WebJsonResponse("", "Grupo não localizado na condição " + rid, 3000, true);
                                break;
                            }

                            if (String.IsNullOrEmpty(Request.Form["rule_" + rid + "_field"]))
                            {
                                contentRet = new WebJsonResponse("", "Campo não localizado na condição " + rid, 3000, true);
                                break;
                            }

                            if (String.IsNullOrEmpty(Request.Form["rule_" + rid + "_order"]))
                            {
                                contentRet = new WebJsonResponse("", "Ordem do campo não localizado na condição " + rid, 3000, true);
                                break;
                            }


                            loginRule newItem = new loginRule();
                            newItem.group = Request.Form["rule_" + rid + "_group"].Split(",".ToCharArray())[0];
                            newItem.order = Int32.Parse(Request.Form["rule_" + rid + "_order"].Split(",".ToCharArray())[0]);

                            try
                            {
                                switch (Request.Form["rule_" + rid + "_field"].Split(",".ToCharArray())[0].ToLower())
                                {
                                    case "first_name":
                                    case "second_name":
                                    case "last_name":
                                    case "char_first_name":
                                    case "char_second_name":
                                    case "char_last_name":
                                    case "index":
                                    case "dot":
                                    case "hyphen":
                                        newItem.field = Request.Form["rule_" + rid + "_field"].Split(",".ToCharArray())[0].ToLower();
                                        break;

                                    default:
                                        throw new Exception("");
                                        break;
                                }
                            }
                            catch
                            {
                                contentRet = new WebJsonResponse("", "ID do campo inválido na condição " + rid, 3000, true);
                                break;
                            }


                            items.Add(newItem);
                        }

                        //Ordena os ítems
                        items.Sort(delegate(loginRule r1, loginRule r2) { return r1.order.CompareTo(r2.order); });

                        List<loginRule> finalRules = new List<loginRule>();
                        foreach (loginRule li in items)
                        {
                            loginRule gi = finalRules.Find(g => (g.group == li.group));
                            if (gi == null)
                                finalRules.Add(li);
                            else
                                gi.field += "," + li.field;

                        }

                        finalRules.Sort(delegate(loginRule r1, loginRule r2) { return r1.order.CompareTo(r2.order); });

                        for (Int32 i = 0; i < finalRules.Count; i++)
                            finalRules[i].order = i;

                        List<Dictionary<String, Object>> sItems = new List<Dictionary<string, object>>();
                        foreach (loginRule li in finalRules)
                        {
                            Dictionary<String, Object> ni = new Dictionary<string, object>();
                            ni.Add("name", li.field);
                            ni.Add("rule", li.field);
                            ni.Add("order", li.order);

                            sItems.Add(ni);
                        }

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "context.changeloginrules",
                            parameters = new
                            {
                                contextid = contextId,
                                rules = sItems
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        BooleanResult retCL = JSON.Deserialize<BooleanResult>(jData);
                        if (retCL == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else if (retCL.error != null)
                        {
                            contentRet = new WebJsonResponse("", retCL.error.data, 3000, true);
                        }
                        else if (!retCL.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
                        }
                        break;

                    case "change_mail_rules":

                        List<loginRule> mItems = new List<loginRule>();

                        //rule_R1410734460301_group
                        String[] mrIds = Request.Form["rule_id"].Split(",".ToCharArray());
                        foreach (String rid in mrIds)
                        {
                            if (String.IsNullOrEmpty(Request.Form["rule_" + rid + "_group"]))
                            {
                                contentRet = new WebJsonResponse("", "Grupo não localizado na condição " + rid, 3000, true);
                                break;
                            }

                            if (String.IsNullOrEmpty(Request.Form["rule_" + rid + "_field"]))
                            {
                                contentRet = new WebJsonResponse("", "Campo não localizado na condição " + rid, 3000, true);
                                break;
                            }

                            if (String.IsNullOrEmpty(Request.Form["rule_" + rid + "_order"]))
                            {
                                contentRet = new WebJsonResponse("", "Ordem do campo não localizado na condição " + rid, 3000, true);
                                break;
                            }


                            loginRule newItem = new loginRule();
                            newItem.group = Request.Form["rule_" + rid + "_group"].Split(",".ToCharArray())[0];
                            newItem.order = Int32.Parse(Request.Form["rule_" + rid + "_order"].Split(",".ToCharArray())[0]);

                            try
                            {
                                switch (Request.Form["rule_" + rid + "_field"].Split(",".ToCharArray())[0].ToLower())
                                {
                                    case "first_name":
                                    case "second_name":
                                    case "last_name":
                                    case "char_first_name":
                                    case "char_second_name":
                                    case "char_last_name":
                                    case "index":
                                    case "dot":
                                    case "hyphen":
                                        newItem.field = Request.Form["rule_" + rid + "_field"].Split(",".ToCharArray())[0].ToLower();
                                        break;

                                    default:
                                        throw new Exception("");
                                        break;
                                }
                            }
                            catch
                            {
                                contentRet = new WebJsonResponse("", "ID do campo inválido na condição " + rid, 3000, true);
                                break;
                            }


                            mItems.Add(newItem);
                        }

                        //Ordena os ítems
                        mItems.Sort(delegate(loginRule r1, loginRule r2) { return r1.order.CompareTo(r2.order); });

                        List<loginRule> mFinalRules = new List<loginRule>();
                        foreach (loginRule li in mItems)
                        {
                            loginRule gi = mFinalRules.Find(g => (g.group == li.group));
                            if (gi == null)
                                mFinalRules.Add(li);
                            else
                                gi.field += "," + li.field;

                        }

                        mFinalRules.Sort(delegate(loginRule r1, loginRule r2) { return r1.order.CompareTo(r2.order); });

                        for (Int32 i = 0; i < mFinalRules.Count; i++)
                            mFinalRules[i].order = i;

                        List<Dictionary<String, Object>> msItems = new List<Dictionary<string, object>>();
                        foreach (loginRule li in mFinalRules)
                        {
                            Dictionary<String, Object> ni = new Dictionary<string, object>();
                            ni.Add("name", li.field);
                            ni.Add("rule", li.field);
                            ni.Add("order", li.order);

                            msItems.Add(ni);
                        }

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "context.changemailrules",
                            parameters = new
                            {
                                contextid = contextId,
                                rules = msItems
                            },
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        BooleanResult mRetCL = JSON.Deserialize<BooleanResult>(jData);
                        if (mRetCL == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else if (mRetCL.error != null)
                        {
                            contentRet = new WebJsonResponse("", mRetCL.error.data, 3000, true);
                        }
                        else if (!mRetCL.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
                        }
                        break;
    

                    case "add_context":
                    case "change":

                        Boolean change = false;
                        if (action == "change")
                            change = true;

                        String contextName = Request.Form["add_context_name"];
                        if (String.IsNullOrEmpty(contextName))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_context_name"), 3000, true);
                            break;
                        }

                        String passwordRule = Request.Form["pwd_rule"];
                        if (String.IsNullOrEmpty(passwordRule))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("type_pwd_rule"), 3000, true);
                            break;
                        }

                        String pwd_length = Request.Form["pwd_length"];
                        if (String.IsNullOrEmpty(pwd_length))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("pwd_len_size"), 3000, true);
                            break;
                        }

                        try
                        {
                            Int32 tmp = Int32.Parse(Request.Form["pwd_length"]);
                            if ((tmp < 4) || (tmp > 20))
                            {
                                contentRet = new WebJsonResponse("", MessageResource.GetMessage("pwd_len_size"), 3000, true);
                                break;
                            }
                        }
                        catch {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("pwd_len_size"), 3000, true);
                            break;
                        }

                        if (!change)
                        {
                            var reqAddR = new
                            {
                                jsonrpc = "1.0",
                                method = "context.new",
                                parameters = new
                                {
                                    name = contextName,
                                    parentid = 0,
                                    password_rule = passwordRule,
                                    password_length = pwd_length,
                                    password_upper_case = (!String.IsNullOrEmpty(Request.Form["pwd_upper_case"]) ? true : false),
                                    password_lower_case = (!String.IsNullOrEmpty(Request.Form["pwd_lower_case"]) ? true : false),
                                    password_digit = (!String.IsNullOrEmpty(Request.Form["pwd_digit"]) ? true : false),
                                    password_symbol = (!String.IsNullOrEmpty(Request.Form["pwd_symbol"]) ? true : false),
                                    password_no_name = (!String.IsNullOrEmpty(Request.Form["pwd_no_name"]) ? true : false)
                                },
                                id = 1
                            };

                            rData = JSON.Serialize2(reqAddR);
                        }
                        else
                        {
                            var reqAddR = new
                            {
                                jsonrpc = "1.0",
                                method = "context.change",
                                parameters = new
                                {
                                    contextid = contextId,
                                    name = contextName,
                                    parentid = 0,
                                    password_rule = passwordRule,
                                    password_length = pwd_length,
                                    password_upper_case = (!String.IsNullOrEmpty(Request.Form["pwd_upper_case"]) ? true : false),
                                    password_lower_case = (!String.IsNullOrEmpty(Request.Form["pwd_lower_case"]) ? true : false),
                                    password_digit = (!String.IsNullOrEmpty(Request.Form["pwd_digit"]) ? true : false),
                                    password_symbol = (!String.IsNullOrEmpty(Request.Form["pwd_symbol"]) ? true : false),
                                    password_no_name = (!String.IsNullOrEmpty(Request.Form["pwd_no_name"]) ? true : false)
                                },
                                id = 1
                            };

                            rData = JSON.Serialize2(reqAddR);
                        }

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ContextGetResult retAddR = JSON.Deserialize<ContextGetResult>(jData);
                        if (retAddR == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else if (retAddR.error != null)
                        {
                            contentRet = new WebJsonResponse("", retAddR.error.data, 3000, true);
                            //Tools.Tool.notifyException(new Exception(retAdd.error.data + retAdd.error.debug), this);
                        }
                        else if ((retAddR.result == null) || (retAddR.result.info == null))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
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