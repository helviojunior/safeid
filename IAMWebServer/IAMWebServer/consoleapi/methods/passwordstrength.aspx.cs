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
using SafeTrend.Json;
using System.Drawing;
using IAM.GlobalDefs;


namespace IAMWebServer.consoleapi.methods
{
    public partial class passwordstrength : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebJsonResponse ret = null;

            try
            {
                String p = Tools.Tool.TrataInjection(Request["password"]);

                PasswordStrength pwdcheck = new PasswordStrength();
                pwdcheck.SetPassword(p);

                Color cor = pwdcheck.GetStrengthColor();

                ret = new WebJsonResponse("#passwordStrength", "<label>" + MessageResource.GetMessage("password_strength") + "</label><div class=\"form-group-content\"><span>" + pwdcheck.GetPasswordStrength() + "</span><div class=\"bar\" style=\"background: rgb(" + cor.R + "," + cor.G + "," + cor.B + ")\"></div></div>");

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