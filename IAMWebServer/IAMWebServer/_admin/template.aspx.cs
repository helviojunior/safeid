using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.GlobalDefs;

namespace IAMWebServer._admin
{
    public partial class template : System.Web.UI.Page
    {
        public String module = "";
        public String moduleName = "";
        public String searchText = "";
        public Boolean showSearchBox = true;
        public Int64 id = 0;
        public Boolean fullWidth;
        protected void Page_Load(object sender, EventArgs e)
        {
            fullWidth = false;

            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["module"]))
                module = (String)RouteData.Values["module"];

            //Contabilizador de cliques por usuário
            using(IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                db.AddLinkCount(this);

            Tools.Tool.UpdateUri(this);

            switch (module.ToLower())
            {
                case "logs":
                    moduleName = "Logs";
                    searchText = "Buscar logs";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "users":
                    moduleName = "Usuários";
                    searchText = "Buscar usuários";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));

                    break;

                case "roles":
                    moduleName = "Perfis";
                    searchText = "Buscar perfis";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "enterprise":
                    moduleName = "Empresa";
                    showSearchBox = false;
                    //btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "container":
                    moduleName = "Pasta";
                    searchText = "Buscar pasta";
                    //btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "context":
                    moduleName = "Contexto";
                    searchText = "Buscar contexto";
                    //btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "plugin":
                    moduleName = "Plugins";
                    searchText = "Buscar plugin";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "proxy":
                    moduleName = "Proxy";
                    searchText = "Buscar proxy";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;


                case "resource":
                    moduleName = "Recurso";
                    searchText = "Buscar recurso";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "resource_plugin":
                    moduleName = "Recurso x Plugin";
                    searchText = "Buscar Recurso x Plugin";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "field":
                    moduleName = "Campo";
                    searchText = "Buscar campo";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "system_roles":
                    moduleName = "Perfis do sistema";
                    searchText = "Buscar perfis do sistema";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "filter":
                    moduleName = "Filtro";
                    searchText = "Buscar filtro";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "service_status":
                    this.fullWidth = true;
                    this.showSearchBox = false;
                    moduleName = "Status dos serviços de servidor";
                    //btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;


                case "license":
                    moduleName = "Licenciamento";
                    showSearchBox = false;
                    //btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "access_request":
                    moduleName = "Requisição de acesso";
                    searchText = "Buscar requisições";
                    showSearchBox = true;
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;

                case "workflow":
                    moduleName = "Workflow";
                    searchText = "Buscar workflow";
                    showSearchBox = true;
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    break;


                default:
                    moduleName = "Dashboard";
                    searchText = "Buscar usuários";
                    btnBox.Controls.Add(new LiteralControl("<div id=\"btnbox\"></div>"));
                    fullWidth = false;
                    break;
            }

        }
    }
}