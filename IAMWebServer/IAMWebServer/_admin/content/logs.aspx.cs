using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.WebAPI;
using System.Data.SqlClient;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;

namespace IAMWebServer._admin.content
{
    public partial class logs : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod != "POST")
                return;

            String area = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["area"]))
                area = (String)RouteData.Values["area"];

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();


            LMenu menu1 = new LMenu("Dashboard", ApplicationVirtualPath + "admin/");
            LMenu menu2 = new LMenu("Logs", ApplicationVirtualPath + "admin/logs/");
            LMenu menu3 = new LMenu("Logs de sistema", ApplicationVirtualPath + "admin/logs/");

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            HashData hashData = new HashData(this);

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está selecionado o usuário
            
            Int64 userId = 0;
            try
            {
                userId = Int64.Parse((String)RouteData.Values["id"]);

                if (userId < 0)
                    userId = 0;
            }
            catch { }

            switch (area)
            {
                case "":
                case "search":
                case "content":

                    Int32 page = 1;
                    Int32 pageSize = 20;
                    Boolean hasNext = true;

                    Int32.TryParse(Request.Form["page"], out page);

                    if (page < 1)
                        page = 1;

                    if (page == 1)
                    {
                        html += "<table class=\"sorter\"><thead>";
                        html += "    <tr>";
                        html += "        <th class=\"w50 tHide mHide {sorter: false}\"></th>";
                        html += "        <th class=\"pointer w150 tHide mHide header headerSortDown\" data-column=\"date\">Data <div class=\"icomoon\"></div></th>";
                        html += "        <th class=\"pointer w80 tHide mHide header\" data-column=\"source\">Fonte <div class=\"icomoon\"></div></th>";
                        html += "        <th class=\"pointer w100 tHide mHide header\" data-column=\"resource\">Recurso <div class=\"icomoon\"></div></th>";
                        html += "        <th class=\"{sorter: false} header\" data-column=\"text\">Texto <div class=\"icomoon\"></div></th>";
                        html += "    </tr>";
                        html += "</thead>";

                        html += "<tbody>";
                    }

                    String trTemplate = "    <tr class=\"system-log\" data-uri=\"/admin/logs/{0}/content/modal/\" data-title=\"{5}\" onclick=\"iamadmin.openLog(this);\">";
                    trTemplate += "            <td class=\"select tHide mHide\"><div class=\"level-icon level-{1}\"></div></td>";
                    trTemplate += "            <td class=\"tHide mHide\">{2}</td>";
                    trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                    trTemplate += "            <td class=\"tHide mHide\">{4}</td>";
                    trTemplate += "            <td class=\"ident10\">{5}</td>";
                    trTemplate += "    </tr>";

                    String query = "";
                    try
                    {

                        String rData = "";
                        

                        if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                            query = (String)RouteData.Values["query"];

                        if (String.IsNullOrWhiteSpace(query))
                        {
                            var tmpReq = new
                            {
                                jsonrpc = "1.0",
                                method = "logs.list",
                                parameters = new
                                {
                                    page_size = pageSize,
                                    page = page,
                                    filter = new { source = hashData.GetValue("source") }
                                },
                                id = 1
                            };

                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                        }
                        else
                        {
                            var tmpReq = new
                            {
                                jsonrpc = "1.0",
                                method = "logs.search",
                                parameters = new
                                {
                                    text = query,
                                    page_size = pageSize,
                                    page = page,
                                    filter = new { source = hashData.GetValue("source") }
                                },
                                id = 1
                            };

                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                        }

                        String jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        SystemLogs ret2 = JSON.Deserialize<SystemLogs>(jData);
                        if (ret2 == null)
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("log_not_found"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);

                            hasNext = false;
                        }
                        else if (ret2.error != null)
                        {
                            eHtml += String.Format(errorTemplate, ret2.error.data);
                            //ret = new WebJsonResponse("", ret2.error.data, 3000, true);
                            hasNext = false;
                        }
                        else if (ret2.result == null || ret2.result.Count == 0)
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("log_not_found"));
                            hasNext = false;
                        }
                        else
                        {
                            foreach (LogItem l in ret2.result)
                                html += String.Format(trTemplate, l.log_id, l.level.ToString().ToLower(), ((DateTime)new DateTime(1970, 1, 1)).AddSeconds(l.date).ToString("yyyy-MM-dd HH:mm:ss"), l.source, l.resource_name, l.text);

                            if (ret2.result.Count < pageSize)
                                hasNext = false;
                        }


                    }
                    catch (Exception ex)
                    {
                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                    }

                    if (page == 1)
                    {

                        html += "</tbody></table>";

                        html += "<span class=\"empty-results content-loading system-logs-loader hide\"></span>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                    }
                    else
                    {
                        contentRet = new WebJsonResponse("#content-wrapper tbody", (eHtml != "" ? eHtml : html), true);
                    }

                    contentRet.js = "$( document ).unbind('end_of_scroll.systemLogLoader');";

                    if (hasNext)
                        contentRet.js += "$( document ).bind( 'end_of_scroll.systemLogLoader', function() { $( document ).unbind('end_of_scroll.systemLogLoader'); $('.system-logs-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'" + (!String.IsNullOrWhiteSpace(query) ? query : "") + "' }, function(){ $('.system-logs-loader').addClass('hide'); } ); });";

                    break;

                case "modal":
                    
                    String logId = "";
                    try
                    {
                        logId = (String)RouteData.Values["id"];
                    }
                    catch { }


                    try
                    {

                        var tmpReq = new
                        {
                            jsonrpc = "1.0",
                            method = "logs.get",
                            parameters = new
                            {
                                logid = logId
                            },
                            id = 1
                        };

                        String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                        String jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);


                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        LogData retLog = JSON.Deserialize<LogData>(jData);
                        if (retLog == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("log_not_found"), 3000, true);
                        }
                        else if (retLog.error != null)
                        {
                            contentRet = new WebJsonResponse("", retLog.error.data, 3000, true);
                        }
                        else if (retLog.result == null || retLog.result.log_id == null)
                        {
                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("log_not_found"), 3000, true);
                        }
                        else
                        {
                            html = "<div class=\"log-info\">";
                            html += "<div class=\"item\">Data:</div>";
                            html += "<div class=\"description\">" + MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(retLog.result.date), false) + "</div>";
                            html += "<div class=\"item\">Fonte:</div>";
                            html += "<div class=\"description\">" + retLog.result.source + "</div>";

                            if (!String.IsNullOrEmpty(retLog.result.resource_name))
                            {
                                html += "<div class=\"item\">Recurso:</div>";
                                html += "<div class=\"description\">" + retLog.result.resource_name + "</div>";
                            }

                            if (!String.IsNullOrEmpty(retLog.result.plugin_name))
                            {
                                html += "<div class=\"item\">Plugin:</div>";
                                html += "<div class=\"description\">" + retLog.result.plugin_name + "</div>";
                            }
                            
                            html += "<div class=\"item\">Executado por:</div>";
                            html += "<div class=\"description\">" + retLog.result.executed_by_name + "</div>";
                            html += "<div class=\"item\">Texto:</div>";
                            html += "<div class=\"description\">" + retLog.result.text + "</div>";
                            html += "<div class=\"item\">Informações adicionais:</div>";
                            html += "<div class=\"description\">";
                            //verifica se é um dado Json para fazer o parse
                            try
                            {
                                html += JSON.Dump(retLog.result.additional_data, true);
                            }
                            catch
                            {
                                html += HttpUtility.HtmlEncode(retLog.result.additional_data);
                            }
                            html += "</div>";
                            html += "</div>";
                            contentRet = new WebJsonResponse("#modal-box .alert-box-content", html);        
                        }

                    }
                    catch (Exception ex)
                    {
                        Tools.Tool.notifyException(ex, this);
                        contentRet = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                    }
                    
                    break;

                case "sidebar":
                    if (menu1 != null)
                    {
                        html += "<div class=\"section-nav-header\">";
                        html += "    <div class=\"crumbs\">";
                        html += "        <div class=\"subject subject-color\">";
                        html += "            <a href=\"" + menu1.HRef + "\">" + menu1.Name + "</a>";
                        html += "        </div>";
                        if (menu2 != null)
                        {
                            html += "        <div class=\"topic topic-color\">";
                            html += "            <a href=\"" + menu2.HRef + "\">" + menu2.Name + "</a>";
                            html += "        </div>";
                        }
                        html += "    </div>";
                        if (menu3 != null)
                        {
                            html += "    <div class=\"crumbs tutorial-title\">";
                            html += "        <h2 class=\"title tutorial-color\">" + menu3.Name + "</h2>";
                            html += "    </div>";
                        }
                        html += "</div>";
                    }
                    
                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":
                    
                    html += "<select id=\"filter_source\" name=\"filter_source\" ><option value=\"\">Todas as origens</option>";
                    html += "<option value=\"source/adminapi\" " + (hashData.GetValue("source") == "adminapi" ? "selected" : "") + ">adminapi</option>";
                    html += "<option value=\"source/api\" " + (hashData.GetValue("source") == "api" ? "selected" : "") + ">api</option>";
                    html += "<option value=\"source/autoservice\" " + (hashData.GetValue("source") == "autoservice" ? "selected" : "") + ">autoservice</option>";
                    html += "<option value=\"source/cas\" " + (hashData.GetValue("source") == "cas" ? "selected" : "") + ">cas</option>";
                    html += "<option value=\"source/deploy\" " + (hashData.GetValue("source") == "deploy" ? "selected" : "") + ">deploy</option>";
                    html += "<option value=\"source/engine\" " + (hashData.GetValue("source") == "engine" ? "selected" : "") + ">engine</option>";
                    html += "<option value=\"source/inbound\" " + (hashData.GetValue("source") == "inbound" ? "selected" : "") + ">inbound</option>";
                    html += "<option value=\"source/proxy\" " + (hashData.GetValue("source") == "proxy" ? "selected" : "") + ">proxy</option>";
                    html += "<option value=\"source/proxyapi\" " + (hashData.GetValue("source") == "proxyapi" ? "selected" : "") + ">proxyapi</option>";
                    html += "</select>";
                    
                    contentRet = new WebJsonResponse("#btnbox", html);
                    contentRet.js = "$('#filter_source').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                    break;
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