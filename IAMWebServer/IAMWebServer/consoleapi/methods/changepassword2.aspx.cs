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
using SafeTrend.Json;
//using IAM.Deploy;
using IAM.CA;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;

namespace IAMWebServer.consoleapi.methods
{
    public partial class changepassword2 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;

            String err = "";
            if (!EnterpriseIdentify.Identify(this, false, out err)) //Se houver falha na identificação da empresa finaliza a resposta
            {
                ret = new WebJsonResponse("", err, 3000, true);
            }
            else if ((Session["entity_id"] == null) || !(Session["entity_id"] is Int64))
            {
                ret = new WebJsonResponse("", MessageResource.GetMessage("expired_session"), 3000, true, "/login/");
            }
            else
            {

                try
                {
                    Int64 enterpriseId = 0;
                    if ((Page.Session["enterprise_data"]) != null && (Page.Session["enterprise_data"] is EnterpriseData) && (((EnterpriseData)Page.Session["enterprise_data"]).Id != null))
                        enterpriseId = ((EnterpriseData)Page.Session["enterprise_data"]).Id;
                    
                    String currentPassword = Tools.Tool.TrataInjection(Request["current_password"]);
                    String password = Tools.Tool.TrataInjection(Request["password"]);
                    String password2 = Request["password2"];
                    if ((currentPassword == null) || (currentPassword == ""))
                    {
                        ret = new WebJsonResponse("", MessageResource.GetMessage("type_password_current"), 3000, true);
                    }
                    else if ((password == null) || (password == ""))
                    {
                        ret = new WebJsonResponse("", MessageResource.GetMessage("type_password"), 3000, true);
                    }
                    else if ((password2 == null) || (password2 == ""))
                    {
                        ret = new WebJsonResponse("", MessageResource.GetMessage("type_password_confirm"), 3000, true);
                    }
                    else if (password != password2)
                    {
                        ret = new WebJsonResponse("", MessageResource.GetMessage("password_not_equal"), 3000, true);
                    }
                    else
                    {
                        Int64 entityId = (Int64)Session["entity_id"];

                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {

                            UserPasswordStrength usrCheck = new UserPasswordStrength(db.Connection, entityId);
                            UserPasswordStrengthResult check = usrCheck.CheckPassword(password);
                            if (check.HasError)
                            {
                                if (check.NameError)
                                {
                                    ret = new WebJsonResponse("", MessageResource.GetMessage("password_name_part"), 3000, true);
                                }
                                else
                                {

                                    String txt = "* " + MessageResource.GetMessage("number_char") + ": " + (!check.LengthError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                    txt += "* " + MessageResource.GetMessage("uppercase") + ":  " + (!check.UpperCaseError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                    txt += "* " + MessageResource.GetMessage("lowercase") + ": " + (!check.LowerCaseError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                    txt += "* " + MessageResource.GetMessage("numbers") + ": " + (!check.DigitError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail")) + "<br />";
                                    txt += "* " + MessageResource.GetMessage("symbols") + ":  " + (!check.SymbolError ? MessageResource.GetMessage("ok") : MessageResource.GetMessage("fail"));

                                    ret = new WebJsonResponse("", MessageResource.GetMessage("password_complexity") + ": <br />" + txt, 5000, true);

                                }

                            }
                            else
                            {

                                DataTable c = db.Select("select * from entity where deleted = 0 and id = " + entityId);
                                if ((c != null) && (c.Rows.Count > 0))
                                {


                                    //Verifica a senha atual
                                    using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, enterpriseId))
                                    using (CryptApi cApi = CryptApi.ParsePackage(sk.ServerPKCS12Cert, Convert.FromBase64String(c.Rows[0]["password"].ToString())))
                                        if (Encoding.UTF8.GetString(cApi.clearData) != currentPassword)
                                        {
                                            ret = new WebJsonResponse("", MessageResource.GetMessage("current_password_invalid"), 3000, true);
                                        }
                                        else
                                        {
                                            using (SqlConnection conn1 = IAMDatabase.GetWebConnection())
                                            using (EnterpriseKeyConfig sk1 = new EnterpriseKeyConfig(conn1, enterpriseId))
                                            using (CryptApi cApi1 = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(password)))
                                            {
                                                DbParameterCollection pPar = new DbParameterCollection();;
                                                String b64 = Convert.ToBase64String(cApi1.ToBytes());
                                                pPar.Add("@password", typeof(String), b64.Length).Value = b64;

                                                db.ExecuteNonQuery("update entity set password = @password, change_password = getdate() , recovery_code = null, must_change_password = 0 where id = " + entityId, CommandType.Text, pPar);
                                            }

                                            db.AddUserLog(LogKey.User_PasswordChanged, null, "AutoService", UserLogLevel.Info, 0, enterpriseId, 0, 0, 0, entityId, 0, "Password changed through autoservice logged user", "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");

                                            //Cria o pacote com os dados atualizados deste usuário 
                                            //Este processo vija agiliar a aplicação das informações pelos plugins
                                            db.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + entityId + ")", CommandType.Text, null);

                                            //Mata a sessão
                                            Session.Abandon();

                                            String html = "";
                                            html += "<div class=\"login_form\">";
                                            html += "<ul>";
                                            html += "    <li class=\"title\">";
                                            html += "        <strong>" + MessageResource.GetMessage("password_changed_sucessfully") + "</strong>";
                                            html += "    </li>";
                                            html += "    <li>";
                                            html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("password_changed_text") + "</p>";
                                            html += "    </li>";
                                            html += "    <li>";
                                            html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("return_default") + "</a></span>";
                                            html += "    </li>";
                                            html += "</ul>     ";
                                            html += "</div>";

                                            ret = new WebJsonResponse("#serviceRecover", html);
                                        }

                                }
                                else
                                {
                                    ret = new WebJsonResponse("", "Internal error", 3000, true);
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.Tool.notifyException(ex);
                    throw ex;
                }
            }

            if (ret != null)
                ReturnHolder.Controls.Add(new LiteralControl(ret.ToJSON()));
        }
    }
}