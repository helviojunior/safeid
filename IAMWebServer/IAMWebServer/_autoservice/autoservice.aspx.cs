using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.WebAPI;
using System.Data.SqlClient;

namespace IAMWebServer._autoservice
{
    public partial class autoservice : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            MAutoservice mClass = ((MAutoservice)this.Master);

            Tools.Tool.UpdateUri(this);

            LoginData login = LoginUser.LogedUser(this.Page);
            Boolean isAdmin = false;
            if (login != null)
            {

                IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString());
                try
                {
                    Int64 enterpriseId = 0;

                    if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                        enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;


                    using (IAMRBAC rbac = new IAMRBAC())
                        isAdmin = rbac.HasAdminConsole(database, login.Id, enterpriseId);

                }
                catch { }
                
            }

            String html = "";
            html += "<ul class=\"home\">";

            if (isAdmin)
                html += "    <li><a href=\"" + Session["ApplicationVirtualPath"] + "admin/\"><div class=\"btn c2\"><div class=\"inner\"><i class=\"icon-change\"></i><span>Admin</span></div></div></a></li>";

            html += "    <li><a href=\"" + Session["ApplicationVirtualPath"] + "autoservice/user/\"><div class=\"btn c3\"><div class=\"inner\"><i class=\"icon-profile\"></i><span>Informações gerais</span></div></div></a></li>";
            html += "    <li><a href=\"" + Session["ApplicationVirtualPath"] + "autoservice/user/changepassword/\"><div class=\"btn c1\"><div class=\"inner\"><i class=\"icon-key\"></i><span>Alterar senha</span></div></div></a></li>";
            html += "    <li><a href=\"" + Session["ApplicationVirtualPath"] + "autoservice/access_request/\"><div class=\"btn c5\"><div class=\"inner\"><i class=\"icon-page\"></i><span>Requisição de acesso</span></div></div></a></li>";
            html += "    <li><a href=\"" + Session["ApplicationVirtualPath"] + "logout/\"><div class=\"btn c4\"><div class=\"inner\"><i class=\"icon-exit\"></i><span>Desconectar</span></div></div></a></li>";
            html += "</ul>";

            contentHolder.Controls.Add(new LiteralControl(html));
        }
    }
}