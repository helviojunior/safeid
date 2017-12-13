using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.WebAPI;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;
using System.Globalization;
using System.Threading;

namespace IAMWebServer._admin.content
{
    public partial class access_request : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod != "POST")
                return;

            String area = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["area"]))
                area = (String)RouteData.Values["area"];

            Int64 enterpriseId = 0;
            if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            LMenu menu1 = new LMenu("Dashboard", ApplicationVirtualPath + "admin/");
            LMenu menu2 = new LMenu("Requisições de acesso", ApplicationVirtualPath + "admin/access_request/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Requisições de acesso", ApplicationVirtualPath + "admin/access_request/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 requestId = 0;
            try
            {
                requestId = Int64.Parse((String)RouteData.Values["id"]);

                if (requestId < 0)
                    requestId = 0;
            }
            catch { }

            String error = "";
            WorkflowRequestGetResult selectedRequest = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((requestId > 0) && (area.ToLower() != "search"))
            {

                try
                {

                    String rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "workflow.getrequest",
                        parameters = new
                        {
                            requestid = requestId
                        },
                        id = 1
                    });

                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    selectedRequest = JSON.Deserialize<WorkflowRequestGetResult>(jData);
                    if (selectedRequest == null)
                    {
                        error = MessageResource.GetMessage("workflow_request_not_found");
                    }
                    else if (selectedRequest.error != null)
                    {
                        error = selectedRequest.error.data;
                        selectedRequest = null;
                    }
                    else if (selectedRequest.result == null || selectedRequest.result.info == null)
                    {
                        error = MessageResource.GetMessage("workflow_request_not_found");
                        selectedRequest = null;
                    }
                    else
                    {
                        menu3.Name = selectedRequest.result.info.workflow.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedRequest = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }

                
            }

            switch (area)
            {
                case "":
                case "search":
                case "content":
                    if (selectedRequest == null)//Listagem
                    {

                        Int32 page = 1;
                        Int32 pageSize = 30;
                        Boolean hasNext = true;

                        Int32.TryParse(Request.Form["page"], out page);

                        if (page < 1)
                            page = 1;

                        if (page == 1)
                        {
                            html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                            html += "    <tr>";
                            html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                            html += "        <th class=\"pointer w150 header headerSortDown\" data-column=\"name\">Data <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Workflow <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"last_login\">Usuário <div class=\"icomoon\"></div></th>";
                            html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                            html += "    </tr>";
                            html += "</thead>";

                            html += "<tbody>";
                        }

                        String trTemplate = "    <tr class=\"access_request\" id=\"request_{0}\" data-login=\"{2}\" data-request=\"{0}\" data-userid=\"{1}\" data-href=\"" + ApplicationVirtualPath + "admin/access_request/{0}/\">";
                        trTemplate += "            <td class=\"pointer select mHide\"><div class=\"checkbox\"></div></td>";
                        trTemplate += "            <td class=\"pointer\">{3}</td>";
                        trTemplate += "            <td class=\"pointer tHide mHide\">{4}</td>";
                        trTemplate += "            <td class=\"pointer tHide mHide\">{5}</td>";
                        trTemplate += "            <td class=\"pointer tHide mHide\">{6}</td>";
                        trTemplate += "    </tr>";

                        try
                        {

                            String rData = "";
                            String query = "";


                            Int32 status = (Int32)IAM.Workflow.WorkflowRequestStatus.Waiting;
                            try
                            {
                                status = Int32.Parse(hashData.GetValue("status"));
                            }
                            catch { }


                            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                query = (String)RouteData.Values["query"];

                            if (String.IsNullOrWhiteSpace(query) && !String.IsNullOrWhiteSpace(hashData.GetValue("query")))
                                query = hashData.GetValue("query");



                            if (String.IsNullOrWhiteSpace(query))
                            {
                                rData = SafeTrend.Json.JSON.Serialize2(new
                                {
                                    jsonrpc = "1.0",
                                    method = "workflow.accessrequestlist",
                                    parameters = new
                                    {
                                        text = query,
                                        page_size = pageSize,
                                        page = page,
                                        filter = new { status = status, contextid = hashData.GetValue("context"), workflowid = hashData.GetValue("workflow") }
                                    },
                                    id = 1
                                });
                            }
                            else
                            {
                                rData = SafeTrend.Json.JSON.Serialize2(new
                                {
                                    jsonrpc = "1.0",
                                    method = "role.accessrequestsearch",
                                    parameters = new
                                    {
                                        text = query,
                                        page_size = pageSize,
                                        page = page,
                                        filter = new { status = status, contextid = hashData.GetValue("context"), workflowid = hashData.GetValue("workflow") }
                                    },
                                    id = 1
                                });
                            }
                      

                            String jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                            if (String.IsNullOrWhiteSpace(jData))
                                throw new Exception("");

                            WorkflowRequestListResult ret2 = JSON.Deserialize<WorkflowRequestListResult>(jData);
                            if (ret2 == null)
                            {
                                eHtml = String.Format(errorTemplate, MessageResource.GetMessage("access_request_none"));
                                hasNext = false;
                            }
                            else if (ret2.error != null)
                            {
                                eHtml = String.Format(errorTemplate, ret2.error.data);
#if DEBUG
                                eHtml += String.Format(errorTemplate, ret2.error.data + ret2.error.debug);
#endif
                                hasNext = false;
                            }
                            else if (ret2.result == null || (ret2.result.Count == 0 && page == 1))
                            {
                                eHtml = String.Format(errorTemplate, MessageResource.GetMessage("access_request_none"));
                                hasNext = false;
                            }
                            else
                            {
                                foreach (WorkflowRequestData request in ret2.result)
                                {


                                    String actions = "";
                                    switch ((IAM.Workflow.WorkflowRequestStatus)request.status)
                                    {
                                        case IAM.Workflow.WorkflowRequestStatus.Waiting:
                                        case IAM.Workflow.WorkflowRequestStatus.Escalated:
                                        case IAM.Workflow.WorkflowRequestStatus.Expired:
                                        case IAM.Workflow.WorkflowRequestStatus.UnderReview:
                                            actions += "<div class=\"a-btn blue data-action no-reload\" data-action=\"" + ApplicationVirtualPath + "admin/access_request/" + request.access_request_id + "/action/allow/\">Aprovar</div>";
                                            actions += "&nbsp;<button href=\"" + ApplicationVirtualPath + "admin/access_request/" + request.access_request_id + "/action/deny/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn red confirm-action no-reload\" confirm-title=\"Negar acesso\" confirm-text=\"Deseja negar o acesso do usuário?\" ok=\"Negar\" cancel=\"Cancelar\">Negar</button>";
                                            break;

                                        case IAM.Workflow.WorkflowRequestStatus.Approved:
                                            actions += "<button href=\"" + ApplicationVirtualPath + "admin/access_request/" + request.access_request_id + "/action/revoke/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn red confirm-action no-reload\" confirm-title=\"Revogar acesso\" confirm-text=\"Deseja revogar o acesso do usuário?\" ok=\"Revogar\" cancel=\"Cancelar\">Revogar</button>";
                                            break;
                                    }

                                    html += String.Format(trTemplate,
                                        request.access_request_id,
                                        request.entity_id,
                                        request.entity_login,
                                        (request.create_date > 0 ? MessageResource.FormatDate(new DateTime(1070, 1, 1).AddSeconds(request.create_date), false) : ""),
                                        request.workflow.name,
                                        request.entity_full_name,
                                        actions
                                        );

                                }

                                if (ret2.result.Count < pageSize)
                                    hasNext = false;
                            }

                        }
                        catch (Exception ex)
                        {
                            eHtml = String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                        }

                        if (page == 1)
                        {

                            html += "</tbody></table>";

                            html += "<span class=\"empty-results content-loading user-list-loader hide\"></span>";

                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#content-wrapper tbody", (eHtml != "" ? eHtml : html), true);
                        }

                        contentRet.js = "$( document ).unbind('end_of_scroll.loader_access_request');";

                        if (hasNext)
                            contentRet.js += "$( document ).bind( 'end_of_scroll.loader_access_request', function() { $( document ).unbind('end_of_scroll.loader_access_request'); $('.user-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'' }, function(){ $('.user-list-loader').addClass('hide'); } ); });";

                    }
                    else//Esta sendo selecionado a requisição
                    {
                        if (error != "")
                        {
                            contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                        }
                        else
                        {
                            String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

                            switch (filter)
                            {

                                case "":
                                case "allow":
                                case "deny":
                                    html += "<h3>" + selectedRequest.result.info.workflow.name + "</h3>";
                                    html += "<div class=\"no-tabs fields\"><table><tbody>";

                                    html += String.Format(infoTemplate, "Acesso", "<span class=\"no-edit\">" + selectedRequest.result.info.workflow.name + "<span class=\"description\">" + selectedRequest.result.info.workflow.description + "</span></span>");

                                    html += String.Format(infoTemplate, "Último status", MessageResource.GetMessage("wf_" + ((IAM.Workflow.WorkflowRequestStatus)selectedRequest.result.info.status).ToString().ToLower()));

                                    switch (((IAM.Workflow.WorkflowRequestStatus)selectedRequest.result.info.status))
                                    {
                                        case IAM.Workflow.WorkflowRequestStatus.Approved:
                                        case IAM.Workflow.WorkflowRequestStatus.Deny:
                                            //html += String.Format(infoTemplate, "Último status", MessageResource.GetMessage("wf_" + ((IAM.Workflow.WorkflowRequestStatus)selectedRequest.result.info.status).ToString().ToLower()));
                                            break;
                                    }


                                    html += String.Format(infoTemplate, "Data da requisição", (new DateTime(1970, 1, 1).AddSeconds(selectedRequest.result.info.create_date)).ToString("yyyy-MM-dd HH:mm:ss"));

                                    html += String.Format(infoTemplate, "Descrição da necessidade do acesso", selectedRequest.result.info.description);

                                    //html += String.Format(infoTemplate, "", "<span type=\"submit\" id=\"cancel\" class=\"button secondary floatleft red\">Cancelar requisição</span>");

                                    html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                    html += "<h3>Todos os status</h3>";

                                    html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                    html += "    <tr>";
                                    html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                    html += "        <th class=\"pointer w150 header headerSortDown\" data-column=\"name\">Data <div class=\"icomoon\"></div></th>";
                                    html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"status\">Status <div class=\"icomoon\"></div></th>";
                                    html += "        <th class=\"pointer tHide mHide header {sorter: false}\" data-column=\"create_date\">Descrição <div class=\"icomoon\"></div></th>";
                                    html += "    </tr>";
                                    html += "</thead>";

                                    html += "<tbody>";

                                    String trTemplate = "    <tr class=\"request\" data-userid=\"{0}\">";
                                    trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                    trTemplate += "            <td class=\"\">{1}</td>";
                                    trTemplate += "            <td class=\"tHide mHide\">{2}</td>";
                                    trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                                    trTemplate += "    </tr>";

                                    /*
                                    DataTable drRequestStatus = database.ExecuteDataTable("select * from st_workflow_request_status r with(nolock) where r.workflow_request_id = " + drRequest.Rows[0]["id"] + " order by date desc");
                                    if ((drRequestStatus != null) && (drRequestStatus.Rows.Count > 0))
                                    {

                                        foreach (DataRow dr in drRequestStatus.Rows)
                                        {
                                            try
                                            {
                                                html += String.Format(trTemplate, dr["id"], ((DateTime)dr["date"]).ToString("yyyy-MM-dd HH:mm:ss"), dr["status"], dr["description"]);
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                        }
                                    }*/

                                    break;
                            }

                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        }
                    }


                    break;

                case "sidebar":
                    if (menu1 != null)
                    {
                        html += "<div class=\"sep\"><div class=\"section-nav-header\">";
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
                        html += "</div></div>";
                    }

                    
                    if (selectedRequest != null)
                    {
                        switch ((IAM.Workflow.WorkflowRequestStatus)selectedRequest.result.info.status)
                        {
                            case IAM.Workflow.WorkflowRequestStatus.Waiting:
                            case IAM.Workflow.WorkflowRequestStatus.Escalated:
                            case IAM.Workflow.WorkflowRequestStatus.Expired:
                            case IAM.Workflow.WorkflowRequestStatus.UnderReview:
                                html += "<div class=\"sep\"><button class=\"a-btn-big a-btn data-action\" type=\"button\" data-action=\"" + ApplicationVirtualPath + "admin/access_request/" + selectedRequest.result.info.access_request_id + "/action/allow/\">Aprovar requisição</button></div>";
                                html += "<div class=\"sep\"><button class=\"a-btn-big a-btn red confirm-action\" type=\"button\" href=\"" + ApplicationVirtualPath + "admin/access_request/" + selectedRequest.result.info.access_request_id + "/action/deny/\" confirm-title=\"Negar acesso\" confirm-text=\"Deseja negar o acesso do usuário?\" ok=\"Negar\" cancel=\"Cancelar\">Negar acesso</button></div>";
                                break;

                            case IAM.Workflow.WorkflowRequestStatus.Approved:
                                html += "<div class=\"sep\"><button class=\"a-btn-big a-btn red confirm-action\" type=\"button\" href=\"" + ApplicationVirtualPath + "admin/access_request/" + selectedRequest.result.info.access_request_id + "/action/revoke/\" confirm-title=\"Revogar acesso\" confirm-text=\"Deseja revogar o acesso do usuário?\" ok=\"Revogar\" cancel=\"Cancelar\">Revogar acesso</button></div>";
                                break;
                        }
                    }

                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":

                    if (selectedRequest == null){

                        switch (filter)
                        {
                            case "":

                                try
                                {

                                    var tmpReq = new
                                    {
                                        jsonrpc = "1.0",
                                        method = "context.list",
                                        parameters = new { },
                                        id = 1
                                    };

                                    error = "";
                                    String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                                    String jData = "";
                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                                    if (String.IsNullOrWhiteSpace(jData))
                                        throw new Exception("");

                                    ContextListResult contextList = JSON.Deserialize<ContextListResult>(jData);
                                    if (contextList == null)
                                    {
                                        error = MessageResource.GetMessage("context_not_found");
                                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                    }
                                    else if (contextList.error != null)
                                    {
                                        error = contextList.error.data;
                                        selectedRequest = null;
                                    }
                                    else if (contextList.result == null)
                                    {
                                        error = MessageResource.GetMessage("context_not_found");
                                        selectedRequest = null;
                                    }
                                    else
                                    {

                                        html += "<select id=\"filter_context\" name=\"filter_context\" ><option value=\"\">Todos os contextos</option>";
                                        foreach (ContextData c in contextList.result)
                                            html += "<option value=\"context/" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                        html += "</select>";
                                        //contentRet = new WebJsonResponse("#btnbox", html);
                                        js += "$('#filter_context').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                                    }

                                }
                                catch (Exception ex)
                                {
                                    error = MessageResource.GetMessage("api_error");
                                }

                                Int32 status = 0;
                                try
                                {
                                    status = Int32.Parse(hashData.GetValue("status"));
                                }
                                catch { }

                                html += "<select id=\"filter_status\" name=\"filter_status\" >";

                                foreach (IAM.Workflow.WorkflowRequestStatus st in (IAM.Workflow.WorkflowRequestStatus[])Enum.GetValues(typeof(IAM.Workflow.WorkflowRequestStatus)))
                                    html += "<option value=\"status/" + (Int32)st + "\" " + (status == (Int32)st ? "selected" : "") + ">" + MessageResource.GetMessage("wf_" + st.ToString().ToLower(), st.ToString()) + "</option>";

                                html += "</select>";

                                js += "$('#filter_status').change(function() { iamadmin.changeHash( $( this ).val() ); });";

                                contentRet = new WebJsonResponse("#btnbox", html);
                                contentRet.js = js;

                                break;
                        }
                    }
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