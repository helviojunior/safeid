using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading;
using System.Globalization;
using System.Resources;
using IAM.GlobalDefs;

namespace IAMWebServer.login
{
    public partial class Login : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

            LoginData login = LoginUser.LogedUser(this);
            if (login != null)
                Response.Redirect("/autoservice/");

            String html = "";
            html += "<div class=\"login_form\">";
            html += "    <ul>";
            html += "        <li>";
            html += "            <span class=\"inputWrap\">";
            //html += "				<span id=\"ph_userLogin\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("login_user_name") + "</span>";
            html += "				<input type=\"text\" id=\"userLogin\" tabindex=\"1\" name=\"userLogin\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("login_user_name") + "\" onfocus=\"$('#userLogin').addClass('focus');\" onblur=\"$('#userLogin').removeClass('focus');\" />";
            html += "				<span id=\"ph_userLoginIcon\" onclick=\"$('#userLogin').focus();\"></span>";
            html += "            </span>";
            html += "        </li>";
            html += "        <li>";
            html += "            <span class=\"inputWrap\">";
            //html += "				<span id=\"ph_password\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("login_password") + "</span>";
            html += "				<input type=\"password\" id=\"password\" tabindex=\"2\" name=\"password\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("login_password") + "\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\" />";
            html += "				<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
            html += "			</span>";
            html += "        </li>";
            //html += "        <li><div class=\"error-box\">fdsafdas</div>";
            html += "        </li>";
            html += "        <li>";
            html += "            <span class=\"forgot\"> <a href=\"/recover/\">" + MessageResource.GetMessage("login_forgot") + "</a> </span>";
            html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_log") + "</button>";
            html += "        </li>";
            html += "    </ul>     ";
            html += "</div>";
            holderContent.Controls.Add(new LiteralControl(html));
            
        }
    }
}