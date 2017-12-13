using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using IAM.Config;
using IAM.GlobalDefs;
using SafeTrend.Data;
using IAM.WebAPI;
using SafeTrend.WebAPI;

namespace IAMWebServer.api
{


    public partial class json : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            /*
            if ((Page.Request.Url.Host.ToLower() == "127.0.0.1") || (Page.Request.Url.Host.ToLower() == "localhost"))
            {
                //Validação diferenciada em caso de requisição vinda de loopback
                //Pois o proprio servidor pode estar requisitando a API
                //Neste caso a empresa deve seve verificar se a empresa ja foi identificada nessa sessão

                if ((Page.Session["enterprise_data"] == null) || !(Page.Session["enterprise_data"] is EnterpriseData))
                {
                    Page.Response.Status = "403 Access denied";
                    Page.Response.StatusCode = 403;
                    Page.Response.End();
                    return;
                }

            }
            else
            {*/

            if (!EnterpriseIdentify.Identify(Page, false, true)) //Se houver falha na identificação da empresa finaliza a resposta
            {
                Page.Response.Status = "403 Access denied";
                Page.Response.StatusCode = 403;
                Page.Response.End();
                return;
            }
            //}

            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {


                ExecutionLog eLogs = new ExecutionLog(delegate(Boolean success, Int64 enterpriseId, String method, AccessControl acl, String jRequest, String jResponse)
                {
                    //Para efeitos de teste vou sempre retornar true
                    //return true;
                    LoginData login = null;

                    if ((Session["login"] != null) && (Session["login"] is LoginData))
                        login = (LoginData)Session["login"];


                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        db.AddUserLog(LogKey.Debug, null, "API", UserLogLevel.Debug, 0, enterpriseId, 0, 0, 0, (login != null ? login.Id : 0), 0, "API Call (" + method + "). Result success? " + success, "{\"Request\":" + jRequest + ", \"Response\":" + jResponse + "}");

                });

                WebPageAPI.Execute(database, this, eLogs);

            }

        }
    }
}