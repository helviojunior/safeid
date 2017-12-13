using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using CAS.Web;
using CAS.PluginInterface;

namespace IAMWebServer._cas
{
    public partial class passwordchanged : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            String html = "";
            String error = "";
            
            html += "<div id=\"recover_container\"><form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\"><div class=\"login_form\">";

            if (Session["user_info"] == null || !(Session["user_info"] is CASUserInfo))
            {
                //Serviço não informado ou não encontrado
                html += "    <ul>";
                html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("invalid_session") + "</div>";
                html += "    </ul>";
            }
            else
            {
                Session["userCode"] = null;

                CASUserInfo userInfo = (CASUserInfo)Session["user_info"];

                html += "<ul>";
                html += "    <li class=\"title\">";
                html += "        <strong>" + MessageResource.GetMessage("password_changed_sucessfully") + "</strong>";
                html += "    </li>";
                html += "    <li>";
                html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("password_changed_text") + "</p>";
                html += "    </li>";
                html += "    <li>";
                html += "        <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "cas/login/?service=" + HttpUtility.UrlEncode(userInfo.Service.AbsoluteUri) + "\">" + MessageResource.GetMessage("return_default") + "</a></span>";
                html += "    </li>";
                html += "</ul>     ";

            }

            html += "</div>";
            html += "</form>";
            html += "</div>";
            
            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}