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
    public partial class configview : System.Web.UI.Page
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


            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();

            LMenu menu1 = null;
            LMenu menu2 = null;
            LMenu menu3 = null;

            WebJsonResponse contentRet = null;

            String html = "";
            String eHtml = "";
            String js = null;

            String errorTemplate = "<span class=\"empty-results\">{0}</span>";

            switch (area)
            {
                case "":
                case "content":
                    html += "<h3>" + MessageResource.GetMessage("users") + "</h3>";
                    html += "<div id=\"chartUserFlow\"></div>";
                    html += "<h3>" + MessageResource.GetMessage("config") + "</h3>";
                    html += "<div id=\"chartConfigFlow\"></div>";

                    js += "$('#chartUserFlow').flowchart({load_uri: '" + ApplicationVirtualPath + "admin/chartdata/flow/users/'});";
                    js += "$('#chartConfigFlow').flowchart({load_uri: '" + ApplicationVirtualPath + "admin/chartdata/flow/config/'});";

                    contentRet = new WebJsonResponse("#content-wrapper", (eHtml != "" ? eHtml : html));
                    contentRet.js = js;
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