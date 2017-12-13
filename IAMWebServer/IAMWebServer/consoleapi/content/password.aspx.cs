using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using IAM.Config;
using System.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Text;
using IAM.Config;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;

namespace IAMWebServer.consoleapi.content
{
    public partial class password : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;



            try
            {

                LoginData login = LoginUser.LogedUser(this);

                String err = "";
                if (!EnterpriseIdentify.Identify(this, false, out err)) //Se houver falha na identificação da empresa finaliza a resposta
                {
                    ret = new WebJsonResponse("", err, 3000, true);
                }
                else if (login == null)
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("expired_session"), 3000, true, "/login/");
                }
                else
                {
                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {
                        DataTable c = db.Select("select * from entity where deleted = 0 and id = " + login.Id);
                        if ((c != null) && (c.Rows.Count > 0))
                        {

                            String html = "";
                            String content = "<div>{0}</div>";
                            html = "";
                            html += "<form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\" action=\"/consoleapi/changepassword/\" onsubmit=\"return iam.GenericSubmit('#serviceRecover');\">";
                            html += "<div class=\"login_form\">";
                            html += "<h1>" + MessageResource.GetMessage("change_password_title") + "</h1> ";
                            html += "<ul>";
                            html += "    <li>";
                            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("change_password_text") + "</p>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <span class=\"inputWrap\">";
                            html += "			<span id=\"ph_current_password\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("current_password") + "</span>";
                            html += "			<input type=\"password\" id=\"current_password\" tabindex=\"1\" name=\"current_password\" value=\"\" style=\"\" onkeyup=\"fnLogin.keyup('current_password');\" onfocus=\"$('#current_password').addClass('focus'); fnLogin.keyup('password');\" onblur=\"$('#current_password').removeClass('focus');\" />";
                            html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                            html += "        </span>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <span class=\"inputWrap\">";
                            html += "			<span id=\"ph_password\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("new_password") + "</span>";
                            html += "			<input type=\"password\" id=\"password\" tabindex=\"1\" name=\"password\" value=\"\" style=\"\" onkeyup=\"fnLogin.keyup('password'); iam.passwordStrength('#password');\" onfocus=\"$('#password').addClass('focus'); fnLogin.keyup('password');\" onblur=\"$('#password').removeClass('focus');\" />";
                            html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                            html += "        </span>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <span class=\"inputWrap\">";
                            html += "			<span id=\"ph_password2\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("new_password_confirm") + "</span>";
                            html += "			<input type=\"password\" id=\"password2\" tabindex=\"1\" name=\"password2\" value=\"\" style=\"\" onkeyup=\"fnLogin.keyup('password2');\" onfocus=\"$('#password2').addClass('focus'); fnLogin.keyup('password2');\" onblur=\"$('#password2').removeClass('focus');\" />";
                            html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password2').focus();\"></span>";
                            html += "        </span>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <div id=\"passwordStrength\"><span>" + MessageResource.GetMessage("password_strength") + ": " + MessageResource.GetMessage("unknow") + "</span><div class=\"bar\"></div></div>";
                            html += "    </li>";
                            html += "    <li>";
                            html += "        <span class=\"forgot\"> <a class=\"cancel\">" + MessageResource.GetMessage("cancel") + "</a></span>";
                            html += "        <input type=\"submit\" tabindex=\"4\" id=\"submitBtn\" value=\"" + MessageResource.GetMessage("change_password") + "\" class=\"action btn btn-success\" />";
                            html += "    </li>";
                            html += "</ul>     ";
                            html += "</div>";
                            html += "</form>";

                            ret = new WebJsonResponse("#pn-password .content", String.Format(content, html));
                        }
                        else
                        {
                            ret = new WebJsonResponse("", MessageResource.GetMessage("valid_username"), 3000, true);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Tools.Tool.notifyException(ex);
                throw ex;
            }


            if (ret != null)
                ReturnHolder.Controls.Add(new LiteralControl(ret.ToJSON()));
        }
    }
}