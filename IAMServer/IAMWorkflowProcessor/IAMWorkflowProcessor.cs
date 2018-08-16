using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

using IAM.Config;
using IAM.Log;
using IAM.CA;
using SafeTrend.Data;
using IAM.LocalConfig;
using IAM.GlobalDefs;
using IAM.GlobalDefs.Messages;
using SafeTrend.Json;
using IAM.Workflow;


namespace IAM.Messenger
{
    public partial class IAMWorkflowProcessor : ServiceBase
    {

        ServerLocalConfig localConfig;
        String basePath = "";

        Timer workflowTimer;
        Timer statusTimer;
        Boolean executing = false;
        private String last_status = "";
        private DateTime startTime = new DateTime(1970, 1, 1);

        public IAMWorkflowProcessor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            /*************
             * Carrega configurações
             */

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            basePath = Path.GetDirectoryName(asm.Location);

            localConfig = new ServerLocalConfig();
            localConfig.LoadConfig();

            if ((localConfig.SqlServer == null) || (localConfig.SqlServer.Trim() == ""))
                StopOnError("Parâmetro 'sqlserver' não localizado no arquivo de configuração 'server.conf'", null);
            
            if ((localConfig.SqlDb == null) || (localConfig.SqlDb.Trim() == ""))
                StopOnError("Parâmetro 'sqldb' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlUsername == null) || (localConfig.SqlUsername.Trim() == ""))
                StopOnError("Parâmetro 'sqlusername' não localizado no arquivo de configuração 'server.conf'", null);

            if ((localConfig.SqlPassword == null) || (localConfig.SqlPassword.Trim() == ""))
                StopOnError("Parâmetro 'sqlpassword' não localizado no arquivo de configuração 'server.conf'", null);


            Int32 cnt = 0;
            Int32 stepWait = 15000;
            while (cnt <= 10)
            {
                try
                {
                    IAMDatabase db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();

                    db.ServiceStart("WorkflowProcessor", null);

                    db.closeDB();

                    break;
                }
                catch (Exception ex)
                {
                    if (cnt < 10)
                    {
                        TextLog.Log("WorkflowProcessor", "Falha ao acessar o banco de dados: " + ex.Message);
                        Thread.Sleep(stepWait);
                        stepWait = stepWait * 2;
                        cnt++;
                    }
                    else
                    {
                        StopOnError("Falha ao acessar o banco de dados", ex);
                    }
                }
            }



            /*************
             * Inicia processo de verificação/atualização da base de dados
             */
#if DEBUG
            try
            {
                using (IAM.GlobalDefs.Update.IAMDbUpdate updt = new GlobalDefs.Update.IAMDbUpdate(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword))
                    updt.Update();
            }
            catch (Exception ex)
            {
                StopOnError("Falha ao atualizar o banco de dados", ex);
            }
#endif

            /*************
             * Inicia timer que processa as mensagens
             */

            workflowTimer = new Timer(new TimerCallback(WorkflowTimer), null, 1000, 60000);
            statusTimer = new Timer(new TimerCallback(TmrServiceStatusCallback), null, 100, 10000);

        }


        private void TmrServiceStatusCallback(Object o)
        {
            IAMDatabase db = null;
            try
            {
                db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                db.openDB();
                db.Timeout = 600;

                db.ServiceStatus("WorkflowProcessor", JSON.Serialize2(new { host = Environment.MachineName, executing = executing, start_time = startTime.ToString("o"), last_status = last_status }), null);

                db.closeDB();
            }
            catch { }
            finally
            {
                if (db != null)
                    db.Dispose();

                db = null;
            }
        }


        private void WorkflowTimer(Object state)
        {

            if (executing)
                return;

            executing = true;

            startTime = DateTime.Now;

            try
            {
                
                IAMDatabase db = null;
                try
                {

                    db = new IAMDatabase(localConfig.SqlServer, localConfig.SqlDb, localConfig.SqlUsername, localConfig.SqlPassword);
                    db.openDB();
                    db.Timeout = 900;

                    DataTable dtRequests = db.ExecuteDataTable("select id, workflow_id from [st_workflow_request] r with(nolock) where r.deployed = 0 order by r.create_date");
                    if ((dtRequests != null) && (dtRequests.Rows.Count > 0))
                    {
                        try
                        {
                            TextLog.Log("WorkflowProcessor", "Starting workflow processor timer");

                            foreach (DataRow dr in dtRequests.Rows)
                            {
                                try
                                {
                                    WorkflowRequest request = new WorkflowRequest((Int64)dr["id"]);
                                    request.GetInicialData(db);

                                    WorkflowConfig workflow = new WorkflowConfig();
                                    workflow.GetDatabaseData(db, (Int64)dr["workflow_id"]);

                                    switch (request.Status)
                                    {
                                        case WorkflowRequestStatus.Deny:
                                        case WorkflowRequestStatus.Expired:
                                        case WorkflowRequestStatus.UserCanceled:
                                            //Somente atualiza como deployed, para não ficar verificando
                                            db.ExecuteNonQuery("update [st_workflow_request] set deployed = 1 where id = " + dr["id"]);
                                            continue;
                                            break;

                                        case WorkflowRequestStatus.Waiting:
                                            //Verifica escalation
                                            DateTime escalation = request.ActivityCreated.AddDays(request.Activity.EscalationDays);
                                            DateTime expired = request.ActivityCreated.AddDays(request.Activity.ExpirationDays);
                                            if (expired.CompareTo(DateTime.Now) < 0)
                                            {
                                                request.SetStatus(db, WorkflowRequestStatus.Escalated, request.UserId);
                                                db.ExecuteNonQuery("update [st_workflow_request] set deployed = 1 where id = " + dr["id"]);
                                            }
                                            else if (escalation.CompareTo(DateTime.Now) < 0)
                                            {
                                                request.SetStatus(db, WorkflowRequestStatus.Escalated, request.UserId);
                                                db.ExecuteNonQuery("update [st_workflow_request] set deployed = 1 where id = " + dr["id"]);
                                            }
                                            break;

                                        case WorkflowRequestStatus.Escalated:
                                            //Verifica escalation
                                            DateTime expired2 = request.ActivityCreated.AddDays(request.Activity.ExpirationDays);
                                            if (expired2.CompareTo(DateTime.Now) < 0)
                                            {
                                                request.SetStatus(db, WorkflowRequestStatus.Expired, request.UserId);
                                                db.ExecuteNonQuery("update [st_workflow_request] set deployed = 1 where id = " + dr["id"]);
                                            }
                                            break;

                                        case WorkflowRequestStatus.Approved:
                                            //Somente executa alguma ação quando não há mais nenhuma atividade a ser executada
                                            if (request.NextActivity == null)
                                            {
                                                switch (workflow.AccessType)
                                                {
                                                    case WorkflowAccessType.RoleGrant:
                                                        WorkflowAccessRoleGrant rg = (WorkflowAccessRoleGrant)workflow.Access;
                                                        //Seleciona todas as identidades do usuário e adiciona na role

                                                        DataTable drIdent = db.ExecuteDataTable("select i.* from [identity] i with(nolock) inner join resource_plugin rp with(nolock) on i.resource_plugin_id = rp.id where rp.enable_import = 1 and rp.permit_add_entity = 1 and i.entity_id = " + request.UserId);
                                                        if ((drIdent == null) || (drIdent.Rows.Count == 0))
                                                        {
                                                            using (DbParameterCollection par2 = new DbParameterCollection())
                                                            {
                                                                par2.Add("@workflow_request_id", typeof(Int64)).Value = request.RequestId;
                                                                par2.Add("@status", typeof(String)).Value = (Int32)request.Status;
                                                                par2.Add("@description", typeof(String)).Value = "No inbound identity found for allow access";
                                                                par2.Add("@activity_id", typeof(Int64)).Value = request.Activity.ActivityId;
                                                                par2.Add("@executed_by_entity_id", typeof(Int64)).Value = request.LastExecutedBy;

                                                                db.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, null);

                                                            }
                                                        }
                                                        else
                                                        {
                                                            //Lista o nome e id de todas as roles que serão utilizadas
                                                            List<String> roleList = new List<String>();
                                                            foreach (Int64 r in rg.Roles)
                                                                roleList.Add(r.ToString());

                                                            DataTable drRoles = db.ExecuteDataTable("select * from [role] where id in ("+ String.Join(",",roleList) +")");
                                                            if ((drRoles == null) || (drRoles.Rows.Count == 0))
                                                            {
                                                                using (DbParameterCollection par2 = new DbParameterCollection())
                                                                {
                                                                    par2.Add("@workflow_request_id", typeof(Int64)).Value = request.RequestId;
                                                                    par2.Add("@status", typeof(String)).Value = (Int32)request.Status;
                                                                    par2.Add("@description", typeof(String)).Value = "No role found for allow access";
                                                                    par2.Add("@activity_id", typeof(Int64)).Value = request.Activity.ActivityId;
                                                                    par2.Add("@executed_by_entity_id", typeof(Int64)).Value = request.LastExecutedBy;

                                                                    db.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, null);

                                                                }
                                                            }
                                                            else
                                                            {

                                                                String roleNames = "";

                                                                //Adiciona as roles
                                                                foreach (DataRow dr2 in drIdent.Rows)
                                                                    foreach (DataRow drRole in drRoles.Rows)
                                                                    {
                                                                        DbParameterCollection par = new DbParameterCollection();
                                                                        par.Add("@identity_id", typeof(Int64)).Value = dr2["id"];
                                                                        par.Add("@role_id", typeof(Int64)).Value = drRole["id"];

                                                                        Boolean added = db.ExecuteScalar<Boolean>("sp_insert_identity_role", CommandType.StoredProcedure, par);

                                                                        if (added)
                                                                            roleNames += drRole["name"] + Environment.NewLine;
                                                                    }

                                                                if (roleNames != null)
                                                                    db.AddUserLog(LogKey.User_IdentityRoleBind, null, "Workflow", UserLogLevel.Info, 0, 0, 0, 0, 0, request.UserId, 0, "Entity bind to roles by workflow access request", roleNames);


                                                                using (DbParameterCollection par2 = new DbParameterCollection())
                                                                {
                                                                    par2.Add("@workflow_request_id", typeof(Int64)).Value = request.RequestId;
                                                                    par2.Add("@status", typeof(String)).Value = (Int32)request.Status;
                                                                    par2.Add("@description", typeof(String)).Value = "Entity bind to roles";
                                                                    par2.Add("@activity_id", typeof(Int64)).Value = request.Activity.ActivityId;
                                                                    par2.Add("@executed_by_entity_id", typeof(Int64)).Value = request.LastExecutedBy;

                                                                    db.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, null);

                                                                }

                                                            }


                                                        }

                                                        db.ExecuteNonQuery("update [st_workflow_request] set deployed = 1 where id = " + dr["id"]);
                                                        break;


                                                }
                                                
                                            }
                                            break;

                                        case WorkflowRequestStatus.Revoked:
                                            //Remove as permissões dadas
                                            switch (workflow.AccessType)
                                            {
                                                case WorkflowAccessType.RoleGrant:
                                                    WorkflowAccessRoleGrant rg = (WorkflowAccessRoleGrant)workflow.Access;

                                                    //Lista o nome e id de todas as roles que serão utilizadas
                                                    List<String> roleList = new List<String>();
                                                    foreach (Int64 r in rg.Roles)
                                                        roleList.Add(r.ToString());

                                                    String log = "";

                                                    DataTable drRoles = db.ExecuteDataTable("select distinct ir.*, r.name role_name from [role] r with(nolock) inner join identity_role ir with(nolock) on ir.role_id = r.id inner join [identity] i with(nolock) on ir.identity_id = i.id where i.entity_id = "+ request.UserId +" and r.id in (" + String.Join(",", roleList) + ")");
                                                    if ((drRoles != null) && (drRoles.Rows.Count > 0))
                                                    {
                                                        foreach (DataRow dr2 in drRoles.Rows)
                                                        {
                                                            log += "Identity unbind to role " + dr2["role_name"] + Environment.NewLine;

                                                            db.AddUserLog(LogKey.User_IdentityRoleUnbind, null, "Workflow", UserLogLevel.Info, 0, 0, 0, 0, 0, request.UserId, (Int64)dr2["identity_id"], "Identity unbind to role " + dr2["role_name"]);
                                                            db.ExecuteNonQuery("delete from identity_role where identity_id = " + dr2["identity_id"] + " and role_id = " + dr2["role_id"], CommandType.Text, null);
                                                        }

                                                        using (DbParameterCollection par2 = new DbParameterCollection())
                                                        {
                                                            par2.Add("@workflow_request_id", typeof(Int64)).Value = request.RequestId;
                                                            par2.Add("@status", typeof(String)).Value = (Int32)request.Status;
                                                            par2.Add("@description", typeof(String)).Value = log;
                                                            par2.Add("@activity_id", typeof(Int64)).Value = request.Activity.ActivityId;
                                                            par2.Add("@executed_by_entity_id", typeof(Int64)).Value = request.LastExecutedBy;

                                                            db.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, null);

                                                        }
                                                    }
                                                    else
                                                    {

                                                        using (DbParameterCollection par2 = new DbParameterCollection())
                                                        {
                                                            par2.Add("@workflow_request_id", typeof(Int64)).Value = request.RequestId;
                                                            par2.Add("@status", typeof(String)).Value = (Int32)request.Status;
                                                            par2.Add("@description", typeof(String)).Value = "No permission to remove";
                                                            par2.Add("@activity_id", typeof(Int64)).Value = request.Activity.ActivityId;
                                                            par2.Add("@executed_by_entity_id", typeof(Int64)).Value = request.LastExecutedBy;

                                                            db.ExecuteNonQuery("INSERT INTO [st_workflow_request_status]([workflow_request_id],[status],[description],[executed_by_entity_id],[activity_id])VALUES(@workflow_request_id,@status,@description,@executed_by_entity_id,@activity_id)", CommandType.Text, par2, null);

                                                        }
                                                    }

                                                    db.ExecuteNonQuery("update [st_workflow_request] set deployed = 1 where id = " + dr["id"]);
                                                    break;

                                            }
                                            break;

                                        case WorkflowRequestStatus.UnderReview:
                                            //Nada
                                            break;
                                    }

                                }
                                catch (Exception ex)
                                {
                                    db.AddUserLog(LogKey.Workflow, null, "Workflow", UserLogLevel.Info, 0, 0, 0, 0, 0, 0, 0, "Workflow proccess error", ex.Message);
                                }
                            }
                        }
                        finally
                        {
                            if (db != null)
                                db.Dispose();

                            TextLog.Log("WorkflowProcessor", "Finishing workflow processor timer");
                        }
                    }

                    db.closeDB();
                }
                finally
                {
                    if (db != null)
                        db.Dispose();
                }

            }
            catch (Exception ex)
            {
                TextLog.Log("WorkflowProcessor", "Error on message timer " + ex.Message);
            }
            finally
            {
                executing = false;
                last_status = "";
                startTime = new DateTime(1970, 1, 1);
            }
            
        }

        private void StopOnError(String text, Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine(text + ex.Message);
                TextLog.Log("WorkflowProcessor", text + ex.Message);
            }
            else
            {
                Console.WriteLine(text);
                TextLog.Log("WorkflowProcessor", text);
            }

            Process.GetCurrentProcess().Kill();
        }

        protected override void OnStop()
        {
            
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

    }
}
