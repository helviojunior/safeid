using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CAS.Web;
using CAS.PluginInterface;
using System.Configuration;
using SafeTrend.Data;

namespace IAMWebServer._cas
{
    public partial class logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            String html = "";
            String error = "";

            html += "<form id=\"serviceLogin\" name=\"serviceLogin\" method=\"post\" action=\"/cas/login/?" + Request.QueryString + "\"><div class=\"login_form\">";


            Uri svc = null;
            try
            {
                svc = new Uri(Request.QueryString["service"]);
            }
            catch { }

            using (DbBase db = DbBase.InstanceFromConfig(ConfigurationManager.ConnectionStrings["CASDatabase"]))
            {

                CASConnectorBase connector = CASUtils.GetService(db, this, null);

                if (connector == null)//Nunca deve ser nulo, em caso de não encontrado deve retornar um Emptylugin
                {
                    //Serviço não informado ou não encontrado
                    html += "    <ul>";
                    html += "        <li><div class=\"error-box\">" + MessageResource.GetMessage("service_invalid_uri") + "</div>";
                    html += "    </ul>";
                }
                else
                {

                    String ticket = (!String.IsNullOrEmpty(Request.QueryString["ticket"]) ? Request.QueryString["ticket"].ToString() : "");
                    connector.DestroyTicket(ticket, null);

                    HttpCookie tgc = Request.Cookies["TGC-SafeID"];
                    if (tgc != null)
                        connector.DestroyTicket(tgc);

                    try
                    {
                        Response.Cookies.Remove("TGC-SafeID");
                        Response.Cookies.Remove("TGT-SafeID");
                    }
                    catch { }

                    try
                    {
                        //Adiciona o cookie do TGC
                        HttpCookie cookie = new HttpCookie("TGC-SafeID");
                        //cookie.Domain = page.Request.Url.Host;
                        cookie.Path = "/cas";
                        cookie.Value = "none";

                        cookie.Expires = DateTime.Now.AddDays(-30);

                        //Adiciona o cookie
                        Response.Cookies.Add(cookie);
                    }
                    catch { }

                    try
                    {
                        //Adiciona o cookie do TGC
                        HttpCookie cookie = new HttpCookie("TGT-SafeID");
                        //cookie.Domain = page.Request.Url.Host;
                        cookie.Path = "/cas";
                        cookie.Value = "none";

                        cookie.Expires = DateTime.Now.AddDays(-30);

                        //Adiciona o cookie
                        Response.Cookies.Add(cookie);
                    }
                    catch { }


                    error = MessageResource.GetMessage("logout_text");
                    String url = (!String.IsNullOrEmpty(Request.QueryString["url"]) ? Request.QueryString["url"].ToString() : "");
                    try
                    {
                        Uri tmp = new Uri(url);
                        error = "<a href=\"" + tmp.AbsoluteUri + "\">" + String.Format(MessageResource.GetMessage("logout_text_url"), tmp.AbsoluteUri) + "</a>";
                    }
                    catch { }

                    if (String.IsNullOrEmpty(url) && svc != null)
                    {
                        Response.Redirect(svc.AbsoluteUri, false);
                        return;
                    }

                    html += "    <ul>";
                    if (error != "")
                        html += "        <li><div class=\"error-box\">" + error + "</div>";
                    html += "        </li>";
                    html += "    </ul>     ";
                }

                html += "</div></form>";

            }
            holderContent.Controls.Add(new LiteralControl(html));

        }
    }
}