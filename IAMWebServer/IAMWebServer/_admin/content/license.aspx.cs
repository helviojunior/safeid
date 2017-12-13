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
using SafeTrend.Data;
using System.Globalization;
using System.Threading;
using IAM.License;

namespace IAMWebServer._admin.content
{
    public partial class license : System.Web.UI.Page
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

            LMenu menu1 = null;
            LMenu menu2 = null;
            LMenu menu3 = null;

            WebJsonResponse contentRet = null;
            HashData hashData = new HashData(this);

            String html = "";
            String eHtml = "";
            String js = null;

            

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";
            String infoTemplate = "<tr><td class=\"col1\">{0}</td><td class=\"col2\"><span class=\"no-edit\">{1}</span></td></tr>";

            switch (area)
            {
                case "":
                case "content":

                    if (newItem)
                    {


                        html += "<div id=\"upload-box\" class=\"upload-box\"><input type=\"file\" name=\"files[]\" multiple=\"\"><div class=\"drag-content\"><span class=\"upload-button-text\">Selecione arquivos para enviar</span><span class=\"upload-drag-drop-description\">Ou arraste e solte aqui</span></div><div class=\"dragDrop-content\"><span class=\"label l1\">Arraste a licença até aqui</span><span class=\"label l2\">Solte a licença</span></div></div>";

                        html += "<h3>Informações da licença</h3>";
                        html += "<form  id=\"form_plugin_add\"  method=\"POST\" action=\"" + ApplicationVirtualPath + "admin/license/action/add_new/\">";
                        html += "<div id=\"files\" class=\"box-container\"><div class=\"no-tabs pb10 none\">Nenhum upload realizado</div></div>";

                        html += "<button type=\"submit\" id=\"upload-save\" class=\"button secondary floatleft\">Aplicar licença</button>    <a href=\"" + ApplicationVirtualPath + "admin/license/" + (Request.Form["hashtag"] != null ? "#" + Request.Form["hashtag"].ToString() : "") + "\" class=\"button link floatleft\">Cancelar</a></form>";
                        html += "</form>";

                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                        contentRet.js = "iamadmin.licenseUploader($('#upload-box'), '" + ApplicationVirtualPath + "admin/license/action/upload/','" + ApplicationVirtualPath + "admin/license/action/upload_item_template/')";

                    }
                    else
                    {

                        html += "<h3>" + MessageResource.GetMessage("licensing");

                        if (hashData.GetValue("edit") != "1")
                            html += "<div class=\"btn-box\"><div class=\"a-btn ico icon-change\" onclick=\"window.location='" + ApplicationVirtualPath + "admin/license/new/'\">Carregar nova licença</div></div>";

                        html += "</h3>";

                        LicenseControl lic = null;

                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            lic = LicenseChecker.GetLicenseData(db.Connection, null, enterpriseId);

                        //Carrega o certificado licenciado
                        if (lic != null)
                        {

                            html += "<div class=\"no-tabs fields\"><table><tbody>";

                            html += String.Format(infoTemplate, "Chave de instalação", lic.InstallationKey);
                            html += String.Format(infoTemplate, "Status da licença", (lic.Valid ? "Válida" : lic.Error));

                            html += "</tbody></table><div class=\"clear-block\"></div></div>";
                        }
                        else
                        {
                            eHtml += String.Format(errorTemplate, "Falha ao carregar a licença");
                        }
                        contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
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


                    DbParameterCollection par = new DbParameterCollection();;
                    par.Add("@enterpriseId", typeof(Int64)).Value = enterpriseId;

                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {

                        DataTable dtUsers = db.ExecuteDataTable("sp_user_statistics", System.Data.CommandType.StoredProcedure, par);
                        if ((dtUsers != null) && (dtUsers.Rows.Count > 0))
                        {
                            Int64 total = (Int64)dtUsers.Rows[0]["total"];
                            Int64 locked = (Int64)dtUsers.Rows[0]["locked"];
                            Int64 logged = (Int64)dtUsers.Rows[0]["logged"];

                            Int32 pLocked = 0;
                            Int32 pLogged = 0;

                            try
                            {
                                pLocked = (Int32)(((Double)locked / (Double)total) * 100F);
                            }
                            catch { }

                            try
                            {
                                pLogged = (Int32)(((Double)logged / (Double)total) * 100F);
                            }
                            catch { }

                            html += "<div class=\"ds1\">";
                            html += "<div class=\"center\">" + MessageResource.GetMessage("entity") + "</div>";
                            html += "<div class=\"center\"><span class=\"big\">" + total + "</span><span class=\"small\"> " + MessageResource.GetMessage("total") + "</span></div>";

                            html += "<div class=\"center\"><canvas id=\"usrLockChart\" width=\"30\" height=\"30\"></canvas><span class=\"big txt1\">" + locked + "<span> <span class=\"small\">" + MessageResource.GetMessage("locked") + "<span></div>";
                            html += "<div class=\"center\"><canvas id=\"usrLoggedChart\" width=\"30\" height=\"30\"></canvas><span class=\"big txt1\">" + logged + "<span> <span class=\"small\">" + MessageResource.GetMessage("logged") + "<span></div>";
                            //html += "<div class=\"center\"><canvas id=\"usrTotalChart\" width=\"30\" height=\"30\"></canvas><span class=\"big txt1\">" + total + "<span> <span class=\"small\">Total<span></div>";

                            html += "</div>";


                            //js += "iamadmin.buildPercentChart('#usrTotalChart',100,{color:'#2d88b4',showText:false});";
                            js += "iamadmin.buildPercentChart('#usrLockChart'," + pLocked + ",{color:'#f5663a',showText:false});";
                            js += "iamadmin.buildPercentChart('#usrLoggedChart'," + pLogged + ",{color:'#76c558',showText:false});";
                        }

                        html += "<div class=\"ds2\">";
                        html += "<div class=\"center\">" + MessageResource.GetMessage("licensing") + "</div>";


                        try
                        {

                            String rData = "";
                            String query = "";

                            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["query"]))
                                query = (String)RouteData.Values["query"];

                            var tmpReq = new
                            {
                                jsonrpc = "1.0",
                                method = "license.info",
                                parameters = new String[0],
                                id = 1
                            };

                            rData = SafeTrend.Json.JSON.Serialize2(tmpReq);

                            String jData = "";
                            try
                            {
                                jData = WebPageAPI.ExecuteLocal(db, this, rData);
                            }
                            finally
                            {

                            }

                            if (String.IsNullOrWhiteSpace(jData))
                                throw new Exception("");

                            License ret2 = JSON.Deserialize<License>(jData);
                            if (ret2 == null)
                            {
                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            }
                            else if (ret2.error != null)
                            {
                                eHtml += String.Format(errorTemplate, ret2.error.data);
                            }
                            else if (ret2.result == null)
                            {
                                eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            }
                            else
                            {

                                Int32 percent = 0;
                                if (ret2.result.hasLicense)
                                {
                                    if (ret2.result.available == 0)//Licença ilimitada
                                    {
                                        percent = 0;
                                    }
                                    else if (ret2.result.used > ret2.result.available)
                                    {
                                        percent = 100;
                                    }
                                    else
                                    {
                                        percent = (ret2.result.used / ret2.result.available) * 100;
                                    }
                                }
                                else
                                {
                                    percent = 100;
                                }


                                String color = "#76c558";
                                if (percent < 70)
                                {
                                    color = "#76c558";
                                }
                                else if (percent < 85)
                                {
                                    color = "#f5663a";
                                }
                                else
                                {
                                    color = "rgb(202, 52, 56)";
                                }

                                js += "iamadmin.buildPercentChart('#licChart'," + percent + ",{color:'" + color + "',showText:true});";

                                html += "<canvas id=\"licChart\" width=\"100\" height=\"100\" class=\"big-center\"></canvas>";
                                if (ret2.result.hasLicense)
                                {
                                    html += "<div class=\"center\">" + MessageResource.GetMessage("licensing_total") + "</div>";
                                    html += "<div class=\"center\"><span class=\"big\">" + ret2.result.used + "</span><span class=\"small\"> " + MessageResource.GetMessage("of") + " " + (ret2.result.available == 0 ? MessageResource.GetMessage("unlimited") : ret2.result.available.ToString()) + "</span></div>";
                                }
                                else
                                    html += "<div class=\"center\"><span class=\"big\">" + MessageResource.GetMessage("no_licecse") + "</span></div>";

                            }


                        }
                        catch (Exception ex)
                        {
                            eHtml += String.Format(errorTemplate, MessageResource.GetMessage("api_error"));
                            //ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                        }


                        html += "</div>";

                    }

                    contentRet = new WebJsonResponse("#main aside", (eHtml != "" ? eHtml : html));
                    contentRet.js = js;

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