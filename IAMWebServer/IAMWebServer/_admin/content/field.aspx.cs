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
    public partial class field : System.Web.UI.Page
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
            LMenu menu2 = new LMenu("Campos", ApplicationVirtualPath + "admin/field/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));
            LMenu menu3 = new LMenu("Campos", ApplicationVirtualPath + "admin/field/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : ""));

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            //Verifica se está sendo selecionada uma role
            Int64 fieldId = 0;
            try
            {
                fieldId = Int64.Parse((String)RouteData.Values["id"]);

                if (fieldId < 0)
                    fieldId = 0;
            }
            catch { }

            String error = "";
            FieldGetResult retField = null;
            String filter = "";
            HashData hashData = new HashData(this);


            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["filter"]))
                filter = (String)RouteData.Values["filter"];

            if ((fieldId > 0) && (area.ToLower() != "search"))
            {

                
                try
                {

                    var tmpReq = new
                    {
                        jsonrpc = "1.0",
                        method = "field.get",
                        parameters = new
                        {
                            fieldid = fieldId
                        },
                        id = 1
                    };

                    String rData = SafeTrend.Json.JSON.Serialize2(tmpReq);
                    
                    String jData = "";
                    using(IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        jData = WebPageAPI.ExecuteLocal(database, this, rData);

                    if (String.IsNullOrWhiteSpace(jData))
                        throw new Exception("");

                    retField = JSON.Deserialize<FieldGetResult>(jData);
                    if (retField == null)
                    {
                        error = MessageResource.GetMessage("field_not_found");
                        //ret = new WebJsonResponse("", MessageResource.GetMessage("user_not_found"), 3000, true);
                    }
                    else if (retField.error != null)
                    {
                        error = retField.error.data;
                        retField = null;
                    }
                    else if (retField.result == null || retField.result.info == null)
                    {
                        error = MessageResource.GetMessage("field_not_found");
                        retField = null;
                    }
                    else
                    {
                        menu3.Name = retField.result.info.name;
                    }

                }
                catch (Exception ex)
                {
                    error = MessageResource.GetMessage("api_error");
                    Tools.Tool.notifyException(ex, this);
                    retField = null;
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

                        html = "<h3>Adição de campo</h3>";
                        html += "<form id=\"form_add_resource\" method=\"post\" action=\"" + ApplicationVirtualPath + "admin/field/action/add_field/\"><div class=\"no-tabs pb10\">";
                        html += "<div class=\"form-group\"><label>Nome</label><input id=\"field_name\" name=\"field_name\" placeholder=\"Digite o nome do campo\" type=\"text\"\"></div>";

                        html += "<div class=\"form-group\"><label>Tipo de dado</label><select id=\"data_type\" name=\"data_type\" >";
                        html += "<option value=\"string\">Texto</option>";
                        html += "<option value=\"numeric\">Número</option>";
                        html += "<option value=\"datetime\">Data e Hora</option>";
                        html += "</select></div>";

                        html += "<div class=\"form-group\"><label>Público</label><input id=\"public\" name=\"public\" type=\"checkbox\"><span class=\"checkbox-label\">Permite visualização por todos os usuários</span></div>";
                        html += "<div class=\"form-group\"><label>Permite edição</label><input id=\"user\" name=\"user\" type=\"checkbox\"><span class=\"checkbox-label\">Permite que o usuário altere o valor</span></div>";
                        html += "<div class=\"clear-block\"></div></div>";
                        html += "<button type=\"submit\" id=\"field-save\" class=\"button secondary floatleft\">Adicionar</button>    <a href=\"" + ApplicationVirtualPath + "admin/field/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        
                    }
                    else
                    {
                        if (retField == null)
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
                                html += "        <th class=\"pointer header headerSortDown\" data-column=\"name\">Campo <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Público <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Permite edição <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer tHide mHide header\" data-column=\"login\">Tipo de dado <div class=\"icomoon\"></div></th>";
                                html += "        <th class=\"pointer w200 tHide mHide header\" data-column=\"last_login\">Ações <div class=\"icomoon\"></div></th>";
                                html += "    </tr>";
                                html += "</thead>";

                                html += "<tbody>";
                            }

                            String trTemplate = "    <tr class=\"user\" data-id=\"{0}\">";
                            trTemplate += "            <td class=\"select mHide\"><div class=\"checkbox\"></div></td>";
                            trTemplate += "            <td class=\"ident10\">{1}</td>";
                            trTemplate += "            <td class=\"tHide mHide\">{2}</td>";
                            trTemplate += "            <td class=\"tHide mHide\">{3}</td>";
                            trTemplate += "            <td class=\"tHide mHide\">{4}</td>";
                            trTemplate += "            <td class=\"tHide mHide\"><button class=\"a-btn\" onclick=\"window.location = '" + ApplicationVirtualPath + "admin/field/{0}/';\">Abrir</button> <button href=\"" + ApplicationVirtualPath + "admin/field/{0}/action/delete/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"a-btn confirm-action\" confirm-title=\"Exclusão\" confirm-text=\"Deseja excluir definitivamente do campo '{1}'?\" ok=\"Excluir\" cancel=\"Cancelar\">Excluir</button></td>";
                            trTemplate += "    </tr>";

                            try
                            {

                                String rData = "";
                                String query = "";

                                if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                    query = (String)RouteData.Values["query"];

                                if (String.IsNullOrWhiteSpace(query) && !String.IsNullOrWhiteSpace(hashData.GetValue("query")))
                                    query = hashData.GetValue("query");

                                if (String.IsNullOrWhiteSpace(query))
                                {
                                    var tmpReq = new
                                    {
                                        jsonrpc = "1.0",
                                        method = "field.list",
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
                                        method = "field.search",
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

                                FieldListResult ret2 = JSON.Deserialize<FieldListResult>(jData);
                                if (ret2 == null)
                                {
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("field_not_found"));
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
                                    eHtml += String.Format(errorTemplate, MessageResource.GetMessage("field_not_found"));
                                    hasNext = false;
                                }
                                else
                                {
                                    foreach (FieldData field in ret2.result)
                                        html += String.Format(trTemplate, field.field_id, field.name, (field.public_field ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")), (field.user_field ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")), MessageResource.GetMessage(field.data_type.ToLower()));

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

                                            html += "<form  id=\"form_resource_change\"  method=\"POST\" action=\"" + ApplicationVirtualPath + "admin/field/" + retField.result.info.field_id + "/action/change/\">";
                                            html += "<h3>Edição de campo</h3>";
                                            html += "<div class=\"no-tabs pb10\">";
                                            html += String.Format(infoTemplate, "Nome", "<input id=\"name\" name=\"name\" placeholder=\"Digite o nome do recurso\" type=\"text\"\" value=\"" + retField.result.info.name + "\">");
                                            html += String.Format(infoTemplate, "Tipo de campo", "<select id=\"data_type\" name=\"data_type\" ><option value=\"string\" " + (retField.result.info.data_type == "string" ? "selected" : "") + ">Texto</option><option value=\"numeric\" " + (retField.result.info.data_type == "numeric" ? "selected" : "") + ">Número</option><option value=\"datetime\" " + (retField.result.info.data_type == "datetime" ? "selected" : "") + ">Data e Hora</option></select>");
                                            html += String.Format(infoTemplate, "Público", "<input id=\"public\" name=\"public\" type=\"checkbox\" " + (retField.result.info.public_field? "checked":"")+"><span class=\"checkbox-label\">Permite visualização por todos os usuários</span>");
                                            html += String.Format(infoTemplate, "Permite edição", "<input id=\"user\" name=\"user\" type=\"checkbox\" " + (retField.result.info.user_field ? "checked" : "") + "><span class=\"checkbox-label\">Permite que o usuário altere o valor</span>");
                                            html += "<div class=\"clear-block\"></div></div>";

                                        }
                                        else
                                        {
                                                                                       
                                            html += "<h3>Informações gerais<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"iamadmin.changeHash( 'edit/1' );\">Editar</div></div></h3>";
                                            html += "<div class=\"no-tabs pb10\">";

                                            html += String.Format(infoTemplate, "Nome", retField.result.info.name);
                                            html += String.Format(infoTemplate, "Tipo de campo", MessageResource.GetMessage(retField.result.info.data_type.ToLower()));
                                            html += String.Format(infoTemplate, "Público", (retField.result.info.public_field ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")));
                                            html += String.Format(infoTemplate, "Permite edição", (retField.result.info.user_field ? MessageResource.GetMessage("yes") : MessageResource.GetMessage("no")));

                                            html += "<div class=\"clear-block\"></div></div>";
                                        }

                                        if (hashData.GetValue("edit") == "1")
                                            html += "<button type=\"submit\" id=\"resource-save\" class=\"button secondary floatleft\">Salvar</button>    <a class=\"button link floatleft\" onclick=\"iamadmin.changeHash( 'edit/0' );\">Cancelar</a></form>";

                                        contentRet = new WebJsonResponse("#content-wrapper", html);
                                        contentRet.js = jsAdd;
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
                        html += "<div class=\"sep\"><button class=\"a-btn-big a-btn\" type=\"button\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/field/new/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "'\">Novo campo</button></div>";

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