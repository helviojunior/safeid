using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;
using IAM.GlobalDefs.Messages;
using SafeTrend.Data;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace IAM.Workflow
{
    public enum WorkflowRequestStatus
    {
        Waiting = 0,
        Approved,
        Deny,
        UnderReview,
        Escalated,
        Expired,
        UserCanceled,
        Revoked
    }

    public class WorkflowRequestProccess
    {
        public Boolean Success { get; set; }
        public String Message { get; set; }
        public String Debug { get; set; }

        public WorkflowRequestProccess(Boolean success, String message)
            : this(success, message, "") { }

        public WorkflowRequestProccess(Boolean success, String message, String debug)
        {
            this.Success = success;
            this.Message = message;
        }
    }

    public class WorkflowRequest : IDisposable
    {
        private Int64 workflow_request_id;
        private Int64 enterprise_id;
        private WorkflowRequestStatus status;

        private WorkflowActivity activity;
        private WorkflowConfig workflow;
        private WorkflowActivity nextActivity;

        private String user_name;
        private String user_login;
        private Int64 user_id;

        private DateTime activity_created;
        private Int64 last_executed_by;

        public Int64 EnterpriseId { get { return enterprise_id; } set { enterprise_id = value; } }
        public Int64 RequestId { get { return workflow_request_id; } set { workflow_request_id = value; } }
        public WorkflowRequestStatus Status { get { return status; } set { status = value; } }

        public WorkflowActivity Activity { get { return activity; } set { activity = value; } }

        public WorkflowConfig Workflow { get { return workflow; } set { workflow = value; } }
        public WorkflowActivity NextActivity { get { return nextActivity; } set { nextActivity = value; } }

        public String UserName { get { return user_name; } set { user_name = value; } }
        public String UserLogin { get { return user_login; } set { user_login = value; } }
        public Int64 UserId { get { return user_id; } set { user_id = value; } }

        public DateTime ActivityCreated { get { return activity_created; } set { activity_created = value; } }
        public Int64 LastExecutedBy { get { return last_executed_by; } set { last_executed_by = value; } }

        public WorkflowRequest(Int64 requestId)
        {
            this.workflow_request_id = requestId;
            this.activity_created = new DateTime(1970, 1, 1);
        }

        public WorkflowRequestProccess GetInicialData(IAMDatabase database)
        {
            DbParameterCollection par = new DbParameterCollection();
            par.Add("@enterprise_id", typeof(Int64)).Value = this.enterprise_id;
            par.Add("@request_id", typeof(Int64)).Value = this.workflow_request_id;

            DataTable dtWorkflowRequest = database.ExecuteDataTable("select r.*, e.full_name, e.id entity_id, e.login, w.name workflow_name, c.enterprise_id from st_workflow_request r with(nolock) inner join st_workflow w with(nolock) on w.id = r.workflow_id inner join entity e  with(nolock) on e.id = r.entity_id inner join context c  with(nolock) on c.id = e.context_id where r.id = @request_id", CommandType.Text, par, null);
            if ((dtWorkflowRequest == null) || (dtWorkflowRequest.Rows.Count == 0))
                return new WorkflowRequestProccess(false, "Access request not found");

            this.status = (WorkflowRequestStatus)((Int32)dtWorkflowRequest.Rows[0]["status"]);
            this.enterprise_id = (Int64)dtWorkflowRequest.Rows[0]["enterprise_id"];

            this.user_name = dtWorkflowRequest.Rows[0]["full_name"].ToString();
            this.user_login = dtWorkflowRequest.Rows[0]["login"].ToString();
            this.user_id = (Int64)dtWorkflowRequest.Rows[0]["entity_id"];

            try
            {
                workflow = new WorkflowConfig();
                workflow.GetDatabaseData(database, (Int64)dtWorkflowRequest.Rows[0]["workflow_id"]);

                if (workflow == null)
                    throw new Exception("");
            }
            catch(Exception ex)
            {
                return new WorkflowRequestProccess(false, "Fail on get workflow config", ex.Message);
            }

            if ((workflow.Activities == null) || (workflow.Activities.Count == 0))
            {
                return new WorkflowRequestProccess(false, "Activity list is empty on workflow " + workflow.Name);
            }


            //Verifica o último status para chegar em que activity esta requisição está
            DataTable dtLogs = database.ExecuteDataTable("select * from st_workflow_request_status where workflow_request_id = @request_id order by date", CommandType.Text, par, null);
            if ((dtLogs == null) || (dtLogs.Rows.Count == 0))
                return new WorkflowRequestProccess(false, "Access request status list not found");

            //Resgata a maior activity
            try
            {
                List<Int64> actList = new List<Int64>();
                foreach (DataRow dr in dtLogs.Rows)
                    if (!actList.Contains((Int64)dr["activity_id"]))
                        actList.Add((Int64)dr["activity_id"]);


                //Ordena de forma descrecente
                workflow.Activities.Sort(delegate(WorkflowActivity a1, WorkflowActivity a2) { return a2.ExeutionOrder.CompareTo(a1.ExeutionOrder); });


                //Remove da lista todas as atividades ja aprovadas
                foreach (WorkflowActivity act in workflow.Activities)
                {
                    DateTime last = new DateTime(1970, 1, 1);
                    WorkflowRequestStatus st = WorkflowRequestStatus.Waiting;

                    foreach (DataRow drSt in dtLogs.Rows)
                        if (drSt["activity_id"].ToString() == act.ActivityId.ToString())
                            if (last.CompareTo((DateTime)drSt["date"]) < 0)
                            {
                                last = (DateTime)drSt["date"];
                                st = (WorkflowRequestStatus)((Int32)drSt["status"]);
                            }

                    if (st == WorkflowRequestStatus.Approved)
                        actList.Remove(act.ActivityId);

                }


                //Primeiro busca a menor atividade
                foreach (WorkflowActivity act in workflow.Activities)
                {
                    if (activity == null)//Como esta ordenado de forma decrescente, pegará a última atividade do array
                        activity = act;

                    if ((actList.Contains(act.ActivityId)) && (act.ExeutionOrder < activity.ExeutionOrder))
                        activity = act;
                }


                if (activity == null)
                    throw new Exception("Activity is empty");

                foreach (DataRow dr in dtLogs.Rows)
                    if ((Int64)dr["activity_id"] == activity.ActivityId)
                    {
                        if (this.activity_created.Year == 1970)
                        {
                            this.activity_created = (DateTime)dr["date"];
                            this.last_executed_by = (Int64)dr["executed_by_entity_id"];
                        }

                        if (this.activity_created.CompareTo((DateTime)dr["date"]) < 0)
                        {
                            this.activity_created = (DateTime)dr["date"];
                            this.last_executed_by = (Int64)dr["executed_by_entity_id"];
                        }

                    }

            }
            catch (Exception ex)
            {
                return new WorkflowRequestProccess(false, "Error on proccess activities");
            }

            //Verifica se essa é a última activity
            //Se sim irá realizar a ação final
            this.nextActivity = null;
            foreach (WorkflowActivity act in workflow.Activities)
            {
                if (act.ExeutionOrder > activity.ExeutionOrder)
                    this.nextActivity = act;
            }

            return new WorkflowRequestProccess(true, "");
        }

        public WorkflowRequestProccess SetStatus(IAMDatabase database, WorkflowRequestStatus status, Int64 executing_user)
        {

            WorkflowRequestProccess initial = GetInicialData(database);
            if (!initial.Success)
                return initial;

            //Verifica se o usuário atual faz parte do grupo de aprovadores 
            if (!database.ExecuteScalar<Boolean>("select case when COUNT(*) > 0 then CAST(1 as bit) else CAST(0 as bit) end from entity e with(nolock) where e.id in (" + workflow.Owner + "," + activity.ManualApproval.EntityApprover + ") or e.id in (select i.entity_id from identity_role ir with(nolock) inner join [identity] i with(nolock) on i.id = ir.identity_id where ir.role_id = " + activity.ManualApproval.RoleApprover + ")", CommandType.Text, null))
            {
                return new WorkflowRequestProccess(false, "Access denied. You are not part of the group of approvers users");
            }

            Object trans = database.BeginTransaction();
            try
            {
                String changeTextAdmin = "";
                String changeText = "";

                changeText = activity.Name + " " + MessageResource.GetMessage("wf_" + status.ToString().ToLower(), status.ToString());

                using (DbParameterCollection par2 = new DbParameterCollection())
                {
                    //Só altera o status do ítem ptincipal quando a aprovação for da última activity
                    if ((status == WorkflowRequestStatus.Approved) && (nextActivity == null))
                    {
                        par2.Add("@request_id", typeof(Int64)).Value = this.workflow_request_id;
                        par2.Add("@status", typeof(Int32)).Value = (Int32)status;
                        database.ExecuteNonQuery("UPDATE [st_workflow_request] SET [status] = @status, deployed = 0 WHERE ID = @request_id", CommandType.Text, par2, trans);
                    }
                    else if (status == WorkflowRequestStatus.Approved)
                    {
                        par2.Add("@request_id", typeof(Int64)).Value = this.workflow_request_id;
                        database.ExecuteNonQuery("UPDATE [st_workflow_request] SET deployed = 0 WHERE ID = @request_id", CommandType.Text, par2, trans);
                    }
                    else
                    {
                        par2.Add("@request_id", typeof(Int64)).Value = this.workflow_request_id;
                        par2.Add("@status", typeof(Int32)).Value = (Int32)status;
                        database.ExecuteNonQuery("UPDATE [st_workflow_request] SET [status] = @status, deployed = 0 WHERE ID = @request_id", CommandType.Text, par2, trans);
                    }

                    //Adiciona o status da activity atual
                    par2.Clear();
                    par2.Add("@workflow_request_id", typeof(Int64)).Value = this.workflow_request_id;
                    par2.Add("@status", typeof(String)).Value = (Int32)status;
                    par2.Add("@description", typeof(String)).Value = changeText;
                    par2.Add("@activity_id", typeof(Int64)).Value = activity.ActivityId;
                    par2.Add("@executed_by_entity_id", typeof(Int64)).Value = executing_user;
                    par2.Add("@date", typeof(DateTime)).Value = DateTime.Now;

                    database.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[date],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@date,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, trans);

                    //Adiciona o status da próxima atividade
                    if ((status == WorkflowRequestStatus.Approved) && (nextActivity != null))
                    {
                        par2.Clear();
                        par2.Add("@workflow_request_id", typeof(Int64)).Value = this.workflow_request_id;
                        par2.Add("@status", typeof(String)).Value = (Int32)WorkflowRequestStatus.Waiting;
                        par2.Add("@description", typeof(String)).Value = "Aguardando análise";
                        par2.Add("@activity_id", typeof(Int64)).Value = nextActivity.ActivityId;
                        par2.Add("@executed_by_entity_id", typeof(Int64)).Value = executing_user;
                        par2.Add("@date", typeof(DateTime)).Value = DateTime.Now.AddSeconds(1);

                        database.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[date],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@date,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, trans);

                    }
                }


                //E-mails para os próximos aprovadores, se houver
                if ((status == WorkflowRequestStatus.Approved) && (nextActivity != null))
                {
                    try
                    {
                        Dictionary<Int64, List<String>> mails = new Dictionary<long, List<string>>();

                        if ((nextActivity.ManualApproval != null) && ((nextActivity.ManualApproval.EntityApprover > 0) || (nextActivity.ManualApproval.RoleApprover > 0)))
                        {
                            DataTable dtUserMails = database.ExecuteDataTable("select distinct entity_id, mail, full_name from vw_entity_mails where entity_id in (" + activity.ManualApproval.EntityApprover + ") or entity_id in (select i.entity_id from identity_role ir with(nolock) inner join [identity] i with(nolock) on i.id = ir.identity_id where ir.role_id = " + activity.ManualApproval.RoleApprover + ")", CommandType.Text, null, trans);
                            if ((dtUserMails != null) && (dtUserMails.Rows.Count > 0))
                                foreach (DataRow dr in dtUserMails.Rows)
                                    try
                                    {
                                        MailAddress m = new MailAddress(dr["mail"].ToString());

                                        if (!mails.ContainsKey((Int64)dr["entity_id"]))
                                            mails.Add((Int64)dr["entity_id"], new List<string>());

                                        mails[(Int64)dr["entity_id"]].Add(m.Address);
                                    }
                                    catch { }
                        }

                        if (mails.Count > 0)
                        {
                            foreach (Int64 admin_id in mails.Keys)
                                try
                                {
                                    Dictionary<String, String> vars = new Dictionary<string, string>();
                                    vars.Add("workflow_name", workflow.Name);
                                    vars.Add("user_name", this.user_name);
                                    vars.Add("user_login", this.user_login);
                                    vars.Add("user_id", this.user_id.ToString());
                                    vars.Add("admin_id", admin_id.ToString());
                                    vars.Add("description", workflow.Description);
                                    vars.Add("approval_link", "%enterprise_uri%/admin/access_request/" + this.workflow_request_id + "/allow/");
                                    vars.Add("deny_link", "%enterprise_uri%/admin/access_request/" + this.workflow_request_id + "/deny/");



                                    MessageBuilder msgAdm = MessageBuilder.BuildFromTemplate(database, this.enterprise_id, "access_request_admin", String.Join(",", mails[admin_id]), vars, trans);
                                    msgAdm.SaveToDb(database, trans);
                                }
                                catch { }
                        }
                    }
                    catch { }
                }

                try
                {
                    //E-mail para o usuário
                    DataTable dtUserMails = database.ExecuteDataTable("select distinct mail from vw_entity_mails where entity_id = " + this.user_id, CommandType.Text, null, trans);
                    if ((dtUserMails != null) && (dtUserMails.Rows.Count > 0))
                    {
                        List<String> mails = new List<string>();

                        foreach (DataRow dr in dtUserMails.Rows)
                        {
                            try
                            {
                                MailAddress m = new MailAddress(dr["mail"].ToString());
                                mails.Add(m.Address);
                            }
                            catch { }
                        }

                        if (mails.Count > 0)
                        {

                            Dictionary<String, String> vars = new Dictionary<string, string>();
                            vars.Add("workflow_name", this.workflow.Name);
                            vars.Add("user_name", this.user_name);
                            vars.Add("user_login", this.user_login);
                            vars.Add("user_id", this.user_id.ToString());
                            vars.Add("change", changeText);

                            MessageBuilder msg1 = MessageBuilder.BuildFromTemplate(database, this.enterprise_id, "access_request_changed", String.Join(",", mails), vars, trans);
                            msg1.SaveToDb(database, trans);

                        }
                    }

                }
                catch { }

                database.Commit();

                return new WorkflowRequestProccess(true, "");
            }
            catch (Exception ex)
            {
                database.Rollback();

                return new WorkflowRequestProccess(false, "Erro on deny access.", ex.Message);
            }

        }

        public void Dispose()
        {

        }
    }
}
