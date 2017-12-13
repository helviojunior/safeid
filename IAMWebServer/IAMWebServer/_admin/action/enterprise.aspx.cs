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
using IAM.AuthPlugins;

namespace IAMWebServer._admin.action
{
    public partial class enterprise : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;

            EnterpriseData ent = (EnterpriseData)Page.Session["enterprise_data"];

            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            String rData = "";
            ////SqlConnection //conn = DB.GetConnection();
            String jData = "";

            try
            {

                switch (action)
                {
                    case "change":

                        String name = Request.Form["name"];
                        if (String.IsNullOrEmpty(name)){
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        String auth_plugin = Request.Form["auth_plugin"];
                        if (String.IsNullOrEmpty(auth_plugin))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_auth_service"), 3000, true);
                            break;
                        }

                        AuthBase plugin = null;
                        try
                        {
                            plugin = AuthBase.GetPlugin(new Uri(auth_plugin));
                            if (plugin == null)
                                throw new Exception();
                        }
                        catch {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_auth_service"), 3000, true);
                            break;
                        }

                        Dictionary<String, String> pgValues = new Dictionary<string, string>();

                        AuthConfigFields[] fields = plugin.GetConfigFields();
                        if (fields.Length > 0)
                        {
                            WebJsonResponse err = null;

                            foreach (AuthConfigFields f in fields)
                            {
                                String value = Request.Form["f_" + f.Key];
                                if (!String.IsNullOrEmpty(value))
                                    pgValues.Add(f.Key, value);

                                if (f.Required && !pgValues.ContainsKey(f.Key))
                                {
                                    err = new WebJsonResponse("", MessageResource.GetMessage("required_field") + " " + f.Name, 3000, true);
                                    break;
                                }
                            }

                            if (err != null)
                            {
                                contentRet = err;
                                break;
                            }

                        }

                        List<String> hosts = new List<String>();
                        foreach (String key in Request.Form.Keys)
                        {
                            if (key.ToLower().IndexOf("host_") == 0)
                            {
                                String[] ht = Request.Form[key].ToString().Split(",".ToCharArray());
                                foreach (String host in ht)
                                {
                                    if (!String.IsNullOrWhiteSpace(host))
                                        hosts.Add(host);
                                }
                            }
                        }


                        var reqD = new
                        {
                            jsonrpc = "1.0",
                            method = "enterprise.change",
                            parameters = new
                            {
                                enterpriseid = ent.Id,
                                name = name,
                                auth_plugin = auth_plugin,
                                fqdn_alias = hosts.ToArray(),
                                auth_paramters = pgValues
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("enterprise_not_found"), 3000, true);
                        }
                        else if (retD.error != null)
                        {
                            contentRet = new WebJsonResponse("", retD.error.data, 3000, true);
                        }
                        else if (retD.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("enterprise_not_found"), 3000, true);
                        }
                        else if (retD.result == null || retD.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("enterprise_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/enterprise/");
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