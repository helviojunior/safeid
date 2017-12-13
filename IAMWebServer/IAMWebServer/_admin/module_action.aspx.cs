using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Compilation;
using System.Web.Routing;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.IO;
using SafeTrend.Json;
using IAM.Config;
using IAM.GlobalDefs;
using SafeTrend.Data;


namespace IAMWebServer._admin
{
    public partial class module_action : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            WebJsonResponse ret = null;


            //if (Request.HttpMethod == "POST")
            //{
            if (!EnterpriseIdentify.Identify(this, true)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

            try
            {
                if ((RouteData.Values["module"] == null) || (RouteData.Values["module"].ToString() == ""))
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("invalid_module"), 3000, true);
                }
                else
                {
                    LoadPage("/_admin/action/" + RouteData.Values["module"] + ".aspx");
                }
            }
            catch (Exception ex)
            {
                if ((ex is HttpException) && (((HttpException)ex).GetHttpCode() == 404))
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("not_implemented"), 3000, true);
                }
                else
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("api_error"), 3000, true);
                }

                try
                {
                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                        db.AddUserLog(LogKey.API_Error, null, "AdminAPI", UserLogLevel.Error, 0, (((Page.Session["enterprise_data"]) != null && (Page.Session["enterprise_data"] is EnterpriseData) && (((EnterpriseData)Page.Session["enterprise_data"]).Id != null)) ? ((EnterpriseData)Page.Session["enterprise_data"]).Id : 0), 0, 0, 0, 0, 0, "API error: " + ex.Message, "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                }
                catch { }

                Tools.Tool.notifyException(ex, this);
            }
            /*}
            else
            {
                ret = new WebJsonResponse("", MessageResource.GetMessage("invalid_http_method"), 3000, true);
            }*/


            if (ret != null)
                Retorno.Controls.Add(new LiteralControl(ret.ToJSON()));
        }


        public static void LoadPage(string pagePath)
        {
            Type type = BuildManager.GetCompiledType(pagePath);

            if (type == null)
                throw new ApplicationException("Page " + pagePath + " not found");

            Page pageView = (Page)Activator.CreateInstance(type);

            ((IHttpHandler)pageView).ProcessRequest(HttpContext.Current);
        }
    }
}