using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;

namespace IAM.AuthPlugins.Plugins
{

    public class AuthInternal : AuthBase
    {

        public override String GetPluginName() { return "Local"; }
        public override String GetPluginDescription() { return "Autenticação direta na base de dados do SafeID"; }

        public override Uri GetPluginId()
        {
            return new Uri("auth://iam/plugins/internal");
        }

        public override AuthConfigFields[] GetConfigFields()
        {
            List<AuthConfigFields> conf = new List<AuthConfigFields>();
            /*conf.Add(new AuthConfigFields("Domínio", "domain", "Domínio", AuthConfigTypes.String, true, ""));
            conf.Add(new AuthConfigFields("Usuário", "username", "Usuário", AuthConfigTypes.String, true, ""));
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


        public override LoginResult Auth(IAMDatabase database, System.Web.UI.Page page)
        {
            if (page.Request.HttpMethod == "POST")
            {
                LoginResult res = LocalAuth(database, page, page.Request.Form["username"], page.Request.Form["password"], false);
                if (res.Success)
                {
                    if (res.ChangePassword)
                    {
                        page.Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "login2/changepassword/", false);
                    }
                    else
                    {
                        page.Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/", false);
                    }
                }

                return res;
            }
            else
            {
                page.Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "login2/", false);
                return new LoginResult(true, "Redirect");
            }
        }

        public override void Logout(IAMDatabase database, System.Web.UI.Page page)
        {
            page.Session["login"] = null;

            page.Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/", false);
        }


        public override event AuthEvent Log;
    }
}
