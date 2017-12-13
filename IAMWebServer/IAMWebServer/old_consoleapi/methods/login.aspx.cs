using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using IAM.Config;
using System.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Text;
using SafeTrend.Json;
using IAM.GlobalDefs;
using System.Data.SqlClient;
using IAM.WebAPI;

namespace IAMWebServer.consoleapi.methods
{
    public partial class login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;

            try
            {
                throw new NotImplementedException();

                /*
                LoginResult auth = LoginUser.AuthUser(this, Request["userLogin"], Request["password"]);

                if ((auth.Success) && (auth.ChangePassword) && (Session["login"] is LoginData))
                {
                    Session["entity_id"] = ((LoginData)Session["login"]).Id;
                    Session["login"] = null;
                    ret = new WebJsonResponse("/login/changepassword/");
                }
                else if ((auth.Success) && (Session["login"] is LoginData))
                {
                    Int64 enterpriseId = 0;

                    LoginData login = (LoginData)Session["login"];

                    if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                        enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;

                    ret = new WebJsonResponse(Session["ApplicationVirtualPath"] + "autoservice/");

                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {
                        try
                        {
                            using (IAMRBAC rbac = new IAMRBAC())
                                if (rbac.UserAdmin(database, login.Id, enterpriseId))
                                    ret = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/");
                        }
                        catch { }
                    }

                    
                }
                else
                    ret = new WebJsonResponse("", auth.Text, 3000, true);*/

            }
            catch (Exception ex)
            {
                Tools.Tool.notifyException(ex);
                throw ex;
            }


            if (ret != null)
                ReturnHolder.Controls.Add(new LiteralControl(ret.ToJSON()));
        }
    }
}