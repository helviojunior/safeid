using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Compilation;
using System.Globalization;
using System.Resources;
using System.Threading;
using SafeTrend.Json;
using IAM.Config;
using IAM.GlobalDefs;
using SafeTrend.Data;

namespace IAMWebServer.proxy
{
    public partial class api : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            if (Request.HttpMethod == "POST")
            {
                if (!EnterpriseIdentify.Identify(this, true)) //Se houver falha na identificação da empresa finaliza a resposta
                    return;

                //ResourceManager rm = new ResourceManager("Resources.Strings", System.Reflection.Assembly.Load("App_GlobalResources"));
                //CultureInfo ci = Thread.CurrentThread.CurrentCulture;

                try
                {
                    JSONRequest req = JSON.GetRequest(Request.InputStream);

                    if ((req.request == null) || (req.request.Trim() == ""))
                    {
                        ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "Request is empty", "")));
                        return;
                    }

                    LoadPage("/proxy/methods/" + req.request.Trim() + ".aspx");
                }
                catch (Exception ex)
                {
                    if ((ex is HttpException) && (((HttpException)ex).GetHttpCode() == 404))
                    {
                        ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, MessageResource.GetMessage("not_implemented"), "")));
                    }
                    else
                    {
                        ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, MessageResource.GetMessage("api_error"), "")));
                    }

                    try
                    {
                        using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                            db.AddUserLog(LogKey.API_Error, null, "ProxyAPI", UserLogLevel.Error, 0, (((Page.Session["enterprise_data"]) != null && (Page.Session["enterprise_data"] is EnterpriseData) && (((EnterpriseData)Page.Session["enterprise_data"]).Id != null)) ? ((EnterpriseData)Page.Session["enterprise_data"]).Id : 0), 0, 0, 0, 0, 0, "Proxy API error: " + ex.Message, "{ \"ipaddr\":\"" + Tools.Tool.GetIPAddress() + "\"} ");
                    }
                    catch { }
                }
            }
            else
            {
                ReturnHolder.Controls.Add(new LiteralControl(JSON.GetResponse(false, "Invalid http method", "")));
            }
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