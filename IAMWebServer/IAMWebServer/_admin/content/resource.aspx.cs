using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.WebAPI;
using SafeTrend.WebAPI;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.GlobalDefs;
using System.Globalization;
using System.Threading;

namespace IAMWebServer._admin.content
{
    public partial class resource : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Recursos", ApplicationVirtualPath + "admin/resource/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Recursos", ApplicationVirtualPath + "admin/resource/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 resourceId = 0;
            try
            {
                resourceId = Int64.Parse((String)RouteData.Values["id"]);

                if (resourceId < 0)
                    resourceId = 0;
            }
            catch { }

            String error = "";
            ResourceGetResult retResource = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((resourceId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "resource.get",
                        parameters = new
                        {
                            resourceid = resourceId
                        },
                        id = 1
                    };

                    String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retResource = JSON.Deserialize<ResourceGetResult>(jData);
                    if (retResource == null)
                    {
                        error = MessageResource.GetMessage("resource_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retResource.error != null)
                    {
                        error = retResource.error.data;
                        retResource = null;
                    }
                    else if (retResource.result == null || retResource.result.info == null)
                    {
                        error = MessageResource.GetMessage("resource_not_found");
                        retResource = null;
                    }
                    else
                    {
                        menu3.Name = retResource.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retResource = null;
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
                            }
                            else if (contextList.result == null)
                            {
                                error = MessageResource.GetMessage("context_not_found");
                            }
                            else
                            {

                                var tmpReq2 = new
                                {
                                    jsonrpc = "1.0",
                                    method = "proxy.list",
                                    parameters = new { },
                                    id = 1
                                };

                                error = "";
                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq2);
                                jData = "";
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);


                                if (String.IsNullOrWhiteSpace(jData))
                                    throw new Exception("");

                                ProxyListResult proxyList = JSON.Deserialize<ProxyListResult>(jData);
                                if (proxyList == null)
                                {
                                    error = MessageResource.GetMessage("proxy_not_found");
                                    //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                }
                                else if (proxyList.error != null)
                                {
                                    error = proxyList.error.data;
                                }
                                else if (proxyList.result == null)
                                {
                                    error = MessageResource.GetMessage("proxy_not_found");
                                }
                                else
                                {

                                    html = "<h3>Adição de recurso</h3>";
                                    html += "<form id=\"form_add_resource\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource/action/add_resource/\"><div class=\"no-tabs pb10\">";
                                    html += "<div class=\"form-group\"><label>Nome</label><input id=\"resource_name\" name=\"resource_name\" placeholder=\"Digite o nome do recurso\" type=\"text\"\"></div>";
                                    html += "<div class=\"form-group\"><label>Contexto</label><select id=\"resource_context\" name=\"resource_context\" ><option value=\"\"></option>";
                                    foreach (ContextData c in contextList.result)
                                        html += "<option value=\"" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                    html += "</select></div>";
                                    html += "<div class=\"form-group\"><label>Proxy</label><select id=\"resource_proxy\" name=\"resource_proxy\" ><option value=\"\"></option>";
                                    foreach (ProxyData p in proxyList.result)
                                        html += "<option value=\"" + p.proxy_id + "\" " + (hashData.GetValue("proxy") == p.proxy_id.ToString() ? "selected" : "") + ">" + p.name + "</option>";
                                    html += "</select></div>";
                                    html += "<div class=\"clear-block\"></div></div>";
                                    html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/resource/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                    contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                    
                                }
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
                        if (retResource == null)
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
                            roleTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/#resource/{0}\">";
                            roleTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver vínculos</span></div>";
                            roleTemplate += "               </a>";
                            roleTemplate += "           </td>";
                            roleTemplate += "           <td class=\"col2\">";
                            roleTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"resource_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#resource_name_{0}',null,resourceNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                            roleTemplate += "               <div class=\"description\">";
                            roleTemplate += "                   <div class=\"first\">{4}</div>";
                            roleTemplate += "               </div>";
                            roleTemplate += "               <div class=\"links\">";
                            roleTemplate += "                   <div class=\"\"><a href=\"" + ApplicationVirtualPath + "admin/resource/{0}/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar</div></a><a href=\"" + ApplicationVirtualPath + "admin/roles/{0}/action/delete_all_users/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente todos os usuários do perfil '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir usuários</div></a></div>";
                            roleTemplate += "                   <div class=\"last\">{5}</div>";
                            roleTemplate += "               </div>";
                            roleTemplate += "           </td>";
                            roleTemplate += "       </tr>";
                            roleTemplate += "   </tbody>";
                            roleTemplate += "</table></div>";

                            js += "resourceNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

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
                                        method = "resource.list",
                                        parameters = new
                                        {
                                            page_size = pageSize,
                                            page = page,
                                            filter = new { contextid = hashData.GetValue("context"), proxyid = hashData.GetValue("proxy") }
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
                                        method = "resource.search",
                                        parameters = new
                                        {
                                            text = query,
                                            page_size = pageSize,
                                            page = page,
                                            filter = new { contextid = hashData.GetValue("context"), proxyid = hashData.GetValue("proxy") }
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

                                ResourceListResult ret2 = JSON.Deserialize<ResourceListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (ResourceData resource in ret2.result)
                                    {
                                        String text = "Contexto: " + resource.context_name + ", Proxy: " + resource.proxy_name;

                                        html += String.Format(roleTemplate, resource.resource_id, resource.name, resource.resource_plugin_qty, (resource.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(resource.create_date), true) : ""), text, (resource.resource_plugin_qty > 0 ? "" : "<a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/resource/" + resource.resource_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o recurso '" + resource.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>"));
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
                                        String infoTemplate = "<div class=\"form-group\">";
                                        infoTemplate += "<label>{0}</label>";
                                        infoTemplate += "<span class=\"no-edit\">{1}</span></div>";
                                        String jsAdd = "";

                                        if (hashData.GetValue("edit") == "1")
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
                                            }
                                            else if (contextList.result == null)
                                            {
                                                error = MessageResource.GetMessage("context_not_found");
                                            }
                                            else
                                            {

                                                var tmpReq2 = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "proxy.list",
                                                    parameters = new { },
                                                    id = 1
                                                };

                                                error = "";
                                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq2);
                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);


                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                ProxyListResult proxyList = JSON.Deserialize<ProxyListResult>(jData);
                                                if (contextList == null)
                                                {
                                                    error = MessageResource.GetMessage("proxy_not_found");
                                                    //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                                }
                                                else if (contextList.error != null)
                                                {
                                                    error = contextList.error.data;
                                                }
                                                else if (contextList.result == null)
                                                {
                                                    error = MessageResource.GetMessage("proxy_not_found");
                                                }
                                                else
                                                {

                                                    html += "<form  id=\"form_resource_change\"  method=\"POST\" action=\"" + ApplicationVirtualPath + "admin/resource/" + retResource.result.info.resource_id + "/action/change/\">";
                                                    html += "<h3>Edição de recurso</h3>";
                                                    html += "<div class=\"no-tabs pb10\">";

                                                    String contexto = "";
                                                    contexto += "<select id=\"resource_context\" name=\"resource_context\" ><option value=\"\"></option>";
                                                    foreach (ContextData c in contextList.result)
                                                        contexto += "<option value=\"" + c.context_id + "\" " + (retResource.result.info.context_id == c.context_id ? "selected" : "") + ">" + c.name + "</option>";
                                                    contexto += "</select>";

                                                    String proxy = "";
                                                    proxy += "<select id=\"resource_proxy\" name=\"resource_proxy\" ><option value=\"\"></option>";
                                                    foreach (ProxyData c in proxyList.result)
                                                        proxy += "<option value=\"" + c.proxy_id + "\" " + (retResource.result.info.proxy_id == c.proxy_id ? "selected" : "") + ">" + c.name + "</option>";
                                                    proxy += "</select>";

                                                    html += String.Format(infoTemplate, "Nome", "<input id=\"name\" name=\"name\" placeholder=\"Digite o nome do recurso\" type=\"text\"\" value=\"" + retResource.result.info.name + "\">");
                                                    html += String.Format(infoTemplate, "Contexto", contexto);
                                                    html += String.Format(infoTemplate, "Proxy", proxy);
                                                    html += String.Format(infoTemplate, "Desabilitado", "<input id=\"disabled\" name=\"disabled\" type=\"checkbox\" " + (!retResource.result.info.enabled ? "checked" : "") + ">");
                                                    html += String.Format(infoTemplate, "Criado em", MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(retResource.result.info.create_date), false));

                                                    html += "<div class=\"clear-block\"></div></div>";

                                                }
                                            }

                                        }
                                        else
                                        {

                                            html += "<h3>Informações gerais<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div></h3>";
                                            html += "<div class=\"no-tabs pb10\">";

                                            html += String.Format(infoTemplate, "Nome", retResource.result.info.name);
                                            html += String.Format(infoTemplate, "Contexto", retResource.result.info.context_name);
                                            html += String.Format(infoTemplate, "Proxy", retResource.result.info.proxy_name);
                                            html += String.Format(infoTemplate, "Desabilitado", (!retResource.result.info.enabled ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")));
                                            html += String.Format(infoTemplate, "Criado em", MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(retResource.result.info.create_date), false));

                                            html += "<div class=\"clear-block\"></div></div>";
                                        }
                                        
                                        if (hashData.GetValue("edit") == "1")
                                            html += "<button type=\"submit\" id=\"resource-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"iamadmin.changeHash( 'edit/0' );\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", html);
                                        contentRet.js = jsAdd;
                                        break;

                                    case "identity":
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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/resource/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo recurso</button></div>";

                        switch (filter)
                        {

                            case "add_user":
                                break;

                            default:
                                /*if (retRole != null)
                                    html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/roles/" + retRole.result.info.role_id + "/add_user/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Adicionar usuários</button></div>";*/
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
                            if (retResource == null)
                            {
                                js = "";
                                try
                                {
                                    String rData = SafeTrend.Json.JSON.Serialize2(new
                                    {
                                        jsonrpc = "1.0",
                                        method = "context.list",
                                        parameters = new
                                        {
                                            page_size = Int32.MaxValue
                                        },
                                        id = 1
                                    });
                                    String jData = "";
                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                                    ContextListResult contextList = JSON.Deserialize<ContextListResult>(jData);
                                    if ((contextList != null) && (contextList.error == null) && (contextList.result != null))
                                    {

                                        html += "<select id=\"filter_context\" name=\"filter_context\" ><option value=\"\">Todos os contextos</option>";
                                        foreach (ContextData c in contextList.result)
                                            html += "<option value=\"context/" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                        html += "</select>";
                                        js += "$('#filter_context').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                                    }

                                }
                                catch (Exception ex) { }

                                try
                                {
                                    String rData = SafeTrend.Json.JSON.Serialize2(new
                                    {
                                        jsonrpc = "1.0",
                                        method = "proxy.list",
                                        parameters = new
                                        {
                                            page_size = Int32.MaxValue
                                        },
                                        id = 1
                                    });
                                    String jData = "";
                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                                    ProxyListResult proxyList = JSON.Deserialize<ProxyListResult>(jData);
                                    if ((proxyList != null) && (proxyList.error == null) && (proxyList.result != null))
                                    {

                                        html += "<select id=\"filter_proxy\" name=\"filter_proxy\" ><option value=\"\">Todos os proxies</option>";
                                        foreach (ProxyData p in proxyList.result)
                                            html += "<option value=\"proxy/" + p.proxy_id + "\" " + (hashData.GetValue("proxy") == p.proxy_id.ToString() ? "selected" : "") + ">" + p.name + "</option>";
                                        html += "</select>";
                                        
                                        js += "$('#filter_proxy').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                                    }

                                }
                                catch (Exception ex) { }

                                contentRet = new WebJsonResponse("#btnbox", html);
                                contentRet.js = js;
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