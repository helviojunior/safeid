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
using IAM.Config;
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;

namespace IAMWebServer.consoleapi.content
{
    public partial class admin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = new WebJsonResponse(Session["ApplicationVirtualPath"] + "admin/");

            if (ret != null)
                ReturnHolder.Controls.Add(new LiteralControl(ret.ToJSON()));
        }
    }
}