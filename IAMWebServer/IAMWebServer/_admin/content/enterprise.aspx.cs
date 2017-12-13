using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.WebAPI;
using System.Data.SqlClient;
using SafeTrend.Json;
using IAM.GlobalDefs.WebApi;
using IAM.AuthPlugins;

namespace IAMWebServer._admin.content
{
    public partial class enterprise : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod != "POST")
                return;

            String area = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["area"]))
                area = (String)RouteData.Values["area"];

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            EnterpriseData ent = (EnterpriseData)Page.Session["enterprise_data"];

            LMenu menu1 = new LMenu("Dashboard", ApplicationVirtualPath + "admin/");
            LMenu menu2 = new LMenu("Empresa", ApplicationVirtualPath + "admin/enterprise/");
            LMenu menu3 = new LMenu(ent.Name, ApplicationVirtualPath + "admin/enterprise/");

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String error = "";
            String filter = "";
            HashData hashData = new HashData(this);
            EnterpriseGetResult selectedEnterprise = null;

            //No caso específico da empresa (que não possibilita que o usuário selecione outra)
            //O ID se tornará o filtro
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["id"]))
                filter = (String)RouteData.Values["id"];

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            if (area.ToLower() != "search")
            {

                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "enterprise.get",
                        parameters = new
                        {
                            enterpriseid = ent.Id
                        },
                        id = 1
                    };

                    String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);
                                            

                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    selectedEnterprise = JSON.Deserialize<EnterpriseGetResult>(jData);
                    if (selectedEnterprise == null)
                    {
                        error = MessageResource.GetMessage("enterprise_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (selectedEnterprise.error != null)
                    {
                        error = selectedEnterprise.error.data;
                        selectedEnterprise = null;
                    }
                    else if (selectedEnterprise.result == null || selectedEnterprise.result.info == null)
                    {
                        error = MessageResource.GetMessage("enterprise_not_found");
                        selectedEnterprise = null;
                    }
                    else
                    {
                        menu3.Name = selectedEnterprise.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedEnterprise = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }


            }

            switch (area)
            {
                case "":
                case "content":
                    if (selectedEnterprise != null)
                    {
                        switch (filter)
                        {

                            case "":
                            case "info":

                                String infoTemplate = "<div class=\"form-group\">";
                                infoTemplate += "<label>{0}</label>";
                                infoTemplate += "<span class=\"no-edit\">{1}</span></div>";
                                String jsAdd = "";

                                if (filter == "" || filter == "info")
                                {

                                    if (hashData.GetValue("edit") == "1")
                                    {
                                        html += "<form  id=\"form_enterprise_change\"  method=\"POST\" action=\"" + ApplicationVirtualPath + "admin/enterprise/action/change/\">";
                                        html += "<h3>Informações gerais</h3>";
                                        html += "<div class=\"no-tabs pb10\">";

                                        html += String.Format(infoTemplate, "Nome", "<input id=\"name\" name=\"name\" placeholder=\"Digite o nome da empresa\" type=\"text\"\" value=\"" + selectedEnterprise.result.info.name + "\">");
                                        html += String.Format(infoTemplate, "Host principal", selectedEnterprise.result.info.fqdn);
                                        html += String.Format(infoTemplate, "Criado em", MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(selectedEnterprise.result.info.create_date), false));

                                        //Resgata a listagem dos plugins de autenticação disponíveis
                                        List<AuthBase> plugins = AuthBase.GetPlugins<AuthBase>();
                                        String select = "";
                                        select += "<select id=\"auth_plugin\" name=\"auth_plugin\" >";

                                        foreach (AuthBase p in plugins)
                                            select += "<option selector=\"" + p.GetPluginId().AbsoluteUri.Replace("/", "").Replace(":", "") + "\" value=\"" + p.GetPluginId().AbsoluteUri + "\" " + (p.Equal(new Uri(selectedEnterprise.result.info.auth_plugin)) ? "selected=\"selected\"" : "") + ">" + p.GetPluginName() + "</option>";

                                        select += "</select>";

                                        html += String.Format(infoTemplate, "Serviço de autenticação", select);

                                        //Caso tenha algum paràmetro p/ o plugin exibe
                                        foreach (AuthBase p in plugins)
                                        {
                                            AuthConfigFields[] fields = p.GetConfigFields();
                                            if (fields.Length > 0)
                                            {
                                                html += "<div class=\"auth_cont " + p.GetPluginId().AbsoluteUri.Replace("/", "").Replace(":", "") + "\" " + (p.Equal(new Uri(selectedEnterprise.result.info.auth_plugin)) ? "" : "style=\"display:none;\"") + ">";
                                                foreach (AuthConfigFields f in fields)
                                                {
                                                    String value = "";

                                                    try
                                                    {
                                                        foreach (EnterpriseAuthPars par in selectedEnterprise.result.auth_parameters)
                                                            if (par.key == f.Key)
                                                                value = par.value;
                                                    }
                                                    catch { }

                                                    html += String.Format(infoTemplate, f.Name, "<input id=\"f_" + f.Key + "\" name=\"f_" + f.Key + "\" placeholder=\"" + f.Description + "\" type=\"text\"\" value=\"" + value + "\">");
                                                }
                                                html += "</div>";
                                            }
                                        }

                                        html += "<div class=\"clear-block\"></div></div>";

                                    }
                                    else
                                    {

                                        html += "<h3>Informações gerais<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div></h3>";
                                        html += "<div class=\"no-tabs pb10\">";

                                        html += String.Format(infoTemplate, "Nome", selectedEnterprise.result.info.name);
                                        html += String.Format(infoTemplate, "Host principal", selectedEnterprise.result.info.fqdn);
                                        html += String.Format(infoTemplate, "Criado em", MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(selectedEnterprise.result.info.create_date), false));

                                        try
                                        {
                                            AuthBase plugin = AuthBase.GetPlugin(new Uri(selectedEnterprise.result.info.auth_plugin));
                                            html += String.Format(infoTemplate, "Serviço de autenticação", plugin.GetPluginName());


                                            AuthConfigFields[] fields = plugin.GetConfigFields();
                                            if (fields.Length > 0)
                                            {
                                                foreach (AuthConfigFields f in fields)
                                                {
                                                    String value = "";

                                                    try
                                                    {
                                                        foreach (EnterpriseAuthPars par in selectedEnterprise.result.auth_parameters)
                                                            if (par.key == f.Key)
                                                                value = par.value;
                                                    }
                                                    catch { }

                                                    html += String.Format(infoTemplate, f.Name, value);
                                                }
                                            }

                                        }
                                        catch
                                        {
                                            html += String.Format(infoTemplate, "Serviço de autenticação", "Erro ao carregar informações do plugin");
                                        }


                                        html += "<div class=\"clear-block\"></div></div>";
                                    }

                                    html += "<h3>Hosts complementares</h3>";
                                    html += "<div class=\"no-tabs pb10\">";


                                    if (hashData.GetValue("edit") == "1")
                                    {
                                        html += "<div id=\"enterprise_hosts\">";

                                        if (selectedEnterprise.result.fqdn_alias != null)
                                            for (Int32 i = 1; i <= selectedEnterprise.result.fqdn_alias.Count; i++)
                                                html += String.Format(infoTemplate, "Host " + i, "<input id=\"host_" + i + "\" name=\"host_" + i + "\" placeholder=\"Digite o host\" type=\"text\"\" value=\"" + selectedEnterprise.result.fqdn_alias[i - 1] + "\">");

                                        html += "</div>"; //Div enterprise_hosts

                                        html += String.Format(infoTemplate, "", "<div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.addHostField()\">Adicionar host</div>");
                                        jsAdd = "iamfnc = $.extend({}, iamfnc, { addHostField: function() { var host = 'host_'+ new Date().getTime(); $('#enterprise_hosts').append('" + String.Format(infoTemplate, "Host ", "<input id=\"'+ host +'\" name=\"'+ host +'\" placeholder=\"Digite o host\" type=\"text\">") + "'); } });";

                                        jsAdd += "$('#auth_plugin').change(function() { $('.auth_cont').css('display','none'); $('.' + $('#auth_plugin option:selected').attr('selector') ).css('display','block'); });";
                                    }
                                    else
                                    {
                                        if (selectedEnterprise.result.fqdn_alias != null)
                                            for (Int32 i = 1; i <= selectedEnterprise.result.fqdn_alias.Count; i++)
                                                html += String.Format(infoTemplate, "Host " + i, selectedEnterprise.result.fqdn_alias[i - 1]);
                                    }


                                    html += "<div class=\"clear-block\"></div></div>";

                                    if (hashData.GetValue("edit") == "1")
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"iamadmin.changeHash( 'edit/0' );\">Cancelar</a></form>";
                                }

                                contentRet = new WebJsonResponse("#content-wrapper", html);
                                contentRet.js = jsAdd;
                                break;


                            case "flow":

                                String js2 = "";
                                if (filter == "" || filter == "flow")
                                {
                                    html += "<h3>Fluxo de dados</h3>";
                                    html += "<div id=\"enterpriseChart\"></div>";
                                    js2 = "$('#enterpriseChart').flowchart({load_uri: '" + ApplicationVirtualPath + "admin/chartdata/flow/enterprise/'});";
                                }

                                contentRet = new WebJsonResponse("#content-wrapper", html);
                                contentRet.js = js2;
                                break;
                        }
                    }
                    else
                    {
                        contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                    }
                    break;

                case "sidebar":
                    if (menu1 != null)
                    {
                        html += "<div class=\"section-nav-header\">";
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
                        html += "</div>";
                    }


                    if (selectedEnterprise != null)
                    {
                        html += "<ul class=\"user-profile\">";
                        html += "<li " + (filter == "" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/enterprise/\">Todas as informações</a></span></li>";
                        html += "<li " + (filter == "flow" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/enterprise/flow\">Fluxo</a></span></li>";
                        html += "</ul>";
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