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
    public partial class recover : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            String html = "";
            String error = "";
            
            html += "<div id=\"recover_container\"><form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\"><div class=\"login_form\">";

            Uri svc = null;
            try
            {
                svc = new Uri(Request.QueryString["service"]);
            }
            catch { }

            using (DbBase db = DbBase.InstanceFromConfig(ConfigurationManager.ConnectionStrings["CASDatabase"]))
            {

                CASConnectorBase connector = CASUtils.GetService(db, this, svc);

                if ((connector == null) || (connector is EmptyPlugin))
                {
                    //Serviço não informado ou não encontrado
                    html += "    <ul>";
                    html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("service_invalid_uri") + "</div>";
                    html += "    </ul>";
                }
                else if ((connector.State != null) && (connector.State is CASPluginService) && !(((CASPluginService)connector.State).Config.PermitPasswordRecover))
                {
                    CASPluginService p = (CASPluginService)connector.State;

                    //Serviço não informado ou não encontrado
                    html += "    <ul>";
                    html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("service_not_permit_recover_pwd") + (!String.IsNullOrEmpty(p.Config.Admin) ? "<br /><br />" + p.Config.Admin : "") + "</div>";
                    html += "    </ul>";
                }
                else
                {
                    //Caso a recuperação de senha seja externa, redireciona
                    if ((connector.State is CASPluginService) && (((CASPluginService)connector.State).Config.ExternalPasswordRecover) && (((CASPluginService)connector.State).Config.PasswordRecoverUri != null))
                    {
                        Response.Redirect(((CASPluginService)connector.State).Config.PasswordRecoverUri.AbsoluteUri, false);
                        return;
                    }

                    Session["recover_service"] = svc.AbsoluteUri;

                    if (Request.HttpMethod == "POST")
                    {
                        try
                        {
                            CASUserInfo user = connector.FindUser(Request["username"]);
                            user.Service = connector.Service;
                            if ((user.Success) && (user.Emails != null) && (user.Emails.Count > 0))
                            {
                                user.NewCode();
                                Session["user_info"] = user;

                                Response.Redirect("/cas/recover/step1/", false);
                                return;
                            }
                            else if ((user.Emails == null) || (user.Emails.Count == 0))
                            {
                                error = MessageResource.GetMessage("user_email_list");
                            }
                            else
                            {
                                error = user.ErrorText;
                            }

                        }
                        catch (Exception ex)
                        {
                            Tools.Tool.notifyException(ex);
                            error = MessageResource.GetMessage("internal_error");
                        }

                    }

                    html += "    <input type=\"hidden\" name=\"do\" value=\"recover1\" />";
                    html += "    <ul>";
                    html += "        <li>";
                    html += "            <p style=\"width:270px;padding:0 0 20px 0;color:#000;\">" + MessageResource.GetMessage("login_recover_message") + "</p>";
                    html += "        </li>";
                    html += "        <li>";
                    html += "            <span class=\"inputWrap\">";
                    //html += "			    <span id=\"ph_userLogin\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("login_user_name") + "</span>";
                    html += "			    <input type=\"text\" id=\"username\" tabindex=\"1\" name=\"username\" value=\"\" style=\"\"  placeholder=\"" + MessageResource.GetMessage("login_user_name") + "\" onfocus=\"$('#userLogin').addClass('focus');\" onblur=\"$('#userLogin').removeClass('focus');\" />";
                    html += "			    <span id=\"ph_usernameIcon\" onclick=\"$('#userLogin').focus();\"></span>";
                    html += "            </span>";
                    html += "        </li>";
                    if (error != "")
                        html += "        <li><div class=\"error-box\">" + error + "</div>";
                    html += "        <li>";
                    html += "            <span class=\"forgot\"> <a href=\"" + svc.AbsoluteUri + "\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                    html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_recover_btn_recover") + "</button>";
                    html += "        </li>";
                    html += "    </ul>     ";

                }

                html += "</div>";
                html += "</form>";
                html += "</div>";
            }

            holderContent.Controls.Add(new LiteralControl(html));            
        }
    }
}