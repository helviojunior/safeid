using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Routing;
using System.Web.Hosting;
using System.IO;
using CAS.PluginManager;
using System.Configuration;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;
using SafeTrend.Data.Update;

namespace IAMWebServer
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            RegisterRoutes(RouteTable.Routes);
            
            ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings["CASDatabase"];
            DbBase db = DbBase.InstanceFromConfig(cs);

            new AutomaticUpdater().Run(db, UpdateScriptRepository.GetScriptsBySqlProviderName(cs));
            new ServiceSynchronizer().Run(db, Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data/config"));
            //Application["plugins"] = CASPlugins.GetPlugins2(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data/config"), Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data/plugins"));
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            Session["ApplicationVirtualPath"] = HostingEnvironment.ApplicationVirtualPath;
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        void RegisterRoutes(RouteCollection routes)
        {
            string[] allowedMethods = { "GET", "POST" };
            HttpMethodConstraint methodConstraints = new HttpMethodConstraint(allowedMethods);
            RouteValueDictionary vd = new RouteValueDictionary { { "httpMethod", methodConstraints } };

            string[] allowedGET = { "GET" };
            HttpMethodConstraint methodConstraints3 = new HttpMethodConstraint(allowedGET);
            RouteValueDictionary vdGET = new RouteValueDictionary { { "httpMethod", methodConstraints3 } };


            /* Métodos GET
            =============================*/

            /* CAS - Central Authentication Service
            -----------------------------*/
            routes.MapPageRoute("cas-default", "cas", "~/_cas/default.aspx", true, null, vd); //Aceita GET e POST
            routes.MapPageRoute("cas-login", "cas/login", "~/_cas/login.aspx", true, null, vd); //Aceita GET e POST
            routes.MapPageRoute("cas-logout", "cas/logout", "~/_cas/logout.aspx", true, null, vdGET);
            routes.MapPageRoute("cas-validate", "cas/validate", "~/_cas/validate.aspx", true, null, vdGET);
            routes.MapPageRoute("cas-serviceValidate", "cas/serviceValidate", "~/_cas/service_validate.aspx", true, null, vdGET);
            routes.MapPageRoute("cas-proxyValidate", "cas/proxyValidate", "~/_cas/proxy_validate.aspx", true, null, vdGET);
            routes.MapPageRoute("cas-proxy", "cas/proxy", "~/_cas/proxy.aspx", true, null, vdGET);
            routes.MapPageRoute("cas-recover", "cas/recover", "~/_cas/recover.aspx", true, null, vd);
            routes.MapPageRoute("cas-recover1", "cas/recover/step1", "~/_cas/recover_st1.aspx", true, null, vd);
            routes.MapPageRoute("cas-recover2", "cas/recover/step2", "~/_cas/recover_st2.aspx", true, null, vd);
            routes.MapPageRoute("cas-recover3", "cas/recover/step3", "~/_cas/recover_st3.aspx", true, null, vd);
            routes.MapPageRoute("cas-recover4", "cas/passwordchanged", "~/_cas/passwordchanged.aspx", true, null, vd);
            routes.MapPageRoute("cas-passwordstrength", "cas/passwordstrength", "~/_cas/passwordstrength.aspx", true, null, vd);
            routes.MapPageRoute("cas-changepassword", "cas/changepassword", "~/_cas/changepassword.aspx", true, null, vd);
        }
    }
}