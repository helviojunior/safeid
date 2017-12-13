using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Hosting;


namespace IAMWebServer
{
    public partial class Test : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            appID.Text = HostingEnvironment.ApplicationID;
            appPPath.Text = HostingEnvironment.ApplicationPhysicalPath;
            appVPath.Text = HostingEnvironment.ApplicationVirtualPath;
            siteName.Text = HostingEnvironment.SiteName;

            Tools.Tool.notifyException(new Exception("teste"), this);

            
        }
    }
}