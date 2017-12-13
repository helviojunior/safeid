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
    public partial class plugin : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Plugins", ApplicationVirtualPath + "admin/plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Plugins do sistema", ApplicationVirtualPath + "admin/plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 pluginId = 0;
            try
            {
                pluginId = Int64.Parse((String)RouteData.Values["id"]);

                if (pluginId < 0)
                    pluginId = 0;
            }
            catch { }

            String error = "";
            PluginGetResult retPlugin = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((pluginId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "plugin.get",
                        parameters = new
                        {
                            pluginid = pluginId
                        },
                        id = 1
                    };

                    String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retPlugin = JSON.Deserialize<PluginGetResult>(jData);
                    if (retPlugin == null)
                    {
                        error = MessageResource.GetMessage("plugin_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retPlugin.error != null)
                    {
                        error = retPlugin.error.data;
                        retPlugin = null;
                    }
                    else if (retPlugin.result == null || retPlugin.result.info == null)
                    {
                        error = MessageResource.GetMessage("plugin_not_found");
                        retPlugin = null;
                    }
                    else
                    {
                        menu3.Name = retPlugin.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retPlugin = null;
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


                        html += "<div id=\"upload-box\" class=\"upload-box\"><input type=\"file\" name=\"files[]\" multiple=\"\"><div class=\"drag-content\"><span class=\"upload-button-text\">Selecione arquivos para enviar</span><span class=\"upload-drag-drop-description\">Ou arraste e solte aqui</span></div><div class=\"dragDrop-content\"><span class=\"label l1\">Arraste o plugin até aqui</span><span class=\"label l2\">Solte o plugin</span></div></div>";
                        
                        html += "<h3>Uploads</h3>";
                        html += "<form  id=\"form_plugin_add\"  method=\"POST\" action=\"" + ApplicationVirtualPath + "admin/plugin/action/add_new/\">";
                        html += "<div id=\"files\" class=\"box-container\"><div class=\"no-tabs pb10 none\">Nenhum upload realizado</div></div>";

                        html += "<button type=\"submit\" id=\"upload-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/plugin/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";
                        html += "</form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        contentRet.js = "iamadmin.pluginUploader($('#upload-box'), '" + ApplicationVirtualPath + "admin/plugin/action/upload/','" + ApplicationVirtualPath + "admin/plugin/action/upload_item_template/')";

                    }
                    else
                    {
                        if (retPlugin == null)
                        {

                            Int32 page = 1;
                            Int32 pageSize = 20;
                            Boolean hasNext = true;

                            Int32.TryParse(Request.Form["page"], out page);

                            if (page < 1)
                                page = 1;

                            String pluginTemplate = "<div id=\"role-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" data-total=\"{2}\" class=\"app-list-item\">";
                            pluginTemplate += "<table>";
                            pluginTemplate += "   <tbody>";
                            pluginTemplate += "       <tr>";
                            pluginTemplate += "           <td class=\"col1\">";
                            pluginTemplate += "               <span id=\"total_{0}\" class=\"total \">{2}</span>";
                            pluginTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/resource_plugin/#plugin/{0}\">";
                            pluginTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver vínculos</span></div>";
                            pluginTemplate += "               </a>";
                            pluginTemplate += "           </td>";
                            pluginTemplate += "           <td class=\"col2\">";
                            pluginTemplate += "               <div class=\"title\"><span class=\"name field-editor\">{1}</span><span class=\"date\">{4}</span><div class=\"clear-block\"></div></div>";
                            pluginTemplate += "               <div class=\"links no-bg\">";
                            pluginTemplate += "                   <div class=\"first\">Uri: {3}<br clear=\"all\"></div>";
                            pluginTemplate += "                   <div class=\"\"><a href=\"" + ApplicationVirtualPath + "admin/plugin/{0}/flow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-sitemap\">Fluxo de dados</div></a></div>";
                            pluginTemplate += "                   <div class=\"last\">{5}<br clear=\"all\"></div>";
                            pluginTemplate += "               </div><br clear=\"all\">";
                            pluginTemplate += "           </td>";
                            pluginTemplate += "       </tr>";
                            pluginTemplate += "   </tbody>";
                            pluginTemplate += "</table></div>";

                            html += "<div id=\"plugin-container\" class=\"box-container\">";

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
                                        method = "plugin.list",
                                        parameters = new
                                        {
                                            page_size = pageSize,
                                            page = page
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
                                        method = "plugin.search",
                                        parameters = new
                                        {
                                            text = query,
                                            page_size = pageSize,
                                            page = page
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

                                PluginListResult ret2 = JSON.Deserialize<PluginListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("plugin_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("plugin_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (PluginData plugin in ret2.result)
                                        html += String.Format(pluginTemplate, plugin.plugin_id, plugin.name, plugin.resource_plugin_qty, plugin.uri, (plugin.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(plugin.create_date), true) : ""), (plugin.enterprise_id > 0 ? "<a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/plugin/" + plugin.plugin_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o plugin '" + plugin.uri + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>" : ""));

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

                                html += "<span class=\"empty-results content-loading plugin-list-loader hide\"></span>";

                                contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                            }
                            else
                            {
                                contentRet = new WebJsonResponse("#content-wrapper #plugin-container", (eHtml != "" ? eHtml : html), true);
                            }

                            contentRet.js = js + "$( document ).unbind('end_of_scroll');";

                            if (hasNext)
                                contentRet.js += "$( document ).bind( 'end_of_scroll.loader_plugin', function() { $( document ).unbind('end_of_scroll.loader_plugin'); $('.plugin-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'" + (!String.IsNullOrWhiteSpace(query) ? query : "") + "' }, function(){ $('.plugin-list-loader').addClass('hide'); } ); });";

                        }
                        else//Esta sendo selecionado o plugin
                        {
                            if (error != "")
                            {
                                contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                            }
                            else
                            {
                                switch (filter)
                                {
                                    case "flow":

                                        String js2 = "";
                                        if (filter == "" || filter == "flow")
                                        {
                                            html += "<h3>Fluxo de dados</h3>";
                                            html += "<div id=\"pluginChart\"></div>";
                                            js2 = "$('#pluginChart').flowchart({load_uri: '" + ApplicationVirtualPath + "admin/chartdata/flow/plugin/" + retPlugin.result.info.plugin_id + "/'});";
                                        }

                                        contentRet = new WebJsonResponse("#content-wrapper", html);
                                        contentRet.js = js2;
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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/plugin/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo plugin</button></div>";

                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    break;


                case "buttonbox":

                    switch (filter)
                    {
                        case "":


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