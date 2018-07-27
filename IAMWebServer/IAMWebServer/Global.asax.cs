using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Routing;
using System.Web.Hosting;
using System.Configuration;
using SafeTrend.Data;
using SafeTrend.Data.Update;
using System.DirectoryServices;

namespace IAMWebServer
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            RegisterRoutes(RouteTable.Routes);

            //HostingEnvironment.

            ConnectionStringSettings cs = IAM.GlobalDefs.IAMDatabase.GetWebConnectionStringSettings();
            DbBase db = DbBase.InstanceFromConfig(cs);

            new AutomaticUpdater().Run(db, UpdateScriptRepository.GetScriptsBySqlProviderName(cs.ProviderName));

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

            string[] allowedPOST = { "POST" };
            HttpMethodConstraint methodConstraints2 = new HttpMethodConstraint(allowedPOST);
            RouteValueDictionary vdPOST = new RouteValueDictionary { { "httpMethod", methodConstraints2 } };

            string[] allowedGET = { "GET" };
            HttpMethodConstraint methodConstraints3 = new HttpMethodConstraint(allowedGET);
            RouteValueDictionary vdGET = new RouteValueDictionary { { "httpMethod", methodConstraints3 } };


            /* Métodos GET
            =============================*/

            /* CAS - Central Authentication Service
            -----------------------------*/
            /*
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
            routes.MapPageRoute("cas-recover4", "cas/recover/step4", "~/_cas/recover_st4.aspx", true, null, vd);
            routes.MapPageRoute("cas-passwordstrength", "cas/passwordstrength", "~/_cas/passwordstrength.aspx", true, null, vd);*/


            /* Console Ping
            -----------------------------*/
            routes.MapPageRoute("Ping", "ping/", "~/ping.aspx", true, null, vdGET);

            /* Proxy Sync protocol
            -----------------------------*/
            routes.MapPageRoute("Sync", "proxy/sync/", "~/proxy/sync.aspx", true, null, vdGET);


            /* Métodos de leitura de e-mail
            -----------------------------*/
            routes.MapPageRoute("Mail", "m/", "~/_mail/mail.aspx", true, null, vdGET);
            routes.MapPageRoute("Mail1", "m/{type}/", "~/_mail/mail.aspx", true, new RouteValueDictionary { { "id", "" }, { "type", "" } }, vd);
            routes.MapPageRoute("Mail2", "m/{type}/{id}/", "~/_mail/mail.aspx", true, new RouteValueDictionary { { "id", "" }, { "type", "" } }, vd);


            /* ChartData
            -----------------------------*/
            routes.MapPageRoute("Admin-chartdata-id", "admin/chartdata/{module}/{type}/{id}/", "~/_admin/chartdata.aspx", true, new RouteValueDictionary { { "module", "" }, { "type", "" }, { "id", "" } }, vd);
            routes.MapPageRoute("Admin-chartdata", "admin/chartdata/{module}/{type}/", "~/_admin/chartdata.aspx", true, new RouteValueDictionary { { "module", "" }, { "type", "" } }, vd);


            /* Autoservice
            -----------------------------*/
            routes.MapPageRoute("AutoService", "autoservice/", "~/_autoservice/autoservice.aspx", true, new RouteValueDictionary { { "action", "" }, { "data", "" } }, vdGET);

            /* Admin Dashboard
            -----------------------------*/
            routes.MapPageRoute("Admin-base", "admin/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "area", "dashboard" } }, vdGET);

            /* Admin Web Shell
            -----------------------------*/
            routes.MapPageRoute("Admin-webshell", "admin/ws/", "~/_admin/_ws/ws.aspx", true, new RouteValueDictionary { { "", "" } }, vd);
            

            /* Admin Modules
            -----------------------------*/
            routes.MapPageRoute("Admin-module-all", "admin/{module}/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" } }, vdGET);
            routes.MapPageRoute("Admin-module-new", "admin/{module}/new/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" }, { "filter", "new" } }, vdGET);
            routes.MapPageRoute("Admin-module-new2", "admin/{module}/new/{filter}/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" }, { "filter", "new" } }, vdGET);
            routes.MapPageRoute("Admin-module-new3", "admin/{module}/new/{filter}/{id}/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" }, { "filter", "new" } }, vdGET);
            routes.MapPageRoute("Admin-module-id", "admin/{module}/{id}/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" } }, vdGET);
            routes.MapPageRoute("Admin-module-id-direct", "admin/{module}/{id}/direct/{area}/", "~/_admin/module_direct.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" } }, vdGET);
            routes.MapPageRoute("Admin-module-id-direct-f", "admin/{module}/{id}/direct/{area}/{filter}/", "~/_admin/module_direct.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" } }, vdGET);
            routes.MapPageRoute("Admin-module-id-f", "admin/{module}/{id}/{filter}/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" } }, vdGET);
            
            //Adicionado novo para subfiltro
            routes.MapPageRoute("Admin-module-id-f2", "admin/{module}/{id}/{filter}/{subfilter}/", "~/_admin/template.aspx", true, new RouteValueDictionary { { "module", "" }, { "userid", "" }, { "subfilter", "" } }, vdGET);

            /* Métodos POST
            =============================*/


            /* Autoservice
            -----------------------------*/
            
            //Módulos novos
            //routes.MapPageRoute("Autoservice-module", "autoservice/{module}/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-search", "autoservice/{module}/search/{query}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "area", "search" }, { "query", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-a-new", "autoservice/{module}/new/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "new", "1" }, { "filter", "new" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-a-new2", "autoservice/{module}/new/{filter}/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "new", "1" }, { "filter", "new" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-a-new3", "autoservice/{module}/new/{filter}/{id}/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "new", "1" }, { "filter", "new" } }, vdPOST);
            routes.MapPageRoute("Autoservice-module-action", "autoservice/{module}/action/{action}/", "~/_autoservice/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-id-content", "autoservice/{module}/{id}/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-id-search", "autoservice/{module}/{id}/search/{query}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "search" }, { "query", "" } }, vdPOST);
            routes.MapPageRoute("Autoservice-module-id-action", "autoservice/{module}/{id}/action/{action}/", "~/_autoservice/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" }, { "filter", "" } }, vdPOST);
            routes.MapPageRoute("Autoservice-module-id-action-f", "autoservice/{module}/{id}/action/{action}/{filter}/", "~/_autoservice/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" }, { "filter", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-id-content-f2", "autoservice/{module}/{id}/{filter}/{subfilter}/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "subfilter", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-id-search-f2", "autoservice/{module}/{id}/{filter}/{subfilter}/search/{query}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "search" }, { "query", "" }, { "subfilter", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-id-content-f", "autoservice/{module}/{id}/{filter}/content/{area}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" } }, vdPOST);
            //routes.MapPageRoute("Autoservice-module-id-search-f", "autoservice/{module}/{id}/{filter}/search/{query}/", "~/_autoservice/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "search" }, { "query", "" } }, vdPOST);

            //Modulos antigos, migrar para o novo
            routes.MapPageRoute("AutoService1", "autoservice/content/", "~/_autoservice/autoservice.aspx", true, new RouteValueDictionary { { "action", "" }, { "data", "" } }, vd);
            routes.MapPageRoute("AutoService2", "autoservice/content/{area}", "~/_autoservice/autoservice.aspx", true, new RouteValueDictionary { { "action", "" }, { "data", "" } }, vd);
            routes.MapPageRoute("AutoServiceUser", "autoservice/user/{action}/{step}/", "~/_autoservice/user.aspx", true, new RouteValueDictionary { { "action", "" }, { "step", "" } }, vd);
            routes.MapPageRoute("AutoServiceUser2", "autoservice/user/{action}/content/{area}", "~/_autoservice/user.aspx", true, new RouteValueDictionary { { "action", "" }, { "step", "" } }, vd);
            routes.MapPageRoute("AutoServiceAccessRequest-new", "autoservice/access_request/new/", "~/_autoservice/access_request.aspx", true, new RouteValueDictionary { { "action", "new" }, { "id", "" } }, vd);
            routes.MapPageRoute("AutoServiceAccessRequest2", "autoservice/access_request/{id}/{action}/", "~/_autoservice/access_request.aspx", true, new RouteValueDictionary { { "action", "" }, { "id", "" } }, vd);
            routes.MapPageRoute("AutoServiceAccessRequest3", "autoservice/access_request/{action}/content/{area}", "~/_autoservice/access_request.aspx", true, new RouteValueDictionary { { "action", "" }, { "step", "" } }, vd);


            /* Admin Dashboard
            -----------------------------*/
            routes.MapPageRoute("Admin-dasboard", "admin/content/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "dashboard" }, { "area", "" } }, vdPOST);
            routes.MapPageRoute("Admin-dasboard2", "admin/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "dashboard" }, { "area", "" } }, vdPOST);
            routes.MapPageRoute("Admin-dasboard-search", "admin/search/{query}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "users" }, { "area", "search" }, { "query", "" } }, vdPOST);

            /* Admin Modules
            -----------------------------*/
            routes.MapPageRoute("Admin-module", "admin/{module}/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { {"module", ""}, { "id", "" }, { "area", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-search", "admin/{module}/search/{query}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "area", "search" }, { "query", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-a-new", "admin/{module}/new/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "new", "1" }, { "filter", "new" } }, vdPOST);
            routes.MapPageRoute("Admin-module-a-new2", "admin/{module}/new/{filter}/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "new", "1" }, { "filter", "new" } }, vdPOST);
            routes.MapPageRoute("Admin-module-a-new3", "admin/{module}/new/{filter}/{id}/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "new", "1" }, { "filter", "new" } }, vdPOST);
            routes.MapPageRoute("Admin-module-action", "admin/{module}/action/{action}/", "~/_admin/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-content", "admin/{module}/{id}/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-search", "admin/{module}/{id}/search/{query}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "search" }, { "query", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-action", "admin/{module}/{id}/action/{action}/", "~/_admin/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" }, { "filter", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-action-f", "admin/{module}/{id}/action/{action}/{filter}/", "~/_admin/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" }, { "filter", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-content-f2", "admin/{module}/{id}/{filter}/{subfilter}/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "subfilter", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-search-f2", "admin/{module}/{id}/{filter}/{subfilter}/search/{query}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "search" }, { "query", "" }, { "subfilter", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-content-f", "admin/{module}/{id}/{filter}/content/{area}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" } }, vdPOST);
            routes.MapPageRoute("Admin-module-id-search-f", "admin/{module}/{id}/{filter}/search/{query}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "search" }, { "query", "" } }, vdPOST);

            //routes.MapPageRoute("Admin-module-action-f", "admin/{module}/action/{action}/{filter}/", "~/_admin/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" }, { "filter", "" } }, vdPOST);
            //routes.MapPageRoute("Admin-module-id-action-c", "admin/{module}/{id}/action/{action}/content/{area}/", "~/_admin/module_action.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" } }, vdPOST);
            //routes.MapPageRoute("Admin-module-id-action-s", "admin/{module}/{id}/action/{action}/search/{query}/", "~/_admin/module_content.aspx", true, new RouteValueDictionary { { "module", "" }, { "id", "" }, { "area", "" }, { "action", "" } }, vdPOST);

            /* Old, Migrar
            =============================*/

            routes.MapPageRoute("AutoServiceApi2", "consoleapi/{do}/{content}", "~/consoleapi/api.aspx", true, new RouteValueDictionary { { "do", "" }, { "content", "" } }, vd);
            
            //Auto serviço
            

            //Passos de login

            routes.MapPageRoute("Login", "login/", "~/_login/login.aspx", true, null, vd);
            routes.MapPageRoute("Logout", "logout/", "~/_login/logout.aspx", true, null, vd);

            routes.MapPageRoute("Login2", "login2/", "~/_login2/login.aspx", true, null, vd);
            routes.MapPageRoute("Login2-changepwd", "login2/changepassword/", "~/_login2/ChangePassword.aspx", true, null, vd);
            routes.MapPageRoute("Login2-recover", "login2/recover", "~/_login2/recover.aspx", true, null, vd);
            routes.MapPageRoute("Login2-recover1", "login2/recover/step1", "~/_login2/recover_st1.aspx", true, null, vd);
            routes.MapPageRoute("Login2-recover2", "login2/recover/step2", "~/_login2/recover_st2.aspx", true, null, vd);
            routes.MapPageRoute("Login2-recover3", "login2/recover/step3", "~/_login2/recover_st3.aspx", true, null, vd);
            routes.MapPageRoute("Login2-recover4", "login2/passwordchanged", "~/_login2/PasswordChanged.aspx", true, null, vd);

            //routes.MapPageRoute("ChangePwd", "login/changepassword/", "~/_login/changepwd.aspx", true, null, vd);
            //routes.MapPageRoute("LoginRecover", "recover/", "~/_login/recover.aspx", true, new RouteValueDictionary { { "do", "" } }, vd);
            //routes.MapPageRoute("Login-cont", "login/{action}/", "~/_autoservice/empty.aspx", true, null, vd);
            //routes.MapPageRoute("Login-cont2", "login/{action}/{data}", "~/_autoservice/empty.aspx", true, null, vd);
            //routes.MapPageRoute("Recover", "recover/{action}/", "~/_autoservice/empty.aspx", true, null, vd);
            //routes.MapPageRoute("Recover2", "recover/{action}/{data}", "~/_autoservice/empty.aspx", true, null, vd);

            //Admin
            /*
            routes.MapPageRoute("AdminUsers", "admin/users/{action}/{data}/{data2}/{data3}/", "~/_admin/users/users.aspx", true, new RouteValueDictionary { { "action", "" }, { "data", "" }, { "data2", "" }, { "data3", "" } }, vd);

            routes.MapPageRoute("adminSearch2", "admin/user/{userid}/search/{data}/", "~/_admin/users/users.aspx", true, new RouteValueDictionary { { "action", "search" }, { "data", "" }, { "data2", "" }, { "data3", "" } }, vd);
            routes.MapPageRoute("adminSearch3", "admin/user/{userid}/{data2}/search/{data}/", "~/_admin/users/users.aspx", true, new RouteValueDictionary { { "action", "search" }, { "data", "" }, { "data2", "" }, { "data3", "" } }, vd);

            
            routes.MapPageRoute("AdminUser", "admin/user/{userid}/{action}/", "~/_admin/users/user.aspx", true, new RouteValueDictionary { { "action", "" }, { "userid", "" }, { "data2", "" }, { "data3", "" } }, vd);
            routes.MapPageRoute("AdminUser2", "admin/user/{userid}/{data2}/{action}/", "~/_admin/users/user.aspx", true, new RouteValueDictionary { { "action", "" }, { "userid", "" }, { "data2", "" } }, vd);
            */
            //Admin User

            //Admin mobile (old)
            /*
            routes.MapPageRoute("adminMobile", "admin/mobile/", "~/_admin/_mobile/mobile.aspx", true, null, vd);
            routes.MapPageRoute("adminMobileList", "admin/mobile/list/", "~/_admin/_mobile/list.aspx", true, null, vd);
            routes.MapPageRoute("adminMobileSearch", "admin/mobile/search/{query}/{ts}/", "~/_admin/_mobile/search.aspx", true, new RouteValueDictionary { { "query", "" } }, vd);
            routes.MapPageRoute("adminMobileUser", "admin/mobile/user/{userid}/", "~/_admin/_mobile/user.aspx", true, new RouteValueDictionary { { "userid", "0" } }, vd);
            routes.MapPageRoute("adminMobileUserLogs", "admin/mobile/user/{userid}/logs/", "~/_admin/_mobile/userlogs.aspx", true, new RouteValueDictionary { { "userid", "0" } }, vd);
            routes.MapPageRoute("adminMobileUserResetPwd", "admin/mobile/user/{userid}/resetpwd/", "~/_admin/_mobile/userresetpwd.aspx", true, new RouteValueDictionary { { "userid", "0" } }, vd);
            routes.MapPageRoute("adminMobileUserUnlock", "admin/mobile/user/{userid}/unlock/", "~/_admin/_mobile/userlock.aspx", true, new RouteValueDictionary { { "userid", "0" }, { "op", "unlock" } }, vd);
            routes.MapPageRoute("adminMobileUserlock", "admin/mobile/user/{userid}/lock/", "~/_admin/_mobile/userlock.aspx", true, new RouteValueDictionary { { "userid", "0" }, { "op", "lock" } }, vd);
            routes.MapPageRoute("adminMobileUserdeploy", "admin/mobile/user/{userid}/deploy/", "~/_admin/_mobile/userdeploy.aspx", true, new RouteValueDictionary { { "userid", "0" }, { "op", "lock" } }, vd);
            */
            //routes.MapPageRoute("AutoServiceLoginRecover", "autoservice/login/recover/", "~/autoservice/recover.aspx", true, new RouteValueDictionary { { "do", "" } }, vd);
            
        }

    }
}