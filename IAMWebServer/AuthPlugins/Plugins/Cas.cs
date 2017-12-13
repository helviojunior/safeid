using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;
using IAM.Config;
using System.Xml;
using System.Net;
using System.Web;

namespace IAM.AuthPlugins.Plugins
{

    public class AuthCAS : AuthBase
    {

        public override String GetPluginName() { return "CAS"; }
        public override String GetPluginDescription() { return "Autenticação através de um serviço CAS"; }

        public override Uri GetPluginId()
        {
            return new Uri("auth://iam/plugins/cas");
        }

        public override AuthConfigFields[] GetConfigFields()
        {
            List<AuthConfigFields> conf = new List<AuthConfigFields>();
            conf.Add(new AuthConfigFields("URL Cas", "uri", "URL de acesso do serviço CAS", AuthConfigTypes.Uri, true, ""));
            /*conf.Add(new AuthConfigFields("Usuário", "username", "Usuário", AuthConfigTypes.String, true, ""));
            conf.Add(new AuthConfigFields("Senha", "password", "Senha", AuthConfigTypes.Password, true, ""));
            conf.Add(new AuthConfigFields("Domínio de e-mail", "mail_domain", "Domínio de e-mail", AuthConfigTypes.String, false, ""));*/

            return conf.ToArray();
        }

        public override Boolean ValidateConfigFields(Dictionary<String, Object> config, AuthEvent Log)
        {

            AuthEvent iLog = new AuthEvent(delegate(Object sender, AuthEventType type, string text)
            {
                if (Log != null)
                    Log(sender, type, text);
            });

            if (!CheckInputConfig(config, iLog))
                return false;

            //Verifica as informações próprias deste plugin
            return true;
        }

        public override void Logout(IAMDatabase database, System.Web.UI.Page page)
        {
            page.Session["login"] = null;

            Dictionary<String, Object> config = GetAuthConfig(database, page);

            if (!CheckInputConfig(config, Log))
                return;

            String cas_service = config["uri"].ToString();
            String service = HttpUtility.UrlEncode(page.Request.Url.Scheme + "://" + page.Request.Url.Host + (page.Request.Url.Port != 80 ? ":" + page.Request.Url.Port : "") + "/login/");

            page.Response.Redirect(cas_service.TrimEnd("/".ToCharArray()) + "/logout/?service=" + service, false);

            //page.Response.Redirect(cas_service.TrimEnd("/".ToCharArray()) + "/login/?service=" + service);
        }


        public override LoginResult Auth(IAMDatabase database, System.Web.UI.Page page)
        {

            Dictionary<String, Object> config = GetAuthConfig(database, page);

            if (!CheckInputConfig(config, Log))
                return new LoginResult(false, "Invalid config");

            String cas_service = config["uri"].ToString();

            String ticket = (!String.IsNullOrEmpty(page.Request.QueryString["ticket"]) ? page.Request.QueryString["ticket"].ToString() : "");
            String service = HttpUtility.UrlEncode(page.Request.Url.Scheme + "://" + page.Request.Url.Host + (page.Request.Url.Port != 80 ? ":" + page.Request.Url.Port : "") + "/login/");

            //String tst = page.Request.Url.AbsoluteUri;

            if (ticket != "")
            {

                page.Session["login"] = null;

                //Verifica o ticket
                using (ServerDBConfig conf = new ServerDBConfig(database.Connection, true))
                {
                    String result = null;
                    try
                    {
                        WebClient client = new WebClient();
                        Uri req = new Uri(cas_service.TrimEnd("/".ToCharArray()) + "/serviceValidate/?service=" + service + "&ticket=" + ticket);
                        result = client.DownloadString(req);

                    }
                    catch { }

                    if (!String.IsNullOrEmpty(result))
                    {

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml("<?xml version=\"1.0\"?>" + result);

                        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
                        namespaceManager.AddNamespace("cas", "http://www.yale.edu/tp/cas");

                        XmlNode failNode = doc.SelectSingleNode("/cas:serviceResponse/cas:authenticationFailure", namespaceManager);
                        XmlNode successNode = doc.SelectSingleNode("/cas:serviceResponse/cas:authenticationSuccess", namespaceManager);

                        if ((failNode == null) && (successNode != null))
                        {
                            XmlNode user = doc.SelectSingleNode("/cas:serviceResponse/cas:authenticationSuccess/cas:user", namespaceManager);

                            LoginResult login = LocalAuth(database, page, user.ChildNodes[0].Value, "", true);
                            if (login.Success)
                            {
                                if (page.Session["last_page"] != null)
                                {
                                    page.Response.Redirect(page.Session["last_page"].ToString(), false);
                                    page.Session["last_page"] = null;
                                }
                                else
                                    page.Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/", false);
                            }
                            else
                                page.Response.Redirect(cas_service.TrimEnd("/".ToCharArray()) + "/login/?renew=true&service=" + service, false);

                            return login;
                        }
                        else
                        {
                            page.Response.Redirect(cas_service.TrimEnd("/".ToCharArray()) + "/login/?renew=true&service=" + service, false);
                            return new LoginResult(false, "XML Error");
                        }

                    }
                    else
                    {
                        page.Response.Redirect(cas_service.TrimEnd("/".ToCharArray()) + "/login/?renew=true&service=" + service, false);
                        return new LoginResult(false, "CAS Result is empry");
                    }

                }
            }
            else
            {

                page.Response.Redirect(cas_service.TrimEnd("/".ToCharArray()) + "/login/?service=" + service, false);
                return new LoginResult(false, "Ticket is empty");
            }
        }


        public override event AuthEvent Log;
    }
}
