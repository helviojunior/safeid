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
    public partial class system_roles : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Perfis", ApplicationVirtualPath + "admin/system_roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Perfis do sistema", ApplicationVirtualPath + "admin/system_roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";
            String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";


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
            SystemRoleGetResult selectedRole = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((roleId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    String rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "systemrole.get",
                        parameters = new
                        {
                            roleid = roleId,
                            permissions = true
                        },
                        id = 1
                    });
                    IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString());
                    String jData = "";
                    try
                    {
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);
                    }
                    finally
                    {
                        if (database != null)
                            database.Dispose();
                    }

                    selectedRole = JSON.Deserialize<SystemRoleGetResult>(jData);
                    if (selectedRole == null)
                    {
                        error = MessageResource.GetMessage("system_role_not_found");
                    }
                    else if (selectedRole.error != null)
                    {
                        error = selectedRole.error.data;
                        selectedRole = null;
                    }
                    else if (selectedRole.result == null || selectedRole.result.info == null)
                    {
                        error = MessageResource.GetMessage("system_role_not_found");
                        selectedRole = null;
                    }
                    else
                    {
                        menu3.Name = selectedRole.result.info.name;
                        menu3.HRef = ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "");
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedRole = null;
                }

                
            }

            switch (area)
            {
                case "":
                case "search":
                case "content":
                    if (newItem)
                    {


                        html = "<h3>Adição de perfil</h3>";
                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/system_roles/action/add_role/\">";
                        html += "<div class=\"no-tabs fields\"><table><tbody>";
                        html += String.Format(infoTemplate, "Nome", "<input id=\"add_role_name\" name=\"add_role_name\" placeholder=\"Digite o nome do perfil\" type=\"text\">");
                        html += String.Format(infoTemplate, "Admin", "<input id=\"enterprise_admin\" name=\"enterprise_admin\" type=\"checkbox\"><span class=\"description\">Perfil com direitos em todas as operações desta empresa</span>");
                        html += "</select></div>";
                        html += "</tbody></table></div>";
                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/system_roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));

                    }
                    else
                    {
                        if (selectedRole == null)
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
                            roleTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/system_roles/{0}/users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\">";
                            roleTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver usuários</span></div>";
                            roleTemplate += "               </a>";
                            roleTemplate += "           </td>";
                            roleTemplate += "           <td class=\"col2\">";
                            roleTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"role_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#role_name_{0}',null,roleNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                            roleTemplate += "               <div class=\"description\">Permissões atribuidas: {4}";
                            roleTemplate += "               </div>";
                            roleTemplate += "               <div class=\"links\">";
                            roleTemplate += "                   <div class=\"line\">";
                            roleTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/system_roles/{0}/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar</div></a>";
                            roleTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/system_roles/{0}/permissions/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-checkmark\">Permissões</div></a>";
                            roleTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/system_roles/{0}/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-user-add\">Adicionar usuário</div></a>";
                            roleTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/system_roles/{0}/action/delete_all_users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente todos os usuários do perfil '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir usuários</div></a>";
                            roleTemplate += "                       <a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/system_roles/{0}/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o perfil '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>";
                            roleTemplate += "                   </div><div class=\"clear-block\"></div>";
                            roleTemplate += "               </div>";
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
                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                    {
                                        jsonrpc = "1.0",
                                        method = "systemrole.list",
                                        parameters = new
                                        {
                                            page_size = pageSize,
                                            page = page,
                                            permissions = true
                                        },
                                        id = 1
                                    });
                                }
                                else
                                {
                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                    {
                                        jsonrpc = "1.0",
                                        method = "systemrole.search",
                                        parameters = new
                                        {
                                            text = query,
                                            page_size = pageSize,
                                            page = page,
                                            permissions = true
                                        },
                                        id = 1
                                    });
                                }

                                IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString());
                                String jData = "";
                                try
                                {
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);
                                }
                                finally
                                {
                                    if (database != null)
                                        database.Dispose();
                                }

                                if (String.IsNullOrWhiteSpace(jData))
                                    throw new Exception("");

                                SystemRoleListResult ret2 = JSON.Deserialize<SystemRoleListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("system_role_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("system_role_not_found"));
                                    hasNext = false;
                                }
                                else
                                {

                                    foreach (SystemRoleData role in ret2.result)
                                    {
                                        List<String> perm = new List<string>();

                                        if (!role.enterprise_admin && (role.permissions != null) && (role.permissions.Count > 0))
                                            foreach (SystemRolePermission p in role.permissions)
                                                perm.Add(p.module_name + "/" + p.sub_module_name + "/" + p.name);
                                        
                                        if (role.enterprise_admin)
                                            perm.Add("Administração da empresa - todas as permissões");

                                        if (perm.Count == 0)
                                            perm.Add("Nenhuma permissão atribuida");

                                        html += String.Format(roleTemplate, role.role_id, role.name, role.entity_qty, (role.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(role.create_date), true) : ""), String.Join(", ", perm));
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

                                        html += "<h3>Configurações gerais";
                                        if (hashData.GetValue("edit") != "1")
                                            html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                        html += "</h3>";
                                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/action/change_role/\">";
                                        html += "<div class=\"no-tabs fields\"><table><tbody>";

                                        if (hashData.GetValue("edit") == "1")
                                        {
                                            html += String.Format(infoTemplate, "Nome", "<input id=\"name\" name=\"name\" placeholder=\"Digite o nome do perfil\" type=\"text\" value=\"" + selectedRole.result.info.name + "\">");
                                            html += String.Format(infoTemplate, "Admin", "<input id=\"enterprise_admin\" name=\"enterprise_admin\" type=\"checkbox\" "+ (selectedRole.result.info.enterprise_admin ? "checked" : "") +"><span class=\"description\">Perfil com direitos em todas as operações desta empresa</span>");
                                        }
                                        else
                                        {

                                            html += String.Format(infoTemplate, "Nome", selectedRole.result.info.name);
                                            html += String.Format(infoTemplate, "Admin", (selectedRole.result.info.enterprise_admin ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")));
                                        }

                                        html += "</tbody></table></div>";

                                        if (hashData.GetValue("edit") == "1")
                                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        break;

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
                                        trTemplate += "            <td class=\"tHide mHide\"><button class=\"a-btn\" onclick=\"window.location = '" + ApplicationVirtualPath + "admin/users/{0}/';\">Abrir</button> <button href=\"" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/action/delete_user/{0}/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o vínculo do usuário '{2}' com o perfil de sistema '" + selectedRole.result.info.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\">Excluir</button></td>";
                                        trTemplate += "    </tr>";

                                        try
                                        {

                                            String rData = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(new
                                            {
                                                jsonrpc = "1.0",
                                                method = "systemrole.users",
                                                parameters = new
                                                {
                                                    page_size = pageSize,
                                                    page = page,
                                                    roleid = roleId
                                                },
                                                id = 1
                                            });

                                            
                                            String jData = "";
                                            using(IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
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
                                        html += "<form id=\"form_add_user\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/system_roles/" + roleId + "/action/add_user/\"><div class=\"no-tabs pb10\">";
                                        html += "<div class=\"form-group\"  id=\"add_user\"><label>Usuário</label><input id=\"add_user_text\" placeholder=\"Digite o nome do usuário\" type=\"text\"\"></div>";
                                        html += "<div class=\"clear-block\"></div></div>";
                                        html += "<h3>Usuários selecionados</h3>";
                                        html += "<div id=\"box-container\" class=\"box-container\"><div class=\"no-tabs pb10 none\">";
                                        html += "Nenhum usuário selecionado";
                                        html += "</div></div>";
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Cadastrar</button>    <a href=\"" + ApplicationVirtualPath + "admin/system_roles/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        contentRet.js = "iamadmin.autoCompleteText('#add_user_text', '" + ApplicationVirtualPath + "admin/users/content/search_user/', null , function(thisId, selectedItem){ $(thisId).val(''); $('.none').remove(); $('.box-container').append(selectedItem.html); } )";

                                        break;


                                    case "permissions":

                                        html += "<h3>Permissões";
                                        if ((hashData.GetValue("edit") != "1") && (!selectedRole.result.info.enterprise_admin))
                                            html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                        html += "</h3>";
                                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/action/change_permissions/\">";
                                        html += "<div class=\"no-tabs fields\"><table><tbody>";

                                        String infoTemplate2 = "<tr><td class=\"colfull\">{0}</td></tr>";

                                        if (selectedRole.result.info.enterprise_admin)
                                        {
                                            html += String.Format(infoTemplate2, "<span style=\"text-align: center; width: 100%; display:block;\">Esto perfil tem permissão de administração total nesta empresa, desta forma não necessita configurar permissões específicas</span>");
                                        }
                                        else
                                        {
                                            String rData = SafeTrend.Json.JSON.Serialize2(new
                                            {
                                                jsonrpc = "1.0",
                                                method = "systemrole.permissionstree",
                                                parameters = new { },
                                                id = 1
                                            });

                                            String jData = "";
                                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                jData = WebPageAPI.ExecuteLocal(database, this, rData);
                                            

                                            SystemRolePermissionsTree retPTree = JSON.Deserialize<SystemRolePermissionsTree>(jData);
                                            if (retPTree == null)
                                            {
                                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("permissions_not_found"));
                                            }
                                            else if (retPTree.error != null)
                                            {
                                                eHtml += String.Format(errorTemplate, retPTree.error.data);
                                            }
                                            else if (retPTree.result == null)
                                            {
                                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("permissions_not_found"));
                                            }
                                            else
                                            {
                                                

                                                if (hashData.GetValue("edit") == "1")
                                                {
                                                    String field = "";

                                                    field += "<div id=\"tree\">";

                                                    field += "<ul>";
                                                    foreach (SystemRolePermissionModule module in retPTree.result)
                                                    {
                                                        if (module.submodules.Count > 0)
                                                        {
                                                            field += "  <li class=\"" + (module.submodules.Count == 0 ? "no-chield" : "") + "\"><input type=\"checkbox\"><span>" + module.name + "</span>";
                                                            field += "      <ul>";
                                                            foreach (SystemRolePermissionSubModule subModule in module.submodules)
                                                            {
                                                                if (subModule.permissions.Count > 0)
                                                                {
                                                                    field += "  <li class=\"" + (subModule.permissions.Count == 0 ? "no-chield" : "") + "\"><input type=\"checkbox\"><span>" + subModule.name + "</span>";
                                                                    field += "      <ul>";

                                                                    foreach (SystemRolePermissionItem permission in subModule.permissions)
                                                                    {
                                                                        field += "  <li class=\"no-chield\"><input type=\"checkbox\" name=\"permission_id\" value=\"" + permission.permission_id + "\" " + (selectedRole.result.info.permissions != null && selectedRole.result.info.permissions.Exists(p => (p.permission_id == permission.permission_id)) ? "checked" : "") + "><span>" + permission.name + "</span></li>";
                                                                    }

                                                                    field += "      </ul>";
                                                                    field += "</li>";
                                                                }

                                                            }
                                                            field += "      </ul>";
                                                            field += "</li>";
                                                        }
                                                        
                                                    }
                                                    field += "</ul>";

                                                    field += "</div>";

                                                    html += String.Format(infoTemplate2, field);
                                                    js = "$('#tree').tree({ dnd: false  });";
                                                }
                                                else
                                                {

                                                    foreach (SystemRolePermissionModule module in retPTree.result)
                                                    {
                                                        if (module.submodules.Count > 0)
                                                        {
                                                            foreach (SystemRolePermissionSubModule subModule in module.submodules)
                                                            {
                                                                if (subModule.permissions.Count > 0)
                                                                {
                                                                    List<String> per = new List<string>();

                                                                    foreach (SystemRolePermissionItem permission in subModule.permissions)
                                                                    {
                                                                        if (selectedRole.result.info.permissions != null && selectedRole.result.info.permissions.Exists(p => (p.permission_id == permission.permission_id)))
                                                                            per.Add(permission.name);
                                                                    }

                                                                    if (per.Count == 0)
                                                                        per.Add("Nenhuma permissão definida");

                                                                    html += String.Format(infoTemplate, module.name + "/" + subModule.name, String.Join(", ",per));
                                                                }
                                                            }
                                                        }

                                                    }

                                                }
                                            }
                                        }
                                        html += "</tbody></table></div>";

                                        if (hashData.GetValue("edit") == "1")
                                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/permissions/\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        contentRet.js = js;
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
                            html += "        <h2 class=\"title tutorial-color\"><a href=\"" + menu3.HRef + "\">" + menu3.Name + "</a></h2>";
                            html += "    </div>";
                        }
                        html += "</div></div>";
                    }

                    if (!newItem)
                    {
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/system_roles/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo perfil</button></div>";

                        if (selectedRole != null)
                        {
                            if (filter != "add_user")
                                html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Adicionar usuários</button></div>";

                            if (filter != "permissions")
                                html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/system_roles/" + selectedRole.result.info.role_id + "/permissions/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Alterar permissões</button></div>";
                        }

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