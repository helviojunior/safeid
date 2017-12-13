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
    public partial class field : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 fieldId = 0;
            if (action != "add_field")
            {
                try
                {
                    fieldId = Int64.Parse((String)RouteData.Values["id"]);

                    if (fieldId < 0)
                        fieldId = 0;
                }
                catch { }

                if (fieldId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
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
                            method = "field.delete",
                            parameters = new
                            {
                                fieldid = fieldId
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqDel);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        FieldDeleteResult retDel = JSON.Deserialize<FieldDeleteResult>(jData);
                        if (retDel == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "add_field":

                        String name = Request.Form["field_name"];
                        if (String.IsNullOrEmpty(name))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        String data_type = Request.Form["data_type"];
                        if (String.IsNullOrEmpty(data_type))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_data_type"), 3000, true);
                            break;
                        }


                        var reqN = new
                        {
                            jsonrpc = "1.0",
                            method = "field.new",
                            parameters = new
                            {
                                name = name,
                                data_type = data_type,
                                public_field = (!String.IsNullOrEmpty(Request.Form["public"]) ? true : false),
                                user_field = (!String.IsNullOrEmpty(Request.Form["user"]) ? true : false)
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqN);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        FieldGetResult retN = JSON.Deserialize<FieldGetResult>(jData);
                        if (retN == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else if (retN.error != null)
                        {
                            contentRet = new WebJsonResponse("", retN.error.data, 3000, true);
                        }
                        else if (retN.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else if (retN.result == null || retN.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/field/" + retN.result.info.field_id + "/");
                        }
                        break;

                    case "change":

                        String name1 = Request.Form["name"];
                        if (String.IsNullOrEmpty(name1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                            break;
                        }

                        String data_type1 = Request.Form["data_type"];
                        if (String.IsNullOrEmpty(data_type1))
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("select_data_type"), 3000, true);
                            break;
                        }


                        var reqC = new
                        {
                            jsonrpc = "1.0",
                            method = "field.change",
                            parameters = new
                            {
                                fieldid = fieldId,
                                name = name1,
                                data_type = data_type1,
                                public_field = (!String.IsNullOrEmpty(Request.Form["public"]) ? true : false),
                                user_field = (!String.IsNullOrEmpty(Request.Form["user"]) ? true : false)
                            },
                            id = 1
                        };

                        rData = JSON.Serialize2(reqC);

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        FieldGetResult retC = JSON.Deserialize<FieldGetResult>(jData);
                        if (retC == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else if (retC.error != null)
                        {
                            contentRet = new WebJsonResponse("", retC.error.data, 3000, true);
                        }
                        else if (retC.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else if (retC.result == null || retC.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/field/" + retC.result.info.field_id + "/");
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