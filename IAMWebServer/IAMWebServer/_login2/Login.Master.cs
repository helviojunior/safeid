using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace IAMWebServer._login2
{
    public partial class Login : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this.Page)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

        }
    }
}