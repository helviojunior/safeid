using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using CAS.Web;
using CAS.PluginInterface;
using System.Configuration;
using SafeTrend.Data;

namespace IAMWebServer._cas
{
    public partial class login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            Boolean renew = (!String.IsNullOrEmpty(Request["renew"]) && (Request["renew"].ToString().ToLower() == "true"));
            Boolean gateway = (!String.IsNullOrEmpty(Request["gateway"]) && (Request["gateway"].ToString().ToLower() == "true"));
            Boolean warn = (!String.IsNullOrEmpty(Request["warn"]) && (Request["warn"].ToString().ToLower() == "true"));

            if (renew || warn)
                gateway = false;

            if (warn)
                renew = true;

            String html = "";
            String error = "";

            html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\"/cas/login/?"+ Request.QueryString +"\"><div class=\"login_form\">";

            try
            {
                Session.Remove("cas_ticket");
            }
            catch { }


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
                else
                {
                    if (Request.HttpMethod == "GET")
                    {
                        //Serviço encontrado

                        //verifica se há cookie com token
                        HttpCookie tgc = Request.Cookies["TGC-SafeID"];
                        if (tgc != null)
                        {
                            //Verifica autenticação através do cookie
                            if (connector.Grant(tgc, renew, warn).Success)
                            {
                                Redirect(tgc.Value);//Autenticado redireciona
                                return;
                            }
                        }
                        else if (gateway)//é Gateway, ou seja não mostra opção do usuário digitar a senha
                        {
                            Redirect("");
                            return;
                        }
                    }
                    else
                    {
                        //Valida usuário e senha
                        try
                        {

                            if (String.IsNullOrEmpty(Request["username"]) || String.IsNullOrEmpty(Request["password"]))
                            {
                                error = MessageResource.GetMessage("valid_username_pwd");
                            }
                            else
                            {
                                CASTicketResult casTicket = connector.Grant(Request["username"], Request["password"]);
                                CASUtils.ClearCookie(Page);
                                if ((casTicket.Success) && (casTicket.ChangePasswordNextLogon))
                                {
                                    //Cria a sessão com as informações necessárias e redireciona
                                    Session["cas_ticket"] = casTicket;
                                    Response.Redirect(Session["ApplicationVirtualPath"] + "cas/changepassword/", false);
                                    return;
                                }
                                else if (casTicket.Success)
                                {

                                    connector.SaveTicket(casTicket);//Salva o token recebido

                                    //Salva o token no cookie
                                    CASUtils.AddCoockie(this, casTicket);

                                    Redirect(casTicket.GrantTicket);//Autenticação OK redireciona
                                    return;
                                }
                                else
                                    error = casTicket.ErrorText;
                            }

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                            Tools.Tool.notifyException(ex);
                            error = MessageResource.GetMessage("internal_error");
                        }

                    }

                    html += "    <ul>";
                    html += "        <li>";
                    html += "            <span class=\"inputWrap\">";
                    html += "				<input type=\"text\" id=\"username\" tabindex=\"1\" name=\"username\" value=\"" + Request["username"] + "\" style=\"\" placeholder=\"" + MessageResource.GetMessage("login_user_name") + "\" onfocus=\"$('#username').addClass('focus');\" onblur=\"$('#username').removeClass('focus');\" />";
                    html += "				<span id=\"ph_usernameIcon\" onclick=\"$('#username').focus();\"></span>";
                    html += "            </span>";
                    html += "        </li>";
                    html += "        <li>";
                    html += "            <span class=\"inputWrap\">";
                    html += "				<input type=\"password\" id=\"password\" tabindex=\"2\" name=\"password\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("login_password") + "\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\" />";
                    html += "				<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                    html += "			</span>";
                    html += "        </li>";
                    if (error != "")
                        html += "        <li><div class=\"error-box\">" + error + "</div>";
                    html += "        </li>";
                    html += "        <li>";
                    html += "            <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "cas/recover/?service=" + HttpUtility.UrlEncode(connector.Service.AbsoluteUri) + "\">" + MessageResource.GetMessage("login_forgot") + "</a> </span>";
                    html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_log") + "</button>";
                    html += "        </li>";
                    html += "    </ul>     ";
                }

                html += "</div></form>";
            }

            holderContent.Controls.Add(new LiteralControl(html));
            
        }

        private void Redirect(String ticket)
        {
            String method = "GET";
            if ((!String.IsNullOrEmpty(Request.QueryString["method"])) && (Request.QueryString["method"].ToUpper() == "POST"))
                method = "POST";

            Uri service = new Uri(Request.QueryString["service"]);


            if (method == "POST")
            {
                String js = "$('#cas_auto_redirect').unbind('submit').submit();";
                String html = "<form id=\"cas_auto_redirect\" action=\"" + service.AbsoluteUri + (service.Query != "" ? "&" : "?") + "ticket=" + ticket + "\" method=\"POST\"></form><script type=\"text/javascript\">jQuery(document).ready(function ($) { " + js + " });</script>";
                holderContent.Controls.Add(new LiteralControl(html));
            }
            else
            {
                Response.Redirect(service.AbsoluteUri + (service.Query != "" ? "&" : "?") + "ticket=" + ticket, false);
            }
        }
    }
}