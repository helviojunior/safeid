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
using IAM.Filters;
using System.Globalization;
using System.Threading;



namespace IAMWebServer._admin.content
{
    public partial class workflow : System.Web.UI.Page
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

            Boolean newItem = false;
            if ((RouteData.Values["new"] != null) && (RouteData.Values["new"] == "1"))
                newItem = true;

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            LMenu menu1 = new LMenu("Dashboard", ApplicationVirtualPath + "admin/");
            LMenu menu2 = new LMenu("Pastas", ApplicationVirtualPath + "admin/workflow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Pastas", ApplicationVirtualPath + "admin/workflow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 workflowId = 0;
            try
            {
                workflowId = Int64.Parse((String)RouteData.Values["id"]);

                if (workflowId < 0)
                    workflowId = 0;
            }
            catch { }

            String error = "";
            WorkflowGetResult selectedWorkflow = null;
            String filter = "";
            HashData hashData = new HashData(this);

            String rData = null;
            //SqlConnection conn = null;
            String jData = "";

            Int32 page = 1;
            Int32 pageSize = 20;
            Boolean hasNext = true;

            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((workflowId > 0) && (area.ToLower() != "search"))
            {


                try
                {

                    rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "workflow.get",
                        parameters = new
                        {
                            workflowid = workflowId
                        },
                        id = 1
                    });
                    jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    selectedWorkflow = JSON.Deserialize<WorkflowGetResult>(jData);
                    if (selectedWorkflow == null)
                    {
                        error = MessageResource.GetMessage("workflow_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (selectedWorkflow.error != null)
                    {
                        error = selectedWorkflow.error.data;
                        selectedWorkflow = null;
                    }
                    else if (selectedWorkflow.result == null || selectedWorkflow.result.info == null)
                    {
                        error = MessageResource.GetMessage("workflow_not_found");
                        selectedWorkflow = null;
                    }
                    else
                    {
                        menu3.Name = selectedWorkflow.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedWorkflow = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }


            }

            String infoTemplate = "<tr {0}><td class=\"col1\">{1}</td><td class=\"col2\"><span class=\"no-edit\">{2}</span></td></tr>";

            switch (area)
            {
                case "":
                case "search":
                case "content":
                    if (newItem)
                    {

                        error = "";
                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "context.list",
                            parameters = new { },
                            id = 1
                        });

                        jData = "";
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
                        }
                        else if (contextList.result == null)
                        {
                            error = MessageResource.GetMessage("context_not_found");
                        }
                        else
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

                            html = "<h3>Adição de workflow</h3>";
                            html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/workflow/action/add_container/\">";
                            html += "<div class=\"no-tabs fields\"><table><tbody>";
                            
                            html += String.Format(infoTemplate, "", "Nome", "<input id=\"workflow_name\" name=\"workflow_name\" placeholder=\"Digite o nome do workflow\" type=\"text\"\">");

                            String select = "<select id=\"workflow_context\" name=\"workflow_context\" ><option value=\"\"></option>";
                            foreach (ContextData c in contextList.result)
                                select += "<option value=\"" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                            select += "</select>";

                            html += String.Format(infoTemplate, "", "Contexto", select);

                            select = "<select id=\"workflow_type\" name=\"workflow_type\" ><option value=\"\"></option>";
                            foreach (IAM.Workflow.WorkflowAccessType type in (IAM.Workflow.WorkflowAccessType[])Enum.GetValues(typeof(IAM.Workflow.WorkflowAccessType)))
                                if (type != IAM.Workflow.WorkflowAccessType.None)
                                    select += "<option value=\"" + (Int32)type + "\">" + MessageResource.GetMessage("wf_" + type.ToString().ToLower(), type.ToString()) + "</option>";
                            select += "</select>";

                            html += String.Format(infoTemplate, "", "Tipo", select);

                            html += String.Format(infoTemplate, "id=\"" + (Int32)IAM.Workflow.WorkflowAccessType.RoleGrant + "\" class=\"workflow_type_hidden\" style=\"display:none;\"", "Perfis", "<input id=\"role\" name=\"role\" placeholder=\"Digite o nome do perfil desejado\" type=\"text\"\"><div id=\"selected_role\" class=\"item-box\"></div><span class=\"description red-text none-role\" style=\"opacity:1;\">Nenhum perfil selecionado</span>");

                            html += String.Format(infoTemplate, "", "Proprietário", "<input id=\"owner\" name=\"owner\" placeholder=\"Digite o nome do usuário\" type=\"text\"\"><div id=\"selected_user\" class=\"item-box\"></div><span class=\"description red-text none-user\" style=\"opacity:1;\">Nenhum usuário selecionado</span>");

                            html += String.Format(infoTemplate, "", "Descrição", "<textarea id=\"description\" name=\"description\" rows=\"5\" placeholder=\"Digite a descrição que será exibida ao usuário requisitante do acesso\"></textarea>");
                            
                            html += "</tbody></table><div class=\"clear-block\"></div></div>";


                            html += "<h3>Passos de aprovação</h3>";
                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                            String stepTemplate = "<div class=\"custom-item\" id=\"act_[id]\" onclick=\"iamadmin.buildWorkflowAct(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/workflow/content/activity-build/\"><input type=\"hidden\" name=\"act\" value=\"[id]\" /><input type=\"hidden\" name=\"act_key_[id]\" class=\"key\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\"  onclick=\"iamfnc.addActivity(this);\"></div>";

                            html += String.Format(infoTemplate, "class=\"activity\"", "Passo de aprovação", stepTemplate.Replace("[id]", "1"));

                            html += "</tbody></table><div class=\"clear-block\"></div></div>";
                            
                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/workflow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                            js = "iamadmin.autoCompleteText('#owner', '" + ApplicationVirtualPath + "admin/workflow/content/search_user/', { content_id: 'owner_id' } , function(thisId, selectedItem){ $(thisId).val(''); $('.none-user').remove(); $('#selected_user').html(selectedItem.html); } );";

                            js += "iamadmin.autoCompleteText('#role', '" + ApplicationVirtualPath + "admin/workflow/content/search_role/', { content_id: 'role_id' } , function(thisId, selectedItem){ $(thisId).val(''); $('.none-role').remove(); $('#selected_role').append(selectedItem.html); } );";

                            js += "$('#workflow_type').change(function() {";
                            js += "    $('.workflow_type_hidden').css('display','none');";
                            js += "    $('#' + $( this ).val()).css('display','table-row');";
                            js += "});";

                            js += "iamfnc.addActivity = function(objThis){ ";
                            js += "    var lc = $(objThis).closest('.activity');";
                            js += "    var id = new Date().getTime(); console.log(lc);";
                            js += "    $('" + String.Format(infoTemplate, "class=\"activity\"", "Passo de aprovação", stepTemplate + "<div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>") + "'.replace(/\\[id\\]/g,id)).insertAfter(lc);";
                            js += "};";

                            js += "iamadmin.removeLine = function(objThis){ ";
                            js += "    $(objThis).closest('.activity').remove();";
                            js += "};";

                            //js += "$('#filter_context').change(function() { iamadmin.changeHash( $( this ).val() ); });";

                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                            contentRet.js = js;
                            
                        }
                    }
                    else
                    {
                        if (selectedWorkflow == null)
                        {

                            Int32.TryParse(Request.Form["page"], out page);

                            if (page < 1)
                                page = 1;

                            String containerTemplate = "<div id=\"proxy-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" data-total=\"{2}\" class=\"app-list-item\">";
                            containerTemplate += "<table>";
                            containerTemplate += "   <tbody>";
                            containerTemplate += "       <tr>";
                            containerTemplate += "           <td class=\"col1\">";
                            containerTemplate += "               <span id=\"total_{0}\" class=\"total \">{2}</span>";
                            containerTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/users/\">";
                            containerTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Requisições</span></div>";
                            containerTemplate += "               </a>";
                            containerTemplate += "           </td>";
                            containerTemplate += "           <td class=\"col2\">";
                            containerTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"workflow_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#workflow_name_{0}',null,workflowNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                            containerTemplate += "               <div class=\"description\">";
                            containerTemplate += "                   <div class=\"first\">{4}</div>";
                            containerTemplate += "               </div>";
                            containerTemplate += "               <div class=\"links\">";
                            containerTemplate += "                   <div class=\"line\">{5}</div><div class=\"clear-block\"></div>";
                            containerTemplate += "               </div>";
                            containerTemplate += "           </td>";
                            containerTemplate += "       </tr>";
                            containerTemplate += "   </tbody>";
                            containerTemplate += "</table></div>";

                            js += "workflowNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

                            html += "<div id=\"box-container\" class=\"box-container\">";

                            String query = "";
                            try
                            {

                                rData = "";

                                if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                    query = (String)RouteData.Values["query"];

                                if (String.IsNullOrWhiteSpace(query) && !String.IsNullOrWhiteSpace(hashData.GetValue("query")))
                                    query = hashData.GetValue("query");

                                if (String.IsNullOrWhiteSpace(query))
                                {
                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                    {
                                        jsonrpc = "1.0",
                                        method = "workflow.list",
                                        parameters = new
                                        {
                                            page_size = pageSize,
                                            page = page
                                        },
                                        id = 1
                                    });
                                }
                                else
                                {
                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                    {
                                        jsonrpc = "1.0",
                                        method = "workflow.search",
                                        parameters = new
                                        {
                                            text = query,
                                            page_size = pageSize,
                                            page = page
                                        },
                                        id = 1
                                    });
                                }

                                jData = "";
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                if (String.IsNullOrWhiteSpace(jData))
                                    throw new Exception("");

                                WorkflowListResult ret2 = JSON.Deserialize<WorkflowListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("workflow_not_found"));
                                    hasNext = false;
                                }
                                else if (ret2.error != null)
                                {
#if DEBUG
                                    eHtml += String.Format(errorTemplate, ret2.error.data + ret2.error.debug);
#else
                                    eHtml += String.Format(errorTemplate, ret2.error.data);
#endif
                                    hasNext = false;
                                }
                                else if (ret2.result == null || (ret2.result.Count == 0 && page == 1))
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("workflow_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    
                                    foreach (WorkflowData w in ret2.result)
                                    {
                                        
                                        String text = "";
                                        if (w.description != null)
                                            text += "<span>Descrição: " + w.description + "</span><br />";

                                        if (w.activities != null)
                                        {
                                            text += "<span>Atividades de aprovação: "+ (w.activities.Count == 0 ? "Nenhuma atividade cadastrada":"");

                                            foreach (WorkflowDataActivity a in w.activities)
                                                text += "<div style=\"padding-left: 20px;\">" + a.name + "</div>";

                                            text += "</span>";
                                        }


                                        //if (w. != null)
                                        //    text += "<span>Contexto: " + c.context_name + "</span>";

                                        String links = "";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/workflow/" + w.workflow_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar</div></a>";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/workflow/" + w.workflow_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-user-add\">Adicionar usuário</div></a>";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/workflow/" + w.workflow_id + "/action/delete_all_users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente todos os usuários da pasta '" + w.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir usuários</div></a>";
                                        links += "<a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/workflow/" + w.workflow_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente a pasta '" + w.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>";

                                        html += String.Format(containerTemplate, w.workflow_id, w.name, w.request_qty, (w.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(w.create_date), true) : ""), text, links);
                                        
                                    }

                                    if (ret2.result.Count < pageSize)
                                        hasNext = false;
                                }

                            }
                            catch (Exception ex)
                            {
                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            }

                            if (page == 1)
                            {

                                html += "</div>";

                                html += "<span class=\"empty-results content-loading proxy-list-loader hide\"></span>";

                                contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                            }
                            else
                            {
                                contentRet = new WebJsonResponse("#content-wrapper #box-container", (eHtml != "" ? eHtml : html), true);
                            }

                            contentRet.js = js + "$( document ).unbind('end_of_scroll');";

                            if (hasNext)
                                contentRet.js += "$( document ).bind( 'end_of_scroll.loader_role', function() { $( document ).unbind('end_of_scroll.loader_role'); $('.proxy-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'" + (!String.IsNullOrWhiteSpace(query) ? query : "") + "' }, function(){ $('.proxy-list-loader').addClass('hide'); } ); });";

                        }
                        else//Esta sendo selecionado o content
                        {
                            if (error != "")
                            {
                                contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                            }
                            else
                            {

                                switch (filter)
                                {
                                    case "":



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

                                        html = "<h3>Edição do workflow</h3>";
                                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/workflow/" + selectedWorkflow.result.info.workflow_id + "/action/change/\">";
                                        html += "<div class=\"no-tabs fields\"><table><tbody>";
                                        html += String.Format(infoTemplate, "", "Nome", "<input id=\"container_name\" name=\"container_name\" placeholder=\"Digite o nome da pasta\" type=\"text\"\" value=\""+ selectedWorkflow.result.info.name +"\">");

                                        html += String.Format(infoTemplate, "", "Contexto", selectedWorkflow.result.info.name);
                                                                                
                                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/workflow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));

                                        break;


                                    case "add_user":
                                        html = "<h3>Adição de usuário</h3>";
                                        html += "<form id=\"form_add_user\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/workflow/" + workflowId + "/action/add_user/\"><div class=\"no-tabs pb10\">";
                                        html += "<div class=\"form-group\"  id=\"add_user\"><label>Usuário</label><input id=\"add_user_text\" placeholder=\"Digite o nome do usuário\" type=\"text\"\"></div>";
                                        html += "<div class=\"clear-block\"></div></div>";
                                        html += "<h3>Usuários selecionados</h3>";
                                        html += "<div id=\"box-container\" class=\"box-container\"><div class=\"no-tabs pb10 none\">";
                                        html += "Nenhum usuário selecionado";
                                        html += "</div></div>";
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Cadastrar</button>    <a href=\"" + ApplicationVirtualPath + "admin/workflow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        contentRet.js = "iamadmin.autoCompleteText('#add_user_text', '" + ApplicationVirtualPath + "admin/users/content/search_user/', {context_id: '" + selectedWorkflow.result.info.context_id + "'} , function(thisId, selectedItem){ $(thisId).val(''); $('.none').remove(); $('.box-container').append(selectedItem.html); } )";

                                        break;

                                }
                            }
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

                    if (!newItem)
                    {
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/workflow/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo workflow</button></div>";
                    }

                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":
                    break;


                case "search_user":
                    List<AutoCompleteItem> users = new List<AutoCompleteItem>();

                    try
                    {

                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "user.search",
                            parameters = new
                            {
                                page_size = 20,
                                page = 1,
                                text = Request.Form["text"],
                                filter = new { contextid = Request.Form["context_id"] }
                            },
                            id = 1
                        });

                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        SearchResult ret2 = JSON.Deserialize<SearchResult>(jData);
                        if (ret2 == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (ret2.error != null)
                        {
                            eHtml += String.Format(errorTemplate, ret2.error.data);
                        }
                        else if (ret2.result == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                        }
                        else
                        {
                            foreach (UserData user in ret2.result)
                            {
                                String tHtml = "<div class=\"item\"><input type=\"hidden\" name=\"" + Request.Form["content_id"] + "\" id=\"" + Request.Form["content_id"] + "\" value=\"" + user.userid + "\"><div class=\"text\">" + user.login + "</div><div class=\"delete ico icon-close\" onclick=\"$(this).closest('.item').remove();\"></div><div class=\"clear-block\"></div></div>";
                                users.Add(new AutoCompleteItem(user.userid, "(" + user.login + ") " + user.full_name, tHtml));
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                    }

                    if (users.Count == 0)
                        users.Add(new AutoCompleteItem(0, MessageResource.GetMessage("user_not_found"), ""));

                    Retorno.Controls.Add(new LiteralControl(JSON.Serialize<List<AutoCompleteItem>>(users)));

                    break;


                case "search_role":
                    List<AutoCompleteItem> roles = new List<AutoCompleteItem>();

                    try
                    {

                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "role.search",
                            parameters = new
                            {
                                page_size = 20,
                                page = 1,
                                text = Request.Form["text"],
                                filter = new { contextid = Request.Form["context_id"] }
                            },
                            id = 1
                        });

                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        RoleListResult ret2 = JSON.Deserialize<RoleListResult>(jData);
                        if (ret2 == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (ret2.error != null)
                        {
                            eHtml += String.Format(errorTemplate, ret2.error.data);
                        }
                        else if (ret2.result == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                        }
                        else
                        {
                            foreach (RoleData role in ret2.result)
                            {
                                String tHtml = "<div class=\"item\"><input type=\"hidden\" name=\"" + Request.Form["content_id"] + "\" id=\"" + Request.Form["content_id"] + "\" value=\"" + role.role_id + "\"><div class=\"text\">" + role.name + "</div><div class=\"delete ico icon-close\" onclick=\"$(this).closest('.item').remove();\"></div><div class=\"clear-block\"></div></div>";
                                roles.Add(new AutoCompleteItem(role.role_id, role.name, tHtml));
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                    }

                    if (roles.Count == 0)
                        roles.Add(new AutoCompleteItem(0, MessageResource.GetMessage("role_not_found"), ""));

                    Retorno.Controls.Add(new LiteralControl(JSON.Serialize<List<AutoCompleteItem>>(roles)));

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