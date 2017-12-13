using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;

namespace IAMWebServer._login2
{
    public partial class PasswordChanged : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            String html = "";

            html += "<div class=\"login_form\">";

            html += "<ul>";
            html += "    <li class=\"title\">";
            html += "        <strong>" + MessageResource.GetMessage("password_changed_sucessfully") + "</strong>";
            html += "    </li>";
            html += "    <li>";
            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("password_changed_text") + "</p>";
            html += "    </li>";
            html += "    <li>";
            html += "        <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "autoservice/\">" + MessageResource.GetMessage("return_default") + "</a></span>";
            html += "    </li>";
            html += "</ul>     ";

            html += "</div>";

            holderContent.Controls.Add(new LiteralControl(html));

        }
    }
}