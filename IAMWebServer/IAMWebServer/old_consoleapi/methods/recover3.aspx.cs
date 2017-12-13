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
using IAM.GlobalDefs;

namespace IAMWebServer.consoleapi.methods
{
    public partial class recover3 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;

            //ResourceManager rm = new ResourceManager("Resources.Strings", System.Reflection.Assembly.Load("App_GlobalResources"));
            //CultureInfo ci = Thread.CurrentThread.CurrentCulture;


            try
            {
                Int64 enterpriseID = ((EnterpriseData)Page.Session["enterprise_data"]).Id;
                Int64 entityId = 0;
                String err = "";

                String userCode = Request["userCode"];
                if ((userCode == null) || (userCode == ""))
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("type_code"), 3000, true);
                }
                else
                {
                    if (Session["entityId"] != null)
                        entityId = (Int64)Session["entityId"];
                    if (entityId > 0)
                    {
                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            DataTable c = db.Select("select * from entity where deleted = 0 and id = " + entityId + " and recovery_code = '" + Tools.Tool.TrataInjection(userCode) + "'");
                            if ((c != null) && (c.Rows.Count > 0))
                            {
                                Session["userCode"] = c.Rows[0]["recovery_code"].ToString();

                                String html = "";
                                html += "<form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\" action=\"/consoleapi/recover4/\">";
                                html += "<div class=\"login_form\">";
                                html += "<input type=\"hidden\" name=\"do\" value=\"recover4\" />";
                                html += "<ul>";
                                html += "    <li>";
                                html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("new_password_title") + "</p>";
                                html += "    </li>";
                                html += "    <li>";
                                html += "        <span class=\"inputWrap\">";
                                //html += "			<span id=\"ph_password\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("new_password") + "</span>";
                                html += "			<input type=\"password\" id=\"password\" tabindex=\"1\" name=\"password\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("new_password") + "\" onkeyup=\"iamadmin.passwordStrength('#password');\" onfocus=\"$('#password').addClass('focus');\" onblur=\"$('#password').removeClass('focus');\" />";
                                html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password').focus();\"></span>";
                                html += "        </span>";
                                html += "    </li>";
                                html += "    <li>";
                                html += "        <span class=\"inputWrap\">";
                                //html += "			<span id=\"ph_password2\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("new_password_confirm") + "</span>";
                                html += "			<input type=\"password\" id=\"password2\" tabindex=\"1\" name=\"password2\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("new_password_confirm") + "\" onfocus=\"$('#password2').addClass('focus');\" onblur=\"$('#password2').removeClass('focus');\" />";
                                html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#password2').focus();\"></span>";
                                html += "        </span>";
                                html += "    </li>";
                                html += "    <li>";
                                html += "        <div id=\"passwordStrength\"><span>" + MessageResource.GetMessage("password_strength") + ": " + MessageResource.GetMessage("unknow") + "</span><div class=\"bar\"></div></div>";
                                html += "    </li>";
                                html += "    <li>";
                                html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                                html += "        <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("change_password") + "</button>";
                                html += "    </li>";
                                html += "</ul>     ";
                                html += "</div>";
                                html += "</form>";
                                ret = new WebJsonResponse("#recover_container", html);

                            }
                            else
                            {
                                ret = new WebJsonResponse("", MessageResource.GetMessage("invalid_code"), 3000, true);
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