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
    public partial class context : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Contexto", ApplicationVirtualPath + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Contexto", ApplicationVirtualPath + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 contextId = 0;
            try
            {
                contextId = Int64.Parse((String)RouteData.Values["id"]);

                if (contextId < 0)
                    contextId = 0;
            }
            catch { }

            String error = "";
            ContextGetResult selectedContext = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((contextId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    String rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "context.get",
                        parameters = new
                        {
                            contextid = contextId
                        },
                        id = 1
                    });
                    String jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);
                    
                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    selectedContext = JSON.Deserialize<ContextGetResult>(jData);
                    if (selectedContext == null)
                    {
                        error = MessageResource.GetMessage("context_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (selectedContext.error != null)
                    {
                        error = selectedContext.error.data;
                        selectedContext = null;
                    }
                    else if (selectedContext.result == null || selectedContext.result.info == null)
                    {
                        error = MessageResource.GetMessage("context_not_found");
                        selectedContext = null;
                    }
                    else
                    {
                        menu3.Name = selectedContext.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    selectedContext = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }

                
            }

            String groupTemplate = "<div id=\"{0}\"><div class=\"group\" group-id=\"{0}\"><div class=\"wrapper\"><div class=\"cmd-bar\"><div class=\"ico icon-close floatright\"></div></div>{1}<div class=\"cmd-bar1\"><div class=\"ico icon-plus floatright\"></div></div></div><div class=\"clear-block\"></div></div><div class=\"selector-wrapper\">{2}</div></div>";
            String groupSelectorTemplate = "<div class=\"group-selector\"><div class=\"item selected\" value=\"or\">OU</div><div class=\"clear-block\"></div></div>";
            String filterSelectorTemplate = "<div class=\"filter-selector\"><div class=\"item selected\">+</div><div class=\"clear-block\"></div></div>";

            switch (area)
            {
                case "":
                case "search":
                case "content":
                    if (newItem)
                    {

                        html = "<h3>Adição de contexto</h3>";
                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/context/action/add_context/\"><div class=\"no-tabs pb10\">";
                        html += "<div class=\"form-group\"><label>Nome</label><input id=\"add_context_name\" name=\"add_context_name\" placeholder=\"Digite o nome do contexto\" type=\"text\"\"></div>";
                        html += "<div class=\"form-group\"><label>Regra de senha</label><div class=\"custom-item\" id=\"pwd_rule\" onclick=\"iamadmin.buildPwdRule(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/context/new/content/pwd-build/\"><input type=\"hidden\" name=\"pwd_rule\" /><span>Nenhuma regra definida</span><i class=\"icon-change\"></i></div></div>";
                        html += "<div class=\"clear-block\"></div></div>";

                        html += "<h3>Complexidade de senha</h3><div class=\"no-tabs pb10\">";
                        html += "<div class=\"form-group\"><label>Tamanho mínimo</label><input id=\"pwd_length\" name=\"pwd_length\" placeholder=\"Digite o tamanho mínimo de senha\" type=\"text\"\" value=\"8\"></div>";
                        html += "<div class=\"form-group\"><label>Caracter maiúsculo</label><input id=\"pwd_upper_case\" name=\"pwd_upper_case\" type=\"checkbox\" checked><span class=\"checkbox-label\">Exigir</span></div>";
                        html += "<div class=\"form-group\"><label>Caracter minúsculo</label><input id=\"pwd_lower_case\" name=\"pwd_lower_case\" type=\"checkbox\" checked><span class=\"checkbox-label\">Exigir</span></div>";
                        html += "<div class=\"form-group\"><label>Número</label><input id=\"pwd_digit\" name=\"pwd_digit\" type=\"checkbox\" checked><span class=\"checkbox-label\">Exigir</span></div>";
                        html += "<div class=\"form-group\"><label>Caracter especiais</label><input id=\"pwd_symbol\" name=\"pwd_symbol\" type=\"checkbox\" checked><span class=\"checkbox-label\">Exigir</span></div>";
                        html += "<div class=\"form-group\"><label>Parte do nome</label><input id=\"pwd_no_name\" name=\"pwd_no_name\" type=\"checkbox\" checked><span class=\"checkbox-label\">Não permitir</span></div>";
                        html += "<div class=\"clear-block\"></div></div>";
                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                    }
                    else
                    {
                        if (selectedContext == null)
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
                            roleTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/users/#context/{0}\">";
                            roleTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver usuários</span></div>";
                            roleTemplate += "               </a>";
                            roleTemplate += "           </td>";
                            roleTemplate += "           <td class=\"col2\">";
                            roleTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"context_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#context_name_{0}',null,contextNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                            roleTemplate += "               <div class=\"links no-bg\">";
                            roleTemplate += "                   <div class=\"first\"><a href=\"" + ApplicationVirtualPath + "admin/context/{0}/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar</div></a><a href=\"" + ApplicationVirtualPath + "admin/context/{0}/login_rule/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-cogs\">Regra de criação de login</div></a><a href=\"" + ApplicationVirtualPath + "admin/context/{0}/mail_rule/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-cogs\">Regra de criação de e-mails</div></a><br clear=\"all\"></div>";
                            roleTemplate += "                   <div class=\"\"><a href=\"" + ApplicationVirtualPath + "admin/context/{0}/flow/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-sitemap\">Fluxo de dados</div></a></div>";
                            roleTemplate += "                   <div class=\"last\"><a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/context/{0}/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o contexto '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a><br clear=\"all\"></div>";
                            roleTemplate += "               </div><br clear=\"all\">";
                            roleTemplate += "           </td>";
                            roleTemplate += "       </tr>";
                            roleTemplate += "   </tbody>";
                            roleTemplate += "</table></div>";

                            js += "contextNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

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
                                        method = "context.list",
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
                                        method = "context.search",
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

                                ContextListResult ret2 = JSON.Deserialize<ContextListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("context_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("context_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (ContextData role in ret2.result)
                                        html += String.Format(roleTemplate, role.context_id, role.name, role.entity_qty, (role.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(role.create_date), true) : ""));

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
                            String jData = "";
                            String rData = "";

                            if (error != "")
                            {
                                contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                            }
                            else
                            {

                                switch (filter)
                                {

                                    case "":
                                        html = "<h3>Edição de contexto</h3>";
                                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/context/" + selectedContext.result.info.context_id + "/action/change/\"><div class=\"no-tabs pb10\">";
                                        html += "<div class=\"form-group\"><label>Nome</label><input id=\"add_context_name\" name=\"add_context_name\" placeholder=\"Digite o nome do contexto\" type=\"text\"\" value=\"" + selectedContext.result.info.name + "\"></div>";
                                        html += "<div class=\"form-group\"><label>Regra de senha</label><div class=\"custom-item\" id=\"pwd_rule\" onclick=\"iamadmin.buildPwdRule(this);\" data-uri=\"" + ApplicationVirtualPath + "admin/context/new/content/pwd-build/\"><input type=\"hidden\" name=\"pwd_rule\" value=\"" + selectedContext.result.info.password_rule + "\" /><span>" + selectedContext.result.info.password_rule + "</span><i class=\"icon-change\"></i></div></div>";
                                        html += "<div class=\"clear-block\"></div></div>";

                                        html += "<h3>Complexidade de senha</h3><div class=\"no-tabs pb10\">";
                                        html += "<div class=\"form-group\"><label>Tamanho mínimo</label><input id=\"pwd_length\" name=\"pwd_length\" placeholder=\"Digite o tamanho mínimo de senha\" type=\"text\"\" value=\"" + selectedContext.result.info.password_length + "\"></div>";
                                        html += "<div class=\"form-group\"><label>Caracter maiúsculo</label><input id=\"pwd_upper_case\" name=\"pwd_upper_case\" type=\"checkbox\" " + (selectedContext.result.info.password_upper_case ? "checked" : "") + "><span class=\"checkbox-label\">Exigir</span></div>";
                                        html += "<div class=\"form-group\"><label>Caracter minúsculo</label><input id=\"pwd_lower_case\" name=\"pwd_lower_case\" type=\"checkbox\" " + (selectedContext.result.info.password_lower_case ? "checked" : "") + "><span class=\"checkbox-label\">Exigir</span></div>";
                                        html += "<div class=\"form-group\"><label>Número</label><input id=\"pwd_digit\" name=\"pwd_digit\" type=\"checkbox\" " + (selectedContext.result.info.password_digit ? "checked" : "") + "><span class=\"checkbox-label\">Exigir</span></div>";
                                        html += "<div class=\"form-group\"><label>Caracter especiais</label><input id=\"pwd_symbol\" name=\"pwd_symbol\" type=\"checkbox\" " + (selectedContext.result.info.password_symbol ? "checked" : "") + "><span class=\"checkbox-label\">Exigir</span></div>";
                                        html += "<div class=\"form-group\"><label>Parte do nome</label><input id=\"pwd_no_name\" name=\"pwd_no_name\" type=\"checkbox\" " + (selectedContext.result.info.password_no_name ? "checked" : "") + "><span class=\"checkbox-label\">Não permitir</span></div>";
                                        html += "<div class=\"clear-block\"></div></div>";
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                        break;


                                    case "login_rule":

                                        //

                                        rData = SafeTrend.Json.JSON.Serialize2(new
                                        {
                                            jsonrpc = "1.0",
                                            method = "context.getloginrules",
                                            parameters = new
                                            {
                                                contextid = contextId
                                            },
                                            id = 1
                                        });

                                        
                                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                            jData = WebPageAPI.ExecuteLocal(database, this, rData);
                    
                                        if (String.IsNullOrWhiteSpace(jData))
                                            throw new Exception("");

                                        ContextLoginMailRulesResult ctxLoginRules = JSON.Deserialize<ContextLoginMailRulesResult>(jData);
                                        if (ctxLoginRules == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                                        }
                                        else if (ctxLoginRules.error != null)
                                        {
                                            error = ctxLoginRules.error.data;
                                            contentRet = new WebJsonResponse("", ctxLoginRules.error.data, 3000, true);
                                        }
                                        else if (ctxLoginRules.result == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                                        }
                                        else
                                        {
                                            String loginTemplate = GetLoginTemplate("");

                                            html = "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/context/" + selectedContext.result.info.context_id + "/action/change_login_rules/\">";
                                            html += "<h3>Regra para criação de login</h3><div class=\"no-tabs pb10\">";
                                            html += "<div id=\"filter_conditions\"><div><div class=\"a-btn blue secondary\" onclick=\"iamfnc.addRule();\">Inserir regra</div></div>";
                                            html += "<div class=\"filter-groups\">";

                                            if (ctxLoginRules.result.Count == 0)
                                            {
                                                String rules = String.Format(loginTemplate, "R0", "0", "");
                                                html += String.Format(groupTemplate, "0", rules, "");
                                            }
                                            else
                                            {

                                                for (Int32 r = 0; r < ctxLoginRules.result.Count; r++)
                                                {
                                                    String rules = "";

                                                    //Split items
                                                    String[] fi = ctxLoginRules.result[r].rule.Split(",".ToCharArray());

                                                    for (Int32 fIndex = 0; fIndex < fi.Length; fIndex++)
                                                    {
                                                        String fId = r + "-" + fIndex;

                                                        String ft = GetLoginTemplate(fi[fIndex]);

                                                        rules += String.Format(ft, "R" + fId, r, (fIndex < fi.Length - 1 ? String.Format(filterSelectorTemplate, fId) : ""));
                                                    }

                                                    html += String.Format(groupTemplate, r, rules, (r < ctxLoginRules.result.Count - 1 ? String.Format(groupSelectorTemplate, r) : ""));

                                                    //html += String.Format(groupTemplate, fr.FilterGroups[g].GroupId, filters, (g < fr.FilterGroups.Count - 1 ? (fr.FilterGroups[g].Selector == FilterSelector.AND ? String.Format(groupSelectorTemplate, fr.FilterGroups[g].GroupId, "", "selected") : String.Format(groupSelectorTemplate, fr.FilterGroups[g].GroupId, "selected", "")) : ""));
                                                }

                                            }

                                            html += "</div>";

                                            html += "</div><div class=\"clear-block\"></div></div>";
                                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                            contentRet.js = GetLoginJS(groupTemplate, groupSelectorTemplate, loginTemplate, filterSelectorTemplate);

                                        }


                                        break;


                                    case "mail_rule":

                                        //

                                        rData = SafeTrend.Json.JSON.Serialize2(new
                                        {
                                            jsonrpc = "1.0",
                                            method = "context.getmailrules",
                                            parameters = new
                                            {
                                                contextid = contextId
                                            },
                                            id = 1
                                        });

                                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                        if (String.IsNullOrWhiteSpace(jData))
                                            throw new Exception("");

                                        ContextLoginMailRulesResult ctxMailRules = JSON.Deserialize<ContextLoginMailRulesResult>(jData);
                                        if (ctxMailRules == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                                        }
                                        else if (ctxMailRules.error != null)
                                        {
                                            error = ctxMailRules.error.data;
                                            contentRet = new WebJsonResponse("", ctxMailRules.error.data, 3000, true);
                                        }
                                        else if (ctxMailRules.result == null)
                                        {
                                            contentRet = new WebJsonResponse("", MessageResource.GetMessage("context_not_found"), 3000, true);
                                        }
                                        else
                                        {
                                            String loginTemplate = GetLoginTemplate("");

                                            html = "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/context/" + selectedContext.result.info.context_id + "/action/change_mail_rules/\">";
                                            html += "<h3>Regra para criação de e-mail</h3><div class=\"no-tabs pb10\">";
                                            html += "<div id=\"filter_conditions\"><div><div class=\"a-btn blue secondary\" onclick=\"iamfnc.addRule();\">Inserir regra</div></div>";
                                            html += "<div class=\"filter-groups\">";

                                            if (ctxMailRules.result.Count == 0)
                                            {
                                                String rules = String.Format(loginTemplate, "R0", "0", "");
                                                html += String.Format(groupTemplate, "0", rules, "");
                                            }
                                            else
                                            {

                                                for (Int32 r = 0; r < ctxMailRules.result.Count; r++)
                                                {
                                                    String rules = "";

                                                    //Split items
                                                    String[] fi = ctxMailRules.result[r].rule.Split(",".ToCharArray());

                                                    for (Int32 fIndex = 0; fIndex < fi.Length; fIndex++)
                                                    {
                                                        String fId = r + "-" + fIndex;

                                                        String ft = GetLoginTemplate(fi[fIndex]);

                                                        rules += String.Format(ft, "R" + fId, r, (fIndex < fi.Length - 1 ? String.Format(filterSelectorTemplate, fId) : ""));
                                                    }

                                                    html += String.Format(groupTemplate, r, rules, (r < ctxMailRules.result.Count - 1 ? String.Format(groupSelectorTemplate, r) : ""));

                                                    //html += String.Format(groupTemplate, fr.FilterGroups[g].GroupId, filters, (g < fr.FilterGroups.Count - 1 ? (fr.FilterGroups[g].Selector == FilterSelector.AND ? String.Format(groupSelectorTemplate, fr.FilterGroups[g].GroupId, "", "selected") : String.Format(groupSelectorTemplate, fr.FilterGroups[g].GroupId, "selected", "")) : ""));
                                                }

                                            }

                                            html += "</div>";

                                            html += "</div><div class=\"clear-block\"></div></div>";
                                            html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/context/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                            contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                            contentRet.js = GetLoginJS(groupTemplate, groupSelectorTemplate, loginTemplate, filterSelectorTemplate);

                                        }


                                        break;


                                    case "flow":

                                        String js2 = "";
                                        if (filter == "" || filter == "flow")
                                        {
                                            html += "<h3>Fluxo de dados</h3>";
                                            html += "<div id=\"contextChart\"></div>";
                                            js2 = "$('#contextChart').flowchart({load_uri: '" + ApplicationVirtualPath + "admin/chartdata/flow/context/" + selectedContext.result.info.context_id + "/'});";
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
                    {
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/context/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo contexto</button></div>";

                        switch (filter)
                        {

                            case "add_user":
                            case "login_rule":
                                break;

                            default:
                                if (selectedContext != null)
                                {
                                    html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" style=\"font-size: 13px;\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/context/" + selectedContext.result.info.context_id + "/login_rule/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Regras de criação de login</button></div>";
                                }
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
                            break;

                    }
                    break;


                case "pwd-build":
                    html += "<div class=\"no-tabs pb10\" style=\"width:480px;\">";
                    html += "<div class=\"form-group\"><label>Tipo</label><select id=\"pwd_rule\" name=\"pwd_rule\"><option value=\"\"></option><option value=\"default\">Senha padrão</option><option value=\"random\">Senha randomica</option></select></div>";
                    html += "<div class=\"form-group\" style=\"visibility:hidden;\" id=\"field_pass\"><label>Senha padrão</label><input id=\"pwd_pass\" name=\"pwd_pass\" placeholder=\"Digite a senha padrão\" type=\"text\"\"></div>";
                    html += "<div class=\"clear-block\"></div></div>";

                    contentRet = new WebJsonResponse("#modal-box .alert-box-content", html);
                    contentRet.js = "$('#modal-box #pwd_rule').change(function(oThis){ if ($( this ).val() == 'default'){ $('#modal-box #field_pass').css('visibility','visible') }else{ $('#modal-box #field_pass').css('visibility','hidden') } });";
                    break;

            }

            if (contentRet != null)
            {
                if (!String.IsNullOrWhiteSpace((String)Request["cid"]))
                    contentRet.callId = (String)Request["cid"];

                Retorno.Controls.Add(new LiteralControl(contentRet.ToJSON()));
            }
        }


        private String GetLoginJS(String groupTemplate, String groupSelectorTemplate, String filterTemplate, String filterSelectorTemplate)
        {

            String js = "";
            js += "function buildTriggers(){ ";

            js += "     $('#filter_conditions .filter .icon-close').unbind('click');";
            js += "     $('#filter_conditions .filter .icon-close').click(function(){";
            js += "         var group = $(this).closest('.group');";
            js += "         $(this).closest('.filter').parent().remove();";
            js += "         var lc = $('.filter', group).last().parent();";
            js += "         $('.filter-selector-wrapper', lc).html('');";
            js += "     });";

            js += "     $('#filter_conditions .cmd-bar .icon-close').unbind('click');";
            js += "     $('#filter_conditions .cmd-bar .icon-close').click(function(){";
            js += "         $(this).closest('.group').parent().remove();";
            js += "         var lc = $('#filter_conditions .group').last().parent();";
            js += "         $('.selector-wrapper', lc).html('');";
            js += "     });";

            js += "     $('#filter_conditions .cmd-bar1 .icon-plus').unbind('click');";
            js += "     $('#filter_conditions .cmd-bar1 .icon-plus').click(function(){";
            js += "         addRule();";
            js += "     });";

            js += "     $('#filter_conditions .filter .icon-plus').unbind('click');";
            js += "     $('#filter_conditions .filter .icon-plus').click(function(){";
            js += "         var group = $(this).closest('.group');";
            js += "         var lc = $(this).closest('.filter').parent();";
            js += "         var id = new Date().getTime(); ";
            js += "         var gid = group.attr('group-id'); ";
            js += "         $('" + String.Format(filterTemplate, "[id]", "[group]", "") + "'.replace(/\\[group\\]/g,gid).replace(/\\[id\\]/g,id)).insertAfter(lc);";
            js += "         $('#' + id + ' .filter-selector-wrapper').html('" + String.Format(filterSelectorTemplate, "[id]", "", "selected") + "'.replace(/\\[id\\]/g,id));";
            js += "         if($('.filter-selector-wrapper', lc).html() == '')";
            js += "             $('.filter-selector-wrapper', lc).html('" + String.Format(filterSelectorTemplate, "[id]", "", "selected") + "'.replace(/\\[id\\]/g,lc.attr('id')));";
            js += "         var lc = $('.filter', group).last().parent();";
            js += "         $('.filter-selector-wrapper', lc).html('');";
            js += "         buildTriggers();";
            js += "     });";

            js += "     var fIndex = 0;";
            js += "     $('#filter_conditions .filter-field').each(function() { ";
            js += "         $('.order', $( this ).closest('.filter') ).val(fIndex++);";
            js += "     });";


            js += "};";

            js += "buildTriggers();";

            js += "function addRule(){ ";
            js += "     var id = new Date().getTime(); ";
            js += "     var lc = $('#filter_conditions .group').last().parent();";
            js += "     if (lc.length > 0){";
            js += "         $('.selector-wrapper', lc).html('" + String.Format(groupSelectorTemplate, "[id]", "selected", "") + "'.replace(/\\[id\\]/g,lc.attr('id')));";
            js += "         $('" + String.Format(groupTemplate, "[id]", "", "") + "'.replace(/\\[id\\]/g,id)).insertAfter(lc);";
            js += "     }else{";
            js += "         $('#filter_conditions .filter-groups').html('" + String.Format(groupTemplate, "[id]", "", "") + "'.replace(/\\[id\\]/g,id));";
            js += "     }";
            js += "     var idF = 'R' + id;";
            js += "     $('" + String.Format(filterTemplate, "[id]", "[group]", "") + "'.replace(/\\[group\\]/g,id).replace(/\\[id\\]/g,idF)).insertAfter($('#' + id + ' .cmd-bar'));";
            js += "     buildTriggers();";
            js += "};";


            js += "iamfnc.addRule = function(){ ";
            js += "     addRule();";
            js += "};";

            return js;
        }


        private String GetLoginTemplate(String condition)
        {


            String ruleTemplate = "<div id=\"{0}\"><div class=\"filter\"><input type=\"hidden\" name=\"rule_id\" value=\"{0}\" /><input type=\"hidden\" name=\"rule_{0}_group\" value=\"{1}\" /><input type=\"hidden\" class=\"order\" name=\"rule_{0}_order\" value=\"0\" /><table><tbody><tr><td class=\"col1\">";
            ruleTemplate += "<select class=\"filter-field\" id=\"rule_{0}_field\" name=\"rule_{0}_field\">";
            Dictionary<String, String> temp = new Dictionary<string, string>();
            temp.Add("first_name","Primeiro nome");
            temp.Add("second_name", "Segundo nome");
            temp.Add("last_name", "Último nome");
            temp.Add("char_first_name", "Primeira letra do primeiro nome");
            temp.Add("char_second_name", "Primeira letra do segundo nome");
            temp.Add("char_last_name", "Primeira letra do último nome");
            temp.Add("index", "Número sequencial");
            temp.Add("dot", "Ponto (.)");
            temp.Add("hyphen", "Hifen (-)");
            //temp.Add("hyphen", "Hifen (-)");

            foreach (String k in temp.Keys)
            {
                ruleTemplate += "<option value=\"" + k + "\" " + (condition == k ? "selected" : "") + ">" + temp[k] + "</option>";
            }

            ruleTemplate += "</select>";
            ruleTemplate += "</td>";

            ruleTemplate += "<td class=\"col4\"><div class=\"ico icon-close floatright\"></div><div class=\"ico icon-plus floatright\"></div></td></tr></tbody></table><div class=\"clear-block\"></div></div><div class=\"filter-selector-wrapper\">{2}</div></div>";

            return ruleTemplate;
        }

    }
}