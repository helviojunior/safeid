using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.Config;
using IAM.AuthPlugins;

namespace IAMWebServer.login
{
    public partial class logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Abandon();

            try
            {
                AuthBase authPlugin = null;
                try
                {
                    authPlugin = AuthBase.GetPlugin(new Uri(((EnterpriseData)Session["enterprise_data"]).AuthPlugin));
                }
                catch { }

                if (authPlugin == null)
                {
                    Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/", false);
                    return;
                }
                else
                {

                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        authPlugin.Logout(db, this);
                }
            }
            catch (Exception ex)
            {
                Tools.Tool.notifyException(ex, this);
                throw ex;
            }

            /*
            try
            {
                String service = HttpUtility.UrlEncode(Request.Url.Scheme + "://" + Request.Url.Host + (Request.Url.Port != 80 ? ":" + Request.Url.Port : "") + "/login/");

                using (ServerDBConfig conf = new ServerDBConfig(IAMDatabase.GetWebConnection()))
                {
                    Response.Redirect(((EnterpriseData)Session["enterprise_data"]).CasService.TrimEnd("/".ToCharArray()) + "/logout/?service=" + service, false);
                }
            }
            catch(Exception ex)
            {
                Response.Redirect("/");
            }*/
        }
    }
}