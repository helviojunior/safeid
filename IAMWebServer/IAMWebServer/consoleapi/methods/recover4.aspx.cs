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
using IAM.Config;
using IAM.GlobalDefs;
using SafeTrend.Data;
using System.Data.SqlClient;

namespace IAMWebServer.consoleapi.methods
{
    public partial class recover4 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;

            try
            {
                Int64 enterpriseID = ((EnterpriseData)Page.Session["enterprise_data"]).Id;
                Int64 entityId = 0;
                String err = "";


                String password = Tools.Tool.TrataInjection(Request["password"]);
                String password2 = Request["password2"];
                if ((password == null) || (password == ""))
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

                    Int64 enterpriseId = 0;
                    if ((Page.Session["enterprise_data"]) != null && (Page.Session["enterprise_data"] is EnterpriseData) && (((EnterpriseData)Page.Session["enterprise_data"]).Id != null))
                        enterpriseId = ((EnterpriseData)Page.Session["enterprise_data"]).Id;
                    
                    String code = "";
                    if (Session["entityId"] != null)
                        entityId = (Int64)Session["entityId"];

                    if (Session["userCode"] != null)
                        code = Session["userCode"].ToString();

                    if ((entityId > 0) && (code != ""))
                    {
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

                                DataTable c = db.Select("select * from entity where deleted = 0 and id = " + entityId + " and recovery_code = '" + code + "'");
                                if ((c != null) && (c.Rows.Count > 0))
                                {

                                    using (EnterpriseKeyConfig sk = new EnterpriseKeyConfig(db.Connection, enterpriseId))
                                    using (CryptApi cApi = new CryptApi(sk.ServerCert, Encoding.UTF8.GetBytes(password)))
                                        db.ExecuteNonQuery("update entity set password = '" + Convert.ToBase64String(cApi.ToBytes()) + "', recovery_code = null, last_login = getdate(), change_password = getdate(),  must_change_password = 0 where id = " + entityId, CommandType.Text, null);

                                    db.AddUserLog(LogKey.User_PasswordChanged, null, "AutoService", UserLogLevel.Info, 0, enterpriseId, 0, 0, 0, entityId, 0, "Password changed through recovery code", "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");

                                    //Cria o pacote com os dados atualizados deste usuário 
                                    //Este processo vija agiliar a aplicação das informações pelos plugins
                                    db.ExecuteNonQuery("insert into deploy_now (entity_id) values(" + entityId + ")", CommandType.Text, null);


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

                                    ret = new WebJsonResponse("#recover_container", html);

                                }
                                else
                                {
                                    ret = new WebJsonResponse("", MessageResource.GetMessage("invalid_code"), 3000, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        ret = new WebJsonResponse("", MessageResource.GetMessage("invalid_session"), 3000, true);
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