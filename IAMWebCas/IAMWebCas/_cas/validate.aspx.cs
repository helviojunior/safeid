using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using CAS.PluginInterface;
using CAS.Web;
using System.Configuration;
using SafeTrend.Data;

namespace IAMWebServer._cas
{
    public partial class validate : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            /*
            2.4. /validate [CAS 1.0]
            /validate checks the validity of a service ticket. /validate is part of the CAS 1.0 protocol and thus does not handle proxy authentication. CAS MUST respond with a ticket validation failure response when a proxy ticket is passed to /validate.
            2.4.1. parameters
            The following HTTP request parameters MAY be specified to /validate. They are case sensitive and MUST all be handled by /validate.
            service [REQUIRED] - the identifier of the service for which the ticket was issued, as discussed in Section 2.2.1. As a HTTP request parameter, the "service" value MUST be URL-encoded as described in Section 2.2 of RFC 1738 [4].
            ticket [REQUIRED] - the service ticket issued by /login. Service tickets are described in Section 3.1.
            renew [OPTIONAL] - if this parameter is set, ticket validation will only succeed if the service ticket was issued from the presentation of the user's primary credentials. It will fail if the ticket was issued from a single sign-on session.

            2.4.2. response
            /validate will return one of the following two responses:

            On ticket validation success:
            yes<LF>

            On ticket validation failure:
            no<LF>

            2.4.3. URL examples of /validate
            Simple validation attempt:
            https://cas.example.org/cas/validate?service=http%3A%2F%2Fwww.example.org%2Fservice&ticket=ST-1856339-aA5Yuvrxzpv8Tau1cYQ7

            Ensure service ticket was issued by presentation of primary credentials:
            https://cas.example.org/cas/validate?service=http%3A%2F%2Fwww.example.org%2Fservice&ticket=ST-1856339-aA5Yuvrxzpv8Tau1cYQ7&renew=true

            */
            
            Boolean renew = (!String.IsNullOrEmpty(Request["renew"]) && (Request["renew"].ToString().ToLower() == "true"));
            String ticket = (!String.IsNullOrEmpty(Request.QueryString["ticket"]) ? Request.QueryString["ticket"].ToString() : "");

            Page.Response.ContentType = "text/plain; charset=UTF-8";
            Page.Response.ContentEncoding = Encoding.UTF8;


            Uri svc = null;
            try
            {
                svc = new Uri(Request.QueryString["service"]);
            }
            catch { }

            using (DbBase db = DbBase.InstanceFromConfig(ConfigurationManager.ConnectionStrings["CASDatabase"]))
            {
                CASConnectorBase connector = CASUtils.GetService(db, this, svc);

                if ((connector == null) || (connector is EmptyPlugin))
                {
                    //Ticket não informado
                    Response.Write("no\n");
                }
                else if (connector.Grant(ticket, renew, false).Success)
                {
                    Response.Write("yes\n");
                }
                else
                {
                    Response.Write("no\n");
                }
            }
            Page.Response.Status = "200 OK";
            Page.Response.StatusCode = 200;
            //Page.Response.OutputStream.Write(bRet, 0, bRet.Length);
            //Page.Response.OutputStream.Flush();
        }
    }
}