using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Resources;
using System.Threading;
using IAM.GlobalDefs;

namespace IAMWebServer.autoservice
{
    public partial class Recover : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

            String html = "";
            html += "<div id=\"recover_container\"><form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\" action=\"/consoleapi/recover1/\">";
            html += "<div class=\"login_form\">";
            html += "    <input type=\"hidden\" name=\"do\" value=\"recover1\" />";
            html += "    <ul>";
            html += "        <li>";
            html += "            <p style=\"width:270px;padding:0 0 20px 0;color:#000;\">" + MessageResource.GetMessage("login_recover_message") + "</p>";
            html += "        </li>";
            html += "        <li>";
            html += "            <span class=\"inputWrap\">";
            //html += "			    <span id=\"ph_userLogin\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("login_user_name") + "</span>";
            html += "			    <input type=\"text\" id=\"userLogin\" tabindex=\"1\" name=\"userLogin\" value=\"\" style=\"\"  placeholder=\"" + MessageResource.GetMessage("login_user_name") + "\" onfocus=\"$('#userLogin').addClass('focus');\" onblur=\"$('#userLogin').removeClass('focus');\" />";
            html += "			    <span id=\"ph_userLoginIcon\" onclick=\"$('#userLogin').focus();\"></span>";
            html += "            </span>";
            html += "        </li>";
            html += "        <li>";
            html += "            <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
            html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_recover_btn_recover") + "</button>";
            html += "        </li>";
            html += "    </ul>     ";
            html += "</div>";
            html += "</form>";
            html += "</div>";

            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}