using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.WebAPI;
using System.Data;
using SafeTrend.Data;
using IAM.Workflow;
using IAM.GlobalDefs.WebApi;
using SafeTrend.Json;


namespace IAMWebServer._autoservice
{
    public partial class access_request : System.Web.UI.Page
    {
        public LMenu menu1, menu2, menu3;
        public LoginData login;
        public String subtitle;
        protected void Page_Load(object sender, EventArgs e)
        {
            MAutoservice mClass = ((MAutoservice)this.Master);

            menu1 = menu2 = menu3 = null;

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            menu1 = new LMenu("Home", ApplicationVirtualPath + "autoservice/");
            menu3 = new LMenu("Requisição de acesso", ApplicationVirtualPath + "autoservice/access_request/");

            login = LoginUser.LogedUser(this.Page);

            if (login == null)
            {
                Session["last_page"] = Request.ServerVariables["PATH_INFO"];
                Response.Redirect("/login/");
            }

            String action = "";
            if (RouteData.Values["action"] != null)
                action = RouteData.Values["action"].ToString().ToLower();

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";
            String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";


            String html = "";
            String eHtml = "";
            String js = "";
            String rData = "";
            String jData = "";


            String sideHTML = "";


            if (action != "new")
                sideHTML += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "autoservice/access_request/new/'\">Nova requisição</button></div>";


            //Verifica se está selecionado o usuário

            switch (action)
            {
                case "new":
                    subtitle = "Nova requisição de acesso";

                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {

                        //Busca todos os workflows disponíveis no mesmo contexto do usuário atual que esteja habilitado
                        DataTable dtWorkflow = database.ExecuteDataTable("select w.* from st_workflow w with(nolock) inner join context c with(nolock) on c.id = w.context_id inner join entity e with(nolock) on e.context_id = c.id where w.enabled = 1 and w.deprecated = 0 and e.id = " + login.Id + " order by w.name");
                        if ((dtWorkflow == null) || (dtWorkflow.Rows.Count == 0))
                        {
                            eHtml += String.Format(errorTemplate, "Nenhuma acesso disponível para solicitação");
                        }
                        else
                        {
                            js += "<script type=\"text/javascript\">";
                            js += "$( document ).ready(function() {";
                            js += "     $('#workflow').change(function() {";
                            js += "        $('#desc_text').html('');";
                            js += "        $('#desc_text').html( $('option:selected', this ).attr('description') );";
                            js += "     });";
                            js += "});";
                            js += "</script>";


                            html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "autoservice/access_request/action/add_request/\">";
                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                            String select = "<select id=\"workflow\" name=\"workflow\" ><option value=\"\"></option>";
                            foreach (DataRow dr in dtWorkflow.Rows)
                                select += "<option value=\"" + dr["id"] + "\" description=\"" + HttpUtility.HtmlEncode(dr["description"]) + "\">" + dr["name"] + "</option>";
                            select += "</select><span id=\"desc_text\" class=\"description\" style=\"padding: 5px 0 0 0;\"></span>";

                            html += String.Format(infoTemplate, "Acesso", select);

                            html += String.Format(infoTemplate, "Descrição da necessidade do acesso", "<textarea id=\"description\" name=\"description\" rows=\"5\" placeholder=\"Digite a justificativa para necessidade de acesso\"></textarea>");

                            html += "</tbody></table><div class=\"clear-block\"></div></div>";

                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "autoservice/access_request/\" class=\"button link floatleft\">Cancelar</a></form>";
                        }
                    }
                    break;

                default:


                    Int64 id = 0;
                    try
                    {
                        id = Int64.Parse((String)RouteData.Values["id"]);

                        if (id < 0)
                            id = 0;
                    }
                    catch { }

                    if (id > 0)
                    {
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {

                            subtitle = "Requisição de acesso";

                            DataTable drRequest = database.ExecuteDataTable("select * from st_workflow_request r with(nolock) where r.id = " + id);
                            if ((drRequest != null) && (drRequest.Rows.Count > 0))
                            {

                                WorkflowConfig workflow = new WorkflowConfig();
                                workflow.GetDatabaseData(database, (Int64)drRequest.Rows[0]["workflow_id"]);

                                WorkflowRequestStatus status = (WorkflowRequestStatus)((Int32)drRequest.Rows[0]["status"]);

                                DataTable drRequestStatus = database.ExecuteDataTable("select r.*, a.name activity_name from st_workflow_request_status r with(nolock) inner join st_workflow_activity a with(nolock) on r.activity_id = a.id where r.workflow_request_id = " + drRequest.Rows[0]["id"] + " order by date desc");
                                DataTable drActivity = database.ExecuteDataTable("select * from st_workflow_activity a with(nolock) where a.workflow_id = " + workflow.WorkflowId + " order by a.execution_order");

                                //html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "autoservice/access_request/action/add_request/\">";
                                html += "<div class=\"no-tabs fields\"><table><tbody>";

                                html += String.Format(infoTemplate, "Acesso", "<span class=\"no-edit\">" + workflow.Name + "<span class=\"description\">" + workflow.Description + "</span></span>");

                                html += String.Format(infoTemplate, "Último status", MessageResource.GetMessage("wf_" + status.ToString().ToLower()));

                                html += String.Format(infoTemplate, "Data da requisição", MessageResource.FormatDate((DateTime)drRequest.Rows[0]["create_date"], false));

                                html += String.Format(infoTemplate, "Descrição da necessidade do acesso", drRequest.Rows[0]["description"].ToString());

                                //html += String.Format(infoTemplate, "", "<span type=\"submit\" id=\"cancel\" class=\"button secondary floatleft red\">Cancelar requisição</span>");


                                //sideHTML += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "autoservice/access_request/new/'\">Nova requisição</button></div>";


                                html += "</tbody></table><div class=\"clear-block\"></div></div>";


                                html += "<h3>Passos de aprovação</h3>";

                                html += "<div class=\"sep\"><table id=\"users-table\" class=\"sorter\"><thead>";
                                html += "    <tr>";
                                html += "        <th class=\"pointer w80 header headerSortDown\" data-column=\"name\">Passo <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Atividade <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"status\">Último status <div class=\"icomoon\"></div></th>";
                                html += "    </tr>";
                                html += "</thead>";

                                html += "<tbody>";


                                String trTemplate = "    <tr class=\"request\" data-userid=\"{0}\">";
                                trTemplate += "            <td class=\"ident10\">{1}</td>";
                                trTemplate += "            <td class=\"tHide mHide\">{2}</td>";
                                trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                                trTemplate += "    </tr>";

                                Int32 step = 1;
                                if ((drActivity != null) && (drActivity.Rows.Count > 0))
                                {
                                    foreach (DataRow dr in drActivity.Rows)
                                    {
                                        String st = "";
                                        DateTime last = new DateTime(1970,1,1);

                                        if ((drRequestStatus != null) && (drRequestStatus.Rows.Count > 0))
                                            foreach (DataRow drSt in drRequestStatus.Rows)
                                                if (drSt["activity_id"].ToString() == dr["id"].ToString())
                                                    if (last.CompareTo((DateTime)drSt["date"]) < 0)
                                                    {
                                                        last = (DateTime)drSt["date"];
                                                        st = MessageResource.GetMessage("wf_" + ((WorkflowRequestStatus)((Int32)drSt["status"])).ToString().ToLower());
                                                    }

                                        if (st == "")
                                            st = "Aguardando aprovação da atividade anterior";

                                        html += String.Format(trTemplate, dr["id"], step++, dr["name"], st);
                                    }
                                }

                                html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                html += "<h3>Todos os status</h3>";

                                html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                html += "    <tr>";
                                html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                html += "        <th class=\"pointer w150 header headerSortDown\" data-column=\"name\">Data <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"status\">Status <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header {sorter: false}\" data-column=\"create_date\">Atividade <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header {sorter: false}\" data-column=\"create_date\">Descrição <div class=\"icomoon\"></div></th>";
                                html += "    </tr>";
                                html += "</thead>";

                                html += "<tbody>";

                                trTemplate = "    <tr class=\"request\" data-userid=\"{0}\">";
                                trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                trTemplate += "            <td class=\"\">{1}</td>";
                                trTemplate += "            <td class=\"tHide mHide\">{2}</td>";
                                trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                                trTemplate += "            <td class=\"tHide mHide\">{4}</td>";
                                trTemplate += "    </tr>";


                                
                                if ((drRequestStatus != null) && (drRequestStatus.Rows.Count > 0))
                                {

                                    foreach (DataRow dr in drRequestStatus.Rows)
                                    {
                                        try
                                        {
                                            html += String.Format(trTemplate, dr["id"], MessageResource.FormatDate((DateTime)dr["date"], false), MessageResource.GetMessage("wf_" + ((WorkflowRequestStatus)((Int32)dr["status"])).ToString().ToLower()), dr["activity_name"], dr["description"]);
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                    }
                                }

                                html += "</tbody></table>";

                                //html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "autoservice/access_request/\" class=\"button link floatleft\">Cancelar</a></form>";
                            }
                            else
                            {
                                eHtml += String.Format(errorTemplate, "Requisição não encontrada");
                            }
                        }
                    }
                    else //Request não selecionado
                    {
                        subtitle = "Requisição de acesso";

                        js += "<script type=\"text/javascript\">";
                        js += "$( document ).ready(function() {";
                        js += "    $('table tbody tr').each(function (index, element) {";
                        js += "        if ($(this).attr('data-href')) {";
                        js += "            $(this).unbind('click');";
                        js += "            $(this).click(function (event) {";
                        js += "                event.preventDefault();";
                        js += "                window.location = $(this).attr('data-href');";
                        js += "            });";
                        js += "        }";
                        js += "    });";
                        js += "});";
                        js += "</script>";

                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {

                            DataTable dtWorkflowRequests = database.ExecuteDataTable("select * from st_workflow_request where entity_id = " + login.Id + " order by create_date desc");
                            if ((dtWorkflowRequests == null) || (dtWorkflowRequests.Rows.Count == 0))
                            {
                                eHtml += String.Format(errorTemplate, "Nenhuma requisição cadastrada");
                            }
                            else
                            {

                                html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                html += "    <tr>";
                                html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Nome <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"status\">Status <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer w150 tHide mHide header\" data-column=\"create_date\">Data de criação <div class=\"icomoon\"></div></th>";
                                html += "    </tr>";
                                html += "</thead>";

                                html += "<tbody>";

                                String trTemplate = "    <tr class=\"request\" data-userid=\"{0}\" data-href=\"" + ApplicationVirtualPath + "autoservice/access_request/{0}/\">";
                                trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                trTemplate += "            <td class=\"pointer ident10\">{1}</td>";
                                trTemplate += "            <td class=\"pointer tHide mHide\">{2}</td>";
                                trTemplate += "            <td class=\"pointer tHide mHide\">{3}</td>";
                                trTemplate += "    </tr>";

                                foreach (DataRow dr in dtWorkflowRequests.Rows)
                                {
                                    try
                                    {
                                        WorkflowConfig workflow = new WorkflowConfig();
                                        workflow.GetDatabaseData(database, (Int64)dr["workflow_id"]);

                                        WorkflowRequestStatus status = (WorkflowRequestStatus)((Int32)dr["status"]);


                                        html += String.Format(trTemplate, dr["id"].ToString(), workflow.Name, MessageResource.GetMessage("wf_" + status.ToString().ToLower()), ((DateTime)dr["create_date"]).ToString("yyyy-MM-dd HH:mm:ss"));
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }


                                html += "</tbody></table>";

                            }
                        }
                    }
                    break;
            }
            
            headContent.Controls.Add(new LiteralControl(js)); 
            contentHolder.Controls.Add(new LiteralControl((eHtml != "" ? eHtml : html)));

            sideHTML += "<ul class=\"user-profile\">";
            sideHTML += "    <li id=\"user-profile-general\" " + (action == "" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "autoservice/access_request/\">Requisições realizadas</a></span></li>";
            //sideHTML += "    <li id=\"user-profile-password\" " + (action == "changepassword" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "autoservice/access_request/new/\">Nova requisição</a></span></li>";
            sideHTML += "</ul>";

            sideHolder.Controls.Add(new LiteralControl(sideHTML));

            String titleBarHTML = "";

            /*
            titleBarHTML += "<ul class=\"mobile-button-bar w50 \">";
            titleBarHTML += "    <li id=\"user-profile-general-mobile\" "+ (action == "" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "autoservice/user/\">Informações gerais</a></li>";
            titleBarHTML += "    <li id=\"user-profile-password-mobile\" " + (action == "changepassword" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "autoservice/user/changepassword/\">Troca de senha</a></li>";
            titleBarHTML += "</ul>";*/

            titleBarContent.Controls.Add(new LiteralControl(titleBarHTML));

        }
    }
}