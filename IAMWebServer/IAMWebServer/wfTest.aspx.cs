using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using IAM.Workflow;
using IAM.GlobalDefs;

namespace IAMWebServer
{
    public partial class wfTest : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
            WorkflowConfig workflow = new WorkflowConfig(1, "Teste de acesso " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Descrição teste", 1, WorkflowAccessType.RoleGrant);
            workflow.AddActivity("act 01", 0, 0, 2, 5, 0, 99);
            workflow.SetAccess(new Int64[] { 100 });

            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                workflow.SaveToDatabase(database);

            
            WorkflowConfig workflow2 = new WorkflowConfig();
            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                workflow2.GetDatabaseData(database, workflow.WorkflowId);

            using (IAMDatabase database = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                workflow2.SaveToDatabase(database);

            Retorno.Controls.Add(new LiteralControl(workflow2.ToJsonString()));


            /*
            try
            {
                WorkflowConfig workflow3 = new WorkflowConfig();
                workflow3.ParseFromJsonString(workflow2.ToJsonString());

                Retorno.Controls.Add(new LiteralControl(workflow3.ToJsonString()));
            }
            catch (Exception ex)
            {
                Retorno.Controls.Add(new LiteralControl(ex.ToString()));
            }*/

        }
    }
}