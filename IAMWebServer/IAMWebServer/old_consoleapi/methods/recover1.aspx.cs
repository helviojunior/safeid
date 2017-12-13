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
    public partial class recover1 : System.Web.UI.Page
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
                entityId = LoginUser.FindUser(this, Request["userLogin"], out err);
                if (entityId > 0)
                {
                    Session["entityId"] = entityId;

                    LoginUser.NewCode(this, entityId, out err);
                    if (err == "")
                    {
                        String html = "";
                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            DataTable c = db.Select("select * from vw_entity_confirmations where enterprise_id = " + enterpriseID + " and  entity_id = " + entityId);
                            html += "<form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\" action=\"/consoleapi/recover2/\">";
                            if ((c != null) && (c.Rows.Count > 0))
                            {

                                html += "<div class=\"login_form\">";
                                html += "<input type=\"hidden\" name=\"do\" value=\"recover2\" />";
                                html += "<ul>";
                                html += "    <li>";
                                html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("send_conf_to") + "</p>";
                                html += "    </li>";

                                foreach (DataRow dr in c.Rows)
                                {
                                    String data = LoginUser.MaskData(dr["value"].ToString(), (Boolean)dr["is_mail"], (Boolean)dr["is_sms"]);
                                    if (data != "")
                                        html += "    <li><p style=\"width:400px;padding:0 0 5px 10px;color:#000;\"><input name=\"sentTo\" type=\"radio\" value=\"" + data + "\">" + data + "</p></li>";
                                }

                                html += "    <li>";
                                html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                                html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("send_code") + "</button>";
                                html += "    </li>";
                                html += "</ul>     ";
                                html += "</div>";
                            }
                            else
                            {

                                html += "<div class=\"login_form\">";
                                html += "<input type=\"hidden\" name=\"do\" value=\"recover2\" />";
                                html += "<ul>";
                                html += "    <li>";
                                html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">No method available</p>";
                                html += "    </li>";
                                html += "    <li>";
                                html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a></span>";
                                html += "    </li>";
                                html += "</ul>     ";
                                html += "</div>";
                            }
                            html += "</form>";
                        }

                        //ret = new WebJsonResponse("recover1.aspx");
                        ret = new WebJsonResponse("#recover_container", html);
                    }
                    else
                        ret = new WebJsonResponse("", err, 3000, true);
                }
                else
                {
                    ret = new WebJsonResponse("", err, 3000, true);
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