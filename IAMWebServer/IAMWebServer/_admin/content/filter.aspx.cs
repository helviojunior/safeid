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
    public partial class filter : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Filtros", ApplicationVirtualPath + "admin/filter/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Filtros", ApplicationVirtualPath + "admin/filter/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 filterId = 0;
            try
            {
                filterId = Int64.Parse((String)RouteData.Values["id"]);

                if (filterId < 0)
                    filterId = 0;
            }
            catch { }

            String error = "";
            FilterGetResult retFilter = null;
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

            if ((filterId > 0) && (area.ToLower() != "search"))
            {


                try
                {

                    rData = SafeTrend.Json.JSON.Serialize2(new
                    {
                        jsonrpc = "1.0",
                        method = "filter.get",
                        parameters = new
                        {
                            filterid = filterId
                        },
                        id = 1
                    });
                    jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retFilter = JSON.Deserialize<FilterGetResult>(jData);
                    if (retFilter == null)
                    {
                        error = MessageResource.GetMessage("filter_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retFilter.error != null)
                    {
                        error = retFilter.error.data;
                        retFilter = null;
                    }
                    else if (retFilter.result == null || retFilter.result.info == null)
                    {
                        error = MessageResource.GetMessage("filter_not_found");
                        retFilter = null;
                    }
                    else
                    {
                        menu3.Name = retFilter.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retFilter = null;
                    //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }


            }

            String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

            String groupTemplate = "<div id=\"{0}\"><div class=\"group\" group-id=\"{0}\"><div class=\"wrapper\"><div class=\"cmd-bar\"><div class=\"ico icon-close floatright\"></div></div>{1}<div class=\"cmd-bar1\"><div class=\"ico icon-plus floatright\"></div></div></div><div class=\"clear-block\"></div></div><div class=\"selector-wrapper\">{2}</div></div>";
            String groupSelectorTemplate = "<div class=\"group-selector\"><select type=\"checkbox\" name=\"group_{0}_selector\"><option value=\"or\" {1}>or</option><option value=\"and\" {2}>and</option></select><div class=\"item {1}\" value=\"or\">OU</div><div class=\"item {2}\" value=\"and\">E</div><div class=\"clear-block\"></div></div>";
            String filterSelectorTemplate = "<div class=\"filter-selector\"><select type=\"checkbox\" name=\"filter_{0}_selector\"><option value=\"or\" {1}>or</option><option value=\"and\" {2}>and</option></select> <div class=\"item {1}\" value=\"or\">OU</div><div class=\"item {2}\" value=\"and\">E</div><div class=\"clear-block\"></div></div>";


            switch (area)
            {
                case "":
                case "search":
                case "content":
                    if (newItem)
                    {

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

                        String filterTemplate = GetFilterTemplate(fieldList, 0, "", "");

                        html = "<h3>Adição do filtro</h3>";
                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/filter/action/add_filter/\">";
                        html += "<div class=\"no-tabs fields\"><table><tbody>";
                        html += String.Format(infoTemplate, "Nome", "<input id=\"filter_name\" name=\"filter_name\" placeholder=\"Digite o nome do filtro\" type=\"text\"\">");
                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                        html += "<h3>Parametros de filtragem</h3><div class=\"no-tabs pb10\">";
                        html += "<div id=\"filter_conditions\"><div><div class=\"a-btn blue secondary\" onclick=\"iamfnc.addGroup();\">Inserir grupo</div></div>";
                        html += "<div class=\"filter-groups\">";


                        String filters = String.Format(filterTemplate, "F0", "0", "");
                        html += String.Format(groupTemplate, "0", filters, "");

                        html += "</div>";

                        html += "</div><div class=\"clear-block\"></div></div>";
                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/filter/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        contentRet.js = GetFilterJS(groupTemplate, groupSelectorTemplate, filterTemplate, filterSelectorTemplate);

                    }
                    else
                    {
                        if (retFilter == null)
                        {

                            Int32.TryParse(Request.Form["page"], out page);

                            if (page < 1)
                                page = 1;

                            String proxyTemplate = "<div id=\"proxy-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" data-total=\"{2}\" class=\"app-list-item\">";
                            proxyTemplate += "<table>";
                            proxyTemplate += "   <tbody>";
                            proxyTemplate += "       <tr>";
                            proxyTemplate += "           <td class=\"col1\">";
                            proxyTemplate += "               <span id=\"total_{0}\" class=\"total \">{2}</span>";
                            proxyTemplate += "               <a href=\"" + ApplicationVirtualPath + "admin/filter/{0}/use/\">";
                            proxyTemplate += "                   <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver utilizações</span></div>";
                            proxyTemplate += "               </a>";
                            proxyTemplate += "           </td>";
                            proxyTemplate += "           <td class=\"col2\">";
                            proxyTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"filter_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#filter_name_{0}',null,filterNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
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

                            js += "filterNameEdit = function(thisId, changedText) { iamadmin.changeName(thisId,changedText); };";

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
                                        method = "filter.list",
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
                                        method = "filter.search",
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

                                FilterListResult ret2 = JSON.Deserialize<FilterListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("filter_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("filter_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (FilterData f in ret2.result)
                                    {

                                        String text = "<span>" + f.conditions_description + "</span>";

                                        String links = "";
                                        links += "<a href=\"" + ApplicationVirtualPath + "admin/filter/" + f.filter_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar</div></a>";
                                        links += (f.ignore_qty > 0 || f.lock_qty > 0 || f.role_qty > 0 ? "" : "<a class=\"confirm-action\" href=\"" + ApplicationVirtualPath + "admin/filter/" + f.filter_id + "/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente o filtro '" + f.name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\"><div class=\"ico icon-close\">Apagar</div></a>");

                                        html += String.Format(proxyTemplate, f.filter_id, f.name, f.ignore_qty + f.lock_qty + f.role_qty, (f.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(f.create_date), true) : ""), text, links);
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
                        else//Esta sendo selecionado o filtro
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


                                        String filterTemplate = GetFilterTemplate(fieldList, 0, "", "");

                                        html = "<h3>Edição do filtro</h3>";
                                        html += "<form id=\"form_add_role\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/filter/" + retFilter.result.info.filter_id + "/action/change/\">";
                                        html += "<div class=\"no-tabs fields\"><table><tbody>";
                                        html += String.Format(infoTemplate, "Nome", "<input id=\"filter_name\" name=\"filter_name\" placeholder=\"Digite o nome do filtro\" type=\"text\"\" value=\"" + retFilter.result.info.name + "\">");
                                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                        html += "<h3>Parametros de filtragem</h3><div class=\"no-tabs pb10\">";
                                        html += "<div id=\"filter_conditions\"><div><div class=\"a-btn blue secondary\" onclick=\"iamfnc.addGroup();\">Inserir grupo</div></div>";
                                        html += "<div class=\"filter-groups\">";

                                        FilterRule fr = new FilterRule(retFilter.result.info.name);
                                        foreach (IAM.GlobalDefs.WebApi.FilterCondition cond in retFilter.result.info.conditions)
                                            fr.AddCondition(cond.group_id.ToString(), cond.group_selector, cond.field_id, cond.field_name, cond.data_type, cond.text, cond.condition, cond.selector);

                                        for (Int32 g = 0; g < fr.FilterGroups.Count; g++)
                                        {
                                            String filters = "";

                                            for (Int32 fIndex = 0; fIndex < fr.FilterGroups[g].FilterRules.Count; fIndex++)
                                            {
                                                String fId = fr.FilterGroups[g].GroupId + "-" + fIndex;

                                                String ft = GetFilterTemplate(fieldList, fr.FilterGroups[g].FilterRules[fIndex].FieldId, fr.FilterGroups[g].FilterRules[fIndex].DataString, fr.FilterGroups[g].FilterRules[fIndex].ConditionType.ToString());

                                                filters += String.Format(ft, fId, fr.FilterGroups[g].GroupId, (fIndex < fr.FilterGroups[g].FilterRules.Count - 1 ? (fr.FilterGroups[g].FilterRules[fIndex].Selector == FilterSelector.AND ? String.Format(filterSelectorTemplate, fId, "", "selected") : String.Format(filterSelectorTemplate, fId, "selected", "")) : ""));
                                            }

                                            html += String.Format(groupTemplate, fr.FilterGroups[g].GroupId, filters, (g < fr.FilterGroups.Count - 1 ? (fr.FilterGroups[g].Selector == FilterSelector.AND ? String.Format(groupSelectorTemplate, fr.FilterGroups[g].GroupId, "", "selected") : String.Format(groupSelectorTemplate, fr.FilterGroups[g].GroupId, "selected", "")) : ""));
                                        }

                                        html += "</div>";

                                        html += "</div><div class=\"clear-block\"></div></div>";
                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/filter/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));

                                        contentRet.js = GetFilterJS(groupTemplate, groupSelectorTemplate, filterTemplate, filterSelectorTemplate);

                                        break;

                                    case "use":
                                        if (retFilter != null)
                                        {

                                            Int32.TryParse(Request.Form["page"], out page);

                                            if (page < 1)
                                                page = 1;

                                            if (page == 1)
                                            {
                                                html = "<h3>Utilização deste perfil</h3>";
                                                html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                                html += "    <tr>";
                                                html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                                html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Recurso x Plugin <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Bloqueio <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Vínculo com perfil <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Desconsiderar registros <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                                html += "    </tr>";
                                                html += "</thead>";

                                                html += "<tbody>";
                                            }

                                            String trTemplate = "    <tr class=\"user\" data-userid=\"{0}\">";
                                            trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                            trTemplate += "            <td class=\"ident10\">{2}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{4}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{5}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\"><button class=\"a-btn\" onclick=\"window.location = '" + ApplicationVirtualPath + "admin/resource_plugin/{1}/';\">Abrir</button></td>";
                                            trTemplate += "    </tr>";

                                            try
                                            {

                                                rData = SafeTrend.Json.JSON.Serialize2(new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "filter.use",
                                                    parameters = new
                                                    {
                                                        page_size = pageSize,
                                                        page = page,
                                                        filterid = retFilter.result.info.filter_id
                                                    },
                                                    id = 1
                                                });

                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                FilterUseResult ret2 = JSON.Deserialize<FilterUseResult>(jData);
                                                if (ret2 == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("filter_not_found"));
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
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("filter_not_found"));
                                                    hasNext = false;
                                                }
                                                else
                                                {
                                                    foreach (FilterUseData f in ret2.result)
                                                        html += String.Format(trTemplate, f.filter_id, f.resource_plugin_id, f.resource_plugin_name, f.lock_qty, f.role_qty, f.ignore_qty);

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
                                        }
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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/filter/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo filtro</button></div>";
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

        private String GetFilterJS(String groupTemplate, String groupSelectorTemplate, String filterTemplate, String filterSelectorTemplate)
        {

            String js = "";
            js += "function buildTriggers(){ ";

            js += "     $('#filter_conditions .item').unbind('click');";
            js += "     $('#filter_conditions .item').click(function(){";
            js += "          $('.item', $(this).parent()).removeClass('selected');";
            js += "          $('select option:selected', $(this).parent()).removeAttr('selected');";
            js += "          $(this).addClass('selected');";
            js += "          $('select option[value='+ $(this).attr('value') +']', $(this).parent()).attr('selected','selected');";
            js += "     });";

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
            js += "         addGroup();";
            js += "     });";

            js += "     $('#filter_conditions .filter-now input[type=checkbox]').unbind('click');";
            js += "     $('#filter_conditions .filter-now input[type=checkbox]').click(function(){";
            js += "         var filter = $(this).closest('.filter');";
            js += "         var fid = filter.attr('filter-id'); ";
            js += "         if($(this).is(':checked')){";
            js += "             var dateField = $('.date-mask', filter); ";
            js += "             $(this).attr('default_val',dateField.val());";
            js += "             $('.time-mask', filter).addClass('dt-off');";
            js += "             dateField.addClass('dt-off');";
            js += "             dateField.addClass('date-mask-off');";
            js += "             dateField.removeClass('date-mask');";
            js += "             dateField.val('now');";
            js += "         }else{";
            js += "             var dateField = $('.date-mask-off', filter); ";
            js += "             $('.time-mask', filter).removeClass('dt-off');";
            js += "             dateField.addClass('date-mask');";
            js += "             dateField.removeClass('dt-off');";
            js += "             dateField.removeClass('date-mask-off');";
            js += "             dateField.val($(this).attr('default_val'));";
            js += "             dateField.mask('99/99/9999'); ";
            js += "         }";
            js += "     });";

            js += "     $('#filter_conditions .filter-field').unbind('change');";
            js += "     $('#filter_conditions .filter-field').change(function(){";
            js += "         $('option:selected',this).each(function() {";
            js += "             var dataType = $(this).attr('data-type');";
            js += "             var filter = $(this).closest('.filter');";
            js += "             $('.dt-check', filter).addClass('dt-off');";
            js += "             $('.dt-'+ dataType, filter).removeClass('dt-off');";
            js += "         });";
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

            //js += "     $('#filter_conditions .item input[type=checkbox]').removeAttr('checked');";
            //js += "     $('#filter_conditions .item.selected input[type=checkbox]').attr('checked','checked');";

            js += "     $('.date-mask').mask('99/99/9999'); $('.time-mask').mask('99:99:99');";

            js += "};";

            js += "buildTriggers();";

            js += "function addGroup(){ ";
            js += "     var id = new Date().getTime(); ";
            js += "     var lc = $('#filter_conditions .group').last().parent();";
            js += "     if (lc.length > 0){";
            js += "         $('.selector-wrapper', lc).html('" + String.Format(groupSelectorTemplate, "[id]", "selected", "") + "'.replace(/\\[id\\]/g,lc.attr('id')));";
            js += "         $('" + String.Format(groupTemplate, "[id]", "", "") + "'.replace(/\\[id\\]/g,id)).insertAfter(lc);";
            js += "     }else{";
            js += "         $('#filter_conditions .filter-groups').html('" + String.Format(groupTemplate, "[id]", "", "") + "'.replace(/\\[id\\]/g,id));";
            js += "     }";
            js += "     var idF = 'F' + id;";
            js += "     $('" + String.Format(filterTemplate, "[id]", "[group]", "") + "'.replace(/\\[group\\]/g,id).replace(/\\[id\\]/g,idF)).insertAfter($('#' + id + ' .cmd-bar'));";
            js += "     buildTriggers();";
            js += "};";


            js += "iamfnc.addGroup = function(){ ";
            js += "     addGroup();";
            js += "};";

            return js;
        }

        private String GetFilterTemplate(List<FieldData> fieldList, Int64 fieldId, String fieldValue, String condition)
        {

            String defaultDataType = "";
            String dtValue = MessageResource.FormatDate(DateTime.Now, true);
            String timeValue = "00:00:00";
            String txtValue = "";
            String numValue = "";

            FilterConditionType dtCondition = FilterConditionType.Equal;
            FilterConditionType txtCondition = FilterConditionType.Equal;
            FilterConditionType numCondition = FilterConditionType.Equal;

            String filterTemplate = "<div id=\"{0}\"><input type=\"hidden\" name=\"filter_id\" value=\"{0}\" /><input type=\"hidden\" name=\"filter_{0}_group\" value=\"{1}\" /><div class=\"filter\" filter-id=\"{0}\"><table><tbody><tr><td class=\"col1\">";
            filterTemplate += "<select class=\"filter-field\" id=\"filter_{0}_field_id\" name=\"filter_{0}_field_id\">";
            foreach (FieldData fd in fieldList)
            {
                if ((defaultDataType == "") || (fd.field_id == fieldId))
                    defaultDataType = fd.data_type;

                if (fd.field_id == fieldId)
                    switch (fd.data_type.ToLower())
                    {
                        case "datetime":
                            try
                            {
                                if (fieldValue == "now")
                                {
                                    dtValue = "now";
                                }
                                else
                                {
                                    DateTime tmp = DateTime.Parse(fieldValue);
                                    dtValue = MessageResource.FormatDate(tmp, true);
                                    timeValue = MessageResource.FormatTime(tmp);
                                }
                            }
                            catch { }

                            foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.DateTime))
                                if (ft.ToString().ToLower() == condition.ToLower())
                                    dtCondition = ft;

                            break;

                        case "numeric":
                            numValue = fieldValue;

                            foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.Numeric))
                                if (ft.ToString().ToLower() == condition.ToLower())
                                    numCondition = ft;
                            break;

                        default:
                            txtValue = fieldValue;

                            foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.Text))
                                if (ft.ToString().ToLower() == condition.ToLower())
                                    txtCondition = ft;
                            break;
                    }

                filterTemplate += "<option value=\"" + fd.field_id + "\" data-type=\"" + fd.data_type.ToLower() + "\" " + (fd.field_id == fieldId ? "selected" : "") + ">" + fd.name + "</option>";
            }
            filterTemplate += "</select>";
            filterTemplate += "</td><td class=\"col2\">";

            filterTemplate += "<div class=\"dt-check dt-datetime " + (defaultDataType == "datetime" ? "" : "dt-off") + "\"><select name=\"filter_{0}_condition_datetime\">";
            foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.DateTime))
                filterTemplate += "<option value=\"" + ft.ToString().ToLower() + "\" " + (dtCondition == ft ? "selected" : "") + ">" + MessageResource.GetMessage(ft.ToString().ToLower(), ft.ToString()) + "</option>";
            filterTemplate += "</select></div>";
            filterTemplate += "<div class=\"dt-check dt-numeric " + (defaultDataType == "numeric" ? "" : "dt-off") + "\"><select name=\"filter_{0}_condition_numeric\">";
            foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.Numeric))
                filterTemplate += "<option value=\"" + ft.ToString().ToLower() + "\" " + (numCondition == ft ? "selected" : "") + ">" + MessageResource.GetMessage(ft.ToString().ToLower(), ft.ToString()) + "</option>";
            filterTemplate += "</select></div>";
            filterTemplate += "<div class=\"dt-check dt-string " + (defaultDataType == "string" ? "" : "dt-off") + "\"><select name=\"filter_{0}_condition_string\">";
            foreach (FilterConditionType ft in IAM.Filters.FilterCondition.ConditionByDataType(DataType.Text))
                filterTemplate += "<option value=\"" + ft.ToString().ToLower() + "\" " + (txtCondition == ft ? "selected" : "") + ">" + MessageResource.GetMessage(ft.ToString().ToLower(), ft.ToString()) + "</option>";
            filterTemplate += "</select></div>";

            filterTemplate += "</td><td class=\"col3\">";

            filterTemplate += "<div class=\"dt-check dt-datetime " + (defaultDataType == "datetime" ? "" : "dt-off") + "\"><div display=\"block\" class=\"filter-now\"><input name=\"filter_{0}_now\" type=\"checkbox\" " + (dtValue == "now" ? "checked" : "") + " state=\"false\" />" + MessageResource.GetMessage("use_now", "Use Now() function") + "</div><input class=\"  " + (dtValue == "now" ? "dt-off date-mask-off" : "date-mask") + "\" name=\"filter_{0}_text_date\" type=\"text\" placeholder=\"Digite a data. Ex. dd/mm/yyyy\" value=\"" + dtValue + "\" /><input class=\"time-mask " + (dtValue == "now" ? "dt-off" : "") + "\" name=\"filter_{0}_text_time\" type=\"text\" placeholder=\"Digite a hora. Ex. hh:mm:ss\" value=\"" + timeValue + "\" /></div>";
            filterTemplate += "<div class=\"dt-check dt-numeric " + (defaultDataType == "numeric" ? "" : "dt-off") + "\"><input name=\"filter_{0}_text_numeric\" type=\"text\" placeholder=\"Digite o valor numérico\" value=\"" + numValue + "\" /></div>";
            filterTemplate += "<div class=\"dt-check dt-string " + (defaultDataType == "string" ? "" : "dt-off") + "\"><input name=\"filter_{0}_text_string\" type=\"text\" placeholder=\"Digite o texto\" value=\"" + txtValue + "\" /></div>";

            filterTemplate += "</td><td class=\"col4\"><div class=\"ico icon-close floatright\"></div><div class=\"ico icon-plus floatright\"></div></td></tr></tbody></table><div class=\"clear-block\"></div></div><div class=\"filter-selector-wrapper\">{2}</div></div>";

            return filterTemplate;
        }

    }
}