using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using CAS.Web;
using CAS.PluginInterface;
using System.Configuration;
using SafeTrend.Data;

namespace IAMWebServer._cas
{
    public partial class changepassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            String html = "";
            String error = "";

            html += "<div id=\"recover_container\"><form id=\"pwdChange\" name=\"pwdChange\" method=\"post\"><div class=\"login_form\">";

            if ((Session["cas_ticket"] == null) || !(Session["cas_ticket"] is CASTicketResult))
            {
                //Serviço não informado ou não encontrado
                html += "    <ul>";
                html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("invalid_session") + "</div>";
                html += "    </ul>";
            }
            else
            {
                CASTicketResult ticket = (CASTicketResult)Session["cas_ticket"];
                using (DbBase db = DbBase.InstanceFromConfig(ConfigurationManager.ConnectionStrings["CASDatabase"]))
                {

                    CASConnectorBase connector = CASUtils.GetService(db, this, ticket.Service);

                    if ((connector == null) || (connector is EmptyPlugin))
                    {
                        //Serviço não informado ou não encontrado
                        html += "    <ul>";
                        html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("service_invalid_uri") + "</div>";
                        html += "    </ul>";
                    }
                    else if ((connector.State != null) && (connector.State is CASPluginService) && !(((CASPluginService)connector.State).Config.PermitChangePassword))
                    {
                        CASPluginService p = (CASPluginService)connector.State;
                        //Serviço não informado ou não encontrado
                        html += "    <ul>";
                        html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("service_not_permit_change_pwd") + (!String.IsNullOrEmpty(p.Config.Admin) ? "<br /><br />" + p.Config.Admin : "") + "</div>";
                        html += "    </ul>";
                    }
                    else
                    {

                        if (Request.HttpMethod == "POST")
                        {

                            try
                            {

                                String password = Tools.Tool.TrataInjection(Request["password"]);
                                String password2 = Request["password2"];
                                if ((password == null) || (password == ""))
                                {
                                    error = MessageResource.GetMessage("type_password");
                                }
                                else if ((password2 == null) || (password2 == ""))
                                {
                                    error = MessageResource.GetMessage("type_password_confirm");
                                }
                                else if (password != password2)
                                {
                                    error = MessageResource.GetMessage("password_not_equal");
                                }
                                else
                                {
                                    CASChangePasswordResult res = connector.ChangePassword(ticket, password);
                                    if (res.Success)
                                    {
                                        connector.SaveTicket(ticket);

                                        CASUtils.AddCoockie(this, ticket);

                                        Session["user_info"] = new CASUserInfo(ticket);

                                        Response.Redirect(Session["ApplicationVirtualPath"] + "cas/passwordchanged/", false);
                                        return;
                                    }
                                    else
                                    {
                                        if (res.ErrorText == null)
                                            throw new Exception("");

                                        error = res.ErrorText;
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
                        html += "        <li>";
                        html += "            <p style=\"width:270px;padding:0 0 20px 0;color:#000;\">" + MessageResource.GetMessage("password_expired_text") + "</p>";
                        html += "        </li>";
                        html += "    <li>";
                        html += "        <span class=\"inputWrap\">";
                        html += "			<input type=\"password\" id=\"password\" tabindex=\"1\" name=\"password\" value=\"\" style=\"\"  placeholder=\"" + MessageResource.GetMessage("new_password") + "\" onkeyup=\"cas.passwordStrength('#password');\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\" />";
                        html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                        html += "        </span>";
                        html += "    </li>";
                        html += "    <li>";
                        html += "        <span class=\"inputWrap\">";
                        html += "			<input type=\"password\" id=\"password2\" tabindex=\"1\" name=\"password2\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("new_password_confirm") + "\" onfocus=\"$('#password2').addClass('focus');\" onblur=\"$('#password2').removeClass('focus');\" />";
                        html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password2').focus();\"></span>";
                        html += "        </span>";
                        html += "    </li>";
                        html += "    <li>";
                        html += "        <div id=\"passwordStrength\"><span>" + MessageResource.GetMessage("password_strength") + ": " + MessageResource.GetMessage("unknow") + "</span><div class=\"bar\"></div></div>";
                        html += "    </li>";

                        if (error != "")
                            html += "        <li><div class=\"error-box\">" + error + "</div>";

                        html += "        <li>";
                        html += "           <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "cas/login/?service=" + HttpUtility.UrlEncode(connector.Service.AbsoluteUri) + "\">" + MessageResource.GetMessage("cancel") + "</a> </span>";
                        html += "           <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("change_password") + "</button>";
                        html += "        </li>";
                        html += "    </ul>";
                    }
                }

                html += "</div>";
                html += "</form>";
                html += "</div>";
            }
            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}