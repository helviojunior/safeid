using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

using CAS.Web;
using CAS.PluginInterface;
using System.Configuration;
using SafeTrend.Data;

namespace IAMWebServer._cas
{
    public partial class service_validate : System.Web.UI.Page
    {
        private enum retCode
        {
            INVALID_REQUEST,
            INVALID_TICKET_SPEC,
            UNAUTHORIZED_SERVICE_PROXY,
            INVALID_PROXY_CALLBACK,
            INVALID_TICKET,
            INVALID_SERVICE,
            INTERNAL_ERROR
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            /*2.5. /serviceValidate [CAS 2.0]

            /serviceValidate checks the validity of a service ticket and returns an XML-fragment response. /serviceValidate MUST also generate and issue proxy-granting tickets when requested. /serviceValidate MUST NOT return a successful authentication if it receives a proxy ticket. It is RECOMMENDED that if /serviceValidate receives a proxy ticket, the error message in the XML response SHOULD explain that validation failed because a proxy ticket was passed to /serviceValidate.
            
            2.5.1. parameters
            The following HTTP request parameters MAY be specified to /serviceValidate. They are case sensitive and MUST all be handled by /serviceValidate.
            service [REQUIRED] - the identifier of the service for which the ticket was issued, as discussed in Section 2.2.1. As a HTTP request parameter, the "service" value MUST be URL-encoded as described in Section 2.2 of RFC 1738 [4].
            ticket [REQUIRED] - the service ticket issued by /login. Service tickets are described in Section 3.1.
            pgtUrl [OPTIONAL] - the URL of the proxy callback. Discussed in Section 2.5.4. As a HTTP request parameter, the "pgtUrl" value MUST be URL-encoded as described in Section 2.2 of RFC 1738 [4].
            renew [OPTIONAL] - if this parameter is set, ticket validation will only succeed if the service ticket was issued from the presentation of the user's primary credentials. It will fail if the ticket was issued from a single sign-on session.

            2.5.2. response
            /serviceValidate will return an XML-formatted CAS serviceResponse as described in the XML schema in Appendix A. Below are example responses:

            On ticket validation success:
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
             <cas:authenticationSuccess>
              <cas:user>username</cas:user>
              <cas:proxyGrantingTicket>PGTIOU-84678-8a9d...</cas:proxyGrantingTicket>
             </cas:authenticationSuccess>
            </cas:serviceResponse>

            On ticket validation failure:
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
             <cas:authenticationFailure code="INVALID_TICKET">
                Ticket ST-1856339-aA5Yuvrxzpv8Tau1cYQ7 not recognized`
              </cas:authenticationFailure>
            </cas:serviceResponse>
            
            For proxy responses, see section 2.6.2.

            2.5.3. error codes
            The following values MAY be used as the "code" attribute of authentication failure responses. The following is the minimum set of error codes that all CAS servers MUST implement. Implementations MAY include others.
            INVALID_REQUEST - not all of the required request parameters were present
            INVALID_TICKET_SPEC - failure to meet the requirements of validation specification
            UNAUTHORIZED_SERVICE_PROXY - the service is not authorized to perform proxy authentication
            INVALID_PROXY_CALLBACK - The proxy callback specified is invalid. The credentials specified for proxy authentication do not meet the security requirements
            INVALID_TICKET - the ticket provided was not valid, or the ticket did not come from an initial login and "renew" was set on validation. The body of the <cas:authenticationFailure> block of the XML response SHOULD describe the exact details.
            INVALID_SERVICE - the ticket provided was valid, but the service specified did not match the service associated with the ticket. CAS MUST invalidate the ticket and disallow future validation of that same ticket.
            INTERNAL_ERROR - an internal error occurred during ticket validation

            For all error codes, it is RECOMMENDED that CAS provide a more detailed message as the body of the <cas:authenticationFailure> block of the XML response.
             */

            Boolean renew = (!String.IsNullOrEmpty(Request["renew"]) && (Request["renew"].ToString().ToLower() == "true"));
            String ticket = (!String.IsNullOrEmpty(Request.QueryString["ticket"]) ? Request.QueryString["ticket"].ToString() : "");

            Page.Response.ContentType = "application/xml; charset=UTF-8";
            Page.Response.ContentEncoding = Encoding.UTF8;

            try
            {


                Uri svc = null;
                try
                {
                    svc = new Uri(Request.QueryString["service"]);
                }
                catch { }

                using (DbBase db = DbBase.InstanceFromConfig(ConfigurationManager.ConnectionStrings["CASDatabase"]))
                {
                    CASConnectorBase connector = CASUtils.GetService(db, this, svc);

                    if (svc == null)
                    {
                        //Serviço não informado ou não encontrado
                        Response.Write(getError(retCode.INVALID_REQUEST, "Service"));
                    }
                    else if ((connector == null) || (connector is EmptyPlugin))
                    {
                        //Serviço não informado ou não encontrado
                        Response.Write(getError(retCode.INVALID_SERVICE, svc.AbsoluteUri));
                    }
                    else if (ticket == "")
                    {
                        //Ticket não informado
                        Response.Write(getError(retCode.INVALID_REQUEST, "Ticket"));
                    }
                    else
                    {


                        CASTicketResult loginRes = connector.Grant(ticket, renew, false);
                        if (loginRes.Success)
                        {
                            StringBuilder xml = new StringBuilder();

                            xml.AppendLine("<cas:serviceResponse xmlns:cas=\"http://www.yale.edu/tp/cas\">");
                            xml.AppendLine("  <cas:authenticationSuccess>");
                            xml.AppendLine("    <cas:user>" + loginRes.UserName + "</cas:user>");
                            if ((loginRes.Attributes != null) && (loginRes.Attributes.Count > 0))
                            {
                                xml.AppendLine("    <cas:attributes>");
                                foreach (String key in loginRes.Attributes.Keys)
                                    xml.AppendLine("        <cas:" + key + ">" + loginRes.Attributes[key] + "</cas:" + key + ">");

                                xml.AppendLine("    </cas:attributes>");
                            }
                            xml.AppendLine("    <cas:proxyGrantingTicket>" + ticket + "</cas:proxyGrantingTicket>");
                            xml.AppendLine("  </cas:authenticationSuccess>");
                            xml.AppendLine("</cas:serviceResponse>");

                            Response.Write(xml.ToString());
                        }
                        else
                        {
                            Response.Write(getError(retCode.INVALID_TICKET, ticket));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                getError(retCode.INTERNAL_ERROR, "");
            }

            Page.Response.Status = "200 OK";
            Page.Response.StatusCode = 200;
            //Page.Response.OutputStream.Write(bRet, 0, bRet.Length);
            //Page.Response.OutputStream.Flush();
        }

        private String getError(retCode type, String text)
        {
            StringBuilder xml = new StringBuilder();

            xml.AppendLine("<cas:serviceResponse xmlns:cas=\"http://www.yale.edu/tp/cas\">");
            xml.AppendLine("  <cas:authenticationFailure code=\"" + type.ToString() + "\">");
            
            switch (type)
            {
                case retCode.INVALID_TICKET:
                    xml.AppendLine("    Ticket " + text + " not recognized");
                    break;

                case retCode.INVALID_SERVICE:
                    xml.AppendLine("    Service " + text + " not recognized");
                    break;

                case retCode.INVALID_REQUEST:
                    xml.AppendLine("    Mandatory field '" + text + "' not supplied");
                    break;
            }
            
            xml.AppendLine("  </cas:authenticationFailure>");
            xml.AppendLine("</cas:serviceResponse>");
            

            return xml.ToString();
        }
    }
}