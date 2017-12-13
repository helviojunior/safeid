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
    public partial class proxy : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Proxy", ApplicationVirtualPath + "admin/proxy/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Proxy", ApplicationVirtualPath + "admin/proxy/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 proxyId = 0;
            try
            {
                proxyId = Int64.Parse((String)RouteData.Values["id"]);

                if (proxyId < 0)
                    proxyId = 0;
            }
            catch { }

            String error = "";
            ProxyGetResult retProxy = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((proxyId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    String rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "proxy.get",
                        parameters = new
                        {
                            proxyid = proxyId
                        },
                        id = 1
                    });
                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);
                                            

                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retProxy = JSON.Deserialize<ProxyGetResult>(jData);
                    if (retProxy == null)
                    {
                        error = MessageResource.GetMessage("proxy_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retProxy.error != null)
                    {
                        error = retProxy.error.data;
                        retProxy = null;
                    }
                    else if (retProxy.result == null || retProxy.result.info == null)
                    {
                        error = MessageResource.GetMessage("proxy_not_found");
                        retProxy = null;
                    }
                    else
                    {
                        menu3.Name = retProxy.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retProxy = null;
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


                        html = "<h3>Adição de proxy</h3>";
                        html += "<form id=\"form_add_proxy\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/proxy/action/add_proxy/\"><div class=\"no-tabs pb10\">";
                        html += "<div class=\"form-group\"><label>Nome</label><input id=\"proxy_name\" name=\"proxy_name\" placeholder=\"Digite o nome do proxy\" type=\"text\"\"></div>";
                        html += "<div class=\"clear-block\"></div></div>";
                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/proxy/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));

                    }
                    else
                    {
                        if (retProxy == null)
                        {

                            Int32 page = 1;
                            Int32 pageSize = 20;
                            Boolean hasNext = true;

                            Int32.TryParse(Request.Form["page"], out page);

                            if (page < 1)
                                page = 1;

                            String proxyTemplate = "<div id=\"proxy-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" data-total=\"{2}\" class=\"app-list-item\">";
                            proxyTemplate += "<table>";
                            proxyTemplate += "   <tbody>";
                            proxyTemplate += "       <tr>";
                            proxyTemplate += "           <td class=\"col1\">";
                            proxyTemplate += "               <span id=\"total_{0}\" class=\"total \">{2}</span>";
                            proxyTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/resource/#proxy/{0}\">";
                            proxyTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver recursos</span></div>";
                            proxyTemplate += "               </a>";
                            proxyTemplate += "           </td>";
                            proxyTemplate += "           <td class=\"col2\">";
                            proxyTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"proxy_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#proxy_name_{0}',null,proxyNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                            proxyTemplate += "               <div class=\"description\">";
                            proxyTemplate += "                   <div class=\"first\">{4}</div>";
                            proxyTemplate += "               </div>";
                            proxyTemplate += "               <div class=\"links\">";
                            proxyTemplate += "                   <div class=\"last\">{5}</div>";
                            proxyTemplate += "               </div>";
                            proxyTemplate += "           </td>";
                            proxyTemplate += "       </tr>";
                            proxyTemplate += "   </tbody>";
                            proxyTemplate += "</table></div>";

                            js += "proxyNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

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
                                        method = "proxy.list",
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
                                        method = "proxy.search",
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

                                ProxyListResult ret2 = JSON.Deserialize<ProxyListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("proxy_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("proxy_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (ProxyData proxy in ret2.result)
                                    {
                                        String text = "";
                                        if (proxy.last_sync > 0)
                                        {
                                            DateTime lastSync = new DateTime(1970, 1, 1).AddSeconds(proxy.last_sync);
                                            TimeSpan ts = DateTime.Now - lastSync;
                                            if (ts.TotalSeconds > 60)
                                            {
                                                text = "<span class=\"red-text\">Última conexão a " + MessageResource.FormatTs(ts) + " através do endereço " + proxy.last_sync_address + ". Versão: " + proxy.last_sync_version + "</span>";
                                            }
                                            else
                                            {
                                                text = "On-line através do endereço " + proxy.last_sync_address + ". Versão: " + proxy.last_sync_version;
                                            }
                                        }
                                        else
                                        {
                                            text = "<span class=\"red-text\">Nunca se conectou no servidor</span>";
                                        }

                                        String links = "";
                                        links += (proxy.resource_qty > 0 ? "" : "<a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/proxy/" + proxy.proxy_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o proxy '" + proxy.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>");
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/proxy/" + proxy.proxy_id + "/direct/download/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-download-alt\">Download (instalador e configuração)</div></a>";

                                        html += String.Format(proxyTemplate, proxy.proxy_id, proxy.name, proxy.resource_qty, (proxy.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(proxy.create_date), true) : ""), text, links);
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
                        else//Esta sendo selecionado o proxy
                        {
                            if (error != "")
                            {
                                contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                            }
                            else
                            {

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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/proxy/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo proxy</button></div>";
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