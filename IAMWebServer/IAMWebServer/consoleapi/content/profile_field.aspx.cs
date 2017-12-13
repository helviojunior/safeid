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
using IAM.CA;
using SafeTrend.Json;
using IAM.GlobalDefs;

namespace IAMWebServer.consoleapi.content
{
    public partial class profile_field : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;



            try
            {

                LoginData login = LoginUser.LogedUser(this);

                String err = "";
                if (!EnterpriseIdentify.Identify(this, false, out err)) //Se houver falha na identificação da empresa finaliza a resposta
                {
                    ret = new WebJsonResponse("", err, 3000, true);
                }
                else if (login == null)
                {
                    ret = new WebJsonResponse("", MessageResource.GetMessage("expired_session"), 3000, true, "/login/");
                }
                else
                {
                    String container = Request.Form["container"];
                    String field = Request.Form["field"];
                    String id = field + Guid.NewGuid().ToString();

                    String html = "";
                    String content = "<div >{0}</div>";

                    html = "teste ok - " + field;

                    ret = new WebJsonResponse(container, String.Format(content, html), true);
                }


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