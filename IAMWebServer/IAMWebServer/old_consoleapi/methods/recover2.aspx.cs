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
    public partial class recover2 : System.Web.UI.Page
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

                String sentTo = Request["sentTo"];
                if ((sentTo == null) || (sentTo == ""))
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("select_option"), 3000, true);
                }
                else
                {
                    if (Session["entityId"] != null)
                        entityId = (Int64)Session["entityId"];
                    if (entityId > 0)
                    {
                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        {
                            DataTable c = db.Select("select * from vw_entity_confirmations where enterprise_id = " + enterpriseID + " and  entity_id = " + entityId);
                            if ((c != null) && (c.Rows.Count > 0))
                            {
                                DataRow drSentTo = null;
                                foreach (DataRow dr in c.Rows)
                                {
                                    String data = LoginUser.MaskData(dr["value"].ToString(), (Boolean)dr["is_mail"], (Boolean)dr["is_sms"]);
                                    if (sentTo.ToString().ToLower() == data)
                                    {
                                        drSentTo = dr;
                                        break;
                                    }
                                }

                                if (drSentTo == null)
                                    ret = new WebJsonResponse("", MessageResource.GetMessage("option_not_found"), 3000, true);
                                else
                                {
                                    if (LoginUser.SendCode(entityId, drSentTo["value"].ToString(), (Boolean)drSentTo["is_mail"], (Boolean)drSentTo["is_sms"], out err))
                                    {
                                        String html = "";
                                        html += "<form id=\"serviceRecover\" name=\"serviceRecover\" method=\"post\" action=\"/consoleapi/recover3/\">";
                                        html += "<div class=\"login_form\">";
                                        html += "<input type=\"hidden\" name=\"do\" value=\"recover3\" />";
                                        html += "<ul>";
                                        html += "    <li>";
                                        html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("enter_code") + "</p>";
                                        html += "    </li>";
                                        html += "    <li>";
                                        html += "        <span class=\"inputWrap\">";
                                        //html += "			<span id=\"ph_userCode\" class=\"noSel\" style=\"position: absolute; z-index: 1; top: 13px; left: 53px; color: rgb(204, 204, 204); display: block;\">" + MessageResource.GetMessage("code") + "</span>";
                                        html += "			<input type=\"text\" id=\"userCode\" tabindex=\"1\" name=\"userCode\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("code") + "\" onfocus=\"$('#userCode').addClass('focus');\" onblur=\"$('#userCode').removeClass('focus');\" />";
                                        html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#userCode').focus();\"></span>";
                                        html += "        </span>";
                                        html += "    </li>";
                                        html += "    <li>";
                                        html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                                        html += "        <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("confirm_code") + "</button>";
                                        html += "    </li>";
                                        html += "</ul>     ";
                                        html += "</div>";
                                        html += "</form>";
                                        ret = new WebJsonResponse("#recover_container", html);

                                    }
                                    else
                                    {
                                        ret = new WebJsonResponse("", err, 3000, true);
                                    }


                                }
                            }
                            else
                            {
                                ret = new WebJsonResponse("", MessageResource.GetMessage("option_not_found"), 3000, true);
                            }
                        }
                    }
                    else
                    {
                        ret = new WebJsonResponse("", MessageResource.GetMessage("ivalid_session"), 3000, true);
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