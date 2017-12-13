using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Web.Hosting;
using IAM.GlobalDefs;

namespace IAMWebServer.js
{
    public partial class Environment : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Page.Response.ContentType = "application/x-javascript; charset=UTF-8";
            Page.Response.ContentEncoding = Encoding.UTF8;

            StringBuilder text = new StringBuilder();
            text.AppendLine("/*! SafeId v1.0.0 | (c) 2013 SafeTrend.com.br.");
            text.AppendLine("//@ Variaveis de ambiente Javascript");
            text.AppendLine("//@ Variaveis necessárias para que os scripts identifiquem o root da aplicação");
            text.AppendLine("*/");
            text.AppendLine("");
            text.AppendLine("var ApplicationVirtualPath = '" + HostingEnvironment.ApplicationVirtualPath + "'");
            text.AppendLine("var LoadingText = '" + MessageResource.GetMessage("loading_mobile") + "'");

            Byte[] bRet = Encoding.UTF8.GetBytes(text.ToString());
            Page.Response.Status = "200 OK";
            Page.Response.StatusCode = 200;
            Page.Response.OutputStream.Write(bRet, 0, bRet.Length);
            Page.Response.OutputStream.Flush();
        }
    }
}