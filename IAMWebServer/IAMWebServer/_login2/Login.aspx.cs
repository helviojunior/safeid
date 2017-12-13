using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.AuthPlugins;

namespace IAMWebServer._login2
{
    public partial class Login1 : System.Web.UI.Page
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
                        AuthBase authPlugin = null;
                        try
                        {
                            //Força sempre usar o plugin 'internal'
                            //Para haver uma autenticação alternativa ao CAS
                            authPlugin = AuthBase.GetPlugin(new Uri("auth://iam/plugins/internal"));
                        }
                        catch { }

                        if (authPlugin == null)
                            throw new Exception("Plugin não encontrado");

                        LoginResult ret = null;

                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            ret = authPlugin.Auth(db, this);

                        if (!ret.Success)
                            error = ret.Text;
                    }
                    catch (Exception ex)
                    {
                        //Tools.Tool.notifyException(ex, this);
                        error = "Erro: " + ex.Message;
                    }
                }


                html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\""+ Session["ApplicationVirtualPath"] +"login2/\"><div class=\"login_form\">";

                html += "    <ul>";
                html += "        <li>";
                html += "            <span class=\"inputWrap\">";
                html += "				<input type=\"text\" id=\"username\" tabindex=\"1\" name=\"username\" value=\"" + Request["username"] + "\" style=\"\" placeholder=\"" + MessageResource.GetMessage("login_user_name") + "\" onfocus=\"$('#username').addClass('focus');\" onblur=\"$('#username').removeClass('focus');\" />";
                html += "				<span id=\"ph_userLoginIcon\" onclick=\"$('#username').focus();\"></span>";
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
                html += "            <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "login2/recover/\">" + MessageResource.GetMessage("login_forgot") + "</a> </span>";
                html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_log") + "</button>";
                html += "        </li>";
                html += "    </ul>     ";

                html += "</div></form>";
                holderContent.Controls.Add(new LiteralControl(html));
            }
        }
    }
}