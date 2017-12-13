using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading;
using System.Globalization;
using System.Resources;
using System.Net;
using System.Xml;
using System.IO;
using IAM.GlobalDefs;
using IAM.Config;
using IAM.AuthPlugins;

namespace IAMWebServer.login
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this)) //Se houver falha na identificação da empresa finaliza a resposta
                return;


            LoginData login = LoginUser.LogedUser(this);
            if (login != null)
            {
                if (Session["last_page"] != null)
                {
                    Response.Redirect(Session["last_page"].ToString());
                    Session["last_page"] = null;
                }
                else
                    Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/");
            }
            else
            {
                try
                {
                    AuthBase authPlugin = null;
                    try
                    {
                        authPlugin = AuthBase.GetPlugin(new Uri(((EnterpriseData)Session["enterprise_data"]).AuthPlugin));
                    }
                    catch { }

                    if (authPlugin == null)
                        throw new Exception("Plugin não encontrado");

                    LoginResult tst = null;

                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        tst = authPlugin.Auth(db, this);
                }
                catch (Exception ex)
                {
                    Tools.Tool.notifyException(ex, this);
                    throw ex;
                }
            }
        }


    }
}