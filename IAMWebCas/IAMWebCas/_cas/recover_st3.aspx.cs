using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using CAS.Web;
using CAS.PluginInterface;
using System.Configuration;
using SafeTrend.Data;

namespace IAMWebServer._cas
{
    public partial class recover_st3 : System.Web.UI.Page
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
                using (DbBase db = DbBase.InstanceFromConfig(ConfigurationManager.ConnectionStrings["CASDatabase"]))
                {

                    CASConnectorBase connector = CASUtils.GetService(db, this, userInfo.Service);

                    if ((connector == null) || (connector is EmptyPlugin))
                    {
                        //Serviço não informado ou não encontrado
                        html += "    <ul>";
                        html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("service_invalid_uri") + "</div>";
                        html += "    </ul>";
                    }
                    if ((userInfo.RecoveryCode == null) || (String.IsNullOrEmpty((String)Session["userCode"])))
                    {
                        html += "    <ul>";
                        html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("invalid_session") + "</div>";
                        html += "    </ul>";
                    }
                    else
                    {

                        if (Request.HttpMethod == "POST")
                        {

                            try
                            {
                                //String pwd = Session["atual_password"].ToString();

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
                                    CASChangePasswordResult res = connector.ChangePassword(userInfo, password);
                                    if (res.Success)
                                    {
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

                        html += "<ul>";
                        html += "    <li>";
                        html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("new_password_title") + "</p>";
                        html += "    </li>";
                        html += "    <li>";
                        html += "        <span class=\"inputWrap\">";
                        html += "			<input type=\"password\" id=\"password\" tabindex=\"1\" name=\"password\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("new_password") + "\" onkeyup=\"cas.passwordStrength('#password');\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\" />";
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

                        html += "    <li>";
                        html += "        <span class=\"forgot\"> <a href=\"" + userInfo.Service.AbsoluteUri + "\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                        html += "        <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("change_password") + "</button>";
                        html += "    </li>";
                        html += "</ul>     ";
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