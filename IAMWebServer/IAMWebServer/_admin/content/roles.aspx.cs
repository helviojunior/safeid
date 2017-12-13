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
    public partial class roles : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Perfis", ApplicationVirtualPath + "admin/roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Perfis de usuário", ApplicationVirtualPath + "admin/roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 roleId = 0;
            try
            {
                roleId = Int64.Parse((String)RouteData.Values["id"]);

                if (roleId < 0)
                    roleId = 0;
            }
            catch { }

            String error = "";
            RoleGetResult retRole = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((roleId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "role.get",
                        parameters = new
                        {
                            roleid = roleId
                        },
                        id = 1
                    };

                    String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retRole = JSON.Deserialize<RoleGetResult>(jData);
                    if (retRole == null)
                    {
                        error = MessageResource.GetMessage("role_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retRole.error != null)
                    {
                        error = retRole.error.data;
                        retRole = null;
                    }
                    else if (retRole.result == null || retRole.result.info == null)
                    {
                        error = MessageResource.GetMessage("role_not_found");
                        retRole = null;
                    }
                    else
                    {
                        menu3.Name = retRole.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retRole = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }

                
            }

            switch (area)
            {
                case "":
                case "search":
                case "content":
                    if (newItem)
                    {

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
                                retRole = null;
                            }
                            else if (contextList.result == null)
                            {
                                error = MessageResource.GetMessage("context_not_found");
                                retRole = null;
                            }
                            else
                            {

                                html = "<h3>Adição de perfil</h3>";
                                html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/roles/action/add_role/\"><div class=\"no-tabs pb10\">";
                                html += "<div class=\"form-group\"><label>Nome</label><input id=\"add_role_name\" name=\"add_role_name\" placeholder=\"Digite o nome do perfil\" type=\"text\"\"></div>";
                                html += "<div class=\"form-group\"><label>Contexto</label><select id=\"add_role_context\" name=\"add_role_context\" ><option value=\"\"></option>";
                                foreach(ContextData c in contextList.result)
                                    html += "<option value=\"" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                html += "</select></div>";
                                html += "<div class=\"clear-block\"></div></div>";
                                html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                            }

                        }
                        catch (Exception ex)
                        {
                            error = MessageResource.GetMessage("api_error");
                        }

                        if (error != "")
                            contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                    }
                    else
                    {
                        if (retRole == null)
                        {

                            Int32 page = 1;
                            Int32 pageSize = 20;
                            Boolean hasNext = true;

                            Int32.TryParse(Request.Form["page"], out page);

                            if (page < 1)
                                page = 1;


                            String roleTemplate = "<div id=\"role-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" data-total=\"{2}\" class=\"app-list-item\">";
                            roleTemplate += "<table>";
                            roleTemplate += "   <tbody>";
                            roleTemplate += "       <tr>";
                            roleTemplate += "           <td class=\"col1\">";
                            roleTemplate += "               <span id=\"total_{0}\" class=\"total \">{2}</span>";
                            roleTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/roles/{0}/users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\">";
                            roleTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver usuários</span></div>";
                            roleTemplate += "               </a>";
                            roleTemplate += "           </td>";
                            roleTemplate += "           <td class=\"col2\">";
                            roleTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"role_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#role_name_{0}',null,roleNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                            roleTemplate += "               <div class=\"links no-bg\">";
                            roleTemplate += "                   <div class=\"first\"><a href=\"" + ApplicationVirtualPath + "admin/roles/{0}/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-user-add\">Adicionar usuário</div></a><br clear=\"all\"></div>";
                            roleTemplate += "                   <div class=\"\"><a href=\"" + ApplicationVirtualPath + "admin/roles/{0}/action/delete_all_users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente todos os usuários do perfil '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir usuários</div></a></div>";
                            roleTemplate += "                   <div class=\"last\"><a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/roles/{0}/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o perfil '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a><br clear=\"all\"></div>";
                            roleTemplate += "               </div><br clear=\"all\">";
                            roleTemplate += "           </td>";
                            roleTemplate += "       </tr>";
                            roleTemplate += "   </tbody>";
                            roleTemplate += "</table></div>";

                            js += "roleNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

                            html += "<div id=\"box-container\" class=\"box-container\">";

                            String query = "";
                            try
                            {

                                String rData = "";

                                if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                    query = (String)RouteData.Values["query"];

                                if (String.IsNullOrWhiteSpace(query) && !String.IsNullOrWhiteSpace(hashData.GetValue("query")))
                                    query = hashData.GetValue("query");

                                if (String.IsNullOrWhiteSpace(query))
                                {
                                    var tmpReq = new
                                    {
                                        jsonrpc = "1.0",
                                        method = "role.list",
                                        parameters = new
                                        {
                                            page_size = pageSize,
                                            page = page,
                                            filter = new { contextid = hashData.GetValue("context") }
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
                                        method = "role.search",
                                        parameters = new
                                        {
                                            text = query,
                                            page_size = pageSize,
                                            page = page,
                                            filter = new { contextid = hashData.GetValue("context") }
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

                                RoleListResult ret2 = JSON.Deserialize<RoleListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("role_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("role_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (RoleData role in ret2.result)
                                        html += String.Format(roleTemplate, role.role_id, role.name, role.entity_qty, (role.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(role.create_date), true) : ""));

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

                                html += "<span class=\"empty-results content-loading role-list-loader hide\"></span>";

                                contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                            }
                            else
                            {
                                contentRet = new WebJsonResponse("#content-wrapper #box-container", (eHtml != "" ? eHtml : html), true);
                            }

                            contentRet.js = js + "$( document ).unbind('end_of_scroll');";

                            if (hasNext)
                                contentRet.js += "$( document ).bind( 'end_of_scroll.loader_role', function() { $( document ).unbind('end_of_scroll.loader_role'); $('.role-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'" + (!String.IsNullOrWhiteSpace(query) ? query : "") + "' }, function(){ $('.role-list-loader').addClass('hide'); } ); });";

                        }
                        else//Esta sendo selecionado a role
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
                                    case "users":

                                        Int32 page = 1;
                                        Int32 pageSize = 20;
                                        Boolean hasNext = true;

                                        Int32.TryParse(Request.Form["page"], out page);

                                        if (page < 1)
                                            page = 1;

                                        if (page == 1)
                                        {
                                            html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                            html += "    <tr>";
                                            html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                            html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Nome <div class=\"icomoon\"></div></th>";
                                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Login <div class=\"icomoon\"></div></th>";
                                            html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                            html += "    </tr>";
                                            html += "</thead>";

                                            html += "<tbody>";
                                        }

                                        String trTemplate = "    <tr class=\"user\" data-login=\"{1}\" data-userid=\"{0}\">";
                                        trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                        trTemplate += "            <td class=\"ident10\">{2}</td>";
                                        trTemplate += "            <td class=\"tHide mHide\">{1}</td>";
                                        trTemplate += "            <td class=\"tHide mHide\"><button class=\"a-btn\" onclick=\"window.location = '" + ApplicationVirtualPath + "admin/users/{0}/';\">Abrir</button> <button href=\"" + ApplicationVirtualPath + "admin/roles/" + retRole.result.info.role_id + "/action/delete_user/{0}/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o vínculo do usuário '{2}' com o perfil '" + retRole.result.info.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\">Excluir</button></td>";
                                        trTemplate += "    </tr>";

                                        try
                                        {

                                            String rData = "";

                                            var tmpReq = new
                                            {
                                                jsonrpc = "1.0",
                                                method = "role.users",
                                                parameters = new
                                                {
                                                    page_size = pageSize,
                                                    page = page,
                                                    roleid = roleId
                                                },
                                                id = 1
                                            };

                                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                                            String jData = "";
                                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                jData = WebPageAPI.ExecuteLocal(database, this, rData);


                                            if (String.IsNullOrWhiteSpace(jData))
                                                throw new Exception("");

                                            SearchResult ret2 = JSON.Deserialize<SearchResult>(jData);
                                            if (ret2 == null)
                                            {
                                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                                                //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                                hasNext = false;
                                            }
                                            else if (ret2.error != null)
                                            {
                                                eHtml += String.Format(errorTemplate, ret2.error.data);
                                                //ret = new WebJsonResponse("", ret2.error.data, 3000, true);
                                                hasNext = false;
                                            }
                                            else if (ret2.result == null || (ret2.result.Count == 0 && page == 1))
                                            {
                                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                                                hasNext = false;
                                            }
                                            else
                                            {
                                                foreach (UserData user in ret2.result)
                                                    html += String.Format(trTemplate, user.userid, user.login, user.full_name);

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

                                            html += "<span class=\"empty-results content-loading user-list-loader hide\"></span>";

                                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        }
                                        else
                                        {
                                            contentRet = new WebJsonResponse("#content-wrapper tbody", (eHtml != "" ? eHtml : html), true);
                                        }

                                        contentRet.js = "$( document ).unbind('end_of_scroll.loader_usr');";

                                        if (hasNext)
                                            contentRet.js += "$( document ).bind( 'end_of_scroll.loader_usr', function() { $( document ).unbind('end_of_scroll.loader_usr'); $('.user-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'' }, function(){ $('.user-list-loader').addClass('hide'); } ); });";

                                        break;

                                    case "add_user":
                                        html = "<h3>Adição de usuário</h3>";
                                        html += "<form id=\"form_add_user\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/roles/" + roleId + "/action/add_user/\"><div class=\"no-tabs pb10\">";
                                        html += "<div class=\"form-group\"  id=\"add_user\"><label>Usuário</label><input id=\"add_user_text\" placeholder=\"Digite o nome do usuário\" type=\"text\"\"></div>";
                                        html += "<div class=\"clear-block\"></div></div>";
                                        html += "<h3>Usuários selecionados</h3>";
                                        html += "<div id=\"box-container\" class=\"box-container\"><div class=\"no-tabs pb10 none\">";
                                        html += "Nenhum usuário selecionado";
                                        html += "</div></div>";
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Cadastrar</button>    <a href=\"" + ApplicationVirtualPath + "admin/roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        contentRet.js = "iamadmin.autoCompleteText('#add_user_text', '" + ApplicationVirtualPath + "admin/users/content/search_user/', {context_id: '" + retRole.result.info.context_id + "'} , function(thisId, selectedItem){ $(thisId).val(''); $('.none').remove(); $('.box-container').append(selectedItem.html); } )";

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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/roles/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo perfil</button></div>";

                        switch (filter)
                        {

                            case "add_user":
                                break;

                            default:
                                if (retRole != null)
                                    html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/roles/" + retRole.result.info.role_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Adicionar usuários</button></div>";
                                break;
                        }
                    }

                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":

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
                                    retRole = null;
                                }
                                else if (contextList.result == null)
                                {
                                    error = MessageResource.GetMessage("context_not_found");
                                    retRole = null;
                                }
                                else
                                {

                                    html += "<select id=\"filter_context\" name=\"filter_context\" ><option value=\"\">Todos os contextos</option>";
                                    foreach(ContextData c in contextList.result)
                                        html += "<option value=\"context/" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                    html += "</select>";
                                    contentRet = new WebJsonResponse("#btnbox", html);
                                    contentRet.js = "$('#filter_context').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                                }

                            }
                            catch (Exception ex)
                            {
                                error = MessageResource.GetMessage("api_error");
                            }


                            break;

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