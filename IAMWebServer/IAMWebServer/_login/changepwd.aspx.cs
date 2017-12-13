using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading;
using System.Globalization;
using System.Resources;
using System.Data;
using IAM.GlobalDefs;

namespace IAMWebServer.login
{
    public partial class changepwd : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

            LoginData login = LoginUser.LogedUser(this);
            if (login != null)
                Response.Redirect("/autoservice/");

            if ((Session["entity_id"] == null) || !(Session["entity_id"] is Int64))
                Response.Redirect("/login/");

            String html = "";

            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {
                DataTable c = db.Select("select * from entity where deleted = 0 and id = " + Session["entity_id"]);
                if ((c != null) && (c.Rows.Count > 0))
                {

                    html = "";
                    html += "<div class=\"login_form\">";
                    html += "<ul>";
                    html += "    <li>";
                    html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("password_expired_text") + "</p>";
                    html += "    </li>";
                    html += "    <li>";
                    html += "        <span class=\"inputWrap\">";
                    //html += "			<span id=\"ph_current_password\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("current_password") + "</span>";
                    html += "			<input type=\"password\" id=\"current_password\" tabindex=\"1\" name=\"current_password\" value=\"\" style=\"\"  placeholder=\"" + MessageResource.GetMessage("current_password") + "\" onfocus=\"$('#current_password').addClass('focus');\" onblur=\"$('#current_password').removeClass('focus');\" />";
                    html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                    html += "        </span>";
                    html += "    </li>";
                    html += "    <li>";
                    html += "        <span class=\"inputWrap\">";
                    //html += "			<span id=\"ph_password\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("new_password") + "</span>";
                    html += "			<input type=\"password\" id=\"password\" tabindex=\"1\" name=\"password\" value=\"\" style=\"\"  placeholder=\"" + MessageResource.GetMessage("new_password") + "\" onkeyup=\"iamadmin.passwordStrength('#password');\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\" />";
                    html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                    html += "        </span>";
                    html += "    </li>";
                    html += "    <li>";
                    html += "        <span class=\"inputWrap\">";
                    //html += "			<span id=\"ph_password2\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("new_password_confirm") + "</span>";
                    html += "			<input type=\"password\" id=\"password2\" tabindex=\"1\" name=\"password2\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("new_password_confirm") + "\" onfocus=\"$('#password2').addClass('focus');\" onblur=\"$('#password2').removeClass('focus');\" />";
                    html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password2').focus();\"></span>";
                    html += "        </span>";
                    html += "    </li>";
                    html += "    <li>";
                    html += "        <div id=\"passwordStrength\"><span>" + MessageResource.GetMessage("password_strength") + ": " + MessageResource.GetMessage("unknow") + "</span><div class=\"bar\"></div></div>";
                    html += "    </li>";
                    html += "    <li>";
                    html += "        <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("change_password") + "</button>";
                    html += "    </li>";
                    html += "</ul>     ";
                    html += "</div>";

                }
                else
                {
                    Tools.Tool.notifyException(new Exception("User not found in change password"), this);

                    html = "";
                    html += "<div class=\"login_form\">";
                    html += "<ul>";
                    html += "    <li>";
                    html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("user_not_found") + "</p>";
                    html += "    </li>";
                    html += "    <li>";
                    html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a></span>";
                    html += "    </li>";
                    html += "</ul>     ";
                    html += "</div>";

                }
            }
            
            holderContent.Controls.Add(new LiteralControl(html));
            
        }
    }
}