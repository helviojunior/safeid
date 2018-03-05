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
using System.Data;
using System.Data.SqlClient;

namespace IAMWebServer._admin.content
{
    public partial class users : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod != "POST")
                return;

            String area = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["area"]))
                area = (String)RouteData.Values["area"];


            Boolean newItem = false;
            if ((RouteData.Values["new"] != null) && (RouteData.Values["new"] == "1"))
                newItem = true;

            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();


            LMenu menu1 = new LMenu("Dashboard", ApplicationVirtualPath + "admin/");
            LMenu menu2 = new LMenu("Usuários", ApplicationVirtualPath + "admin/users/");
            LMenu menu3 = new LMenu("Todos usuários", ApplicationVirtualPath + "admin/users/");

            WebJsonResponse contentRet = null;

            String html = "";
            String js = "";
            String eHtml = "";

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está selecionado o usuário

            Int64 id = 0;
            try
            {
                id = Int64.Parse((String)RouteData.Values["id"]);

                if (id < 0)
                    id = 0;
            }
            catch { }


            String error = "";
            GetResult retUser = null;
            String filter = "";
            HashData hashData = new HashData(this);

            String rData = "";
            //SqlConnection conn = null;
            String jData = "";


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];


            if ((!newItem) && (id > 0) && (area.ToLower() != "search"))
            {

                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "user.get",
                        parameters = new
                        {
                            userid = id
                        },
                        id = 1
                    };

                    rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    jData = "";
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);


                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retUser = JSON.Deserialize<GetResult>(jData);
                    if (retUser == null)
                    {
                        error = MessageResource.GetMessage("user_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retUser.error != null)
                    {
                        error = retUser.error.data;
                        retUser = null;
                    }
                    else if (retUser.result == null || retUser.result.info == null)
                    {
                        error = MessageResource.GetMessage("user_not_found");
                        retUser = null;
                    }
                    else
                    {
                        menu3.Name = retUser.result.info.full_name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retUser = null;
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
                        switch (filter)
                        {
                            case "":
                            case "new":
                            case "step1":
                                String rpTemplate = "<a href=\"" + ApplicationVirtualPath + "admin/users/new/step2/{0}/\"><div id=\"resource-plugin-{0}\" class=\"app-selector-item\">{1}</div></a>";

                                html += "<h3>Selecione o template de adição</h3>";

                                rData = SafeTrend.Json.JSON.Serialize2(new
                                {
                                    jsonrpc = "1.0",
                                    method = "resourceplugin.list",
                                    parameters = new
                                    {
                                        page_size = Int32.MaxValue,
                                        page = 1,
                                        checkconfig = false
                                    },
                                    id = 1
                                });

                                jData = "";
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                if (String.IsNullOrWhiteSpace(jData))
                                    throw new Exception("");

                                ResourcePluginListResult rpList = JSON.Deserialize<ResourcePluginListResult>(jData);
                                if (rpList == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                }
                                else if (rpList.error != null)
                                {
#if DEBUG
                                    eHtml += String.Format(errorTemplate, rpList.error.data + rpList.error.debug);
#else
                                        eHtml += String.Format(errorTemplate, rpList.error.data);
#endif
                                }
                                else if (rpList.result == null || (rpList.result.Count == 0))
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                }
                                else
                                {
                                    html += "<div class=\"app-selector-containet\">";
                                    foreach (ResourcePluginFullData resourcePlugin in rpList.result)
                                        if (resourcePlugin.info.permit_add_entity && resourcePlugin.info.enable_import)
                                            html += String.Format(rpTemplate, resourcePlugin.info.resource_plugin_id, resourcePlugin.info.context_name + " - " + resourcePlugin.info.name);

                                    html += "</div>";
                                }

                                break;

                            case "step2":
                                String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

                                rData = SafeTrend.Json.JSON.Serialize2(new
                                {
                                    jsonrpc = "1.0",
                                    method = "resourceplugin.get",
                                    parameters = new
                                    {
                                        resourcepluginid = id,
                                    },
                                    id = 1
                                });
                                jData = "";
                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                if (String.IsNullOrWhiteSpace(jData))
                                    throw new Exception("");

                                ResourcePluginGetResult rp = JSON.Deserialize<ResourcePluginGetResult>(jData);
                                if (rp == null)
                                {
                                    eHtml = String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                }
                                else if (rp.error != null)
                                {
                                    eHtml = String.Format(errorTemplate, rp.error.data);
                                }
                                else if (rp.result == null || rp.result.info == null)
                                {
                                    eHtml = String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
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
                                                resourcepluginid = id
                                            },
                                            id = 1
                                        });
                                    jData = "";
                                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                    ResourcePluginMappingList rpMapping = JSON.Deserialize<ResourcePluginMappingList>(jData);
                                    if (rpMapping == null)
                                    {
                                        eHtml = String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                    }
                                    else if (rpMapping.error != null)
                                    {
                                        eHtml = String.Format(errorTemplate, rpMapping.error.data);
                                    }
                                    else if (rpMapping.result == null)
                                    {
                                        eHtml = String.Format(errorTemplate, MessageResource.GetMessage("resource_plugin_not_found"));
                                    }
                                    else
                                    {
                                        if (rpMapping.result.Count == 0)
                                        {
                                            html += "<div class=\"no-tabs none\">Nenhum campo mapeado</div>";
                                        }
                                        else
                                        {

                                            html += "<h3>Adição de usuário (" + rp.result.info.name + ")</h3>";
                                            html += "<form id=\"form_add_resource_plugin\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/users/action/add_user/\">";
                                            html += "<div class=\"no-tabs fields\"><table><tbody>";
                                            html += "<input type=\"hidden\" id=\"resource_plugin\" name=\"resource_plugin\" value=\"" + rp.result.info.resource_plugin_id + "\" />";

                                            //Os obrigatórios primeiro

                                            foreach (ResourcePluginMapping map in rpMapping.result)
                                            {
                                                if (!map.is_id && !map.is_unique_property)
                                                    continue;

                                                String form = "";
                                                form += "<input type=\"hidden\" id=\"field_id\" name=\"field_id\" value=\"" + map.field_id + "\" />";
                                                if (map.is_password)
                                                {
                                                    form += "<input id=\"" + map.field_id + "\" name=\"" + map.field_id + "\" placeholder=\"" + map.field_name + "\" type=\"password\">";
                                                }
                                                else
                                                {
                                                    form += "<input id=\"" + map.field_id + "\" name=\"" + map.field_id + "\" placeholder=\"" + map.field_name + "\" type=\"text\">";
                                                }

                                                html += String.Format(infoTemplate, map.field_name, form + "<span class=\"description\">" + (map.is_id ? "(ID) " : (map.is_unique_property ? "(Campo único) " : "")) + "</span>");
                                            }

                                            foreach (ResourcePluginMapping map in rpMapping.result)
                                            {
                                                if (map.is_id || map.is_unique_property)
                                                    continue;

                                                String form = "";
                                                form += "<input type=\"hidden\" id=\"field_id\" name=\"field_id\" value=\"" + map.field_id + "\" />";
                                                if (map.is_password)
                                                {
                                                    form += "<input id=\"" + map.field_id + "\" name=\"" + map.field_id + "\" placeholder=\"" + map.field_name + "\" type=\"password\">";
                                                }
                                                else
                                                {
                                                    form += "<input id=\"" + map.field_id + "\" name=\"" + map.field_id + "\" placeholder=\"" + map.field_name + "\" type=\"text\">";
                                                }

                                                html += String.Format(infoTemplate, map.field_name, form + "<span class=\"description\">" + (map.is_id || map.is_unique_property ? "(obrigatório) " : "") + "</span>");
                                            }

                                            html += "</tbody></table><div class=\"clear-block\"></div></div>";
                                            html += "<button type=\"submit\" id=\"resource-plugin-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/users/new/';\">Cancelar</a></form>";
                                        }
                                    }
                                }

                                break;

                        }

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                    }
                    else
                    {
                        if (retUser == null)
                        {

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
                                html += "        <th class=\"pointer w100 tHide mHide header\" data-column=\"login\">Contexto <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ultimo Login <div class=\"icomoon\"></div></th>";
                                html += "    </tr>";
                                html += "</thead>";

                                html += "<tbody>";
                            }

                            String trTemplate = "    <tr class=\"user\" data-login=\"{1}\" data-userid=\"{0}\" data-href=\"" + ApplicationVirtualPath + "admin/users/{0}/\">";
                            trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                            trTemplate += "            <td class=\"pointer ident10\">{2}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{1}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{3}</td>";
                            trTemplate += "            <td class=\"pointer tHide mHide\">{4}</td>";
                            trTemplate += "    </tr>";

                            String query = "";
                            try
                            {

                                if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                    query = (String)RouteData.Values["query"];



                                if (String.IsNullOrWhiteSpace(query))
                                {
                                    var tmpReq = new
                                    {
                                        jsonrpc = "1.0",
                                        method = "user.list",
                                        parameters = new
                                        {
                                            page_size = pageSize,
                                            page = page,
                                            filter = new { contextid = hashData.GetValue("context"), containerid = hashData.GetValue("container") }
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
                                        method = "user.search",
                                        parameters = new
                                        {
                                            text = query,
                                            page_size = pageSize,
                                            page = page,
                                            filter = new { contextid = hashData.GetValue("context"), containerid = hashData.GetValue("container") }
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
                                        html += String.Format(trTemplate, user.userid, user.login, user.full_name, user.context_name, (user.last_login == 0 ? "Nunca foi feito o login" : ((DateTime)new DateTime(1970, 1, 1)).AddSeconds(user.last_login).ToString("yyyy-MM-dd HH:mm:ss")));

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

                            contentRet.js = "$( document ).unbind('end_of_scroll');";
                            if (hasNext)
                                contentRet.js += "$( document ).bind( 'end_of_scroll.loader_usr', function() { $( document ).unbind('end_of_scroll.loader_usr'); $('.user-list-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + ", search:'" + (!String.IsNullOrWhiteSpace(query) ? query : "") + "' }, function(){ $('.user-list-loader').addClass('hide'); } ); });";

                        }
                        else//Está selecionado o usuário, 
                        {
                            if (error != "")
                            {
                                contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                            }
                            else
                            {
                                String infoTemplate = "<div class=\"form-group\">";
                                infoTemplate += "<label>{0}</label>";
                                infoTemplate += "<span class=\"no-edit\">{1}</span></div>";

                                Int32 page = 1;
                                Int32 pageSize = 20;
                                Boolean hasNext = true;

                                Int32.TryParse(Request.Form["page"], out page);

                                if (page < 1)
                                    page = 1;

                                switch (filter)
                                {

                                    case "":
                                    case "info":
                                        Int32 rStart = ((page - 1) * pageSize) + 1;
                                        Int32 rEnd = rStart + (pageSize - 1);

                                        String sectionTemplate = "<div class=\"section " + (page > 1 ? " scroll " : "") + "\">{0}<div class=\"col1\">{1}</div><div class=\"col-middle\"></div><div class=\"col2\">{2}</div><div class=\"center-line\"></div><div class=\"clear-block\"></div></div>";
                                        String titleTemplate = "<div class=\"title\">{0}</div>";
                                        String eventTemplate = "<div class=\"item\" data-uri=\"/admin/logs/{3}/content/modal/\" data-title=\"{0}\" onclick=\"iamadmin.openLog(this);\"><div class=\"arrow\"></div><div class=\"marker\"></div><div class=\"inner\"><div class=\"ititle\">{0}</div><div class=\"date\">{1}</div><div class=\"description\">{2}</div></div></div>";

                                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                        {

                                            if (page == 1)
                                            {
                                                String userTemplate = "<div id=\"role-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" data-total=\"{2}\" class=\"app-list-item\">";
                                                userTemplate += "<table>";
                                                userTemplate += "   <tbody>";
                                                userTemplate += "       <tr>";
                                                userTemplate += "           <td class=\"col1\">";
                                                userTemplate += "               <span class=\"total \">{2}</span>";
                                                userTemplate += "               <a href=\"/admin/users/" + retUser.result.info.userid + "/identity/#\">";
                                                userTemplate += "               <div class=\"app-btn a-btn\"><span class=\"a-btn-inner\">Ver identidades</span></div>";
                                                userTemplate += "               </a>";
                                                userTemplate += "           </td>";
                                                userTemplate += "           <td class=\"col2\">";
                                                userTemplate += "               <div class=\"title\"><span class=\"name field-editor\" id=\"context_name_{0}\" data-id=\"{0}\" data-function=\"iamadmin.editTextField('#context_name_{0}',null,contextNameEdit);\">{1}</span><span class=\"date\">{3}</span><div class=\"clear-block\"></div></div>";
                                                userTemplate += "               <div class=\"description\">";
                                                userTemplate += "                   <div class=\"line\"><span class=\"info-text\">Pasta: " + retUser.result.general.container_path + "</span><br clear=\"all\"></div>";
                                                userTemplate += "                   <div class=\"line\"><span class=\"info-text\">Login: " + retUser.result.info.login + "</span><span class=\"info-text\">Último login: " + (retUser.result.info.last_login == 0 ? "Nunca foi feito o login" : MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(retUser.result.info.last_login), false)) + "</span><br clear=\"all\"></div>";
                                                userTemplate += "                   <div class=\"line\"><span class=\"info-text\">Bloqueado: " + (retUser.result.info.locked ? "Sim" : "Não") + "</span><span class=\"info-text\">Exige troca de senha no próximo logon: " + (retUser.result.info.must_change_password ? "Sim" : "Não") + "</span></div>";
                                                userTemplate += "                   <div class=\"line\">" + ((retUser.result.general != null) ? "<span class=\"info-text\">Empresa: " + retUser.result.general.enterprise_name + "</span><span class=\"info-text\">Contexto: " + retUser.result.general.context_name + "</span>" : "") + "<br clear=\"all\"></div>";

                                                if ((retUser.result.identities != null) && (retUser.result.identities.Count > 0))
                                                {
                                                    String l = "";

                                                    foreach (UserDataIdentity i in retUser.result.identities)
                                                        if (i.temp_locked)
                                                            l += "<span class=\"info-text red-text\">Bloqueio temporário no recurso " + i.resource_name + "</span>";

                                                    if (l != "")
                                                        userTemplate += "                   <div class=\"line\">" + l + "<br clear=\"all\"></div>";
                                                }

                                                userTemplate += "               </div>";
                                                userTemplate += "               <div class=\"links\">";
                                                userTemplate += "                   <div class=\"line\">";
                                                userTemplate += "                   <a href=\"" + ApplicationVirtualPath + "admin/users/" + retUser.result.info.userid + "/container/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\"><div class=\"ico icon-change\">Editar pasta</div></a>";
                                                userTemplate += "                   <a href=\"" + ApplicationVirtualPath + "admin/users/" + retUser.result.info.userid + "/property/#edit/1\"><div class=\"ico icon-change\">Editar propriedades</div></a>";
                                                userTemplate += "                   </div><div class=\"clear-block\"></div>";
                                                userTemplate += "               </div>";
                                                userTemplate += "           </td>";
                                                userTemplate += "       </tr>";
                                                userTemplate += "   </tbody>";
                                                userTemplate += "</table></div>";

                                                html += "<div class=\"box-container\">" + String.Format(userTemplate, retUser.result.info.context_id, retUser.result.info.full_name, retUser.result.info.identity_qty, (retUser.result.info.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(retUser.result.info.create_date), false) : "")) + "</div>";

                                                html += "<div class=\"timeline\" id=\"user-timeline\">";

                                                //Section

                                                DataTable dtToday = db.Select("select * from entity_timeline where entity_id = " + retUser.result.info.userid + " and date between CONVERT(varchar(10), getdate(), 120) + ' 00:00:00' and CONVERT(varchar(10), getdate(), 120) + ' 23:59:59' order by date desc");
                                                if ((dtToday != null) && (dtToday.Rows.Count > 0))
                                                {
                                                    String col1 = "";
                                                    String col2 = "";
                                                    for (Int32 r = 0; r < dtToday.Rows.Count; r++)
                                                    {
                                                        if (r % 2 == 0)
                                                            col1 += String.Format(eventTemplate, dtToday.Rows[r]["title"].ToString(), MessageResource.FormatDate((DateTime)dtToday.Rows[r]["date"], false), dtToday.Rows[r]["text"].ToString().Replace("\r\n", "<br />").Replace("\r\n", "<br />"), dtToday.Rows[r]["log_id"].ToString());
                                                        else
                                                            col2 += String.Format(eventTemplate, dtToday.Rows[r]["title"].ToString(), MessageResource.FormatDate((DateTime)dtToday.Rows[r]["date"], false), dtToday.Rows[r]["text"].ToString().Replace("\r\n", "<br />"), dtToday.Rows[r]["log_id"].ToString());

                                                    }

                                                    html += String.Format(sectionTemplate, String.Format(titleTemplate, "Eventos de hoje"), col1, col2);
                                                }

                                                DataTable dtMonth = db.Select("select * from entity_timeline where entity_id = " + retUser.result.info.userid + " and date between CONVERT(varchar(10), dateadd(month,-1,getdate()), 120) + ' 00:00:00' and CONVERT(varchar(10), dateadd(day,-1,getdate()), 120) + ' 23:59:59' order by date desc");
                                                if ((dtMonth != null) && (dtMonth.Rows.Count > 0))
                                                {
                                                    String col1 = "";
                                                    String col2 = "";
                                                    for (Int32 r = 0; r < dtMonth.Rows.Count; r++)
                                                    {
                                                        if (r % 2 == 0)
                                                            col1 += String.Format(eventTemplate, dtMonth.Rows[r]["title"].ToString(), MessageResource.FormatDate((DateTime)dtMonth.Rows[r]["date"], false), dtMonth.Rows[r]["text"].ToString().Replace("\r\n", "<br />"), dtMonth.Rows[r]["log_id"].ToString());
                                                        else
                                                            col2 += String.Format(eventTemplate, dtMonth.Rows[r]["title"].ToString(), MessageResource.FormatDate((DateTime)dtMonth.Rows[r]["date"], false), dtMonth.Rows[r]["text"].ToString().Replace("\r\n", "<br />"), dtMonth.Rows[r]["log_id"].ToString());

                                                    }

                                                    html += String.Format(sectionTemplate, String.Format(titleTemplate, "Eventos do mês"), col1, col2);
                                                }
                                            }


                                            String sql = "";
                                            sql += "WITH result_set AS (";
                                            sql += "  SELECT";
                                            sql += "    ROW_NUMBER() OVER (ORDER BY t.date desc) AS [row_number], t.*";
                                            sql += "    from entity_timeline t";
                                            sql += "  WHERE";
                                            sql += "    entity_id = " + retUser.result.info.userid + " and date < CONVERT(varchar(10), dateadd(month,-1,getdate()), 120) + ' 23:59:59'";
                                            sql += ") SELECT";
                                            sql += "  *";
                                            sql += " FROM";
                                            sql += "  result_set";
                                            sql += " WHERE";
                                            sql += "  [row_number] BETWEEN " + rStart + " AND " + rEnd;


                                            DataTable dtOld = db.Select(sql);

                                            if ((dtOld == null) || (dtOld.Rows.Count < pageSize))
                                                hasNext = false;

                                            if ((dtOld != null) && (dtOld.Rows.Count > 0))
                                            {
                                                String col1 = "";
                                                String col2 = "";
                                                for (Int32 r = 0; r < dtOld.Rows.Count; r++)
                                                {

                                                    if (r % 2 == 0)
                                                        col1 += String.Format(eventTemplate, dtOld.Rows[r]["title"].ToString(), MessageResource.FormatDate((DateTime)dtOld.Rows[r]["date"], false), dtOld.Rows[r]["text"].ToString().Replace("\r\n", "<br />"), dtOld.Rows[r]["log_id"].ToString());
                                                    else
                                                        col2 += String.Format(eventTemplate, dtOld.Rows[r]["title"].ToString(), MessageResource.FormatDate((DateTime)dtOld.Rows[r]["date"], false), dtOld.Rows[r]["text"].ToString().Replace("\r\n", "<br />"), dtOld.Rows[r]["log_id"].ToString());

                                                }

                                                if (page == 1)
                                                    html += String.Format(sectionTemplate, String.Format(titleTemplate, "Outros"), col1, col2);
                                                else
                                                    html += String.Format(sectionTemplate, "", col1, col2);
                                            }

                                            if (page == 1)
                                            {
                                                //Final criado em
                                                html += "</div>";
                                                html += "<div class=\"timeline\">";
                                                html += "<div class=\"section\"><div class=\"title big\">Criado em " + MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(retUser.result.info.create_date), false) + "</div><div class=\"center-line\"></div><div class=\"clear-block\"></div></div>";
                                                html += "</div>";
                                                html += "<span class=\"empty-results content-loading user-timeline-loader hide\"></span>";

                                                contentRet = new WebJsonResponse("#content-wrapper", html);
                                            }
                                            else
                                            {

                                                contentRet = new WebJsonResponse("#content-wrapper #user-timeline", html, true);
                                            }

                                            contentRet.js = "$( document ).unbind('end_of_scroll');";
                                            if (hasNext)
                                                contentRet.js += "$( document ).bind( 'end_of_scroll.user-timeline', function() { $( document ).unbind('end_of_scroll.user-timeline'); $('.user-timeline-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + " }, function(){ $('.user-timeline-loader').addClass('hide'); } ); });";
                                        }

                                        break;


                                    case "container":

                                        infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

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
                                        html += "<form id=\"user_change_container\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/users/" + retUser.result.info.userid + "/action/change_container/\">";
                                        html += "<div class=\"no-tabs fields\"><table><tbody>";

                                        Func<String, Int64, Int64, String> chields = null;
                                        chields = new Func<String, long, long, string>(delegate(String padding, Int64 root, Int64 ctx)
                                        {
                                            String h = "";
                                            foreach (ContainerData c in conteinerList)
                                                if ((c.parent_id == root) && (c.context_id == ctx))
                                                {
                                                    h += "<option value=\"" + c.container_id + "\" " + (retUser.result.info.container_id.ToString() == c.container_id.ToString() ? "selected" : "") + ">" + padding + " " + c.name + "</option>";
                                                    h += chields(padding + "---", c.container_id, ctx);
                                                }

                                            return h;
                                        });

                                        String select = "<select id=\"container\" name=\"container\" >";
                                        select += "<option value=\"0\">Raiz</option>";
                                        select += chields("|", 0, retUser.result.info.context_id);
                                        select += "</select>";
                                        html += String.Format(infoTemplate, "Pasta", select);
                                        
                                        html += "</tbody></table><div class=\"clear-block\"></div></div>";

                                        html += "<button type=\"submit\" id=\"user-profile-password-save\" class=\"button secondary floatleft\">Salvar</button>    <a href=\"" + ApplicationVirtualPath + "admin/users/"+ retUser.result.info.userid +"/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));

                                        break;


                                    case "property":

                                        infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

                                        if (filter == "property")
                                        {

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

                                                List<FieldData> fields = new List<FieldData>();
                                                FieldListResult fieldList = JSON.Deserialize<FieldListResult>(jData);
                                                if ((fieldList != null) && (fieldList.error == null) && (fieldList.result != null))
                                                    fields.AddRange(fieldList.result);

                                                html += "<h3>Propriedades</h3>";
                                                html += "<form id=\"user_change_property\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/users/" + retUser.result.info.userid + "/action/change_property/\">";
                                                html += "<div class=\"no-tabs fields\"><table><tbody class=\"property-fields\">";

                                                String fieldTemplate = "";
                                                String sel = "<select id=\"field_id_[id]\" name=\"field_id_[id]\">";
                                                sel += "<option value=\"\"></option>";
                                                foreach (FieldData f in fields)
                                                    sel += "<option value=\"" + f.field_id + "\">" + f.name + "</option>";
                                                sel += "</select>";
                                                fieldTemplate += String.Format(infoTemplate, "<input id=\"field_index\" name=\"field_index\" type=\"hidden\" value=\"[id]\" />" + sel, "<input id=\"field_value_[id]\" name=\"field_value_[id]\" type=\"text\"\">");

                                                if (retUser.result.properties != null)
                                                {
                                                    //foreach (UserDataProperty p in retUser.result.properties)
                                                    for (Int32 i = 0; i < retUser.result.properties.Count; i++)
                                                        if (retUser.result.properties[i].resource_name.ToLower() == "entity data")
                                                        {
                                                            sel = "<select id=\"field_id_" + i + "\" name=\"field_id_" + i + "\">";
                                                            foreach (FieldData f in fields)
                                                                sel += "<option value=\"" + f.field_id + "\" " + (retUser.result.properties[i].field_id == f.field_id ? "selected" : "") + ">" + f.name + "</option>";
                                                            sel += "</select>";
                                                            html += String.Format(infoTemplate, "<input id=\"field_index\" name=\"field_index\" type=\"hidden\" value=\"" + i + "\" />" + sel, "<input id=\"field_value_" + i + "\" name=\"field_value_" + i + "\" type=\"text\"\" value=\"" + retUser.result.properties[i].value + "\">");
                                                        }
                                                }
                                                else
                                                {
                                                    html += fieldTemplate.Replace("[id]", "0");
                                                }

                                                html += "</tbody></table>";
                                                html += "<table><tbody>";
                                                html += String.Format(infoTemplate, "", "<div class=\"a-btn blue secondary floatleft\" onclick=\"iamfnc.addProperty();\">Adicionar nova propriedade</div>");

                                                html += "</tbody></table><div class=\"clear-block\"></div></div>";
                                                html += "<button type=\"submit\" id=\"resource-plugin-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"iamadmin.changeHash( 'edit/0' );\">Cancelar</a></form><div class=\"clear-block\"></div>";
                                                html += "</form>";

                                                js = "iamfnc.addProperty = function(){ var id = new Date().getTime(); $('.property-fields').append('" + fieldTemplate + "'.replace(/\\[id\\]/g,id)); }";
                                            }
                                            else
                                            {
                                                html += "<h3>Propriedades";
                                                if (hashData.GetValue("edit") != "1")
                                                    html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div>";
                                                html += "</h3>";
                                                html += "<div class=\"no-tabs fields\"><table><tbody>";

                                                if (retUser.result.properties != null)
                                                {
                                                    foreach (UserDataProperty p in retUser.result.properties)
                                                        if (p.resource_name.ToLower() == "entity data")
                                                            html += String.Format(infoTemplate, "[" + p.resource_name + "] " + p.name, p.value);

                                                    foreach (UserDataProperty p in retUser.result.properties)
                                                        if (p.resource_name.ToLower() != "entity data")
                                                            html += String.Format(infoTemplate, "[" + p.resource_name + "] " + p.name, p.value);
                                                }
                                                else
                                                {
                                                    html += String.Format(infoTemplate, "Nenhuma propriedade", "");
                                                }
                                                html += "</tbody></table><div class=\"clear-block\"></div></div>";
                                            }


                                        }

                                        contentRet = new WebJsonResponse("#content-wrapper", html);
                                        contentRet.js = js;
                                        break;

                                    case "roles":
                                        html += "<h3>Perfis</h3>";
                                        html += "<div class=\"no-tabs pb10\">";
                                        if (retUser.result.roles != null)
                                        {
                                            foreach (UserDataRole r in retUser.result.roles)
                                                html += String.Format(infoTemplate, r.name, r.resource_name);
                                        }
                                        else
                                        {
                                            html += String.Format(infoTemplate, "Nenhum perfil", "");
                                        }
                                        html += "<div class=\"clear-block\"></div></div>";

                                        contentRet = new WebJsonResponse("#content-wrapper", html);
                                        break;

                                    case "flow":
                                        if (error != "")
                                        {
                                            contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                                        }
                                        else
                                        {

                                            String js2 = "";
                                            if (filter == "" || filter == "flow")
                                            {
                                                html += "<h3>Fluxo</h3>";
                                                html += "<div id=\"userChart\"></div>";
                                                js2 = "$('#userChart').flowchart({load_uri: '" + ApplicationVirtualPath + "admin/chartdata/flow/user/" + retUser.result.info.userid + "/'});";
                                            }

                                            contentRet = new WebJsonResponse("#content-wrapper", html);
                                            contentRet.js = js2;
                                        }
                                        break;

                                    case "logs":
                                        if (error != "")
                                        {
                                            contentRet = new WebJsonResponse("#content-wrapper", String.Format(errorTemplate, error));
                                        }
                                        else
                                        {

                                            if (page == 1)
                                            {
                                                html += "<table class=\"sorter\"><thead>";
                                                html += "    <tr>";
                                                html += "        <th class=\"w50 tHide mHide {sorter: false}\"></th>";
                                                html += "        <th class=\"pointer w150 tHide mHide header headerSortDown\" data-column=\"date\">Data <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer w80 tHide mHide header\" data-column=\"source\">Fonte <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"pointer w100 tHide mHide header\" data-column=\"resource\">Recurso <div class=\"icomoon\"></div></th>";
                                                html += "        <th class=\"{sorter: false} header\" data-column=\"text\">Texto <div class=\"icomoon\"></div></th>";
                                                html += "    </tr>";
                                                html += "</thead>";

                                                html += "<tbody>";
                                            }

                                            String trTemplate = "    <tr class=\"user-log\" data-uri=\"/admin/logs/{0}/content/modal/\" data-title=\"{5}\" onclick=\"iamadmin.openLog(this);\">";
                                            trTemplate += "            <td class=\"select tHide mHide\"><div class=\"level-icon level-{1}\"></div></td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{2}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                                            trTemplate += "            <td class=\"tHide mHide\">{4}</td>";
                                            trTemplate += "            <td class=\"ident10\">{5}</td>";
                                            trTemplate += "    </tr>";

                                            try
                                            {

                                                String query = "";

                                                if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                                    query = (String)RouteData.Values["query"];

                                                var tmpReq = new
                                                {
                                                    jsonrpc = "1.0",
                                                    method = "user.logs",
                                                    parameters = new
                                                    {
                                                        userid = id,
                                                        page_size = 20,
                                                        page = page,
                                                        filter = new { source = hashData.GetValue("source") }
                                                    },
                                                    id = 1
                                                };

                                                rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                                                jData = "";
                                                using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                                    jData = WebPageAPI.ExecuteLocal(database, this, rData);

                                                if (String.IsNullOrWhiteSpace(jData))
                                                    throw new Exception("");

                                                Logs ret2 = JSON.Deserialize<Logs>(jData);
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
                                                else if (ret2.result == null || ret2.result.info == null)
                                                {
                                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                                                    hasNext = false;
                                                }
                                                else
                                                {
                                                    foreach (LogItem l in ret2.result.logs)
                                                        html += String.Format(trTemplate, l.log_id, l.level.ToString().ToLower(), MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(l.date), false), l.source, l.resource_name, l.text);

                                                    if (ret2.result.logs.Count < tmpReq.parameters.page_size)
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

                                                html += "<span class=\"empty-results content-loading user-logs-loader hide\"></span>";

                                                contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                                            }
                                            else
                                            {
                                                contentRet = new WebJsonResponse("#content-wrapper tbody", (eHtml != "" ? eHtml : html), true);
                                            }

                                            contentRet.js = "$( document ).unbind('end_of_scroll');";
                                            if (hasNext)
                                                contentRet.js += "$( document ).bind( 'end_of_scroll.loader1', function() { $( document ).unbind('end_of_scroll.loader1'); $('.user-logs-loader').removeClass('hide'); iamadmin.getPageContent2( { page: " + ++page + " }, function(){ $('.user-logs-loader').addClass('hide'); } ); });";

                                        }
                                        break;

                                    case "identity":

                                        if (page == 1)
                                        {
                                            html += "<table id=\"users-table\" class=\"sorter\"><thead>";
                                            html += "    <tr>";
                                            html += "        <th class=\"w50 mHide {sorter: false}\"><div class=\"select-all\"></div></th>";
                                            html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Identidade <div class=\"icomoon\"></div></th>";
                                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Data da criação <div class=\"icomoon\"></div></th>";
                                            html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Bloqueio temporário <div class=\"icomoon\"></div></th>";
                                            html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                            html += "    </tr>";
                                            html += "</thead>";

                                            html += "<tbody>";
                                        }

                                        String trTemplate2 = "    <tr class=\"user-identity\" data-identity-id=\"{0}\">";
                                        trTemplate2 += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                                        trTemplate2 += "            <td class=\"ident10\">{1}</td>";
                                        trTemplate2 += "            <td class=\"tHide mHide\">{2}</td>";
                                        trTemplate2 += "            <td class=\"tHide mHide\">{3}</td>";
                                        trTemplate2 += "            <td class=\"tHide mHide\">{4}</td>";
                                        trTemplate2 += "    </tr>";

                                        if ((retUser.result.identities == null) || (retUser.result.identities.Count == 0))
                                        {
                                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("identity_not_found"));
                                        }
                                        else
                                        {
                                            foreach (UserDataIdentity identity in retUser.result.identities)
                                            {
                                                String actions = "";

                                                if (identity.temp_locked)
                                                    actions += "<div data-action=\"" + ApplicationVirtualPath + "admin/users/" + retUser.result.info.userid + "/action/unlock_identity/" + identity.identity_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn data-action\">Desbloquear</div>&nbsp;";

                                                actions += "<button href=\"" + ApplicationVirtualPath + "admin/users/" + retUser.result.info.userid + "/action/delete_identity/" + identity.identity_id + "/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente a identidade '" + identity.resource_name + "' do usuário '" + retUser.result.info.full_name + "'?\" ok=\"Excluir\" cancel=\"Cancelar\">Excluir</button>";
                                                html += String.Format(trTemplate2, identity.identity_id, identity.resource_name, MessageResource.FormatDate(((DateTime)new DateTime(1970, 1, 1)).AddSeconds(identity.create_date), false), (identity.temp_locked ? "Sim" : "Não"), actions);
                                            }

                                            if (retUser.result.identities.Count < pageSize)
                                                hasNext = false;
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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/users/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo usuário</button></div>";

                    if (retUser != null)
                    {
                        html += "<ul class=\"user-profile\">";
                        html += "<li " + (filter == "" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/\">Informações gerais</a></span></li>";
                        html += "<li " + (filter == "identity" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/identity\">Identidades</a></span></li>";
                        html += "<li " + (filter == "property" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/property\">Propriedades</a></span></li>";
                        html += "<li " + (filter == "roles" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/roles\">Perfis</a></span></li>";
                        html += "<li " + (filter == "flow" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/flow\">Fluxo</a></span></li>";
                        html += "<li " + (filter == "logs" ? "class=\"bold\"" : "") + "><span><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/logs\">Logs</a></span></li>";
                        html += "</ul>";
                    }


                    contentRet = new WebJsonResponse("#main aside", html);
                    break;

                case "mobilebar":
                    if (retUser != null)
                    {
                        html += "<div class=\"mobile-button-bar-wrapper mOnly\"><ul class=\"mobile-button-bar w20 \">";
                        html += "<li id=\"user-profile-general-mobile\" " + (filter == "" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/\">Informações gerais</a></li>";
                        html += "<li id=\"user-profile-general-mobile\" " + (filter == "property" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/property\">Propriedades</a></li>";
                        html += "<li id=\"user-profile-general-mobile\" " + (filter == "roles" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/roles\">Perfis</a></li>";
                        html += "<li id=\"user-profile-general-mobile\" " + (filter == "flow" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/flow\">Fluxo</a></li>";
                        html += "<li id=\"user-profile-general-mobile\" " + (filter == "logs" ? "class=\"on\"" : "") + "><a href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/logs\">Logs</a></li>";
                        html += "</ul>";
                        html += "<div class=\"clear-block\"></div></div>";
                    }

                    contentRet = new WebJsonResponse("#titlebar #mobilebar", html);
                    break;


                case "buttonbox":
                    if (retUser != null)
                    {

                        html += "    <ul>";
                        html += "        <li class=\"btn mOnly\" onclick=\"history.back();\"><div class=\"content\"><div class=\"ico icon-arrow-left\"></div><div class=\"text\">Voltar</div></div></li>";

                        if ((retUser.result.info.locked))
                            html += "        <li class=\"btn\"><a class=\"user-lock\" href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/action/unlock/\" confirm-title=\"Desbloqueio\" confirm-text=\"Deseja realizar o desbloqueio do usuário '" + retUser.result.info.login + "'?\" ok=\"Desbloquear\" cancel=\"Cancelar\"><div class=\"content\"><div class=\"ico icon-unlocked\"></div><div class=\"text\">Desbloquear</div></div></a></li>";
                        else
                            html += "        <li class=\"btn\"><a class=\"user-lock\" href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/action/lock/\" confirm-title=\"Bloqueio\" confirm-text=\"Deseja realizar o bloqueio do usuário '" + retUser.result.info.login + "'?\" ok=\"Bloquear\" cancel=\"Cancelar\"><div class=\"content\"><div class=\"ico icon-lock\"></div><div class=\"text\">Bloquear</div></div></a></li>";

                        //html += "        <li class=\"btn\"><a class=\"user-logs\" href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/logs/\"><div class=\"content\"><div class=\"ico icon-drawer\"></div><div class=\"text\">Logs</div></div></a></li>";
                        html += "        <li class=\"btn\"><a class=\"user-resetpwd\" href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/action/resetpwd/\" confirm-title=\"Redefinição de senha\" confirm-text=\"Deseja realizar a redefinição da senha do usuário '" + retUser.result.info.login + "'?\" ok=\"Redefinir\" cancel=\"Cancelar\"><div class=\"content\"><div class=\"ico icon-loop\"></div><div class=\"text\">Senha</div></div></a></li>";
                        html += "        <li class=\"btn\"><a class=\"user-deploy\" href=\"" + ApplicationVirtualPath + "admin/users/" + id + "/action/deploy/\" confirm-title=\"Publicação dos dados\" confirm-text=\"Deseja realizar a publicação dos dados do usuário '" + retUser.result.info.login + "'?\" ok=\"Publicar\" cancel=\"Cancelar\"><div class=\"content\"><div class=\"ico icon-upload\"></div><div class=\"text\">Publicar</div></div></a></li>";
                        html += "    </ul>";

                        switch (filter)
                        {

                            case "logs":
                                html += "<select id=\"filter_source\" name=\"filter_source\" ><option value=\"\">Todas as origens</option>";
                                html += "<option value=\"source/adminapi\" " + (hashData.GetValue("source") == "adminapi" ? "selected" : "") + ">adminapi</option>";
                                html += "<option value=\"source/api\" " + (hashData.GetValue("source") == "api" ? "selected" : "") + ">api</option>";
                                html += "<option value=\"source/autoservice\" " + (hashData.GetValue("source") == "autoservice" ? "selected" : "") + ">autoservice</option>";
                                html += "<option value=\"source/cas\" " + (hashData.GetValue("source") == "cas" ? "selected" : "") + ">cas</option>";
                                html += "<option value=\"source/deploy\" " + (hashData.GetValue("source") == "deploy" ? "selected" : "") + ">deploy</option>";
                                html += "<option value=\"source/engine\" " + (hashData.GetValue("source") == "engine" ? "selected" : "") + ">engine</option>";
                                html += "<option value=\"source/inbound\" " + (hashData.GetValue("source") == "inbound" ? "selected" : "") + ">inbound</option>";
                                html += "<option value=\"source/proxy\" " + (hashData.GetValue("source") == "proxy" ? "selected" : "") + ">proxy</option>";
                                html += "</select>";

                                js = "$('#filter_source').change(function() { iamadmin.changeHash( $( this ).val() ); });";
                                break;
                        }


                    }
                    else
                    {

                        try
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

                                html += "<select id=\"filter_context\" name=\"filter_context\" ><option value=\"\">Todos os contextos</option>";
                                foreach (ContextData c in contextList.result)
                                    html += "<option value=\"context/" + c.context_id + "\" " + (hashData.GetValue("context") == c.context_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                html += "</select>";

                                js = "$('#filter_context').change(function() { iamadmin.changeHash( $( this ).val() ); });";

                            }

                            rData = SafeTrend.Json.JSON.Serialize2(new
                            {
                                jsonrpc = "1.0",
                                method = "container.list",
                                parameters = new { },
                                id = 1
                            });

                            jData = "";
                            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                                jData = WebPageAPI.ExecuteLocal(database, this, rData);

                            if (String.IsNullOrWhiteSpace(jData))
                                throw new Exception("");

                            ContainerListResult containerList = JSON.Deserialize<ContainerListResult>(jData);
                            if ((containerList != null) && (containerList.error == null) && (containerList.result != null))
                            {

                                Func<String, Int64, String> chields = null;
                                chields = new Func<String, long, string>(delegate(String padding, Int64 root)
                                 {
                                     String h = "";
                                     foreach (ContainerData c in containerList.result)
                                         if (c.parent_id == root)
                                         {
                                             h += "<option value=\"container/" + c.container_id + "\" " + (hashData.GetValue("container") == c.container_id.ToString() ? "selected" : "") + ">" + padding + " " + c.name + "</option>";
                                             h += chields(padding + "---", c.container_id);
                                         }

                                     return h;
                                 });

                                html += "<select id=\"filter_container\" name=\"filter_container\" ><option value=\"\">Todos as pastas</option>";
                                html += chields("|", 0);

                                /*
                                foreach (ContainerData c in containerList.result)
                                {
                                    html += "<option value=\"container/" + c.container_id + "\" " + (hashData.GetValue("container") == c.container_id.ToString() ? "selected" : "") + ">" + c.name + "</option>";
                                }*/
                                html += "</select>";

                                js += "$('#filter_container').change(function() { iamadmin.changeHash( $( this ).val() ); });";

                            }


                        }
                        catch (Exception ex)
                        {
                            error = MessageResource.GetMessage("api_error");
                        }
                    }

                    contentRet = new WebJsonResponse("#btnbox", html);
                    contentRet.js = js;
                    break;


                case "search_user":

                    List<AutoCompleteItem> users = new List<AutoCompleteItem>();

                    String userHtmlTemplate = "<div id=\"role-role-list-{0}\" data-id=\"{0}\" data-name=\"{1}\" class=\"app-list-item\">";
                    userHtmlTemplate += "<input type=\"hidden\" name=\"user_id\" value=\"{0}\">";
                    userHtmlTemplate += "<table>";
                    userHtmlTemplate += "   <tbody>";
                    userHtmlTemplate += "       <tr>";
                    userHtmlTemplate += "           <td class=\"colfull\">";
                    userHtmlTemplate += "               <div class=\"title\"><span class=\"name\" id=\"role_name_{0}\" data-id=\"{0}\">{1}</span><span class=\"date\">{2}</span><div class=\"clear-block\"></div></div>";
                    userHtmlTemplate += "               <div class=\"description\">{3}</div></div>";
                    userHtmlTemplate += "               <div class=\"links small\">";
                    userHtmlTemplate += "                   <div class=\"last\"><div class=\"ico icon-close\" onclick=\"$('#role-role-list-{0}').remove();\">Excluir usuário</div></a><div class=\"clear-block\"></div></div>";
                    userHtmlTemplate += "               </div>";
                    userHtmlTemplate += "           </td>";
                    userHtmlTemplate += "       </tr>";
                    userHtmlTemplate += "   </tbody>";
                    userHtmlTemplate += "</table></div>";

                    String infoTemplate2 = "<div class=\"line\">";
                    infoTemplate2 += "<label>{0}</label>";
                    infoTemplate2 += "<span class=\"no-edit\">{1}</span></div>";

                    try
                    {

                        rData = SafeTrend.Json.JSON.Serialize2(new
                        {
                            jsonrpc = "1.0",
                            method = "user.search",
                            parameters = new
                            {
                                page_size = 20,
                                page = 1,
                                text = Request.Form["text"],
                                filter = new { contextid = Request.Form["context_id"] }
                            },
                            id = 1
                        });

                        jData = "";
                        using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            jData = WebPageAPI.ExecuteLocal(database, this, rData);

                        if (String.IsNullOrWhiteSpace(jData))
                            throw new Exception("");

                        SearchResult ret2 = JSON.Deserialize<SearchResult>(jData);
                        if (ret2 == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                        }
                        else if (ret2.error != null)
                        {
                            eHtml += String.Format(errorTemplate, ret2.error.data);
                        }
                        else if (ret2.result == null)
                        {
                            //eHtml += String.Format(errorTemplate, MessageResource.GetMessage("user_not_found"));
                        }
                        else
                        {
                            foreach (UserData user in ret2.result)
                            {
                                String desc = "";

                                desc += String.Format(infoTemplate2, "Login", user.login);
                                desc += String.Format(infoTemplate2, "Bloqueado", (user.locked ? "Sim" : "Não"));
                                desc += String.Format(infoTemplate2, "Último login", (user.last_login == 0 ? "Nunca foi feito o login" : ((DateTime)new DateTime(1970, 1, 1)).AddSeconds(user.last_login).ToString("yyyy-MM-dd HH:mm:ss")));
                                desc += String.Format(infoTemplate2, "Exige troca de senha no próximo logon", (user.must_change_password ? "Sim" : "Não"));

                                String tHtml = String.Format(userHtmlTemplate, user.userid, user.full_name, (user.create_date > 0 ? "Criado em " + MessageResource.FormatDate(new DateTime(1970, 1, 1).AddSeconds(user.create_date), true) : ""), desc);
                                users.Add(new AutoCompleteItem(user.userid, "(" + user.login + ") " + user.full_name, tHtml));
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                    }

                    if (users.Count == 0)
                        users.Add(new AutoCompleteItem(0, MessageResource.GetMessage("user_not_found"), ""));

                    Retorno.Controls.Add(new LiteralControl(JSON.Serialize<List<AutoCompleteItem>>(users)));

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