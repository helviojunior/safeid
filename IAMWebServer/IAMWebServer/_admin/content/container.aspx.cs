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
    public partial class container : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Pastas", ApplicationVirtualPath + "admin/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Pastas", ApplicationVirtualPath + "admin/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 containerId = 0;
            try
            {
                containerId = Int64.Parse((String)RouteData.Values["id"]);

                if (containerId < 0)
                    containerId = 0;
            }
            catch { }

            String error = "";
            ContainerGetResult selectedContainer = null;
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

            if ((containerId > 0) && (area.ToLower() != "search"))
            {


                try
                {

                    rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "container.get",
                        parameters = new
                        {
                            containerid = containerId
                        },
                        id = 1
                    });
                    jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    selectedContainer = JSON.Deserialize<ContainerGetResult>(jData);
                    if (selectedContainer == null)
                    {
                        error = MessageResource.GetMessage("container_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (selectedContainer.error != null)
                    {
                        error = selectedContainer.error.data;
                        selectedContainer = null;
                    }
                    else if (selectedContainer.result == null || selectedContainer.result.info == null)
                    {
                        error = MessageResource.GetMessage("container_not_found");
                        selectedContainer = null;
                    }
                    else
                    {
                        menu3.Name = selectedContainer.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedContainer = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }


            }

            String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

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

                            html = "<h3>Adição de pasta</h3>";
                            html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/container/action/add_container/\">";
                            html += "<div class=\"no-tabs fields\"><table><tbody>";
                            html += String.Format(infoTemplate, "Nome", "<input id=\"container_name\" name=\"container_name\" placeholder=\"Digite o nome da pasta\" type=\"text\"\">");

                            String select = "<select id=\"container_context\" name=\"container_context\" ><option value=\"\"></option>";
                            foreach (ContextData c in contextList.result)
                                select += "<option value=\"" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                            select += "</select>";

                            html += String.Format(infoTemplate, "Contexto", select);

                            Func<String, Int64, Int64, String> chields = null;
                            chields = new Func<String, long, long, string>(delegate(String padding, Int64 root, Int64 ctx)
                            {
                                String h = "";
                                foreach (ContainerData c in conteinerList)
                                    if ((c.parent_id == root) && (c.context_id == ctx))
                                    {
                                        h += "<option value=\"" + c.container_id + "\" " + (hashData.GetValue("container") == c.container_id.ToString() ? "selected" : "") + ">" + padding + " " + c.name + "</option>";
                                        h += chields(padding + "---", c.container_id, ctx);
                                    }

                                return h;
                            });

                            select = "<select id=\"parent_container\" name=\"parent_container\" >";
                            foreach (ContextData ctx in contextList.result)
                            {
                                select += "<option value=\"0\">Raiz no contexto " + ctx.name +"</option>";
                                select += chields("|", 0, ctx.context_id);
                            }
                            select += "</select>";
                            html += String.Format(infoTemplate, "Pasta pai", select);

                            html += "</tbody></table><div class=\"clear-block\"></div></div>";

                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        }
                    }
                    else
                    {
                        if (selectedContainer == null)
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
                            containerTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/users/#container/{0}\">";
                            containerTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver usuários</span></div>";
                            containerTemplate += "               </a>";
                            containerTemplate += "           </td>";
                            containerTemplate += "           <td class=\"col2\">";
                            containerTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"container_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#container_name_{0}',null,containerNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
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

                            js += "containerNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

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
                                        method = "container.list",
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
                                        method = "container.search",
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

                                ContainerListResult ret2 = JSON.Deserialize<ContainerListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("container_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("container_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    
                                    foreach (ContainerData c in ret2.result)
                                    {

                                        String text = "";
                                        if (c.path != null)
                                            text += "<span>Path: " + c.path + "</span><br />";
                                        
                                        if (c.context_name != null)
                                            text += "<span>Contexto: " + c.context_name + "</span>";

                                        String links = "";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/container/" + c.container_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar</div></a>";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/container/" + c.container_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-user-add\">Adicionar usuário</div></a>";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/container/" + c.container_id + "/action/delete_all_users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente todos os usuários da pasta '" + c.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir usuários</div></a>";
                                        links += (c.entity_qty > 0 ? "" : "<a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/container/" + c.container_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente a pasta '" + c.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>");

                                        html += String.Format(containerTemplate, c.container_id, c.name, c.entity_qty, (c.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(c.create_date), true) : ""), text, links);
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

                                        html = "<h3>Edição da pasta</h3>";
                                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/container/" + selectedContainer.result.info.container_id + "/action/change/\">";
                                        html += "<div class=\"no-tabs fields\"><table><tbody>";
                                        html += String.Format(infoTemplate, "Nome", "<input id=\"container_name\" name=\"container_name\" placeholder=\"Digite o nome da pasta\" type=\"text\"\" value=\""+ selectedContainer.result.info.name +"\">");

                                        html += String.Format(infoTemplate, "Contexto", selectedContainer.result.info.context_name);

                                        Func<String, Int64, Int64, String> chields = null;
                                        chields = new Func<String, long, long, string>(delegate(String padding, Int64 root, Int64 ctx)
                                        {
                                            String h = "";
                                            foreach (ContainerData c in conteinerList)
                                                if ((c.parent_id == root) && (c.context_id == ctx))
                                                {
                                                    h += "<option value=\"" + c.container_id + "\" " + (selectedContainer.result.info.parent_id.ToString() == c.container_id.ToString() ? "selected" : "") + ">" + padding + " " + c.name + "</option>";
                                                    h += chields(padding + "---", c.container_id, ctx);
                                                }

                                            return h;
                                        });

                                        String select = "<select id=\"parent_container\" name=\"parent_container\" >";
                                        select += "<option value=\"0\">Raiz no contexto " + selectedContainer.result.info.context_name + "</option>";
                                        select += chields("|", 0, selectedContainer.result.info.context_id);
                                        select += "</select>";
                                        html += String.Format(infoTemplate, "Pasta pai", select);

                                        
                                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));

                                        break;


                                    case "add_user":
                                        html = "<h3>Adição de usuário</h3>";
                                        html += "<form id=\"form_add_user\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/container/" + containerId + "/action/add_user/\"><div class=\"no-tabs pb10\">";
                                        html += "<div class=\"form-group\"  id=\"add_user\"><label>Usuário</label><input id=\"add_user_text\" placeholder=\"Digite o nome do usuário\" type=\"text\"\"></div>";
                                        html += "<div class=\"clear-block\"></div></div>";
                                        html += "<h3>Usuários selecionados</h3>";
                                        html += "<div id=\"box-container\" class=\"box-container\"><div class=\"no-tabs pb10 none\">";
                                        html += "Nenhum usuário selecionado";
                                        html += "</div></div>";
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Cadastrar</button>    <a href=\"" + ApplicationVirtualPath + "admin/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        contentRet.js = "iamadmin.autoCompleteText('#add_user_text', '" + ApplicationVirtualPath + "admin/users/content/search_user/', {context_id: '" + selectedContainer.result.info.context_id + "'} , function(thisId, selectedItem){ $(thisId).val(''); $('.none').remove(); $('.box-container').append(selectedItem.html); } )";

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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/container/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Nova pasta</button></div>";
                    }

                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":
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