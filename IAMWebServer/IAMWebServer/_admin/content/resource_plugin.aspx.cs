using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
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
    public partial class resource_plugin : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Recurso x plugin", ApplicationVirtualPath + "admin/resource_plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Recurso x plugin", ApplicationVirtualPath + "admin/resource_plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 resourcePluginId = 0;
            try
            {
                resourcePluginId = Int64.Parse((String)RouteData.Values["id"]);

                if (resourcePluginId < 0)
                    resourcePluginId = 0;
            }
            catch { }

            String error = "";
            ResourcePluginGetResult selectedResourcePlugin = null;
            String filter = "";
            String subfilter = "";
            HashData hashData = new HashData(this);
            String jData = "";

            String rData = "";
            SqlConnection conn = null;

            Int32 page = 1;
            Int32 pageSize = 5;
            Boolean hasNext = true;


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["subfilter"]))
                subfilter = (String)RouteData.Values["subfilter"];


            if ((resourcePluginId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "resourceplugin.get",
                        parameters = new
                        {
                            resourcepluginid = resourcePluginId,
                            checkconfig = ((area == "content") || (area == ""))
                        },
                        id = 1
                    };

                    rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    selectedResourcePlugin = JSON.Deserialize<ResourcePluginGetResult>(jData);
                    if (selectedResourcePlugin == null)
                    {
                        error = MessageResource.GetMessage("resource_plugin_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (selectedResourcePlugin.error != null)
                    {
                        error = selectedResourcePlugin.error.data;
                        selectedResourcePlugin = null;
                    }
                    else if (selectedResourcePlugin.result == null || selectedResourcePlugin.result.info == null)
                    {
                        error = MessageResource.GetMessage("resource_plugin_not_found");
                        selectedResourcePlugin = null;
                    }
                    else
                    {
                        menu3.Name = selectedResourcePlugin.result.info.name;
                        menu3.HRef = ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "");
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedResourcePlugin = null;
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
                        html = "<h3>Adição de recurso x plugin</h3>";
                        html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/action/add_resource_plugin/\">";
                        html += "<div class=\"no-tabs-header pb10 relative\">";
                        html += "   <ul id=\"nav-resource-plugin\" class=\"nav count4\">";
                        html += "       <li onclick=\" $('#form_add_resource_plugin').submit();\" class=\"step current\"><span></span><span class=\"text\">Geral</span></li>";
                        html += "       <li onclick=\" $('#form_add_resource_plugin').submit();\" class=\"step\"><span></span><span class=\"text\">Entrada</span></li>";
                        html += "       <li onclick=\" $('#form_add_resource_plugin').submit();\" class=\"step\"><span></span><span class=\"text\">Saída</span></li>";
                        html += "       <li onclick=\" $('#form_add_resource_plugin').submit();\" class=\"step\"><span></span><span class=\"text\">Campos</span></li>";
                        html += "   </ul>";
                        html += "<div class=\"clear-block\"></div></div>";
                        html += "<div class=\"no-tabs pb10\">";

                        try
                        {

                            var tmpReq = new
                            {
                                jsonrpc = "1.0",
                                method = "resource.list",
                                parameters = new {
                                page_size = Int32.MaxValue
                                },
                                id = 1
                            };

                            error = "";
                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                            if (String.IsNullOrWhiteSpace(jData))
                                throw new Exception("");

                            ResourceListResult resourceList = JSON.Deserialize<ResourceListResult>(jData);
                            if (resourceList == null)
                            {
                                error = MessageResource.GetMessage("resource_not_found");
                                //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                            }
                            else if (resourceList.error != null)
                            {
                                error = resourceList.error.data;
                            }
                            else if (resourceList.result == null)
                            {
                                error = MessageResource.GetMessage("resource_not_found");
                            }
                            else
                            {

                                var tmpReq2 = new
                                {
                                    jsonrpc = "1.0",
                                    method = "plugin.list",
                                    parameters = new
                                    {
                                        page_size = Int32.MaxValue
                                    },
                                    id = 1
                                };

                                error = "";
                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq2);
                                jData = "";
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                if (String.IsNullOrWhiteSpace(jData))
                                    throw new Exception("");

                                PluginListResult pluginList = JSON.Deserialize<PluginListResult>(jData);
                                if (pluginList == null)
                                {
                                    error = MessageResource.GetMessage("plugin_not_found");
                                    //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                }
                                else if (pluginList.error != null)
                                {
                                    error = pluginList.error.data;
                                }
                                else if (pluginList.result == null)
                                {
                                    error = MessageResource.GetMessage("plugin_not_found");
                                }
                                else
                                {

                                    html += "<div class=\"form-group\"><label>Recurso</label><select id=\"resource\" name=\"resource\" ><option value=\"\"></option>";
                                    foreach (ResourceData r in resourceList.result)
                                        html += "<option value=\"" + r.resource_id + "\" " + (hashData.GetValue("resource") == r.resource_id.ToString() ? "selected" : "") + ">" + r.name + "</option>";
                                    html += "</select></div>";
                                    html += "<div class=\"form-group\"><label>Plugin</label><select id=\"plugin\" name=\"plugin\" ><option value=\"\"></option>";
                                    foreach (PluginData p in pluginList.result)
                                        html += "<option value=\"" + p.plugin_id + "\" " + (hashData.GetValue("plugin") == p.plugin_id.ToString() ? "selected" : "") + ">" + p.name + "</option>";
                                    html += "</select></div>";
                                    html += "<div class=\"form-group\"><label>Domínio de e-mail</label><input id=\"mail_domain\" name=\"mail_domain\" placeholder=\"Digite o domínio de e-mail\" type=\"text\"\"></div>";
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            error = MessageResource.GetMessage("api_error");
                        }

                        if (error != "")
                            eHtml = String.Format(errorTemplate, error);

                        html += "<div class=\"clear-block\"></div></div>";
                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar " + (hashData.GetValue("step") != "3" ? " e continuar" : "") + "</button>    <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                    }
                    else
                    {
                        if (error != "")
                        {
                            contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                        }
                        else
                        {

                            if (selectedResourcePlugin == null)
                            {
                                
                                Int32.TryParse(Request.Form["page"], out page);

                                if (page < 1)
                                    page = 1;

                                String rpTemplate = "<div id=\"resource-plugin-{0}\" data-id=\"{0}\" class=\"app-list-item\">";
                                rpTemplate += "<table>";
                                rpTemplate += "   <tbody>";
                                rpTemplate += "       <tr>";
                                rpTemplate += "           <td class=\"col1\">";
                                rpTemplate += "               <span id=\"total_{0}\" class=\"total \">{2}</span>";
                                rpTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/identity/\">";
                                rpTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver identidades</span></div>";
                                rpTemplate += "               </a>";
                                rpTemplate += "           </td>";
                                rpTemplate += "           <td class=\"col2\">";
                                rpTemplate += "               <div class=\"title\"><span class=\"name\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                                rpTemplate += "               <div class=\"description\">{4}<div class=\"clear-block\"></div></div>";
                                rpTemplate += "               <div class=\"links\">{5}<div class=\"clear-block\"></div></div>";
                                rpTemplate += "           </td>";
                                rpTemplate += "       </tr>";
                                rpTemplate += "   </tbody>";
                                rpTemplate += "</table></div>";


                                html += "<div class=\"box-container\" id=\"box-container\">";

                                String query = "";
                                try
                                {

                                    if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                        query = (String)RouteData.Values["query"];

                                    if (String.IsNullOrWhiteSpace(query) && !String.IsNullOrWhiteSpace(hashData.GetValue("query")))
                                        query = hashData.GetValue("query");

                                    if (String.IsNullOrWhiteSpace(query))
                                    {
                                        var tmpReq = new
                                        {
                                            jsonrpc = "1.0",
                                            method = "resourceplugin.list",
                                            parameters = new
                                            {
                                                page_size = pageSize,
                                                page = page,
                                                checkconfig = true,
                                                filter = new { contextid = hashData.GetValue("context"), pluginid = hashData.GetValue("plugin"), resourceid = hashData.GetValue("resource") }
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
                                            method = "resourceplugin.search",
                                            parameters = new
                                            {
                                                text = query,
                                                page_size = pageSize,
                                                page = page,
                                                //checkconfig = true,
                                                filter = new { contextid = hashData.GetValue("context"), pluginid = hashData.GetValue("plugin"), resourceid = hashData.GetValue("resource") }
                                            },
                                            id = 1
                                        };

                                        rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                                    }

                                    jData = "";
                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                    if (String.IsNullOrWhiteSpace(jData))
                                        throw new Exception("");

                                    ResourcePluginListResult rpList = JSON.Deserialize<ResourcePluginListResult>(jData);
                                    if (rpList == null)
                                    {
                                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                        hasNext = false;
                                    }
                                    else if (rpList.error != null)
                                    {
#if DEBUG
                                        eHtml += String.Format(errorTemplate, rpList.error.data + rpList.error.debug);
#else
                                        eHtml += String.Format(errorTemplate, rpList.error.data);
#endif
                                        hasNext = false;
                                    }
                                    else if (rpList.result == null || (rpList.result.Count == 0 && page == 1))
                                    {
                                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                        hasNext = false;
                                    }
                                    else
                                    {
                                        foreach (ResourcePluginFullData resourcePlugin in rpList.result)
                                        {
                                            String desc = "";
                                            String links = "";

                                            desc += "                   <div class=\"line\">";
                                            desc += "                       <span class=\"info-text\">Status: " + (resourcePlugin.info.enabled ? "habilitado" : "<span class=\"red-text\">desabilitado</span>") + "</span>";
                                            desc += "                       <span class=\"info-text\">Status do recurso: " + (resourcePlugin.info.resource_enabled ? "habilitado" : "<span class=\"red-text\">desabilitado</span>") + "</span>";
                                            desc += "                       <span class=\"info-text\">Status do proxy: ";

                                            if (resourcePlugin.info.proxy_last_sync > 0)
                                            {
                                                DateTime lastSync = new DateTime(1970, 1, 1).AddSeconds(resourcePlugin.info.proxy_last_sync);
                                                TimeSpan ts = DateTime.Now - lastSync;
                                                if (ts.TotalSeconds > 60)
                                                {
                                                    desc += "<span class=\"red-text\">Última conexão a " + MessageResource.FormatTs(ts) + "</span>";
                                                }
                                                else
                                                {
                                                    desc += "On-line";
                                                }
                                            }
                                            else
                                            {
                                                desc += "<span class=\"red-text\">Nunca se conectou no servidor</span>";
                                            }

                                            desc += "                       </span>";
                                            desc += "                   </div>";

                                            if (resourcePlugin.check_config != null)
                                            {
                                                desc += "                   <div class=\"line\">";
                                                desc += "                       <span class=\"info-text\">Configurações gerais: " + (resourcePlugin.check_config != null && resourcePlugin.check_config.general ? "completa" : "<span class=\"red-text\">incompleta</span>") + "</span>";
                                                desc += "                       <span class=\"info-text\">Parâmetros do plugin: " + (resourcePlugin.check_config != null && resourcePlugin.check_config.plugin_par ? "completa" : "<span class=\"red-text\">incompleto</span>") + "</span>";
                                                desc += "                       <span class=\"info-text\">Mapeamento de campos: " + (resourcePlugin.check_config != null && resourcePlugin.check_config.mapping ? "completa" : "<span class=\"red-text\">incompleto</span>") + "</span>";
                                                desc += "                   </div>";
                                            }

                                            links += "                   <div class=\"line\">";

                                            if (resourcePlugin.info.enabled)
                                                links += "                       <a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/action/disable/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Desativação\" confirm-text=\"Deseja desabilitar o recurso x plugin '" + resourcePlugin.info.name + "'?\" ok=\"Desabilitar\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Desabilitar</div></a>";
                                            else
                                                links += "                       <div class=\"ico icon-checkmark data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/action/enable/\">Habilitar</div>";

                                            //

                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_step1/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-cogs\">Configurações gerais</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-stack\">Parâmetros do plugin</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_mapping/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-exchange\">Mapeamento de campos</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_role/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-link\">Vínculo com perfil</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_schedule/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-calendar\">Agendamento</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_lockrules/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-ban-circle\">Regras de bloqueio</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/config_ignore/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-erase\">Desconsiderar registros na importação</div></a>";
                                            links += "                       <div class=\"ico icon-upload data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/action/deploy_now/\">Publicar agora</div>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource/" + resourcePlugin.info.resource_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-wrench\">Configuração do recurso</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/plugin/" + resourcePlugin.info.plugin_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-wrench\">Configuração do plugin</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/identity/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-users\">Identidades</div></a>";
                                            links += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/add_identity/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-user-add\">Adicionar identidade</div></a>";
                                            links += "                       <div class=\"ico icon-paste data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/action/clone/\">Duplicar</div>";
                                            links += "                       <a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + resourcePlugin.info.resource_plugin_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o recurso x plugin '" + resourcePlugin.info.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir</div></a>";
                                            links += "                   </div>";


                                            html += String.Format(rpTemplate, resourcePlugin.info.resource_plugin_id, resourcePlugin.info.name, resourcePlugin.info.identity_qty, (resourcePlugin.info.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(resourcePlugin.info.create_date), false) : ""), desc, links);
                                        }

                                        if (rpList.result.Count < pageSize)
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

                                    html += "<span class=\"empty-results content-loading resource-plugin-list-loader hide\"></span>";

                                    contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                }
                                else
                                {
                                    contentRet = new WebJsonResponse("#content-wrapper #box-container", (eHtml != "" ? eHtml : html), true);
                                }

                                contentRet.js = js + "$( document ).unbind('end_of_scroll');";

                                if (hasNext)
                                    contentRet.js += "$( document ).bind( 'end_of_scroll.loader_resource_plugin', function() { $( document ).unbind('end_of_scroll.loader_resource_plugin'); $('.resource-plugin-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'" + (!String.IsNullOrWhiteSpace(query) ? query : "") + "' }, function(){ $('.resource-plugin-list-loader').addClass('hide'); } ); });";

                            }
                            else//Esta sendo selecionado o resource_plugin
                            {
                                if (error != "")
                                {
                                    contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                                }
                                else
                                {
                                    String hash = (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "");

                                    String jsEdit = "";
                                    String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

                                    #region header

                                    switch (filter)
                                    {

                                        case "identity":
                                            break;

                                        default:
                                            String userTemplate = "<div id=\"role-list-{0}\" data-id=\"{0}\" class=\"app-list-item\">";
                                            userTemplate += "<table>";
                                            userTemplate += "   <tbody>";
                                            userTemplate += "       <tr>";
                                            userTemplate += "           <td class=\"colfull\">";
                                            userTemplate += "               <div class=\"title\"><span class=\"name\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                                            userTemplate += "               <div class=\"description\">";
                                            userTemplate += "                   <div class=\"line\">";
                                            userTemplate += "                       <span class=\"info-text\">Status: " + (selectedResourcePlugin.result.info.enabled ? "habilitado" : "<span class=\"red-text\">desabilitado</span>") + "</span>";
                                            userTemplate += "                       <span class=\"info-text\">Status do recurso: " + (selectedResourcePlugin.result.info.resource_enabled ? "habilitado" : "<span class=\"red-text\">desabilitado</span>") + "</span>";
                                            userTemplate += "                       <span class=\"info-text\">Status do proxy: ";

                                            if (selectedResourcePlugin.result.info.proxy_last_sync > 0)
                                            {
                                                DateTime lastSync = new DateTime(1970, 1, 1).AddSeconds(selectedResourcePlugin.result.info.proxy_last_sync);
                                                TimeSpan ts = DateTime.Now - lastSync;
                                                if (ts.TotalSeconds > 60)
                                                {
                                                    userTemplate += "<span class=\"red-text\">Última conexão a " + MessageResource.FormatTs(ts) + "</span>";
                                                }
                                                else
                                                {
                                                    userTemplate += "On-line";
                                                }
                                            }
                                            else
                                            {
                                                userTemplate += "<span class=\"red-text\">Nunca se conectou no servidor</span>";
                                            }

                                            userTemplate += "                       </span>";
                                            userTemplate += "                   </div>";

                                            userTemplate += "                   <div class=\"line\">";
                                            userTemplate += "                       <span class=\"info-text\">Configurações gerais: " + (selectedResourcePlugin.result.check_config != null && selectedResourcePlugin.result.check_config.general ? "completa" : "<span class=\"red-text\">incompleta</span>") + "</span>";
                                            userTemplate += "                       <span class=\"info-text\">Parâmetros do plugin: " + (selectedResourcePlugin.result.check_config != null && selectedResourcePlugin.result.check_config.plugin_par ? "completa" : "<span class=\"red-text\">incompleto</span>") + "</span>";
                                            userTemplate += "                       <span class=\"info-text\">Mapeamento de campos: " + (selectedResourcePlugin.result.check_config != null && selectedResourcePlugin.result.check_config.mapping ? "completa" : "<span class=\"red-text\">incompleto</span>") + "</span>";
                                            userTemplate += "                   </div>";

                                            userTemplate += "               <div class=\"clear-block\"></div></div>";
                                            userTemplate += "               <div class=\"links\">";
                                            userTemplate += "                   <div class=\"line\">";

                                            if (selectedResourcePlugin.result.info.enabled)
                                                userTemplate += "                       <a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/action/disable/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Desativação\" confirm-text=\"Deseja desabilitar o recurso x plugin '{1}'?\" ok=\"Desabilitar\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Desabilitar</div></a>";
                                            else
                                                userTemplate += "                       <div class=\"ico icon-checkmark data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/action/enable/\">Habilitar</div>";

                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_step1/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-cogs\">Configurações gerais</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-stack\">Parâmetros do plugin</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_mapping/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-exchange\">Mapeamento de campos</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_role/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-link\">Vínculo com perfil</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_schedule/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-calendar\">Agendamento</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_lockrules/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-ban-circle\">Regras de bloqueio</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/config_ignore/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-erase\">Desconsiderar registros na importação</div></a>";
                                            userTemplate += "                       <div class=\"ico icon-upload data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/action/deploy_now/\">Publicar agora</div>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource/" + selectedResourcePlugin.result.info.resource_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-wrench\">Configuração do recurso</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/plugin/" + selectedResourcePlugin.result.info.plugin_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-wrench\">Configuração do plugin</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/identity/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-users\">Identidades</div></a>";
                                            userTemplate += "                       <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/add_identity/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-user-add\">Adicionar identidade</div></a>";
                                            userTemplate += "                       <div class=\"ico icon-paste data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/action/clone/\">Duplicar</div>";
                                            userTemplate += "                       <a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/resource_plugin/{0}/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o recurso x plugin '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Excluir</div></a>";
                                            userTemplate += "                   </div>";
                                            userTemplate += "               <div class=\"clear-block\"></div></div>";
                                            userTemplate += "           </td>";
                                            userTemplate += "       </tr>";
                                            userTemplate += "   </tbody>";
                                            userTemplate += "</table></div>";

                                            html += "<div class=\"box-container no-hover no-top-margin\">" + String.Format(userTemplate, selectedResourcePlugin.result.info.resource_plugin_id, selectedResourcePlugin.result.info.name, 0, (selectedResourcePlugin.result.info.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(selectedResourcePlugin.result.info.create_date), false) : "")) + "</div>";

                                            break;
                                    }

                                    
                                    switch (filter)
                                    {

                                        case "":
                                        case "identity":
                                            break;

                                        case "config_step1":
                                        case "config_step2":
                                        case "config_step3":
                                        case "config_step4":

                                            html += "<h3>Configurações gerais";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change/" + filter + "/\">";
                                            html += "<input type=\"hidden\" id=\"next_step\" name=\"next_step\" value=\"\">";
                                            html += "<div class=\"no-tabs-header pb10 relative\">";
                                            html += "   <ul id=\"nav-resource-plugin\" class=\"nav count4\">";
                                            html += "       <li " + (filter == "" || filter == "config_step1" ? "" : (hashData.GetValue("edit") == "1" ? "onclick=\"$('#next_step').val('config_step1'); $('#form_add_resource_plugin').submit();\"" : "onclick=\"window.location = '" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/config_step1/" + hash + "';\"")) + " class=\"step " + (filter == "" || filter == "config_step1" ? "current" : "") + "\"><span></span><span class=\"text\">Geral</span></li>";
                                            html += "       <li " + (filter == "config_step2" ? "" : (hashData.GetValue("edit") == "1" ? "onclick=\"$('#next_step').val('config_step2');$('#form_add_resource_plugin').submit();\"" : "onclick=\"window.location = '" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/config_step2/" + hash + "';\"")) + " class=\"step " + (filter == "config_step2" ? "current" : "") + "\"><span></span><span class=\"text\">Entrada</span></li>";
                                            html += "       <li " + (filter == "config_step3" ? "" : (hashData.GetValue("edit") == "1" ? "onclick=\"$('#next_step').val('config_step3');$('#form_add_resource_plugin').submit();\"" : "onclick=\"window.location = '" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/config_step3/" + hash + "';\"")) + " class=\"step " + (filter == "config_step3" ? "current" : "") + "\"><span></span><span class=\"text\">Saída</span></li>";
                                            html += "       <li " + (filter == "config_step4" ? "" : (hashData.GetValue("edit") == "1" ? "onclick=\"$('#next_step').val('config_step4');$('#form_add_resource_plugin').submit();\"" : "onclick=\"window.location = '" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/config_step4/" + hash + "';\"")) + " class=\"step " + (filter == "config_step4" ? "current" : "") + "\"><span></span><span class=\"text\">Campos</span></li>";
                                            html += "   </ul>";
                                            html += "<div class=\"clear-block\"></div></div>";
                                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                                            break;

                                        case "config_plugin":
                                            html += "<h3>Parâmetros de configuração do plugin";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_par/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                                            break;

                                        case "config_mapping":
                                            html += "<h3>Mapeamento de campos<div class=\"btn-box\">";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div>";
                                            html += "<a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/fields_fetch/\"><div class=\"a-btn ico icon-search\">Busca automática</div></a>";
                                            html += "</div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_mapping/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                                            break;

                                        case "config_role":
                                            html += "<h3>Vínculo com perfil";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_role/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                                            break;

                                        case "add_identity":
                                            html += "<h3>Vínculo com identidades";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/add_identity/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody>";
                                            break;


                                        case "config_lockrules":
                                            html += "<h3>Regras para bloquear entidades na importação";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_lockrules/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody class=\"role-fields\">";
                                            break;

                                        case "config_ignore":
                                            html += "<h3>Regras para desconsiderar registros na importação";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_ignore/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody class=\"role-fields\">";
                                            break;

                                        case "config_schedule":
                                            html += "<h3>Agendamentos";
                                            if (hashData.GetValue("edit") != "1")
                                                html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_schedules/" + filter + "/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody class=\"role-fields\">";
                                            break;

                                        case "fields_fetch":
                                            html += "<h3>Mapeamento de campos automatizado";
                                            html += "<div class=\"btn-box\"><div class=\"a-btn ico blue icon-search data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/new_fetch/\">Iniciar nova busca</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_mapping/" + filter + "/\">";
                                            break;


                                        case "fields_fetch_view":
                                            html += "<h3>Mapeamento de campos automatizado";
                                            html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-arrow-left\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/fields_fetch/'\">Voltar para listagem</div></div>";
                                            html += "</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/change_mapping_fetch/" + filter + "/\">";
                                            break;

                                    }

                                    #endregion header

                                    #region body

                                    switch (filter)
                                    {

                                        case "":
                                            break;

                                        case "config_step1":
                                            if (hashData.GetValue("edit") == "1")
                                            {
                                                try
                                                {

                                                    var tmpReq = new
                                                    {
                                                        jsonrpc = "1.0",
                                                        method = "resource.list",
                                                        parameters = new
                                                        {
                                                            page_size = Int32.MaxValue
                                                        },
                                                        id = 1
                                                    };

                                                    error = "";
                                                    rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                                                    jData = "";
                                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                    if (String.IsNullOrWhiteSpace(jData))
                                                        throw new Exception("");

                                                    ResourceListResult resourceList = JSON.Deserialize<ResourceListResult>(jData);
                                                    if (resourceList == null)
                                                    {
                                                        error = MessageResource.GetMessage("resource_not_found");
                                                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                                    }
                                                    else if (resourceList.error != null)
                                                    {
                                                        error = resourceList.error.data;
                                                    }
                                                    else if (resourceList.result == null)
                                                    {
                                                        error = MessageResource.GetMessage("resource_not_found");
                                                    }
                                                    else
                                                    {

                                                        var tmpReq2 = new
                                                        {
                                                            jsonrpc = "1.0",
                                                            method = "plugin.list",
                                                            parameters = new
                                                            {
                                                                page_size = Int32.MaxValue
                                                            },
                                                            id = 1
                                                        };

                                                        error = "";
                                                        rData = SafeTrend.Json.JSON.Serialize2(tmpReq2);
                                                        jData = "";
                                                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                        if (String.IsNullOrWhiteSpace(jData))
                                                            throw new Exception("");

                                                        PluginListResult pluginList = JSON.Deserialize<PluginListResult>(jData);
                                                        if (pluginList == null)
                                                        {
                                                            error = MessageResource.GetMessage("plugin_not_found");
                                                            //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                                        }
                                                        else if (pluginList.error != null)
                                                        {
                                                            error = pluginList.error.data;
                                                        }
                                                        else if (pluginList.result == null)
                                                        {
                                                            error = MessageResource.GetMessage("plugin_not_found");
                                                        }
                                                        else
                                                        {

                                                            String resource = "";
                                                            resource += "<select id=\"resource\" name=\"resource\" ><option value=\"\"></option>";
                                                            foreach (ResourceData r in resourceList.result)
                                                                resource += "<option value=\"" + r.resource_id + "\" " + (selectedResourcePlugin.result.info.resource_id == r.resource_id ? "selected" : "") + ">" + r.name + "</option>";
                                                            resource += "</select>";

                                                            String plugin = "";
                                                            plugin += "<select id=\"plugin\" name=\"plugin\" ><option value=\"\"></option>";
                                                            foreach (PluginData p in pluginList.result)
                                                                plugin += "<option value=\"" + p.plugin_id + "\" " + (selectedResourcePlugin.result.info.plugin_id == p.plugin_id ? "selected" : "") + ">" + p.name + "</option>";
                                                            plugin += "</select>";

                                                            html += String.Format(infoTemplate, "Recurso", resource);
                                                            html += String.Format(infoTemplate, "Plugin", plugin);
                                                            html += String.Format(infoTemplate, "Domínio de e-mail", "<input id=\"mail_domain\" name=\"mail_domain\" placeholder=\"Digite o domínio de e-mail\" type=\"text\"\" value=\"" + selectedResourcePlugin.result.info.mail_domain + "\">");
                                                            html += String.Format(infoTemplate, "Desabilitado", (!selectedResourcePlugin.result.info.enabled ? "<div id=\"resource-plugin-save\" class=\"a-btn blue data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/enable/\">Habilitar</div><span class=\"description\">Para habilitar o plugin todo o cadastro deve estar completo.</span>" : "<div id=\"resource-plugin-save\" class=\"a-btn blue data-action\" data-action=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/disable/\">Desabilitar</div>"));
                                                            html += String.Format(infoTemplate, "Criado em", MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(selectedResourcePlugin.result.info.create_date), false));
                                                        }
                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    error = MessageResource.GetMessage("api_error");
                                                }

                                            }
                                            else
                                            {

                                                html += String.Format(infoTemplate, "Nome", selectedResourcePlugin.result.info.name);
                                                html += String.Format(infoTemplate, "Recurso", selectedResourcePlugin.result.related_names.resource_name);
                                                html += String.Format(infoTemplate, "Plugin", selectedResourcePlugin.result.related_names.plugin_name);
                                                html += String.Format(infoTemplate, "Domínio de e-mail", selectedResourcePlugin.result.info.mail_domain);
                                                html += String.Format(infoTemplate, "Desabilitado", (!selectedResourcePlugin.result.info.enabled ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")));
                                                html += String.Format(infoTemplate, "Criado em", MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(selectedResourcePlugin.result.info.create_date), false));

                                            }


                                            contentRet = new WebJsonResponse("#content-wrapper", html);
                                            contentRet.js = jsEdit;
                                            break;

                                        case "config_step2":
                                            if (hashData.GetValue("edit") == "1")
                                            {
                                                html += String.Format(infoTemplate, "Habilita importação", "<input id=\"enable_import\" name=\"enable_import\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.enable_import ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a importação dos dados deste plugin</span>");
                                                html += String.Format(infoTemplate, "Permite adição de entidade", "<input id=\"permit_add_entity\" name=\"permit_add_entity\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.permit_add_entity ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a criação de nova entidade pelos dados importados através deste plugin</span>");
                                                html += String.Format(infoTemplate, "Criação de login", "<input id=\"build_login\" name=\"build_login\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.build_login ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a criação de novo login para novas entidades</span>");
                                                html += String.Format(infoTemplate, "Criação de e-mail", "<input id=\"build_mail\" name=\"build_mail\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.build_mail ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a criação de novo campo de e-mail para novas entidades</span>");
                                                html += String.Format(infoTemplate, "Importação de grupos", "<input id=\"import_groups\" name=\"import_groups\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.import_groups ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a importação dos grupos de usuário. Estes grupos serão importados como perfis.</span>");
                                                html += String.Format(infoTemplate, "Importação de pastas", "<input id=\"import_containers\" name=\"import_containers\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.import_containers ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a importação das pastas de usuário.</span>");
                                                
                                            }
                                            else
                                            {

                                                html += String.Format(infoTemplate, "Habilita importação", (selectedResourcePlugin.result.info.enable_import ? MessageResource.GetMessage("yes") + "<span class=\"description\">Permite a importação dos dados do plugin vínculado.</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Os dados do plugin não serão importados</span>"));
                                                html += String.Format(infoTemplate, "Permite adição de entidade", (selectedResourcePlugin.result.info.permit_add_entity ? MessageResource.GetMessage("yes") + "<span class=\"description\">Entidades e identidades serão inseridas</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Entidades não serão inseridas, somente identidade quando encontrado uma entidade existente.</span>"));
                                                html += String.Format(infoTemplate, "Criação de login", (selectedResourcePlugin.result.info.build_login ? MessageResource.GetMessage("yes") + "<span class=\"description\">Permite a criação de login caso os pados importados não contenham um.</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Não serão criados novos logins, caso os dados importados não contenha um login o registro não será importado.</span>"));
                                                html += String.Format(infoTemplate, "Criação de e-mail", (selectedResourcePlugin.result.info.build_mail ? MessageResource.GetMessage("yes") + "<span class=\"description\">Permite a criação de nome de e-mail caso os pados importados não contenham um.</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Não será criado nome de e-mail, caso os dados importados não contenha um e-mail válido o registro será importado porém sem um e-mail cadastrado.</span>"));
                                                html += String.Format(infoTemplate, "Importação de grupos", (selectedResourcePlugin.result.info.import_groups ? MessageResource.GetMessage("yes") + "<span class=\"description\">Permite a importação dos grupos de usuário. Estes grupos serão importados como perfis.</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Não serão importados os grupos de usuário.</span>"));
                                                html += String.Format(infoTemplate, "Importação de pastas", (selectedResourcePlugin.result.info.import_containers ? MessageResource.GetMessage("yes") + "<span class=\"description\">Permite a importação das pastas de usuário.</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Não serão importados as pastas de usuário.</span>"));
                                                
                                            }

                                            contentRet = new WebJsonResponse("#content-wrapper", html);
                                            contentRet.js = jsEdit;
                                            break;

                                        case "config_step3":
                                            if (hashData.GetValue("edit") == "1")
                                            {
                                                html += String.Format(infoTemplate, "Habilita publicação", "<input id=\"enable_deploy\" name=\"enable_deploy\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.enable_deploy ? "checked" : "") + "><span class=\"description\">Habilita/desabilita a publicação dos dados para este plugin</span>");
                                                html += String.Format(infoTemplate, "Publicação em pacotes idividuais", "<input id=\"deploy_individual_package\" name=\"deploy_individual_package\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.deploy_individual_package ? "checked" : "") + "><span class=\"description\">Quando habilitado o sistema enviará cada entidade/identidade em um pacote de dados separado, isso forçará que o plugin execute a re-importação logo após a publicação</span>");
                                                
                                                html += String.Format(infoTemplate, "Publicação de todas identidades", "<input id=\"deploy_all\" name=\"deploy_all\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.deploy_all ? "checked" : "") + "><span class=\"description\">Quando habilitado o sistema enviará todas as identidades do contexto deste recurso caso contrário somente as identidades pertencente as 'perfis' vinculadas a este recurso x plugin serão publicadas</span>");
                                                html += String.Format(infoTemplate, "Publicação depois do primeiro login", "<input id=\"deploy_after_login\" name=\"deploy_after_login\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.deploy_after_login ? "checked" : "") + "><span class=\"description\">Quando habilitado somente serão enviadas para publicação as entidades que realizaram login no sistema</span>");
                                                html += String.Format(infoTemplate, "Publicação da senha depois do primeiro login", "<input id=\"password_after_login\" name=\"password_after_login\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.password_after_login ? "checked" : "") + "><span class=\"description\">Quando habilitado somente serão enviadas as senhas das entidades que realizaram login no sistema.<br>***Ao desabilitar esta opção a senha padrão (cadastrada no sistema) será realizado para as entidades que nunca realizaram login no sistema, desta forma, trocará a senha deste recurso.</span>");
                                                html += String.Format(infoTemplate, "Tipo de hash para envio de senha", "<select id=\"password_hash\" name=\"password_hash\"><option value=\"none\" " + (selectedResourcePlugin.result.info.deploy_password_hash == "none" ? "selected" : "") + ">Nenhum (clear text)</option><option value=\"md5\" " + (selectedResourcePlugin.result.info.deploy_password_hash == "md5" ? "selected" : "") + ">MD5</option><option value=\"sha1\" " + (selectedResourcePlugin.result.info.deploy_password_hash == "sha1" ? "selected" : "") + ">SHA1</option><option value=\"sha256\" " + (selectedResourcePlugin.result.info.deploy_password_hash == "sha256" ? "selected" : "") + ">SHA256</option><option value=\"sha512\" " + (selectedResourcePlugin.result.info.deploy_password_hash == "sha512" ? "selected" : "") + ">SHA512</option></select><span class=\"description\">Tipo de hash para envio da senha ao sistema integrado.</span>");

                                                html += String.Format(infoTemplate, "Utilizar 'SALT' de senha", "<input id=\"use_password_salt\" name=\"use_password_salt\" type=\"checkbox\" " + (selectedResourcePlugin.result.info.use_password_salt ? "checked" : "") + "><span class=\"description\">Habilita/desabilita autilização de um texto como SALT na senha</span>");
                                                html += String.Format(infoTemplate, "Posição do SALT", "<select id=\"password_salt_end\" name=\"password_salt_end\"><option value=\"0\" " + (selectedResourcePlugin.result.info.password_salt_end == false ? "selected" : "") + ">Antes da senha</option><option value=\"1\" " + (selectedResourcePlugin.result.info.password_salt_end == true ? "selected" : "") + ">Depois da senha</option></select><span class=\"description\">Posição onde o texto do SALT será posicionado.</span>");
                                                html += String.Format(infoTemplate, "Texto SALT da senha", "<input id=\"password_salt\" name=\"password_salt\" placeholder=\"Digite o texto que será utilizado como SALT\" type=\"text\"\" value=\"" + selectedResourcePlugin.result.info.password_salt + "\">");
                                            }
                                            else
                                            {
                                                html += String.Format(infoTemplate, "Habilita publicação", (selectedResourcePlugin.result.info.enable_deploy ? MessageResource.GetMessage("yes") + "<span class=\"description\">Plugin será utilizado para publicação de dados</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Plugin não será utilizado para publicação de dados.</span>"));
                                                html += String.Format(infoTemplate, "Publicação em pacotes idividuais", (selectedResourcePlugin.result.info.deploy_individual_package ? MessageResource.GetMessage("yes") + "<span class=\"description\">Plugin enviará cada entidade/identidade em um pacote de dados separado, isso forçará que o plugin execute a re-importação logo após a publicação</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Plugin enviará várias entidade/identidade em um unico pacote de dados</span>"));
                                                
                                                html += String.Format(infoTemplate, "Publicação de todas identidades", (selectedResourcePlugin.result.info.deploy_all ? MessageResource.GetMessage("yes") + "<span class=\"description\">Plugin enviará todas as identidades do contexto deste recurso</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Somente as identidades pertencente as 'perfis' vinculadas a este recurso x plugin serão publicadas</span>"));
                                                html += String.Format(infoTemplate, "Publicação depois do primeiro login", (selectedResourcePlugin.result.info.deploy_after_login ? MessageResource.GetMessage("yes") + "<span class=\"description\">Somente serão enviadas para publicação as entidades que realizaram login no sistema</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Todas as identidades serão enviadas, independente se realizaram, ou não, login no sistema</span>"));
                                                html += String.Format(infoTemplate, "Publicação da senha depois do primeiro login", (selectedResourcePlugin.result.info.password_after_login ? MessageResource.GetMessage("yes") + "<span class=\"description\">Somente serão enviadas as senhas das entidades que realizaram login no sistema</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Todas as senhas serão enviadas, independente se realizaram, ou não, login no sistema.<br>***Esta opção deve ser utilizada com cuidado pois trocará a senha do recurso vincuado para a senha padrão cadastrada no sistema.</span>"));
                                                html += String.Format(infoTemplate, "Tipo de hash para envio de senha", selectedResourcePlugin.result.info.deploy_password_hash.ToUpper() + "<span class=\"description\">Tipo de hash para envio da senha ao sistema integrado.</span>");

                                                html += String.Format(infoTemplate, "Utilizar 'SALT' de senha", (selectedResourcePlugin.result.info.use_password_salt ? MessageResource.GetMessage("yes") + "<span class=\"description\">Será adicionado o texto 'SALT' a senha do usuário no momento da publicação</span>" : MessageResource.GetMessage("no") + "<span class=\"description\">Não será adicionado o texto 'SALT' a senha do usuário no momento da publicação.</span>"));
                                                html += String.Format(infoTemplate, "Posição do SALT", (selectedResourcePlugin.result.info.password_salt_end ? "Depois da senha<span class=\"description\">O texto do SALT será posicionado ao final da senha do usuário. (PASSWORD + SALT)</span>" : "Antes da senha<span class=\"description\">O texto do SALT será posicionado antes da senha do usuário. (SALT + PASSWORD)</span>"));
                                                html += String.Format(infoTemplate, "Texto SALT da senha", (!String.IsNullOrWhiteSpace(selectedResourcePlugin.result.info.password_salt) ? selectedResourcePlugin.result.info.password_salt : "(Vazio)"));
                                            }

                                            contentRet = new WebJsonResponse("#content-wrapper", html);
                                            contentRet.js = jsEdit;
                                            break;

                                        case "config_step4":
                                            if (hashData.GetValue("edit") == "1")
                                            {

                                                var tmpReq2 = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "field.list",
                                                    parameters = new
                                                    {
                                                        page_size = Int32.MaxValue
                                                    },
                                                    id = 1
                                                };

                                                error = "";
                                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq2);
                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                FieldListResult fieldList = JSON.Deserialize<FieldListResult>(jData);
                                                if (fieldList == null)
                                                {
                                                    error = MessageResource.GetMessage("plugin_not_found");
                                                }
                                                else if (fieldList.error != null)
                                                {
                                                    error = fieldList.error.data;
                                                }
                                                else if (fieldList.result == null)
                                                {
                                                    error = MessageResource.GetMessage("plugin_not_found");
                                                }
                                                else
                                                {

                                                    String nameField = "";
                                                    nameField += "<select id=\"name_field_id\" name=\"name_field_id\" ><option value=\"\"></option>";
                                                    foreach (FieldData f in fieldList.result)
                                                        nameField += "<option value=\"" + f.field_id + "\" " + (selectedResourcePlugin.result.info.name_field_id == f.field_id ? "selected" : "") + ">" + f.name + "</option>";
                                                    nameField += "</select>";

                                                    String mailField = "";
                                                    mailField += "<select id=\"mail_field_id\" name=\"mail_field_id\" ><option value=\"\"></option>";
                                                    foreach (FieldData f in fieldList.result)
                                                        mailField += "<option value=\"" + f.field_id + "\" " + (selectedResourcePlugin.result.info.mail_field_id == f.field_id ? "selected" : "") + ">" + f.name + "</option>";
                                                    mailField += "</select>";


                                                    String loginField = "";
                                                    loginField += "<select id=\"login_field_id\" name=\"login_field_id\" ><option value=\"\"></option>";
                                                    foreach (FieldData f in fieldList.result)
                                                        loginField += "<option value=\"" + f.field_id + "\" " + (selectedResourcePlugin.result.info.login_field_id == f.field_id ? "selected" : "") + ">" + f.name + "</option>";
                                                    loginField += "</select>";

                                                    html += String.Format(infoTemplate, "Campo de nome", nameField + "<span class=\"description\">Selecione o campo que será utilizado para publicação, importação e vínculo do nome do usuário</span>");
                                                    html += String.Format(infoTemplate, "Campo de e-mail", mailField + "<span class=\"description\">Selecione o campo que será utilizado para publicação, importação e vínculo do e-mail do usuário</span>");
                                                    html += String.Format(infoTemplate, "Campo de login", loginField + "<span class=\"description\">Selecione o campo que será utilizado para publicação, importação e vínculo do login do usuário</span>");
                                                }

                                            }
                                            else
                                            {
                                                html += String.Format(infoTemplate, "Campo de nome", (!String.IsNullOrEmpty(selectedResourcePlugin.result.related_names.name_field_name) ? selectedResourcePlugin.result.related_names.name_field_name + "<span class=\"description\">Este campo será utilizado para publicação/vínculo do nome</span>" : "Nenhum campo cadastrado"));
                                                html += String.Format(infoTemplate, "Campo de e-mail", (!String.IsNullOrEmpty(selectedResourcePlugin.result.related_names.mail_field_name) ? selectedResourcePlugin.result.related_names.mail_field_name + "<span class=\"description\">Este campo será utilizado para publicação/vínculo do e-mail</span>" : "Nenhum campo cadastrado"));
                                                html += String.Format(infoTemplate, "Campo de login", (!String.IsNullOrEmpty(selectedResourcePlugin.result.related_names.login_field_name) ? selectedResourcePlugin.result.related_names.login_field_name + "<span class=\"description\">Este campo será utilizado para publicação/vínculo do login</span>" : "Nenhum campo cadastrado"));
                                            }

                                            contentRet = new WebJsonResponse("#content-wrapper", html);
                                            contentRet.js = jsEdit;
                                            break;

                                        case "config_plugin":
                                            var tmpReq3 = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "plugin.get",
                                                    parameters = new
                                                    {
                                                        pluginid = selectedResourcePlugin.result.info.plugin_id,
                                                        parameters = true
                                                    },
                                                    id = 1
                                                };

                                            error = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq3);
                                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                            PluginGetResult pluginData = JSON.Deserialize<PluginGetResult>(jData);
                                            if (pluginData == null)
                                            {
                                                error = MessageResource.GetMessage("plugin_not_found");
                                            }
                                            else if (pluginData.error != null)
                                            {
                                                error = pluginData.error.data;
                                            }
                                            else if (pluginData.result == null)
                                            {
                                                error = MessageResource.GetMessage("plugin_not_found");
                                            }
                                            else if (pluginData.result.parameters == null)
                                            {
                                                error = MessageResource.GetMessage("plugin_not_found");
                                            }
                                            else if (pluginData.result.parameters.Count == 0)
                                            {
                                                html += String.Format(infoTemplate, "", "Este plugin não exige configuração");
                                            }
                                            else
                                            {

                                                var tmpReq4 = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "resourceplugin.parameters",
                                                    parameters = new
                                                    {
                                                        resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                    },
                                                    id = 1
                                                };

                                                error = "";
                                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq4);
                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                ResourcePluginParamterList rpParameters = JSON.Deserialize<ResourcePluginParamterList>(jData);
                                                if (rpParameters == null)
                                                {
                                                    error = MessageResource.GetMessage("resource_plugin_not_found");
                                                }
                                                else if (rpParameters.error != null)
                                                {
                                                    error = rpParameters.error.data;
                                                }
                                                else if (rpParameters.result == null)
                                                {
                                                    error = MessageResource.GetMessage("resource_plugin_not_found");
                                                }
                                                else
                                                {
                                                    if (hashData.GetValue("edit") == "1")
                                                    {

                                                        foreach (PluginParamterData pd in pluginData.result.parameters)
                                                        {
                                                            String value = "";

                                                            foreach (ResourcePluginParameter par in rpParameters.result)
                                                                if (par.key == pd.key)
                                                                    value = par.value;

                                                            String form = "";

                                                            if (pd.type.ToLower() == "stringfixedlist")
                                                            {
                                                                form += "<select id=\"login_field_id\" name=\"login_field_id\" ><option value=\"\"></option>";
                                                                foreach (String lv in pd.list_value)
                                                                    form += "<option value=\"" + lv + "\" " + (value == lv ? "selected" : "") + ">" + lv + "</option>";
                                                                form += "</select>";

                                                            }
                                                            else if (pd.type.ToLower() == "base64filedata")
                                                            {
                                                                form = "<div class=\"field-upload-box\" id=\"" + pd.key + "-upload-content\"></div>";
                                                                js += "$('#" + pd.key + "-upload-content').fileDragDrop({ name: '" + pd.key + "' " + (value != "" ? ", click_text: 'Clique para enviar um novo arquivo', pre_load_value: '" + value + "' " : ", click_text: 'Clique para enviar o arquivo'") + " })";

                                                                if (value != "")
                                                                    pd.description = "Arquivo existente, clique para substitui-lo.<br>" + pd.description;
                                                            }
                                                            else if (pd.type.ToLower() == "password")
                                                            {
                                                                form = "<input id=\"" + pd.key + "\" name=\"" + pd.key + "\" placeholder=\"" + pd.name + "\" type=\"password\" value=\"" + value + "\">";
                                                            }
                                                            else
                                                            {
                                                                form = "<input id=\"" + pd.key + "\" name=\"" + pd.key + "\" placeholder=\"" + pd.name + "\" type=\"text\" value=\"" + value + "\">";
                                                            }


                                                            //<input type="file" name="datafile" size="40">
                                                            html += String.Format(infoTemplate, pd.name, form + "<span class=\"description\">" + (pd.import_required || pd.deploy_required ? "(obrigatório) " : "") + pd.description + "</span>");
                                                        }


                                                    }
                                                    else
                                                    {

                                                        foreach (PluginParamterData pd in pluginData.result.parameters)
                                                        {
                                                            String value = "Não definido";

                                                            foreach (ResourcePluginParameter par in rpParameters.result)
                                                                if (par.key == pd.key)
                                                                    value = par.value;

                                                            if ((pd.type.ToLower() == "password") && (value != "Não definido"))
                                                                value = "****";

                                                            if (pd.type == "base64filedata")
                                                                value = "Base64 String: " + value;

                                                            if (value.Length > 50)
                                                                value = value.Substring(0, 50) + "...";

                                                            html += String.Format(infoTemplate, pd.name, value + "<span class=\"description\">" + pd.description + "</span>");
                                                        }

                                                    }
                                                }
                                            }

                                            break;

                                        case "config_mapping":
                                            var tmpReq5 = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "resourceplugin.mapping",
                                                    parameters = new
                                                    {
                                                        resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                    },
                                                    id = 1
                                                };

                                            error = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq5);
                                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                            ResourcePluginMappingList rpMapping = JSON.Deserialize<ResourcePluginMappingList>(jData);
                                            if (rpMapping == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else if (rpMapping.error != null)
                                            {
                                                error = rpMapping.error.data;
                                            }
                                            else if (rpMapping.result == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else
                                            {
                                                if (hashData.GetValue("edit") == "1")
                                                {
                                                    List<String> fieldNames = new List<string>();

                                                    try
                                                    {

                                                        rData = "";

                                                        var tmpReq = new
                                                        {
                                                            jsonrpc = "1.0",
                                                            method = "field.list",
                                                            parameters = new
                                                            {
                                                                page_size = Int32.MaxValue,
                                                                page = 1
                                                            },
                                                            id = 1
                                                        };

                                                        rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                                                        jData = "";
                                                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                        FieldListResult fieldList = JSON.Deserialize<FieldListResult>(jData);
                                                        if ((fieldList != null) && (fieldList.error == null) && (fieldList.result != null))
                                                        {

                                                            foreach (FieldData field in fieldList.result)
                                                                fieldNames.Add(field.name);

                                                        }

                                                    }
                                                    catch (Exception ex) { }

                                                    html += String.Format(infoTemplate, "Nome do campo", "<input id=\"add_field\" name=\"add_field\" placeholder=\"Digite o nome do campo a ser mapeado\" type=\"text\">");

                                                    if (fieldNames.Count > 0)
                                                        html += String.Format(infoTemplate, "Campos disponíveis", "<span class=\"description\">" + String.Join(", ", fieldNames) + "</span>");

                                                    html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                                    html += "<h3>Campos mapeados</h3>";
                                                    html += "<div id=\"field_mapping_fields\" class=\"box-container\">";

                                                    if (rpMapping.result.Count == 0)
                                                    {
                                                        html += "<div class=\"no-tabs none\">Nenhum campo mapeado</div>";
                                                    }
                                                    else
                                                    {

                                                        String fieldHtmlTemplate = "<div id=\"new-field-{0}\" data-id=\"{0}\" data-name=\"{2}\" class=\"app-list-item p50 left\">";
                                                        fieldHtmlTemplate += "<input type=\"hidden\" name=\"content_id\" value=\"{0}\">";
                                                        fieldHtmlTemplate += "<input type=\"hidden\" name=\"field_id_{0}\" value=\"{1}\">";
                                                        fieldHtmlTemplate += "<input type=\"hidden\" name=\"field_name_{0}\" value=\"{2}\">";
                                                        fieldHtmlTemplate += "<table>";
                                                        fieldHtmlTemplate += "   <tbody>";
                                                        fieldHtmlTemplate += "       <tr>";
                                                        fieldHtmlTemplate += "           <td class=\"colfull\">";
                                                        fieldHtmlTemplate += "               <div class=\"title\"><span class=\"name\" id=\"field_name_{0}\" data-id=\"{0}\">{2}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                                                        fieldHtmlTemplate += "               <div class=\"description fields small\">{4}<div class=\"clear-block\"></div></div>";
                                                        fieldHtmlTemplate += "               <div class=\"links small\">";
                                                        fieldHtmlTemplate += "                   <div class=\"last\"><div class=\"ico icon-close\" onclick=\"$(this).closest('.app-list-item').remove();\">Excluir campo</div></a><div class=\"clear-block\"></div></div>";
                                                        fieldHtmlTemplate += "               </div>";
                                                        fieldHtmlTemplate += "           </td>";
                                                        fieldHtmlTemplate += "       </tr>";
                                                        fieldHtmlTemplate += "   </tbody>";
                                                        fieldHtmlTemplate += "</table></div>";



                                                        foreach (ResourcePluginMapping map in rpMapping.result)
                                                        {
                                                            String id = Guid.NewGuid().ToString();

                                                            String desc = "";
                                                            desc += "<table><tbody>";
                                                            desc += String.Format(infoTemplate, "Campo do recurso", "<input id=\"data_name_" + id + "\" name=\"data_name_" + id + "\" placeholder=\"Digite o nome do campo relacionado\" type=\"text\" value=\"" + map.data_name + "\">");
                                                            desc += String.Format(infoTemplate, "É um identificador?", "<input id=\"is_id_" + id + "\" name=\"is_id_" + id + "\" type=\"checkbox\" " + (map.is_id ? "checked" : "") + ">");
                                                            desc += String.Format(infoTemplate, "É senha?", "<input id=\"is_password_" + id + "\" name=\"is_password_" + id + "\" type=\"checkbox\" " + (map.is_password ? "checked" : "") + ">");
                                                            desc += String.Format(infoTemplate, "É um campo único?", "<input id=\"is_unique_" + id + "\" name=\"is_unique_" + id + "\" type=\"checkbox\" " + (map.is_unique_property ? "checked" : "") + ">");
                                                            desc += "</tbody></table>";

                                                            html += String.Format(fieldHtmlTemplate, id, map.field_id, map.field_name, "", desc);
                                                        }
                                                    }

                                                    html += "</div><div class=\"clear-block\"></div>";

                                                    js = "iamadmin.autoCompleteText('#add_field', '" + ApplicationVirtualPath + "admin/resource_plugin/content/search_field/', {context_id: '"+ selectedResourcePlugin.result.info.context_id +"'} ,function(thisId, selectedItem){ $(thisId).val(''); $('#field_mapping_fields .none').remove(); $('#field_mapping_fields').append(selectedItem.html); } )";

                                                }
                                                else
                                                {
                                                    if (rpMapping.result.Count == 0)
                                                        html += String.Format(infoTemplate, "Nenhum campo mapeado", "");


                                                    foreach (ResourcePluginMapping map in rpMapping.result)
                                                    {
                                                        List<String> prop = new List<string>();
                                                        if (map.is_id)
                                                            prop.Add("é identificador (id)");

                                                        if (map.is_password)
                                                            prop.Add("é campo de senha");

                                                        if (map.is_unique_property)
                                                            prop.Add("é campo único (não pode conter duplicado)");

                                                        if (prop.Count == 0)
                                                            prop.Add("campo simples");

                                                        html += String.Format(infoTemplate, map.field_name, "Nome do campo: " + map.data_name + "<span class=\"description\">Propriedades: " + String.Join(", ", prop) + "</span>");

                                                    }
                                                    html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                                }


                                            }
                                            break;


                                        case "config_role":
                                            var tmpReqPg = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "plugin.get",
                                                    parameters = new
                                                    {
                                                        pluginid = selectedResourcePlugin.result.info.plugin_id,
                                                        parameters = true
                                                    },
                                                    id = 1
                                                };

                                            error = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(tmpReqPg);
                                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                            PluginGetResult pluginData1 = JSON.Deserialize<PluginGetResult>(jData);
                                            if (pluginData1 == null)
                                            {
                                                error = MessageResource.GetMessage("plugin_not_found");
                                            }
                                            else if (pluginData1.error != null)
                                            {
                                                error = pluginData1.error.data;
                                            }
                                            else if (pluginData1.result == null)
                                            {
                                                error = MessageResource.GetMessage("plugin_not_found");
                                            }
                                            else if ((pluginData1.result.actions == null))
                                            {
                                                html += String.Format(errorTemplate, "Este plugin não permite esta operação");
                                                hashData.Clear();
                                            }
                                            else
                                            {

                                                var tmpReq6 = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "resourceplugin.roles",
                                                    parameters = new
                                                    {
                                                        resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                    },
                                                    id = 1
                                                };

                                                error = "";
                                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq6);
                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                ResourcePluginRoleList rpRole = JSON.Deserialize<ResourcePluginRoleList>(jData);
                                                if (rpRole == null)
                                                {
                                                    error = MessageResource.GetMessage("resource_plugin_not_found");
                                                }
                                                else if (rpRole.error != null)
                                                {
                                                    error = rpRole.error.data;
                                                }
                                                else if (rpRole.result == null)
                                                {
                                                    error = MessageResource.GetMessage("resource_plugin_not_found");
                                                }
                                                else
                                                {
                                                    if (hashData.GetValue("edit") == "1")
                                                    {
                                                        List<String> roleNames = new List<string>();

                                                        try
                                                        {

                                                            rData = "";

                                                            var tmpReq = new
                                                            {
                                                                jsonrpc = "1.0",
                                                                method = "role.list",
                                                                parameters = new
                                                                {
                                                                    page_size = Int32.MaxValue,
                                                                    page = 1,
                                                                    filter = new { contextid = selectedResourcePlugin.result.info.context_id }
                                                                },
                                                                id = 1
                                                            };

                                                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                                                            jData = "";
                                                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                            RoleListResult roleList = JSON.Deserialize<RoleListResult>(jData);
                                                            if ((roleList != null) && (roleList.error == null) && (roleList.result != null))
                                                            {

                                                                foreach (RoleData role in roleList.result)
                                                                    roleNames.Add(role.name);

                                                            }

                                                        }
                                                        catch (Exception ex) { }

                                                        
                                                        rData = SafeTrend.Json.JSON.Serialize2(new
                                                        {
                                                            jsonrpc = "1.0",
                                                            method = "filter.list",
                                                            parameters = new
                                                            {
                                                                page_size = Int16.MaxValue,
                                                                page = 1
                                                            },
                                                            id = 1
                                                        });
                                                        jData = "";
                                                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                        FilterListResult filters = JSON.Deserialize<FilterListResult>(jData);


                                                        html += String.Format(infoTemplate, "Nome do campo", "<input id=\"add_role\" name=\"add_role\" placeholder=\"Digite o nome do perfil\" type=\"text\">");

                                                        if (roleNames.Count > 0)
                                                            html += String.Format(infoTemplate, "Perfis disponíveis", "<span class=\"description\">" + String.Join(", ", roleNames) + "</span>");

                                                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                                        html += "<h3>Perfis vinculados</h3>";
                                                        html += "<div id=\"field_mapping_fields\" class=\"box-container\">";

                                                        String fSel = "<input type=\"hidden\" name=\"role_[role_id]_filter\" value=\"[id]\" /><select id=\"role_[role_id]_filter_[id]\" name=\"role_[role_id]_filter_[id]\">";
                                                        fSel += "<option value=\"\"></option>";
                                                        foreach (FilterData f in filters.result)
                                                            fSel += "<option value=\"" + f.filter_id + "\">" + f.name + "</option>";
                                                        fSel += "</select>";
                                                                        

                                                        String newAct = String.Format(infoTemplate, "Ação", "<div class=\"custom-item\" id=\"act_[id]\" onclick=\"iamadmin.buildRoleActRule(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-action-build/\"><input type=\"hidden\" name=\"role_[role_id]_act\" value=\"[id]\" /><input type=\"hidden\" name=\"role_[role_id]_act_key_[id]\" class=\"key\" /><input type=\"hidden\" name=\"role_[role_id]_act_add_value_[id]\" class=\"add_value\" /><input type=\"hidden\" name=\"role_[role_id]_act_del_value_[id]\" class=\"del_value\" /><input type=\"hidden\" name=\"role_[role_id]_act_additional_data_[id]\" class=\"additional_data\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\"  onclick=\"iamadmin.addAction(this,[role_id]);\"></div><div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>");
                                                        String newTimeAcl = String.Format(infoTemplate, "Controle por horário", "<div class=\"custom-item\" id=\"act_[id]\" onclick=\"iamadmin.buildRoleTimeAcl(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-build/\" check-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-check/\"><input type=\"hidden\" name=\"role_[role_id]_timeacl\" value=\"[id]\" /><input type=\"hidden\" name=\"role_[role_id]_timeacl_type_[id]\" class=\"type\" /><input type=\"hidden\" name=\"role_[role_id]_timeacl_start_time_[id]\" class=\"start_time\" /><input type=\"hidden\" name=\"role_[role_id]_timeacl_end_time_[id]\" class=\"end_time\" /><input type=\"hidden\" name=\"role_[role_id]_timeacl_week_day_[id]\" class=\"week_day\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\"  onclick=\"iamadmin.addAcl(this,[role_id]);\"></div><div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>");
                                                        String newFilter = String.Format(infoTemplate, "Filtro de vínculo", fSel + "<div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addFilter(this,[role_id]);\"></div><div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>");
                                                        if (rpRole.result.Count == 0)
                                                        {
                                                            html += "<div class=\"no-tabs none\">Nenhum perfil vinculado</div>";
                                                        }
                                                        else
                                                        {

                                                            String fieldHtmlTemplate = "<div id=\"new-field-{0}\" data-id=\"{0}\" data-name=\"{1}\" class=\"app-list-item p50 left\">";
                                                            fieldHtmlTemplate += "<input type=\"hidden\" name=\"role_id\" value=\"{0}\">";
                                                            fieldHtmlTemplate += "<input type=\"hidden\" name=\"role_name_{0}\" value=\"{1}\">";
                                                            fieldHtmlTemplate += "<table>";
                                                            fieldHtmlTemplate += "   <tbody>";
                                                            fieldHtmlTemplate += "       <tr>";
                                                            fieldHtmlTemplate += "           <td class=\"colfull\">";
                                                            fieldHtmlTemplate += "               <div class=\"title\"><span class=\"name\" id=\"field_name_{0}\" data-id=\"{0}\">{1}</span><span class=\"date\">{2}</span><div class=\"clear-block\"></div></div>";
                                                            fieldHtmlTemplate += "               <div class=\"description fields small\">{3}<div class=\"clear-block\"></div></div>";
                                                            fieldHtmlTemplate += "               <div class=\"links small\">";
                                                            fieldHtmlTemplate += "                   <div class=\"last\"><div class=\"ico icon-close\" onclick=\"$(this).closest('.app-list-item').remove();\">Excluir perfil</div></a><div class=\"clear-block\"></div></div>";
                                                            fieldHtmlTemplate += "               </div>";
                                                            fieldHtmlTemplate += "           </td>";
                                                            fieldHtmlTemplate += "       </tr>";
                                                            fieldHtmlTemplate += "   </tbody>";
                                                            fieldHtmlTemplate += "</table></div>";


                                                            foreach (ResourcePluginRole role in rpRole.result)
                                                            {
                                                                String desc = "";

                                                                //new Date().getTime()
                                                                //$('act-content').append($('"+ newAct +"').replace('[id]',new Date().getTime()));
                                                                desc += "<table><tbody id=\"act-content\">";

                                                                
                                                                if ((role.actions != null) && (role.actions.Count > 0))
                                                                {
                                                                    foreach (PluginActionData ad in pluginData1.result.actions)
                                                                        for (Int32 i = 0; i < role.actions.Count; i++)
                                                                            if (ad.key == role.actions[i].action_key)
                                                                                desc += String.Format(infoTemplate, "Ação", "<div class=\"custom-item\" id=\"act_" + i + "\" onclick=\"iamadmin.buildRoleActRule(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-action-build/\"><input type=\"hidden\" name=\"role_" + role.role_id + "_act\" value=\"" + i + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_key_" + i + "\" class=\"key\" value=\"" + role.actions[i].action_key + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_add_value_" + i + "\" class=\"add_value\" value=\"" + role.actions[i].action_add_value + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_del_value_" + i + "\" class=\"del_value\" value=\"" + role.actions[i].action_del_value + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_additional_data_" + i + "\" class=\"additional_data\" value=\"" + role.actions[i].additional_data + "\" /><span>" + ad.name + "</span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addAction(this, '" + role.role_id + "');\"></div>" + (i > 0 ? "<div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>" : ""));
                                                                }
                                                                else
                                                                {
                                                                    desc += String.Format(infoTemplate, "Ação", "<div class=\"custom-item\" id=\"act_1\" onclick=\"iamadmin.buildRoleActRule(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-action-build/\"><input type=\"hidden\" name=\"role_" + role.role_id + "_act\" value=\"1\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_key_1\" class=\"key\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_add_value_1\" class=\"add_value\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_del_value_1\" class=\"del_value\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_additional_data_1\" class=\"additional_data\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addAction(this, '" + role.role_id + "');\"></div>");
                                                                }

                                                                desc += "</tbody></table>";

                                                                desc += "<table><tbody id=\"exp-content\">";
                                                                if ((role.filters != null) && (role.filters.Count > 0))
                                                                {
                                                                    for (Int32 i = 0; i < role.filters.Count; i++)
                                                                    {
                                                                        String sel = "<input type=\"hidden\" name=\"role_" + role.role_id + "_filter\" value=\"" + i + "\" /><select id=\"role_" + role.role_id + "_filter_" + i + "\" name=\"role_" + role.role_id + "_filter_" + i + "\">";
                                                                        sel += "<option value=\"\"></option>";

                                                                        foreach (FilterData f in filters.result)
                                                                            sel += "<option value=\"" + f.filter_id + "\" " + (f.filter_id == role.filters[i].filter_id ? "selected" : "") + ">" + f.name + "</option>";

                                                                        sel += "</select>";

                                                                        desc += String.Format(infoTemplate, "Filtro de vínculo", sel + "<div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addFilter(this, '" + role.role_id + "');\"></div>" + (i > 0 ? "<div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>" : ""));
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    String sel = "<input type=\"hidden\" name=\"role_" + role.role_id + "_filter\" value=\"1\" /><select id=\"role_" + role.role_id + "_filter_1\" name=\"role_" + role.role_id + "_filter_1\">";
                                                                    sel += "<option value=\"\"></option>";

                                                                    foreach (FilterData f in filters.result)
                                                                        sel += "<option value=\"" + f.filter_id + "\">" + f.name + "</option>";

                                                                    sel += "</select>";
                                                                        
                                                                    desc += String.Format(infoTemplate, "Filtro de vínculo", sel + "<div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addFilter(this, '" + role.role_id + "');\"></div>");
                                                                }
                                                                desc += "</tbody></table>";


                                                                desc += "<table><tbody id=\"timeacl-content\">";
                                                                if ((role.time_acl != null) && (role.time_acl.Count > 0))
                                                                {
                                                                    for (Int32 i = 0; i < role.time_acl.Count; i++)
                                                                        desc += String.Format(infoTemplate, "Controle por horário", "<div class=\"custom-item\" id=\"timeacl_" + i + "\" onclick=\"iamadmin.buildRoleTimeAcl(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-build/\" check-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-check/\"><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl\" value=\"" + i + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_type_" + i + "\" class=\"type\" value=\"" + role.time_acl[i].Type.ToString().ToLower() + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_start_time_" + i + "\" class=\"start_time\" value=\"" + role.time_acl[i].StartTime.ToString("HH:mm").ToLower() + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_end_time_" + i + "\" class=\"end_time\" value=\"" + role.time_acl[i].EndTime.ToString("HH:mm").ToLower() + "\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_week_day_" + i + "\" class=\"week_day\" value=\"" + String.Join(",", role.time_acl[i].WeekDay2.ToArray()) + "\" /><span>" + role.time_acl[i].ToString() + "</span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addAcl(this, '" + role.role_id + "');\"></div>" + (i > 0 ? "<div class=\"ico icon-subtract act-add-subtract left\" onclick=\"iamadmin.removeLine(this);\"></div>" : ""));
                                                                }
                                                                else
                                                                {
                                                                    desc += String.Format(infoTemplate, "Controle por horário", "<div class=\"custom-item\" id=\"timeacl_1\" onclick=\"iamadmin.buildRoleTimeAcl(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-build/\" check-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-check/\"><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl\" value=\"1\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_type_1\" class=\"type\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_start_time_1\" class=\"start_time\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_end_time_1\" class=\"end_time\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_week_day_1\" class=\"week_day\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addAcl(this, '" + role.role_id + "');\"></div>");
                                                                }
                                                                desc += "</tbody></table>";
                                                                
                                                                html += String.Format(fieldHtmlTemplate, role.role_id, role.role_name, "", desc);
                                                            }
                                                        }

                                                        html += "</div><div class=\"clear-block\"></div>";

                                                        js = "iamadmin.removeLine = function(obj) { $(obj).closest('tr').remove(); }; iamadmin.addAction = function(obj, role_id) { $(obj).closest('tbody').append('" + newAct + "'.replace(/\\[role_id\\]/g,role_id).replace(/\\[id\\]/g,new Date().getTime())); }; iamadmin.addFilter = function(obj, role_id) { $(obj).closest('tbody').append('" + newFilter + "'.replace(/\\[role_id\\]/g,role_id).replace(/\\[id\\]/g,new Date().getTime())); }; iamadmin.addAcl = function(obj, role_id) { $(obj).closest('tbody').append('" + newTimeAcl + "'.replace(/\\[role_id\\]/g,role_id).replace(/\\[id\\]/g,new Date().getTime())); }; iamadmin.autoCompleteText('#add_role', '" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/search_role/', {context_id:'" + selectedResourcePlugin.result.info.context_id + "'} , function(thisId, selectedItem){ $(thisId).val(''); $('#field_mapping_fields .none').remove(); $('#field_mapping_fields').append(selectedItem.html); } )";

                                                    }
                                                    else
                                                    {
                                                        if (rpRole.result.Count == 0)
                                                            html += String.Format(errorTemplate, "Nenhum perfil vinculado", "");

                                                        foreach (ResourcePluginRole role in rpRole.result)
                                                        {
                                                            List<String> actions = new List<string>();
                                                            List<String> rFilters = new List<string>();
                                                            List<String> timeAcl = new List<string>();


                                                            if (role.actions != null)
                                                                foreach (PluginActionData ad in pluginData1.result.actions)
                                                                    foreach (ResourcePluginRoleAction a in role.actions)
                                                                        if (ad.key == a.action_key)
                                                                            actions.Add(ad.name + " -> Ao incluir: " + ad.field_name + " = " + a.action_add_value + ". Ao Remover: " + ad.field_name + " = " + a.action_del_value);

                                                            if (role.filters != null)
                                                                foreach (ResourcePluginFilter f in role.filters)
                                                                    rFilters.Add(f.filter_name);

                                                            if (role.time_acl != null)
                                                                foreach (IAM.TimeACL.TimeAccess acl in role.time_acl)
                                                                    timeAcl.Add(acl.ToString());

                                                            if (actions.Count == 0)
                                                                actions.Add("Nenhuma ação definida");

                                                            if (rFilters.Count == 0)
                                                                rFilters.Add("Nenhum filtro definido");


                                                            if (timeAcl.Count == 0)
                                                                timeAcl.Add("Nenhuma controle por horário definido");

                                                            html += String.Format(infoTemplate, role.role_name, "Ações:<span class=\"description\">" + String.Join("<br />", actions) + "</span><br/>Filtro de vínculo:<span class=\"description\">" + String.Join("<br />", rFilters) + "</span><br/>Controle por horário:<span class=\"description\">" + String.Join("<br />", timeAcl) + "</span>");

                                                        }
                                                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                                    }

                                                }
                                            }
                                            break;


                                        case "identity":
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
                                                html += "        <th class=\"pointer w300 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                                html += "    </tr>";
                                                html += "</thead>";

                                                html += "<tbody>";
                                            }

                                            String trTemplate = "    <tr class=\"user\" data-login=\"{1}\" data-userid=\"{0}\">";
                                            trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                            trTemplate += "            <td class=\"ident10\">{2}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{1}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\"><button class=\"a-btn\" onclick=\"window.location = '" + ApplicationVirtualPath + "admin/users/{0}/';\">Abrir</button> <button href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/delete_identity/{0}/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o vínculo do usuário '{2}' com o recurso '" + selectedResourcePlugin.result.related_names.resource_name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\">Excluir</button></td>";
                                            trTemplate += "    </tr>";

                                            try
                                            {

                                                rData = "";

                                                var tmpReq = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "resourceplugin.identity",
                                                    parameters = new
                                                    {
                                                        page_size = pageSize,
                                                        page = page,
                                                        resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                    },
                                                    id = 1
                                                };

                                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                SearchResult ret2 = JSON.Deserialize<SearchResult>(jData);
                                                if (ret2 == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
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
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
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


                                        case "add_identity":

                                            html += String.Format(infoTemplate, "Usuário", "<input id=\"add_user\" name=\"add_user\" placeholder=\"Digite o nome do usuário\" type=\"text\">");

                                            html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                            html += "<h3>Novas identidades</h3>";
                                            html += "<div id=\"identities\" class=\"box-container\">";

                                            html += "<div class=\"no-tabs none\">Nenhuma identidade nova vinculada</div>";

                                            html += "</div><div class=\"clear-block\"></div>";
                                            //admin/users/content/search_user/

                                            js = "iamadmin.autoCompleteText('#add_user', '" + ApplicationVirtualPath + "admin/users/content/search_user/', {context_id: '" + selectedResourcePlugin.result.info.context_id + "'} ,function(thisId, selectedItem){ $(thisId).val(''); $('#identities .none').remove(); $('#identities').append(selectedItem.html); } )";
                                            break;

                                        case "config_lockrules":
                                            error = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(new
                                            {
                                                jsonrpc = "1.0",
                                                method = "resourceplugin.lockrules",
                                                parameters = new
                                                {
                                                    resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                },
                                                id = 1
                                            });
                                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                            ResourcePluginFilterList rpLockRules = JSON.Deserialize<ResourcePluginFilterList>(jData);
                                            if (rpLockRules == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else if (rpLockRules.error != null)
                                            {
                                                error = rpLockRules.error.data;
                                            }
                                            else if (rpLockRules.result == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else
                                            {

                                                if (hashData.GetValue("edit") == "1")
                                                {

                                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                                    {
                                                        jsonrpc = "1.0",
                                                        method = "filter.list",
                                                        parameters = new
                                                        {
                                                            page_size = Int16.MaxValue,
                                                            page = 1
                                                        },
                                                        id = 1
                                                    });
                                                    jData = "";
                                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                    FilterListResult filters = JSON.Deserialize<FilterListResult>(jData);

                                                    for (Int32 i = 0; i < rpLockRules.result.Count; i++)
                                                    {
                                                        String sel = "<select id=\"filter_" + i + "\" name=\"filter_" + i + "\">";
                                                        sel += "<option value=\"\"></option>";

                                                        foreach(FilterData f in filters.result)
                                                            sel += "<option value=\""+ f.filter_id +"\" "+ (f.filter_id == rpLockRules.result[i].filter_id ? "selected" : "") +">"+ f.name +"</option>";

                                                        sel += "</select>";

                                                        html += String.Format(infoTemplate, "Regra " + i, sel);
                                                    }

                                                    String sel2 = "<select id=\"filter_[id]\" name=\"filter_[id]\">";
                                                    sel2 += "<option value=\"\"></option>";

                                                    foreach (FilterData f in filters.result)
                                                        sel2 += "<option value=\"" + f.filter_id + "\">" + f.name + "</option>";

                                                    sel2 += "</select>";

                                                    String empty = String.Format(infoTemplate, "Nova regra", sel2);

                                                    html += "</tbody></table><table><tbody>";

                                                    html += String.Format(infoTemplate, "", "<div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.addLockRoleField();\">Adicionar nova de regra</div>");
                                                    js = "iamfnc.addLockRoleField = function(){ var id = new Date().getTime(); $('.role-fields').append('" + empty + "'.replace(/\\[id\\]/g,id)); }";
                                                }
                                                else
                                                {
                                                    if (rpLockRules.result.Count == 0)
                                                        html += String.Format(errorTemplate, "Nenhuma regra cadastrada");

                                                    for (Int32 i = 0; i < rpLockRules.result.Count; i++)
                                                    {
                                                        html += String.Format(infoTemplate, "Regra " + i, "Filtro: " + rpLockRules.result[i].filter_name + "<span class=\"description\">" + rpLockRules.result[i].conditions_description + "</span>");
                                                    }


                                                }


                                            }
                                            break;

                                        case "config_ignore":
                                            error = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(new
                                            {
                                                jsonrpc = "1.0",
                                                method = "resourceplugin.ignore",
                                                parameters = new
                                                {
                                                    resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                },
                                                id = 1
                                            });
                                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                            ResourcePluginFilterList rpIgnoreRules = JSON.Deserialize<ResourcePluginFilterList>(jData);
                                            if (rpIgnoreRules == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else if (rpIgnoreRules.error != null)
                                            {
                                                error = rpIgnoreRules.error.data;
                                            }
                                            else if (rpIgnoreRules.result == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else
                                            {

                                                if (hashData.GetValue("edit") == "1")
                                                {

                                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                                    {
                                                        jsonrpc = "1.0",
                                                        method = "filter.list",
                                                        parameters = new
                                                        {
                                                            page_size = Int16.MaxValue,
                                                            page = 1
                                                        },
                                                        id = 1
                                                    });
                                                    jData = "";
                                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                    FilterListResult filters = JSON.Deserialize<FilterListResult>(jData);

                                                    for (Int32 i = 0; i < rpIgnoreRules.result.Count; i++)
                                                    {
                                                        String sel = "<select id=\"filter_" + i + "\" name=\"filter_" + i + "\">";
                                                        sel += "<option value=\"\"></option>";

                                                        foreach (FilterData f in filters.result)
                                                            sel += "<option value=\"" + f.filter_id + "\" " + (f.filter_id == rpIgnoreRules.result[i].filter_id ? "selected" : "") + ">" + f.name + "</option>";

                                                        sel += "</select>";

                                                        html += String.Format(infoTemplate, "Regra " + i, sel);
                                                    }

                                                    String sel2 = "<select id=\"filter_[id]\" name=\"filter_[id]\">";
                                                    sel2 += "<option value=\"\"></option>";

                                                    foreach (FilterData f in filters.result)
                                                        sel2 += "<option value=\"" + f.filter_id + "\">" + f.name + "</option>";

                                                    sel2 += "</select>";

                                                    String empty = String.Format(infoTemplate, "Nova regra", sel2);

                                                    html += "</tbody></table><table><tbody>";

                                                    html += String.Format(infoTemplate, "", "<div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.addLockRoleField();\">Adicionar nova de regra</div>");
                                                    js = "iamfnc.addLockRoleField = function(){ var id = new Date().getTime(); $('.role-fields').append('" + empty + "'.replace(/\\[id\\]/g,id)); }";
                                                }
                                                else
                                                {
                                                    if (rpIgnoreRules.result.Count == 0)
                                                        html += String.Format(errorTemplate, "Nenhuma regra cadastrada");

                                                    for (Int32 i = 0; i < rpIgnoreRules.result.Count; i++)
                                                    {
                                                        html += String.Format(infoTemplate, "Regra " + i, "Filtro: " + rpIgnoreRules.result[i].filter_name + "<span class=\"description\">" + rpIgnoreRules.result[i].conditions_description + "</span>");
                                                    }


                                                }


                                            }
                                            break;

                                        case "config_schedule":
                                            error = "";
                                            rData = SafeTrend.Json.JSON.Serialize2(new
                                            {
                                                jsonrpc = "1.0",
                                                method = "resourceplugin.schedules",
                                                parameters = new
                                                {
                                                    resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                },
                                                id = 1
                                            });
                                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                            ResourcePluginScheduleList rpSchedules = JSON.Deserialize<ResourcePluginScheduleList>(jData);
                                            if (rpSchedules == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else if (rpSchedules.error != null)
                                            {
                                                error = rpSchedules.error.data;
                                            }
                                            else if (rpSchedules.result == null)
                                            {
                                                error = MessageResource.GetMessage("resource_plugin_not_found");
                                            }
                                            else
                                            {
                                                if (hashData.GetValue("edit") == "1")
                                                {

                                                    String form = "";
                                                    for (Int32 i = 0; i < rpSchedules.result.Count; i++)
                                                    {
                                                        form = "<input type=\"hidden\" name=\"schedule_id\" value=\"" + i + "\">";
                                                        form += "<span class=\"description\">Tipo de agendamento</span><select name=\"schedule_" + i + "_type\"><option value=\"\"></option><option value=\"Annually\" " + (rpSchedules.result[i].Trigger == IAM.Scheduler.ScheduleTtiggers.Annually ? "selected" : "") + ">Anualmente</option><option value=\"Monthly\" " + (rpSchedules.result[i].Trigger == IAM.Scheduler.ScheduleTtiggers.Monthly ? "selected" : "") + ">Mensalmente</option><option value=\"Weekly\" " + (rpSchedules.result[i].Trigger == IAM.Scheduler.ScheduleTtiggers.Weekly ? "selected" : "") + ">Semanalmente</option><option value=\"Dialy\" " + (rpSchedules.result[i].Trigger == IAM.Scheduler.ScheduleTtiggers.Dialy ? "selected" : "") + ">Diariamente</option></select><br clear=\"all\"><br clear=\"all\">";
                                                        form += "<span class=\"description\">Data de início</span><input class=\"date-mask\" name=\"schedule_" + i + "_date\" placeholder=\"Digite a data de início\" type=\"text\" value=\"" + MessageResource.FormatDate(rpSchedules.result[i].StartDate, true) + "\"><br clear=\"all\"><br clear=\"all\">";
                                                        form += "<span class=\"description\">Hora de início</span><input class=\"time-mask\" name=\"schedule_" + i + "_time\" placeholder=\"Digite a hora de início\" type=\"text\" value=\"" + MessageResource.FormatTime(rpSchedules.result[i].TriggerTime) + "\"><br clear=\"all\"><br clear=\"all\">";
                                                        form += "<span class=\"description\">Repetição (em minutos)</span><input name=\"schedule_" + i + "_repeat\" placeholder=\"Digite o periodo de repetição (em minutos)\" type=\"text\" value=\"" + rpSchedules.result[i].Repeat + "\">";
                                                        html += String.Format(infoTemplate, "Agendamento " + i + "<br clear=\"all\"><br clear=\"all\"><div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.delSchedule(this);\">Remover</div>", form);
                                                    }

                                                    form = "<input type=\"hidden\" name=\"schedule_id\" value=\"[id]\">";
                                                    form += "<span class=\"description\">Tipo de agendamento</span><select name=\"schedule_[id]_type\"><option value=\"\"></option><option value=\"Annually\">Anualmente</option><option value=\"Monthly\">Mensalmente</option><option value=\"Weekly\">Semanalmente</option><option value=\"Dialy\">Diariamente</option></select><br clear=\"all\"><br clear=\"all\">";
                                                    form += "<span class=\"description\">Data de início</span><input class=\"date-mask\" name=\"schedule_[id]_date\" placeholder=\"Digite a data de início\" type=\"text\" value=\""+ MessageResource.FormatDate(DateTime.Now, true) +"\"><br clear=\"all\"><br clear=\"all\">";
                                                    form += "<span class=\"description\">Hora de início</span><input class=\"time-mask\" name=\"schedule_[id]_time\" placeholder=\"Digite a hora de início\" type=\"text\"><br clear=\"all\"><br clear=\"all\">";
                                                    form += "<span class=\"description\">Repetição (em minutos)</span><input name=\"schedule_[id]_repeat\" placeholder=\"Digite o periodo de repetição (em minutos)\" type=\"text\">";
                                                    String empty = String.Format(infoTemplate, "Novo agendamento<br clear=\"all\"><br clear=\"all\"><div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.delSchedule(this);\">Remover</div>", form);

                                                    if (rpSchedules.result.Count == 0)
                                                        html += empty.Replace("[id]", "0");

                                                    html += "</tbody></table><table><tbody>";

                                                    html += String.Format(infoTemplate, "", "<div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.addSchedule();\">Adicionar novo agendamento</div>");
                                                    js = "iamfnc.mask = function() { $('.date-mask').mask('99/99/9999'); $('.time-mask').mask('99:99:99'); $('.number-mask').mask('999999'); }; iamfnc.mask(); iamfnc.delSchedule = function(obj){ $(obj).closest('tr').remove(); }; iamfnc.addSchedule = function(){ var id = new Date().getTime(); $('.role-fields').append('" + empty + "'.replace(/\\[id\\]/g,id)); iamfnc.mask(); }";
                                                }
                                                else
                                                {
                                                    if (rpSchedules.result.Count == 0)
                                                        html += String.Format(errorTemplate, "Nenhum agendamento");

                                                    for (Int32 i = 0; i < rpSchedules.result.Count; i++)
                                                    {
                                                        html += String.Format(infoTemplate, "Agendamento " + i, rpSchedules.result[i].ToString());
                                                    }

                                                }


                                            }
                                            break;


                                        case "fields_fetch":
                                            Int32.TryParse(Request.Form["page"], out page);

                                            if (page < 1)
                                                page = 1;

                                            if (page == 1)
                                            {
                                                html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                                html += "    <tr>";
                                                html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                                html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Data da requisição <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Data de retorno <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Status <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                                html += "    </tr>";
                                                html += "</thead>";

                                                html += "<tbody>";
                                            }

                                            String trTemplate2 = "    <tr class=\"user\" data-id=\"{0}\">";
                                            trTemplate2 += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                            trTemplate2 += "            <td class=\"ident10\">{1}</td>";
                                            trTemplate2 += "            <td class=\"tHide mHide\">{2}</td>";
                                            trTemplate2 += "            <td class=\"tHide mHide\">{3}</td>";
                                            trTemplate2 += "            <td class=\"tHide mHide\">{4}</td>";
                                            trTemplate2 += "    </tr>";

                                            try
                                            {

                                                rData = "";

                                                rData = SafeTrend.Json.JSON.Serialize2(new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "resourceplugin.fieldsfetch",
                                                    parameters = new
                                                    {
                                                        page_size = Int32.MaxValue,
                                                        page = page,
                                                        resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                    },
                                                    id = 1
                                                });

                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                ResourcePluginFetchList ret2 = JSON.Deserialize<ResourcePluginFetchList>(jData);
                                                if (ret2 == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                                    //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                                    hasNext = false;
                                                }
                                                else if (ret2.error != null)
                                                {
                                                    eHtml += String.Format(errorTemplate, ret2.error.data);
                                                    //ret = new WebJsonResponse("", ret2.error.data, 3000, true);
                                                    hasNext = false;
                                                }
                                                else if (ret2.result == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                                    hasNext = false;
                                                }
                                                else
                                                {
                                                    foreach (ResourcePluginFetchData fetch in ret2.result)
                                                    {
                                                        String btn = "";
                                                        if(fetch.success)
                                                            btn += "<div class=\"a-btn\" onclick=\"window.location = '" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/fields_fetch_view/" + fetch.fetch_id + "/';\">Abrir</div>&nbsp;";
                                                        
                                                        if(fetch.response_date > 0)
                                                            btn += "<div class=\"a-btn\" onclick=\"iamadmin.openModal(this, { show_cancel: false } , null);\"  data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/" + fetch.fetch_id + "/content/fields_fetch_logs/\" modal-title=\"Log\" cancel=\"Cancelar\" ok=\"Fechar\">Logs</div>&nbsp;";

                                                        btn += "<button href=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/action/delete_fetch/" + fetch.fetch_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente a busca automática de campos?\" ok=\"Excluir\" cancel=\"Cancelar\">Excluir</button>";

                                                        html += String.Format(trTemplate2, fetch.fetch_id, MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(fetch.request_date), false), (fetch.response_date > 0 ? MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(fetch.response_date), false) : ""), (fetch.response_date > 0 ? "Finalizado com " + (fetch.success ? "sucesso" : "erro") : "Aguardando retorno do proxy"), btn);
                                                    }

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

                                            js = "iamadmin.doReload(15000); $( document ).unbind('end_of_scroll.loader_usr');";

                                            if (hasNext)
                                                js += "$( document ).bind( 'end_of_scroll.loader_usr', function() { $( document ).unbind('end_of_scroll.loader_usr'); $('.user-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'' }, function(){ $('.user-list-loader').addClass('hide'); } ); });";

                                            break;


                                        case "fields_fetch_view":
                                            Int32.TryParse(Request.Form["page"], out page);

                                            if (page < 1)
                                                page = 1;

                                            if (page == 1)
                                            {
                                                html += "<table id=\"users-table\" class=\"sorter form-table small\"><thead>";
                                                html += "    <tr>";
                                                html += "        <th class=\"pointer w200 header headerSortDown\" data-column=\"name\">Campo mapeado <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Campo no sistema <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer w300 tHide mHide header\" data-column=\"last_login\">Opções <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer w80 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                                html += "    </tr>";
                                                html += "</thead>";

                                                html += "<tbody>";
                                            }

                                            String trTemplate3 = "    <tr class=\"user\" data-id=\"{0}\">";
                                            trTemplate3 += "            <td class=\"ident10\">{1}</td>";
                                            trTemplate3 += "            <td class=\"tHide mHide\">{2}</td>";
                                            trTemplate3 += "            <td class=\"tHide mHide\">{3}</td>";
                                            trTemplate3 += "            <td class=\"tHide mHide\">{4}</td>";
                                            trTemplate3 += "    </tr>";

                                            try
                                            {

                                                rData = "";

                                                rData = SafeTrend.Json.JSON.Serialize2(new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "resourceplugin.fieldsfetch",
                                                    parameters = new
                                                    {
                                                        page_size = pageSize,
                                                        page = page,
                                                        resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id,
                                                        filter = new { fetchid = subfilter }
                                                    },
                                                    id = 1
                                                });

                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                ResourcePluginFetchList ret2 = JSON.Deserialize<ResourcePluginFetchList>(jData);
                                                if (ret2 == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                                    //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                                                    hasNext = false;
                                                }
                                                else if (ret2.error != null)
                                                {
                                                    eHtml += String.Format(errorTemplate, ret2.error.data);
                                                    //ret = new WebJsonResponse("", ret2.error.data, 3000, true);
                                                    hasNext = false;
                                                }
                                                else if (ret2.result == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                                    hasNext = false;
                                                }
                                                else
                                                {


                                                    error = "";
                                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                                        {
                                                            jsonrpc = "1.0",
                                                            method = "resourceplugin.mapping",
                                                            parameters = new
                                                            {
                                                                resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id
                                                            },
                                                            id = 1
                                                        });

                                                    jData = "";
                                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                    List<ResourcePluginMapping> mappingList = new List<ResourcePluginMapping>();
                                                    ResourcePluginMappingList rpMapping1 = JSON.Deserialize<ResourcePluginMappingList>(jData);
                                                    if ((rpMapping1 != null) && (rpMapping1.error == null) && (rpMapping1.result != null))
                                                    {
                                                        mappingList = rpMapping1.result;
                                                    }

                                                    rData = SafeTrend.Json.JSON.Serialize2(new
                                                    {
                                                        jsonrpc = "1.0",
                                                        method = "field.list",
                                                        parameters = new
                                                        {
                                                            page_size = Int32.MaxValue
                                                        },
                                                        id = 1
                                                    });
                                                    jData = "";
                                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                    List<FieldData> fieldList = new List<FieldData>();
                                                    FieldListResult flR = JSON.Deserialize<FieldListResult>(jData);
                                                    if ((flR != null) && (flR.error == null) && (flR.result != null))
                                                    {
                                                        fieldList = flR.result;
                                                    }

                                                    foreach (ResourcePluginFetchData fetch in ret2.result)
                                                    {
                                                        if (fetch.fetch_id.ToString() == subfilter)
                                                        {
                                                            if ((fetch.success) && (fetch.fetch_fields != null))
                                                                foreach (ResourcePluginFetchField ff in fetch.fetch_fields)
                                                                {

                                                                    ResourcePluginMapping m = mappingList.Find(m1 => (m1.data_name == ff.key));

                                                                    String btn = "<div class=\"a-btn\" onclick=\"$(this).closest('tr').remove();\">Excluir</div>";
                                                                    String select = "<input type=\"hidden\" name=\"field_key\" value=\"" + ff.key + "\" ><select name=\"field_id_" + ff.key + "\" >";

                                                                    select += "<option value=\"\"></option>";

                                                                    foreach (FieldData f in fieldList)
                                                                    {
                                                                        Boolean selected = false;
                                                                        if (m != null && m.field_id == f.field_id)
                                                                            selected = true;
                                                                        else if (ff.key.ToLower() == f.name.ToLower())
                                                                            selected = true;

                                                                        select += "<option value=\"" + f.field_id + "\" " + (selected ? "selected" : "") + ">" + f.name + "</option>";
                                                                    }

                                                                    select += "</select><span class=\"description\">Exemplo de dados: ";
                                                                    select += String.Join(", ", ff.sample_data);
                                                                    select += "</span>";

                                                                    String opt = "";
                                                                    opt += "<table class=\"inside\"><tbody>";
                                                                    opt += String.Format(infoTemplate, "É um identificador?", "<input id=\"is_id_" + ff.key + "\" name=\"is_id_" + ff.key + "\" type=\"checkbox\" " + (m != null && m.is_id ? "checked" : "") + ">");
                                                                    opt += String.Format(infoTemplate, "É senha?", "<input id=\"is_password_" + ff.key + "\" name=\"is_password_" + ff.key + "\" type=\"checkbox\" " + (m != null && m.is_password ? "checked" : "") + ">");
                                                                    opt += String.Format(infoTemplate, "É um campo único?", "<input id=\"is_unique_" + ff.key + "\" name=\"is_unique_" + ff.key + "\" type=\"checkbox\" " + (m != null && m.is_unique_property ? "checked" : "") + ">");
                                                                    opt += "</tbody></table>";

                                                                    html += String.Format(trTemplate3, ff.key, ff.key, select, opt, btn);
                                                                }
                                                        }
                                                    }

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

                                            break;


                                    }

                                    if (error != "")
                                        eHtml = String.Format(errorTemplate, error);

                                    #endregion body

                                    switch (filter)
                                    {

                                        case "":
                                        case "config_mapping":
                                        case "config_role":
                                        case "identity":
                                        case "fields_fetch":
                                            break;

                                        case "add_identity":
                                            html += "</tbody></table><div class=\"clear-block\"></div></div>";
                                            html += "<button type=\"submit\" id=\"resource-plugin-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"iamadmin.changeHash( 'edit/0' );\">Cancelar</a></form>";
                                            break;

                                        case "fields_fetch_view":
                                            hashData.Clear();
                                            html += "<button type=\"submit\" id=\"resource-plugin-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/fields_fetch/'\">Cancelar</a></form>";
                                            break;


                                        default:
                                            html += "</tbody></table><div class=\"clear-block\"></div></div>";
                                            break;
                                    }


                                    if (hashData.GetValue("edit") == "1")
                                        html += "<button type=\"submit\" id=\"resource-plugin-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"iamadmin.changeHash( 'edit/0' );\">Cancelar</a></form>";

                                    contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                    contentRet.js = js;
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
                            html += "        <h2 class=\"title tutorial-color\">";
                            html += "            <a href=\"" + menu3.HRef + "\">" + menu3.Name + "</a>";
                            html += "       </h2>";
                            html += "    </div>";
                        }
                        html += "</div></div>";
                    }

                    if (!newItem)
                    {
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/resource_plugin/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo recurso x plugin</button></div>";

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

                case "search_field":
                    List<AutoCompleteItem> users = new List<AutoCompleteItem>();

                    String userHtmlTemplate = "<div id=\"new-field-{0}\" data-id=\"{0}\" data-name=\"{2}\" class=\"app-list-item p50 left\">";
                    userHtmlTemplate += "<input type=\"hidden\" name=\"content_id\" value=\"{0}\">";
                    userHtmlTemplate += "<input type=\"hidden\" name=\"field_id_{0}\" value=\"{1}\">";
                    userHtmlTemplate += "<input type=\"hidden\" name=\"field_name_{0}\" value=\"{2}\">";
                    userHtmlTemplate += "<table>";
                    userHtmlTemplate += "   <tbody>";
                    userHtmlTemplate += "       <tr>";
                    userHtmlTemplate += "           <td class=\"colfull\">";
                    userHtmlTemplate += "               <div class=\"title\"><span class=\"name\" id=\"field_name_{0}\" data-id=\"{0}\">{2}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                    userHtmlTemplate += "               <div class=\"description fields small\">{4}<div class=\"clear-block\"></div></div>";
                    userHtmlTemplate += "               <div class=\"links small\">";
                    userHtmlTemplate += "                   <div class=\"last\"><div class=\"ico icon-close\" onclick=\"$(this).closest('.app-list-item').remove();\">Excluir campo</div></a><div class=\"clear-block\"></div></div>";
                    userHtmlTemplate += "               </div>";
                    userHtmlTemplate += "           </td>";
                    userHtmlTemplate += "       </tr>";
                    userHtmlTemplate += "   </tbody>";
                    userHtmlTemplate += "</table></div>";


                    String infoTemplate2 = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

                    try
                    {

                        rData = "";

                        var tmpReq = new
                        {
                            jsonrpc = "1.0",
                            method = "field.search",
                            parameters = new
                            {
                                page_size = 20,
                                page = 1,
                                text = Request.Form["text"]
                            },
                            id = 1
                        };

                        rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        FieldListResult fieldList = JSON.Deserialize<FieldListResult>(jData);
                        if (fieldList == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (fieldList.error != null)
                        {
                            eHtml += String.Format(errorTemplate, fieldList.error.data);
                        }
                        else if (fieldList.result == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                        }
                        else
                        {
                            foreach (FieldData field in fieldList.result)
                            {
                                String id = Guid.NewGuid().ToString();

                                String desc = "";
                                desc += "<table><tbody>";
                                desc += String.Format(infoTemplate2, "Campo do recurso", "<input id=\"data_name_" + id + "\" name=\"data_name_" + id + "\" placeholder=\"Digite o nome do campo relacionado\" type=\"text\">");
                                desc += String.Format(infoTemplate2, "É um identificador?", "<input id=\"is_id_" + id + "\" name=\"is_id_" + id + "\" type=\"checkbox\">");
                                desc += String.Format(infoTemplate2, "É senha?", "<input id=\"is_password_" + id + "\" name=\"is_password_" + id + "\" type=\"checkbox\">");
                                desc += String.Format(infoTemplate2, "É um campo único?", "<input id=\"is_unique_" + id + "\" name=\"is_unique_" + id + "\" type=\"checkbox\">");
                                desc += "</tbody></table>";

                                String tHtml = String.Format(userHtmlTemplate, id, field.field_id, field.name, "", desc);
                                users.Add(new AutoCompleteItem(field.field_id, field.name, tHtml));
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                    }

                    if (users.Count == 0)
                        users.Add(new AutoCompleteItem(0, MessageResource.GetMessage("field_not_found"), ""));

                    Retorno.Controls.Add(new LiteralControl(JSON.Serialize<List<AutoCompleteItem>>(users)));
                    break;


                case "search_role":
                    
                    List<AutoCompleteItem> roles = new List<AutoCompleteItem>();

                    if (selectedResourcePlugin != null)
                    {



                        String roleHtmlTemplate = "<div id=\"new-field-{0}\" data-id=\"{0}\" data-name=\"{1}\" class=\"app-list-item p50 left\">";
                        roleHtmlTemplate += "<input type=\"hidden\" name=\"role_id\" value=\"{0}\">";
                        roleHtmlTemplate += "<input type=\"hidden\" name=\"role_name_{0}\" value=\"{1}\">";
                        roleHtmlTemplate += "<table>";
                        roleHtmlTemplate += "   <tbody>";
                        roleHtmlTemplate += "       <tr>";
                        roleHtmlTemplate += "           <td class=\"colfull\">";
                        roleHtmlTemplate += "               <div class=\"title\"><span class=\"name\" id=\"role_name_{0}\" data-id=\"{0}\">{1}</span><span class=\"date\">{2}</span><div class=\"clear-block\"></div></div>";
                        roleHtmlTemplate += "               <div class=\"description fields small\">{3}<div class=\"clear-block\"></div></div>";
                        roleHtmlTemplate += "               <div class=\"links small\">";
                        roleHtmlTemplate += "                   <div class=\"last\"><div class=\"ico icon-close\" onclick=\"$(this).closest('.app-list-item').remove();\">Excluir perfil</div></a><div class=\"clear-block\"></div></div>";
                        roleHtmlTemplate += "               </div>";
                        roleHtmlTemplate += "           </td>";
                        roleHtmlTemplate += "       </tr>";
                        roleHtmlTemplate += "   </tbody>";
                        roleHtmlTemplate += "</table></div>";

                        String infoTemplate3 = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

                        try
                        {

                            rData = "";

                            var tmpReq = new
                            {
                                jsonrpc = "1.0",
                                method = "role.search",
                                parameters = new
                                {
                                    page_size = 20,
                                    page = 1,
                                    text = Request.Form["text"],
                                    filter = new { contextid = selectedResourcePlugin.result.info.context_id }
                                },
                                id = 1
                            };

                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                            if (String.IsNullOrWhiteSpace(jData))
                                throw new Exception("");

                            RoleListResult roleList = JSON.Deserialize<RoleListResult>(jData);
                            if (roleList == null)
                            {
                                //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                                //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                            }
                            else if (roleList.error != null)
                            {
                                eHtml += String.Format(errorTemplate, roleList.error.data);
                            }
                            else if (roleList.result == null)
                            {
                                //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                            }
                            else
                            {

                                rData = SafeTrend.Json.JSON.Serialize2(new
                                {
                                    jsonrpc = "1.0",
                                    method = "filter.list",
                                    parameters = new
                                    {
                                        page_size = Int16.MaxValue,
                                        page = 1
                                    },
                                    id = 1
                                });
                                jData = "";
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                FilterListResult filters = JSON.Deserialize<FilterListResult>(jData);


                                foreach (RoleData role in roleList.result)
                                {

                                    String fSel = "<input type=\"hidden\" name=\"role_" + role.role_id + "_filter\" value=\"1\" /><select id=\"role_" + role.role_id + "_filter_1\" name=\"role_" + role.role_id + "_filter_1\">";
                                    fSel += "<option value=\"\"></option>";
                                    foreach (FilterData f in filters.result)
                                        fSel += "<option value=\"" + f.filter_id + "\">" + f.name + "</option>";
                                    fSel += "</select>";
                                                                

                                    String desc = "";
                                    desc += "<table><tbody>";
                                    desc += String.Format(infoTemplate3, "Ação", "<div class=\"custom-item\" id=\"role_" + role.role_id + "_act_" + role.role_id + "\" onclick=\"iamadmin.buildRoleActRule(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-action-build/\"><input type=\"hidden\" name=\"role_" + role.role_id + "_act\" value=\"1\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_key_1\" class=\"key\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_add_value_1\" class=\"add_value\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_del_value_1\" class=\"del_value\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_act_additional_data_1\" class=\"additional_data\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\"  onclick=\"iamadmin.addAction(this, '" + role.role_id + "');\"></div>");
                                    desc += "</tbody></table>";
                                    desc += "<table><tbody>";
                                    desc += String.Format(infoTemplate3, "Filtro de vínculo", fSel + "<div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addFilter(this, '" + role.role_id + "');\"></div>");
                                    desc += "</tbody></table>";
                                    desc += "<table><tbody>";
                                    desc += String.Format(infoTemplate3, "Controle por horário", "<div class=\"custom-item\" id=\"role_" + role.role_id + "_timeacl_" + role.role_id + "\" onclick=\"iamadmin.buildRoleTimeAcl(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-build/\" check-uri=\"" + ApplicationVirtualPath + "admin/resource_plugin/" + selectedResourcePlugin.result.info.resource_plugin_id + "/content/role-timeacl-check/\"><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl\" value=\"1\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_type_1\" class=\"type\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_start_time_1\" class=\"start_time\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_end_time_1\" class=\"end_time\" /><input type=\"hidden\" name=\"role_" + role.role_id + "_timeacl_week_day_1\" class=\"week_day\" /><span></span><i class=\"icon-change\"></i></div><div class=\"ico icon-add act-add-subtract left\" onclick=\"iamadmin.addAcl(this, '" + role.role_id + "');\"></div>");
                                    desc += "</tbody></table>";
                                    

                                    String tHtml = String.Format(roleHtmlTemplate, role.role_id, role.name, "", desc);
                                    roles.Add(new AutoCompleteItem(role.role_id, role.name, tHtml));
                                }

                            }

                        }
                        catch (Exception ex)
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                        }
                    }

                    if (roles.Count == 0)
                        roles.Add(new AutoCompleteItem(0, MessageResource.GetMessage("role_not_found"), ""));

                    Retorno.Controls.Add(new LiteralControl(JSON.Serialize<List<AutoCompleteItem>>(roles)));
                    break;


                case "role-action-build":
                    if (selectedResourcePlugin == null)
                    {
                        html = String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                    }
                    else
                    {

                        var tmpReqPg1 = new
                            {
                                jsonrpc = "1.0",
                                method = "plugin.get",
                                parameters = new
                                {
                                    pluginid = selectedResourcePlugin.result.info.plugin_id,
                                    parameters = true
                                },
                                id = 1
                            };

                        error = "";
                        rData = SafeTrend.Json.JSON.Serialize2(tmpReqPg1);
                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        PluginGetResult pluginData2 = JSON.Deserialize<PluginGetResult>(jData);
                        if (pluginData2 == null)
                        {
                            error = MessageResource.GetMessage("plugin_not_found");
                        }
                        else if (pluginData2.error != null)
                        {
                            error = pluginData2.error.data;
                        }
                        else if (pluginData2.result == null)
                        {
                            error = MessageResource.GetMessage("plugin_not_found");
                        }
                        else if ((pluginData2.result.actions == null) || (pluginData2.result.actions.Count == 0))
                        {
                            html += String.Format(errorTemplate, "Este plugin não permite esta operação");
                            hashData.Clear();
                        }
                        else
                        {
                            
                            String key = "";
                            String add = "";
                            String del = "";
                            foreach (String k in Request.Form.Keys)
                                if (k.IndexOf("act_key_") != -1)
                                    key = Request.Form[k];
                                else if (k.IndexOf("act_add_value_") != -1)
                                    add = Request.Form[k];
                                else if (k.IndexOf("act_del_value_") != -1)
                                    del = Request.Form[k];

                            html += "<div class=\"no-tabs\" style=\"width:480px;\">";
                            html += "<div class=\"form-group\"><label>Ação</label><select id=\"action\" name=\"action\" class=\"key\"><option value=\"\"></option>";

                            foreach (PluginActionData ad in pluginData2.result.actions)
                                html += "<option value=\"" + ad.key + "\" "+ (key == ad.key ? "selected" : "") +">" + ad.name + "</option>";

                            html += "</select></div>";

                            foreach (PluginActionData ad in pluginData2.result.actions)
                            {

                                html += "<div class=\"form-group opt " + ad.key + "\" style=\"display:" + (key == ad.key ? "block" : "none") + ";\"><label>" + ad.field_name + " -> adição</label><input name=\"add_value\" class=\"add_value\" placeholder=\"" + ad.field_description + "\" type=\"text\"\" value=\"" + add + "\"></div>";
                                html += "<div class=\"form-group opt " + ad.key + "\" style=\"display:" + (key == ad.key ? "block" : "none") + ";\"><label>" + ad.field_name + " -> exclusão</label><input name=\"del_value\" class=\"del_value\" placeholder=\"" + ad.field_description + "\" type=\"text\"\" value=\"" + del + "\"></div>";
                            }
                            
                            html += "<div class=\"clear-block\"></div></div>";
                        }
                    }
                    //$( this ).val()
                    contentRet = new WebJsonResponse("#modal-box .alert-box-content", html);
                    contentRet.js = "$('#modal-box #action').change(function(oThis){ $('#modal-box .opt').css('display','none'); if ($( this ).val() != '') { $('#modal-box .' + $( this ).val()).css('display','block'); } });";
                    break;


                case "role-timeacl-build":
                    if (selectedResourcePlugin == null)
                    {
                        html = String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                    }
                    else
                    {

                        IAM.TimeACL.TimeAccess ta = new IAM.TimeACL.TimeAccess();
                        
                        String type = "";
                        String start_time = "";
                        String end_time = "";
                        String week_day = "";
                        foreach (String k in Request.Form.Keys)
                            if (k.IndexOf("timeacl_type_") != -1)
                                type = Request.Form[k];
                            else if (k.IndexOf("timeacl_start_time_") != -1)
                                start_time = Request.Form[k];
                            else if (k.IndexOf("timeacl_end_time_") != -1)
                                end_time = Request.Form[k];
                            else if (k.IndexOf("timeacl_week_day_") != -1)
                                week_day = Request.Form[k];

                        ta.FromString(type, start_time, end_time, week_day);

                        CultureInfo ci = Thread.CurrentThread.CurrentCulture;

                        html += "<div class=\"fields small\" style=\"width:510px;\"><table><tbody>";
                        html += "<tr><td class=\"col1\">Tipo</td><td class=\"col2\"><select id=\"type\" name=\"type\" class=\"type\"><option value=\"\"></option>";

                        html += "<option value=\"notdefined\" " + (type == "notdefined" ? "selected" : "") + ">" + MessageResource.GetMessage("not_defined") + "</option>";
                        html += "<option value=\"never\" " + (type == "never" ? "selected" : "") + ">" + MessageResource.GetMessage("never") + "</option>";
                        html += "<option value=\"always\" " + (type == "always" ? "selected" : "") + ">" + MessageResource.GetMessage("always") + "</option>";
                        html += "<option value=\"specifictime\" " + (type == "specifictime" ? "selected" : "") + ">" + MessageResource.GetMessage("specific_time") + "</option>";

                        html += "</select></td></tr>";

                        html += "<tr class=\"opt specifictime\"><td class=\"col1\" style=\"display:" + (type == "specifictime" ? "table-cell" : "none") + ";\">Hora de início</td><td class=\"col2\" style=\"display:" + (type == "specifictime" ? "table-cell" : "none") + ";\"><input name=\"start_time\" class=\"start_time hour-mask\" type=\"text\" value=\"" + start_time + "\"></td></tr>";
                        html += "<tr class=\"opt specifictime\"><td class=\"col1\" style=\"display:" + (type == "specifictime" ? "table-cell" : "none") + ";\">Hora final</td><td class=\"col2\" style=\"display:" + (type == "specifictime" ? "table-cell" : "none") + ";\"><input name=\"end_time\" class=\"end_time hour-mask\" type=\"text\" value=\"" + end_time + "\"></td></tr>";
                        html += "<tr class=\"opt specifictime\"><td class=\"col1\" style=\"display:" + (type == "specifictime" ? "table-cell" : "none") + ";\">Dias da semana</td><td class=\"col2\" style=\"display:" + (type == "specifictime" ? "table-cell" : "none") + ";\">";

                        for (Int32 i = 0; i < 7; i++)
                        {
                            DayOfWeek d = (DayOfWeek)i;
                            html += "<span class=\"no-edit\"><input name=\"week_day\" class=\"left week_day\" type=\"checkbox\" value=\"" + d.ToString().ToLower() + "\" " + (ta.WeekDay.Contains(d) ? "checked" : "") + "><span class=\"checkbox-label\">" + ci.DateTimeFormat.GetDayName(d) + "</span></span>";
                        }

                        html += "</td></tr>";

                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                    }
                    //$( this ).val()
                    contentRet = new WebJsonResponse("#modal-box .alert-box-content", html);
                    contentRet.js = "$('.hour-mask').mask('99:99'); $('#modal-box #type').change(function(oThis){ $('#modal-box .opt td').css('display','none'); if ($( this ).val() != '') { $('#modal-box .' + $( this ).val() + ' td').css('display','table-cell'); } });";
                    break;

                    
                case "role-timeacl-check":
                    if (selectedResourcePlugin == null)
                    {
                        contentRet = new WebJsonResponse("", MessageResource.GetMessage("resource_plugin_not_found"), 3000, true);
                        Response.Clear();
                        Response.Status = "500 Error";
                        Response.StatusCode = 500;
                    }
                    else
                    {
                        IAM.TimeACL.TimeAccess ta = new IAM.TimeACL.TimeAccess();

                        String type = "";
                        String start_time = "";
                        String end_time = "";
                        String week_day = "";
                        foreach (String k in Request.Form.Keys)
                            if (k.IndexOf("type") != -1)
                                type = Request.Form[k];
                            else if (k.IndexOf("start_time") != -1)
                                start_time = Request.Form[k];
                            else if (k.IndexOf("end_time") != -1)
                                end_time = Request.Form[k];
                            else if (k.IndexOf("week_day") != -1)
                                week_day = Request.Form[k];

                        ta.FromString(type, start_time, end_time, week_day);

                        WebJsonResponse error2 = null;

                        if (String.IsNullOrWhiteSpace(type))
                            error2 = new WebJsonResponse("", MessageResource.GetMessage("select_type"), 3000, true);

                        if (ta.Type == IAM.TimeACL.TimeAccessType.SpecificTime)
                        {
                            if (String.IsNullOrWhiteSpace(start_time))
                                error2 = new WebJsonResponse("", MessageResource.GetMessage("type_start_time"), 3000, true);
                            else if (String.IsNullOrWhiteSpace(end_time))
                                error2 = new WebJsonResponse("", MessageResource.GetMessage("type_end_time"), 3000, true);
                            else if (String.IsNullOrWhiteSpace(week_day))
                                error2 = new WebJsonResponse("", MessageResource.GetMessage("select_week_day"), 3000, true);
                            else
                            {
                                
                                String tf = "{0:00}";

                                try
                                {
                                    String[] tm = start_time.ToString().Split(":".ToCharArray());

                                    DateTime tmp = DateTime.ParseExact("1970-01-01 " + String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]), "yyyy-MM-dd HH:mm", null);

                                    start_time = String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]);
                                }
                                catch
                                {
                                    error2 = new WebJsonResponse("", MessageResource.GetMessage("invalid_start_time"), 3000, true);
                                }


                                try
                                {
                                    String[] tm = end_time.ToString().Split(":".ToCharArray());

                                    DateTime tmp = DateTime.ParseExact("1970-01-01 " + String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]), "yyyy-MM-dd HH:mm", null);

                                    end_time = String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]);
                                }
                                catch
                                {
                                    error2 = new WebJsonResponse("", MessageResource.GetMessage("invalid_end_time"), 3000, true);
                                }

                                try
                                {
                                    List<DayOfWeek> wd = new List<DayOfWeek>();
                                    if (!String.IsNullOrWhiteSpace(week_day))
                                        foreach (String w in week_day.Split(",".ToCharArray()))
                                        {
                                            switch (w.ToLower())
                                            {
                                                case "sunday":
                                                    wd.Add(DayOfWeek.Sunday);
                                                    break;

                                                case "monday":
                                                    wd.Add(DayOfWeek.Monday);
                                                    break;

                                                case "tuesday":
                                                    wd.Add(DayOfWeek.Tuesday);
                                                    break;

                                                case "wednesday":
                                                    wd.Add(DayOfWeek.Wednesday);
                                                    break;

                                                case "thursday":
                                                    wd.Add(DayOfWeek.Thursday);
                                                    break;

                                                case "friday":
                                                    wd.Add(DayOfWeek.Friday);
                                                    break;

                                                case "saturday":
                                                    wd.Add(DayOfWeek.Saturday);
                                                    break;

                                                case "":
                                                    break;

                                                default:
                                                    throw new Exception("Invalid week day '" + w + "'");
                                                    break;
                                            }
                                        }
                                }
                                catch
                                {
                                    error2 = new WebJsonResponse("", MessageResource.GetMessage("invalid_week_day"), 3000, true);
                                }
                            }
                        }

                        if (error2 != null)
                        {
                            contentRet = null;
                            Response.Clear();
                            Response.Status = "500 Error";
                            Response.StatusCode = 500;

                            contentRet = error2;
                        }
                        else
                        {
                            contentRet = new WebJsonResponse("#modal-box form", "<input type=\"hidden\" class=\"title\" value=\""+ ta.ToString() +"\">", true);
                        }
                    }
                    break;

                case "fields_fetch_logs":
                    try
                    {

                        rData = "";

                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resourceplugin.fieldsfetch",
                            parameters = new
                            {
                                resourcepluginid = selectedResourcePlugin.result.info.resource_plugin_id,
                                filter = new { fetchid = filter }
                            },
                            id = 1
                        });

                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        ResourcePluginFetchList ret2 = JSON.Deserialize<ResourcePluginFetchList>(jData);
                        if (ret2 == null)
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
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
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                            hasNext = false;
                        }
                        else
                        {
                            String log = "Log não encontrado";

                            foreach (ResourcePluginFetchData fetch in ret2.result)
                                if ((fetch.fetch_id.ToString() == filter) && (fetch.logs != null))
                                    log = "<div class=\"log-info\">" + fetch.logs.Replace("\r\n", "<br>") + "</div>";

                            html += log ;
                        }

                    }
                    catch (Exception ex)
                    {
                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                    }

                    contentRet = new WebJsonResponse("#modal-box .alert-box-content", (eHtml != ""? eHtml : html));
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":

                    if (selectedResourcePlugin != null)
                        break;

                    js = "";
                    try
                    {
                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "resource.list",
                            parameters = new
                            {
                                page_size = Int32.MaxValue
                            },
                            id = 1
                        });
                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        ResourceListResult resourceList = JSON.Deserialize<ResourceListResult>(jData);
                        if ((resourceList != null) && (resourceList.error == null) && (resourceList.result != null))
                        {

                            html += "<select id=\"filter_resource\" name=\"filter_resource\" ><option value=\"\">Todos os recursos</option>";
                            foreach (ResourceData r in resourceList.result)
                                html += "<option value=\"resource/" + r.resource_id + "\" " + (hashData.GetValue("resource") == r.resource_id.ToString() ? "selected" : "") + ">" + r.name + "</option>";
                            html += "</select>";
                            js += "$('#filter_resource').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                        }

                    }
                    catch (Exception ex) { }

                    try
                    {
                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "plugin.list",
                            parameters = new
                            {
                                page_size = Int32.MaxValue
                            },
                            id = 1
                        });
                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        PluginListResult pluginList = JSON.Deserialize<PluginListResult>(jData);
                        if ((pluginList != null) && (pluginList.error == null) && (pluginList.result != null))
                        {

                            html += "<select id=\"filter_plugin\" name=\"filter_plugin\" ><option value=\"\">Todos os plugins</option>";
                            foreach (PluginData p in pluginList.result)
                                html += "<option value=\"plugin/" + p.plugin_id + "\" " + (hashData.GetValue("plugin") == p.plugin_id.ToString() ? "selected" : "") + ">" + p.name + "</option>";
                            html += "</select>";
                            js += "$('#filter_plugin').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                        }

                    }
                    catch (Exception ex) { }

                    contentRet = new WebJsonResponse("#btnbox", html);
                    contentRet.js = js;
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