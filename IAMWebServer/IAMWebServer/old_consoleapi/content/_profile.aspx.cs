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
    public partial class profile : System.Web.UI.Page
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

                    using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                    {
                        DataTable c = db.Select("select * from entity where id = " + login.Id);
                        if ((c != null) && (c.Rows.Count > 0))
                        {

                            String html = "";
                            String content = "<div>{0}</div>";

                            html = "";
                            html += "<div class=\"as_form\">";
                            html += "<div class=\"se\" id=\"tst001\"><div class=\"tb\"><span class=\"t\">teste OK</span> fdklsjflds<div class=\"btn btn-base icon-plus btn-right\" onclick=\"fnMetroUIUser.getField('profile_field','mail','#tst001 .b');\"></div></div><div class=\"b\">fdsafads</div><div class=\"btn btn-base btn-success icon-ok\"></div></div>";
                            html += "<div class=\"se\"><div class=\"tb\"><span class=\"t\">teste OK</span> fdklsjflds</div><div class=\"b\">fdsafads</div></div>";
                            html += "<div class=\"se\"><div class=\"tb\"><span class=\"t\">teste OK</span> fdklsjflds</div><div class=\"b\">fdsafads</div></div>";

                            if (Tools.Tool.IsMobile(this))
                                html += "<span class=\"forgot\"> <a class=\"cancel\">" + MessageResource.GetMessage("cancel") + "</a></span>";

                            html += "</div>";

                            if (!Tools.Tool.IsMobile(this))
                                ret = new WebJsonResponse("#pn-profile .content", String.Format(content, html), 950, 400);
                            else
                                ret = new WebJsonResponse("#pn-profile .content", String.Format(content, html));
                        }
                        else
                        {
                            ret = new WebJsonResponse("", MessageResource.GetMessage("valid_username"), 3000, true);
                        }
                    }
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