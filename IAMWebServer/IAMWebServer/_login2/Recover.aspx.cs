using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;

namespace IAMWebServer._login2
{
    public partial class Recover : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
            String html = "";
            String error = "";

            LoginData login = LoginUser.LogedUser(this);
            if (login != null)
            {
                if (Session["last_page"] != null)
                {
                    Response.Redirect(Session["last_page"].ToString());
                    Session["last_page"] = null;
                }
                else
                    Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/");
            }
            else
            {

                if (Request.HttpMethod == "POST")
                {
                    try
                    {
                        Int64 userId = LoginUser.FindUser(this, Request["username"], out error);
                        if (userId > 0)
                        {

                            Session["user_info"] = userId;

                            Response.Redirect(Session["ApplicationVirtualPath"] + "login2/recover/step1/", false);
                            return;

                        }
                        /*else if ((user.Emails == null) || (user.Emails.Count == 0))
                        {
                            error = MessageResource.GetMessage("user_email_list");
                        }
                        else
                        {
                            error = user.ErrorText;
                        }*/

                    }
                    catch (Exception ex)
                    {
                        Tools.Tool.notifyException(ex);
                        error = MessageResource.GetMessage("internal_error");
                    }
                }


                html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\""+ Session["ApplicationVirtualPath"] +"login2/recover/\"><div class=\"login_form\">";

                html += "    <ul>";
                html += "        <li>";
                html += "            <p style=\"width:270px;padding:0 0 20px 0;color:#000;\">" + MessageResource.GetMessage("login_recover_message") + "</p>";
                html += "        </li>";
                html += "        <li>";
                html += "            <span class=\"inputWrap\">";
                html += "				<input type=\"text\" id=\"username\" tabindex=\"1\" name=\"username\" value=\"" + Request["username"] + "\" style=\"\" placeholder=\"" + MessageResource.GetMessage("login_user_name") + "\" onfocus=\"$('#username').addClass('focus');\" onblur=\"$('#username').removeClass('focus');\" />";
                html += "				<span id=\"ph_userLoginIcon\" onclick=\"$('#username').focus();\"></span>";
                html += "            </span>";
                html += "        </li>";

                if (error != "")
                    html += "        <li><div class=\"error-box\">" + error + "</div>";

                html += "        </li>";
                html += "        <li>";
                html += "            <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "login2/\">" + MessageResource.GetMessage("cancel") + "</a> </span>";
                html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_recover_btn_recover") + "</button>";
                html += "        </li>";
                html += "    </ul>     ";

                html += "</div></form>";
                holderContent.Controls.Add(new LiteralControl(html));
            }
        
        }
    }
}