using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using CAS.Web;
using CAS.PluginInterface;

namespace IAMWebServer._cas
{
    public partial class recover_st2 : System.Web.UI.Page
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

                CASUserInfo userInfo = (CASUserInfo)Session["user_info"];

                if (Request.HttpMethod == "POST")
                {
                    
                    try
                    {

                        String userCode = Request["userCode"];
                        if ((userCode == null) || (userCode == ""))
                        {
                            error = MessageResource.GetMessage("type_code");
                        }
                        else
                        {
                            if (userCode.ToLower() == userInfo.RecoveryCode.ToLower())
                            {
                                Session["userCode"] = userInfo.RecoveryCode;
                                Response.Redirect("/cas/recover/step3/", false);
                                return;
                            }
                            else
                            {
                                error = MessageResource.GetMessage("invalid_code");
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        Tools.Tool.notifyException(ex);
                        error = MessageResource.GetMessage("internal_error");
                    }
                    
                }

                html += "    <ul>";
                html += "    <li>";
                html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("enter_code") + "</p>";
                html += "    </li>";
                html += "    <li>";
                html += "        <span class=\"inputWrap\">";
                html += "			<input type=\"text\" id=\"userCode\" tabindex=\"1\" name=\"userCode\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("code") + "\" onfocus=\"$('#userCode').addClass('focus');\" onblur=\"$('#userCode').removeClass('focus');\" />";
                html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#userCode').focus();\"></span>";
                html += "        </span>";
                html += "    </li>";

                if (error != "")
                    html += "        <li><div class=\"error-box\">" + error + "</div>";

                html += "    <li>";
                html += "        <span class=\"forgot\"> <a href=\"" + userInfo.Service.AbsoluteUri + "\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                html += "        <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("confirm_code") + "</button>";
                html += "    </li>";
                html += "    </ul>";

            }

            html += "</div>";
            html += "</form>";
            html += "</div>";
            
            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}