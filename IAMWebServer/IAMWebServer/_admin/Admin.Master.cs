using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;
using IAM.WebAPI;
using IAM.WebAPI;
using System.Data.SqlClient;

namespace IAMWebServer.admin
{
    public partial class Admin : System.Web.UI.MasterPage
    {
        public Boolean l1, l2, l3;
        public Boolean isAdmin;
        public Int64 enterpriseId;
        public String userName;
        public LoginData login;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this.Page)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

            login = LoginUser.LogedUser(this.Page);

            if (login == null)
            {
                Session["last_page"] = Request.ServerVariables["PATH_INFO"];
                Response.Redirect("/login/");
            }
            
            if ((Session["enterprise_data"]) != null && (Session["enterprise_data"] is EnterpriseData))
                    enterpriseId = ((EnterpriseData)Session["enterprise_data"]).Id;

            if (login != null)
            {

                try
                {
                    using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    using (IAMRBAC rbac = new IAMRBAC())
                        isAdmin = rbac.HasAdminConsole(database, login.Id, enterpriseId);
                }
                catch { }
            }


            if (!isAdmin)
                Response.Redirect(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "autoservice/");

            //Identifica a página atual com objetivo de mostrar o ícone como selecionado no rodapé
            String scriptName = Request.Params["SCRIPT_NAME"].ToLower();
            String ApplicationVirtualPath = Session["ApplicationVirtualPath"].ToString();
            if (ApplicationVirtualPath == "/")
                ApplicationVirtualPath = "";

            if (ApplicationVirtualPath != "")
                scriptName = scriptName.Replace(ApplicationVirtualPath, "");


            l1 = l2 = l3 = false;
            scriptName = scriptName.Trim("/ ".ToCharArray());
            switch (scriptName.ToLower())
            {
                case "admin":
                    l1 = true;
                    break;
            }
    
    
        }
    }
}