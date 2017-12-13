using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using IAM.GlobalDefs;
using IAM.AuthPlugins;
using IAM.Config;
using IAM.CA;
using SafeTrend.Data;

namespace IAMWebServer._login2
{
    public partial class recover_st3 : System.Web.UI.Page
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
                    Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/", false);
            }
            else if (Session["user_info"] == null || !(Session["user_info"] is Int64))
            {
                //Serviço não informado ou não encontrado
                html += "    <ul>";
                html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("invalid_session") + "</div>";
                html += "    </ul>";
            }
            else if (Session["userCode"] == null || (String.IsNullOrWhiteSpace((String)Session["userCode"])))
            {
                //Serviço não informado ou não encontrado
                html += "    <ul>";
                html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("invalid_session") + "</div>";
                html += "    </ul>";
            }
            else
            {
                html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\"" + Session["ApplicationVirtualPath"] + "login2/recover/step3/\"><div class=\"login_form\">";

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

                            Int64 entityId = (Int64)Session["user_info"];


                            Int64 enterpriseId = 0;
                            if ((Page.Session["enterprise_data"]) != null && (Page.Session["enterprise_data"] is EnterpriseData) && (((EnterpriseData)Page.Session["enterprise_data"]).Id != null))
                                enterpriseId = ((EnterpriseData)Page.Session["enterprise_data"]).Id;

                            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            {

                                UserPasswordStrength usrCheck = new UserPasswordStrength(db.Connection, entityId);
                                UserPasswordStrengthResult check = usrCheck.CheckPassword(password);
                                if (check.HasError)
                                {
                                    if (check.NameError)
                                    {
                                        error = MessageResource.GetMessage("password_name_part");
                                    }
                                    else
                                    {

                                        String txt = "* " + MessageResource.GetMessage("number_char") + ": " + (!check.LengthError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                        txt += "* " + MessageResource.GetMessage("uppercase") + ":  " + (!check.UpperCaseError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                        txt += "* " + MessageResource.GetMessage("lowercase") + ": " + (!check.LowerCaseError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                        txt += "* " + MessageResource.GetMessage("numbers") + ": " + (!check.DigitError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                        txt += "* " + MessageResource.GetMessage("symbols") + ":  " + (!check.SymbolError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail"));

                                        error = MessageResource.GetMessage("password_complexity") + ": <br />" + txt;

                                    }

                                }
                                else
                                {

                                    DataTable c = db.Select("select * from entity where deleted = 0 and id = " + entityId + " and recovery_code = '" + Session["userCode"] + "'");
                                    if ((c != null) && (c.Rows.Count > 0))
                                    {


                                        //Verifica a senha atual
                                        using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, enterpriseId))
                                        using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(c.Rows[0]["password"].ToString())))
                                        {
                                            using (SqlConnection conn1 = IAMDatabase.GetWebConnection())
                                            using (EnterpriseKeyConfig sk1 = new EnterpriseKeyConfig(conn1, enterpriseId))
                                            using (CryptApi cApi1 = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(password)))
                                            {
                                                DbParameterCollection pPar = new DbParameterCollection();
                                                String b64 = Convert.ToBase64String(cApi1.ToBytes());
                                                pPar.Add("@password", typeof(String), b64.Length).Value = b64;

                                                db.ExecuteNonQuery("update entity set password = @password, change_password = getdate() , recovery_code = null, must_change_password = 0 where id = " + entityId, CommandType.Text, pPar);
                                            }

                                            db.AddUserLog(LogKey.User_PasswordChanged, null, "AutoService", UserLogLevel.Info, 0, enterpriseId, 0, 0, 0, entityId, 0, "Password changed through logged user", "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");

                                            //Cria o pacote com os dados atualizados deste usuário 
                                            //Este processo visa agiliar a aplicação das informações pelos plugins
                                            db.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + entityId + ")", CommandType.Text, null);

                                            //Mata a sessão
                                            //Session.Abandon();

                                            Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "login2/passwordchanged/", false);

                                        }

                                    }
                                    else
                                    {
                                        error = MessageResource.GetMessage("internal_error");
                                    }
                                }

                            }

                        }
                    }
                    catch (Exception ex)
                    {

                        Tools.Tool.notifyException(ex);
                        error = MessageResource.GetMessage("internal_error") + ": " + ex.Message;
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
                html += "           <span class=\"forgot\"> <a href=\"" + Session["ApplicationVirtualPath"] + "logout/\">" + MessageResource.GetMessage("cancel") + "</a> </span>";
                html += "           <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("change_password") + "</button>";
                html += "        </li>";
                html += "    </ul>";
                

                html += "</div></form>";

                holderContent.Controls.Add(new LiteralControl(html));
            }
        
        }
    }
}