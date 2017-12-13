using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using System.Data;

namespace IAMWebServer._login2
{
    public partial class recover_st2 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            String html = "";
            String error = "";

            html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\"" + Session["ApplicationVirtualPath"] + "login2/recover/step2/\"><div class=\"login_form\">";

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
            else
            {

                Int64 entityId = (Int64)Session["user_info"];

                String err = "";

                if (Request.HttpMethod == "POST")
                {
                    String userCode = Request["userCode"];
                    if ((userCode == null) || (userCode == ""))
                    {
                        error = MessageResource.GetMessage("type_code");
                    }
                    else
                    {
                        
                        if (entityId > 0)
                        {
                            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            {
                                DataTable c = db.Select("select * from entity where deleted = 0 and id = " + entityId + " and recovery_code = '" + Tools.Tool.TrataInjection(userCode) + "'");
                                if ((c != null) && (c.Rows.Count > 0))
                                {
                                    Session["userCode"] = c.Rows[0]["recovery_code"].ToString();

                                    Response.Redirect(Session["ApplicationVirtualPath"] + "login2/recover/step3/", false);
                                    return;

                                }
                                else
                                {
                                    error = MessageResource.GetMessage("invalid_code");
                                }
                            }
                        }
                        else
                        {
                            error = MessageResource.GetMessage("invalid_session");
                        }
                    }
                }

                html += "<ul>";
                html += "    <li>";
                html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("enter_code") + "</p>";
                html += "    </li>";
                html += "    <li>";
                html += "        <span class=\"inputWrap\">";
                html += "			<input type=\"text\" id=\"userCode\" tabindex=\"1\" name=\"userCode\" value=\"\" style=\"\" placeholder=\"" + MessageResource.GetMessage("code") + "\" onfocus=\"$('#userCode').addClass('focus');\" onblur=\"$('#userCode').removeClass('focus');\" />";
                html += "			<span id=\"ph_passwordIcon\" onclick=\"$('#userCode').focus();\"></span>";
                html += "        </span>";
                html += "    </li>";


                if (error != "")
                {
                    html += "    <ul>";
                    html += "        <li><div class=\"error-box\">" + error + "</div>";
                    html += "    </ul>";
                }


                html += "    <li>";
                html += "        <span class=\"forgot\"> <a href=\"/\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                html += "        <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("confirm_code") + "</button>";
                html += "    </li>";
                html += "</ul>     ";

            }

            html += "</div></form>";

            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}