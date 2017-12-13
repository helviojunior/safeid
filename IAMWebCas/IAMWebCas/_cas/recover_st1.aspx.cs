﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using CAS.Web;
using CAS.PluginInterface;

namespace IAMWebServer._cas
{
    public partial class recover_st1 : System.Web.UI.Page
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

                if (Request.HttpMethod == "POST")
                {
                    
                    try
                    {
                        String sentTo = Request["sentTo"];
                        if ((sentTo == null) || (sentTo == ""))
                        {
                            error = MessageResource.GetMessage("select_option");
                        }
                        else
                        {

                            if ((userInfo.Emails != null) && (userInfo.Emails.Count > 0))
                            {
                                String emlSentTo = null;
                                foreach (String eml in userInfo.Emails)
                                {
                                    String data = Tools.Tool.MaskData(eml, true, false);
                                    if (sentTo.ToString().ToLower() == data)
                                    {
                                        emlSentTo = eml;
                                        break;
                                    }
                                }

                                if (emlSentTo == null)
                                    error = MessageResource.GetMessage("option_not_found");
                                else
                                {
                                    Tools.Tool.sendEmail("Password recover code", emlSentTo, "Code: " + userInfo.RecoveryCode, false);

                                    Response.Redirect("/cas/recover/step2/", false);
                                    return;

                                }

                            }
                            else
                            {
                                error = MessageResource.GetMessage("option_not_found");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Tools.Tool.notifyException(ex);
                        error = MessageResource.GetMessage("internal_error");
                    }

                }

                html += "    <ul>";


                if ((userInfo.Emails != null) && (userInfo.Emails.Count > 0))
                {

                    html += "    <li>";
                    html += "        <p style=\"width:100%;padding:0 0 5px 0;color:#000;\">" + MessageResource.GetMessage("send_conf_to") + "</p>";
                    html += "    </li>";

                    foreach (String eml in userInfo.Emails)
                    {
                        String data = Tools.Tool.MaskData(eml, true, false);
                        if (data != "")
                            html += "    <li><p style=\"width:400px;padding:0 0 5px 10px;color:#000;\"><input name=\"sentTo\" type=\"radio\" value=\"" + data + "\">" + data + "</p></li>";
                    }

                }
                else
                {
                    error = "No method available";
                }

                if (error != "")
                    html += "        <li><div class=\"error-box\">" + error + "</div>";

                html += "        <li>";
                html += "            <span class=\"forgot\"> <a href=\"" + userInfo.Service.AbsoluteUri + "\">" + MessageResource.GetMessage("cancel") + "</a> " + MessageResource.GetMessage("or") + " </span>";
                html += "            <button tabindex=\"4\" id=\"submitBtn\" class=\"action button floatright\">" + MessageResource.GetMessage("login_recover_btn_recover") + "</button>";
                html += "        </li>";
                html += "    </ul>     ";

            }

            html += "</div>";
            html += "</form>";
            html += "</div>";
            
            holderContent.Controls.Add(new LiteralControl(html));
        }
    }
}