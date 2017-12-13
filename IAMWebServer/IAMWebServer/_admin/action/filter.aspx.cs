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
using IAM.Filters;

namespace IAMWebServer._admin.action
{
    public partial class filter : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse contentRet = null;


            String action = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["action"]))
                action = (String)RouteData.Values["action"];

            Int64 filterId = 0;
            if (action != "add_filter")
            {
                try
                {
                    filterId = Int64.Parse((String)RouteData.Values["id"]);

                    if (filterId < 0)
                        filterId = 0;
                }
                catch { }

                if (filterId == 0)
                {
                    contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
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
                            method = "filter.delete",
                            parameters = new
                            {
                                filterid = filterId
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
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else if (retDel.error != null)
                        {
                            contentRet = new WebJsonResponse("", retDel.error.data, 3000, true);
                        }
                        else if (!retDel.result)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse();
                        }
                        break;

                    case "add_filter":
                        
                        contentRet = null;
                        FilterRule newItem = GetFilterByForm(out contentRet);
                        if ((contentRet != null) || (newItem == null))
                            break;
                        
                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "filter.new",
                            parameters = newItem.ToJsonObject(),
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        FilterGetResult retN = JSON.Deserialize<FilterGetResult>(jData);
                        if (retN == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else if (retN.error != null)
                        {
                            contentRet = new WebJsonResponse("", retN.error.data, 3000, true);
                        }
                        else if (retN.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else if (retN.result == null || retN.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/filter/" + retN.result.info.filter_id + "/");
                        }
                        break;

                    case "change":
                        
                        contentRet = null;
                        FilterRule newItem1 = GetFilterByForm(out contentRet);
                        if ((contentRet != null) || (newItem1 == null))
                            break;

                        Dictionary<String, Object> par = newItem1.ToJsonObject();
                        par.Add("filterid", filterId);

                        rData = JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "filter.change",
                            parameters = par,
                            id = 1
                        });

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        FilterGetResult retC = JSON.Deserialize<FilterGetResult>(jData);
                        if (retC == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else if (retC.error != null)
                        {
                            contentRet = new WebJsonResponse("", retC.error.data, 3000, true);
                        }
                        else if (retC.result == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else if (retC.result == null || retC.result.info == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("filter_not_found"), 3000, true);
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("info_saved"), 3000, false);
                            contentRet.redirectURL = Session["ApplicationVirtualPath"] + "admin/filter/" + retC.result.info.filter_id + "/";
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

        private FilterRule GetFilterByForm(out WebJsonResponse contentRet)
        {
            contentRet = null;

            String name = Request.Form["filter_name"];
            if (String.IsNullOrEmpty(name))
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                return null;
            }

            String filters = Request.Form["filter_name"];
            if (String.IsNullOrEmpty(name))
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("invalid_name"), 3000, true);
                return null;
            }

            //Verifica e trata as regras

            if (String.IsNullOrEmpty(Request.Form["filter_id"]))
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("conditions_is_empty"), 3000, true);
                return null;
            }

            String rData = JSON.Serialize2(new
            {
                jsonrpc = "1.0",
                method = "field.list",
                parameters = new
                {
                    page_size = Int32.MaxValue
                },
                id = 1
            });
            //SqlConnection //conn = DB.GetConnection();
            String jData = "";
            try
            {
                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString())) jData = WebPageAPI.ExecuteLocal(database, this, rData);
            }
            finally
            {
            }

            List<FieldData> fieldList = new List<FieldData>();
            FieldListResult flR = JSON.Deserialize<FieldListResult>(jData);
            if ((flR != null) && (flR.error == null) && (flR.result != null))
            {
                fieldList = flR.result;
            }

            if (fieldList.Count == 0)
            {
                contentRet = new WebJsonResponse("", MessageResource.GetMessage("field_not_found"), 3000, true);
                return null;
            }

            FilterRule newItem = new FilterRule(name);


            String[] fIds = Request.Form["filter_id"].Split(",".ToCharArray());
            foreach (String fid in fIds)
            {
                if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_group"]))
                {
                    contentRet = new WebJsonResponse("", "Grupo não localizado na condição " + fid, 3000, true);
                    break;
                }

                if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_field_id"]))
                {
                    contentRet = new WebJsonResponse("", "ID do campo não localizado na condição " + fid, 3000, true);
                    break;
                }

                String fGroup = Request.Form["filter_" + fid + "_group"].Split(",".ToCharArray())[0];

                FieldData fieldData = null;
                try
                {
                    Int64 tmp = Int64.Parse(Request.Form["filter_" + fid + "_field_id"].Split(",".ToCharArray())[0]);
                    foreach (FieldData fd in fieldList)
                        if (fd.field_id == tmp)
                            fieldData = fd;

                    if (fieldData == null)
                        throw new Exception();
                }
                catch
                {
                    contentRet = new WebJsonResponse("", "ID do campo inválido na condição " + fid, 3000, true);
                    break;
                }

                FilterSelector filterSelector = FilterSelector.AND;

                if (!String.IsNullOrEmpty(Request.Form["filter_" + fid + "_selector"]))
                {
                    switch (Request.Form["filter_" + fid + "_selector"].Split(",".ToCharArray())[0].ToLower())
                    {
                        case "or":
                            filterSelector = FilterSelector.OR;
                            break;

                        default:
                            filterSelector = FilterSelector.AND;
                            break;
                    }
                }

                FilterConditionType condition = FilterConditionType.Equal;
                switch (fieldData.data_type)
                {
                    case "numeric":
                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_condition_numeric"]))
                        {
                            contentRet = new WebJsonResponse("", "Condição de comparação não localizada na condição " + fid, 3000, true);
                            break;
                        }

                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_text_numeric"]))
                        {
                            contentRet = new WebJsonResponse("", "Valor não localizado na condição " + fid, 3000, true);
                            break;
                        }

                        Int64 nValue = 0;
                        try
                        {
                            nValue = Int64.Parse(Request.Form["filter_" + fid + "_text_numeric"].Split(",".ToCharArray())[0]);
                        }
                        catch
                        {
                            contentRet = new WebJsonResponse("", "Valor inválido na condição " + fid, 3000, true);
                            break;
                        }

                        String c1 = Request.Form["filter_" + fid + "_condition_numeric"].Split(",".ToCharArray())[0].ToLower();
                        foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.Numeric))
                            if (c1 == ft.ToString().ToLower())
                                condition = ft;

                        newItem.AddCondition(fGroup, FilterSelector.AND, fieldData.field_id, "", DataType.Numeric, nValue.ToString(), condition, filterSelector);

                        break;

                    case "datetime":
                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_condition_datetime"]))
                        {
                            contentRet = new WebJsonResponse("", "Condição de comparação não localizada na condição " + fid, 3000, true);
                            break;
                        }

                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_text_date"]))
                        {
                            contentRet = new WebJsonResponse("", "Valor de data não localizado na condição " + fid, 3000, true);
                            break;
                        }

                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_text_time"]))
                        {
                            contentRet = new WebJsonResponse("", "Valor de hora não localizado na condição " + fid, 3000, true);
                            break;
                        }

                        DateTime dtValue = new DateTime(1970, 1, 1);
                        try
                        {
                            dtValue = DateTime.Parse(Request.Form["filter_" + fid + "_text_date"].Split(",".ToCharArray())[0] + " " + Request.Form["filter_" + fid + "_text_time"].Split(",".ToCharArray())[0]);
                        }
                        catch
                        {
                            contentRet = new WebJsonResponse("", "Valor de data e hora inválidos na condição " + fid, 3000, true);
                            break;
                        }

                        String c2 = Request.Form["filter_" + fid + "_condition_datetime"].Split(",".ToCharArray())[0].ToLower();
                        foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.DateTime))
                            if (c2 == ft.ToString().ToLower())
                                condition = ft;

                        newItem.AddCondition(fGroup, FilterSelector.AND, fieldData.field_id, "", DataType.DateTime, dtValue.ToString("o"), condition, filterSelector);

                        break;

                    default:
                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_condition_string"]))
                        {
                            contentRet = new WebJsonResponse("", "Condição de comparação não localizada na condição " + fid, 3000, true);
                            break;
                        }

                        if (String.IsNullOrEmpty(Request.Form["filter_" + fid + "_text_string"]))
                        {
                            contentRet = new WebJsonResponse("", "Valor não localizado na condição " + fid, 3000, true);
                            break;
                        }


                        String c3 = Request.Form["filter_" + fid + "_condition_string"].Split(",".ToCharArray())[0].ToLower();
                        foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.Text))
                            if (c3 == ft.ToString().ToLower())
                                condition = ft;

                        newItem.AddCondition(fGroup, FilterSelector.AND, fieldData.field_id, "", DataType.Text, Request.Form["filter_" + fid + "_text_string"].Split(",".ToCharArray())[0], condition, filterSelector);
                        break;
                }
            }

            //Atualiza os seletores dos grupos caso haja mais de 1 grupo
            if (newItem.FilterGroups.Count > 1)
            {
                foreach (FilterGroup g in newItem.FilterGroups)
                {

                    if (!String.IsNullOrEmpty(Request.Form["group_" + g.GroupId + "_selector"]))
                    {
                        switch (Request.Form["group_" + g.GroupId + "_selector"].Split(",".ToCharArray())[0].ToLower())
                        {
                            case "and":
                                g.Selector = FilterSelector.AND;
                                break;

                            default:
                                g.Selector = FilterSelector.OR;
                                break;
                        }
                    }

                }
            }

            return newItem;
        }
    }
}