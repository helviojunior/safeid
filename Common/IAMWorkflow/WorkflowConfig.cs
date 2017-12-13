using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;
using SafeTrend.Data;
using System.Data;
using System.Data.SqlClient;

namespace IAM.Workflow
{

    [Serializable()]
    public class WorkflowConfig : IDisposable
    {
        private Int64 workflow_id;
        private Int64 context_id;
        private String name;
        private String description;
        private Int64 owner;
        private Boolean enabled;
        private Boolean deleted;
        private Boolean deprecated;
        private Int64 original_id;
        private Int64 version;
        private DateTime create_date;

        private WorkflowAccessType access_type;
        private WorkflowAccessBase access;
        private List<WorkflowActivity> activities;

        public Int64 WorkflowId { get { return workflow_id; } }
        public Int64 ContextId { get { return context_id; } set { context_id = value; } }
        public String Name { get { return name; } set { name = value; } }
        public String Description { get { return description; } set { description = value; } }
        public Int64 Owner { get { return owner; } set { owner = value; } }
        public Boolean Enabled { get { return enabled; } set { enabled = value; } }
        public Boolean Deleted { get { return deleted; } set { deleted = value; } }
        public Boolean Deprecated { get { return deprecated; } set { deprecated = value; } }
        public Int64 Original_id { get { return original_id; } set { original_id = value; } }
        public Int64 Version { get { return version; } set { version = value; } }
        public DateTime CreateDate { get { return create_date; } set { create_date = value; } }

        public WorkflowAccessType AccessType { get { return access_type; } set { access_type = value; } }
        public WorkflowAccessBase Access { get { return access; } }
        public List<WorkflowActivity> Activities { get { return activities; } }

        public WorkflowConfig()
        {
            this.activities = new List<WorkflowActivity>();
            this.access_type = WorkflowAccessType.None;
        }


        public WorkflowConfig(Int64 contextId, String name, String description, Int64 owner, WorkflowAccessType accessType)
            : this()
        {
            this.context_id = contextId;
            this.name = name;
            this.description = description;
            this.owner = owner;
            this.access_type = accessType;
            this.enabled = true;
            this.deleted = false;
            this.deprecated = false;
            this.original_id = 0;
            this.version = 1;
            this.create_date = DateTime.Now;

            if (this.access_type == WorkflowAccessType.Unlock)
                this.access = new WorkflowAccessUnlock();
        }

        public void SetAccess(IEnumerable<Int64> roles)
        {
            if (this.access_type != WorkflowAccessType.RoleGrant)
                throw new Exception("Invalid type of access");

            this.access = new WorkflowAccessRoleGrant(roles);
        }

        public void SetAccess(Int64 entity)
        {
            if (this.access_type != WorkflowAccessType.Delegation)
                throw new Exception("Invalid type of access");

            this.access = new WorkflowAccessDelegation(entity);
        }

        public void AddActivity(String name, Int64 autoApproval, Int64 autoDeny, Int32 escalationDays, Int32 expirationDays)
        {
            AddActivity(new WorkflowActivity(name, autoApproval, autoDeny, escalationDays, expirationDays));
        }

        public void AddActivity(String name, Int64 autoApproval, Int64 autoDeny, Int32 escalationDays, Int32 expirationDays, Int64 entityApprover, Int64 roleApprover)
        {
            AddActivity(new WorkflowActivity(name, autoApproval, autoDeny, escalationDays, expirationDays, entityApprover, roleApprover));
        }

        public void AddActivity(WorkflowActivity activity)
        {
            this.activities.Add(activity);
        }

        public void GetDatabaseData(IAMDatabase database, Int64 workflowId)
        {
            GetDatabaseData(database, workflowId, null);
        }

        public void GetDatabaseData(IAMDatabase database, Int64 workflowId, Object transaction)
        {
            this.workflow_id = workflowId;

            DataTable dtWorkflow = database.ExecuteDataTable("select * from st_workflow where id = " + this.workflow_id, CommandType.Text, null, transaction);
            if ((dtWorkflow == null) || (dtWorkflow.Rows.Count == 0))
                throw new Exception("Workflow not found");

            this.context_id = (Int64)dtWorkflow.Rows[0]["context_id"];
            this.name = (String)dtWorkflow.Rows[0]["name"];
            this.description = (String)dtWorkflow.Rows[0]["description"];
            this.owner = (Int64)dtWorkflow.Rows[0]["owner_id"];
            this.enabled = (Boolean)dtWorkflow.Rows[0]["enabled"];
            this.deleted = (Boolean)dtWorkflow.Rows[0]["deleted"];
            this.deprecated = (Boolean)dtWorkflow.Rows[0]["deprecated"];
            this.original_id = (Int64)dtWorkflow.Rows[0]["original_id"];
            this.version = (Int64)dtWorkflow.Rows[0]["version"];
            this.create_date = (DateTime)dtWorkflow.Rows[0]["create_date"];

            switch (dtWorkflow.Rows[0]["type"].ToString().ToLower())
            {
                case "rolegrant":
                    this.access_type = WorkflowAccessType.RoleGrant;
                    break;

                case "delegation":
                    this.access_type = WorkflowAccessType.Delegation;
                    break;

                case "unlock":
                    this.access_type = WorkflowAccessType.Unlock;
                    break;

                default:
                    throw new Exception(String.Format("Access type {0} not implemented yet", dtWorkflow.Rows[0]["type"]));
                    break;
            }

            switch (this.access_type)
            {
                case WorkflowAccessType.RoleGrant:
                    WorkflowAccessRoleGrant roleGrant = new WorkflowAccessRoleGrant();

                    DataTable dtRG = database.ExecuteDataTable("select * from st_workflow_access_role where workflow_id = " + this.workflow_id, CommandType.Text, null, transaction);
                    if (dtRG != null)
                        foreach (DataRow dr in dtRG.Rows)
                            roleGrant.Add((Int64)dr["role_id"]);

                    if ((roleGrant.Roles == null) || (roleGrant.Roles.Count == 0))
                        throw new Exception("Role list is empty");

                    this.access = roleGrant;
                    break;

                case WorkflowAccessType.Delegation:
                    WorkflowAccessDelegation entityDelegation = new WorkflowAccessDelegation();


                    DataTable dtED = database.ExecuteDataTable("select * from st_workflow_access_role where workflow_id = " + this.workflow_id, CommandType.Text, null, transaction);
                    if (dtED != null)
                        foreach (DataRow dr in dtED.Rows)
                            entityDelegation.Entity = (Int64)dr["entity_id"];

                    if (entityDelegation.Entity == 0)
                        throw new Exception("Entity id is empty");

                    this.access = entityDelegation;
                    break;

                case WorkflowAccessType.Unlock:
                    this.access = new WorkflowAccessUnlock();
                    break;
            }

            DataTable dtActivity = database.ExecuteDataTable("select * from st_workflow_activity where workflow_id = " + this.workflow_id + " order by execution_order", CommandType.Text, null, transaction);
            if (dtActivity != null)
                foreach (DataRow dr in dtActivity.Rows)
                {
                    WorkflowActivity activity = new WorkflowActivity(
                        dr["name"].ToString(),
                        (dr["auto_approval"] == DBNull.Value ? 0 : (Int64)dr["auto_approval"]),
                        (dr["auto_deny"] == DBNull.Value ? 0 : (Int64)dr["auto_deny"]),
                        (Int32)dr["escalation_days"],
                        (Int32)dr["expiration_days"]
                        );

                    activity.ActivityId = (Int64)dr["id"];
                    activity.ExeutionOrder = (Int32)dr["execution_order"];

                    DataTable dtManual = database.ExecuteDataTable("select * from st_workflow_activity_manual_approval where workflow_activity_id = " + activity.ActivityId, CommandType.Text, null, transaction);
                    if (dtManual != null)
                        foreach (DataRow dr2 in dtManual.Rows)
                            activity.SetApprover(
                                (dr2["entity_approver"] == DBNull.Value ? 0 : (Int64)dr2["entity_approver"]),
                                (dr2["role_approver"] == DBNull.Value ? 0 : (Int64)dr2["role_approver"])
                                );

                    if ((activity.AutoDeny == 0) && (activity.AutoDeny == 0) && (activity.ManualApproval == null || (activity.ManualApproval.EntityApprover == 0 && activity.ManualApproval.RoleApprover == 0)))
                        throw new Exception("All activity approvers is empty in activity " + activity.Name);

                    this.activities.Add(activity);
                }

            if (this.activities.Count == 0)
                throw new Exception("Activity list is empty");

        }

        public void SaveToDatabase(IAMDatabase database)
        {
            if (this.context_id == 0)
                throw new Exception("ContextId can not be empty");

            if (String.IsNullOrEmpty(this.name))
                throw new Exception("EnterpriseId can not be empty");

            if (String.IsNullOrEmpty(this.description))
                this.description = "";

            if (this.owner == 0)
                throw new Exception("EnterpriseId can not be empty");

            if (this.access == null)
                throw new Exception("Access can not be empty");

            if ((this.activities == null) || (this.activities.Count == 0))
                throw new Exception("Activities list can not be empty");

            Boolean newWorkflow = (this.workflow_id == 0);

            Object trans = database.BeginTransaction();
            try
            {

                if (newWorkflow)//New config
                {
                    using (DbParameterCollection par = new DbParameterCollection())
                    {
                        par.Add("@context_id", typeof(Int64)).Value = this.context_id;
                        par.Add("@name", typeof(String)).Value = this.name;
                        par.Add("@description", typeof(String)).Value = this.description;
                        par.Add("@owner", typeof(Int64)).Value = this.owner;
                        par.Add("@enabled", typeof(Int64)).Value = this.owner;
                        par.Add("@type", typeof(String)).Value = this.access_type.ToString().ToLower();

                        DataTable dtNewWorkflow = database.ExecuteDataTable("sp_st_new_workflow", CommandType.StoredProcedure, par, trans);
                        if ((dtNewWorkflow == null) || (dtNewWorkflow.Rows.Count == 0))
                            throw new Exception("Database error on insert workflow");

                        this.workflow_id = (Int64)dtNewWorkflow.Rows[0]["id"];
                    }
                }
                else//update config
                {
                    using (DbParameterCollection par = new DbParameterCollection())
                    {
                        par.Add("@workflow_id", typeof(Int64)).Value = this.workflow_id;
                        par.Add("@name", typeof(String)).Value = this.name;
                        par.Add("@description", typeof(String)).Value = this.description;
                        par.Add("@owner", typeof(Int64)).Value = this.owner;
                        par.Add("@type", typeof(String)).Value = this.access_type.ToString().ToLower();
                        par.Add("@enabled", typeof(Int64)).Value = this.enabled;

                        //Na atualização a trigger irá criar um novo ID, desta forma retorna o novo ID
                        this.workflow_id = database.ExecuteScalar<Int64>("update [st_workflow] set name = @name, description = @description, owner_id = @owner, [type] = @type, [enabled] = @enabled WHERE id = @workflow_id; select MAX(id) id from st_workflow where (id = @workflow_id and [deprecated] = 0) or ([original_id] = @workflow_id and [deprecated] = 0)", CommandType.Text, par, trans);
                    }
                }

                //Exclui todos os access
                database.ExecuteNonQuery("delete from st_workflow_access_entity where workflow_id = " + this.workflow_id + "; delete from st_workflow_access_role where workflow_id = " + this.workflow_id, CommandType.Text, null, trans);
                switch (access_type)
                {
                    case WorkflowAccessType.RoleGrant:
                        WorkflowAccessRoleGrant roleGrant = ((WorkflowAccessRoleGrant)this.access);

                        if ((roleGrant.Roles == null) || (roleGrant.Roles.Count == 0))
                            throw new Exception("Role list can not be empty");

                        foreach (Int64 role in roleGrant.Roles)
                            using (DbParameterCollection par = new DbParameterCollection())
                            {
                                par.Add("@workflow_id", typeof(Int64)).Value = this.workflow_id;
                                par.Add("@role_id", typeof(Int64)).Value = role;

                                database.ExecuteNonQuery("INSERT INTO st_workflow_access_role (workflow_id, role_id) VALUES (@workflow_id, @role_id)", CommandType.Text, par, trans);
                            }
                        break;

                    case WorkflowAccessType.Delegation:
                        WorkflowAccessDelegation entityDelegation = ((WorkflowAccessDelegation)this.access);

                        if (entityDelegation.Entity == 0)
                            throw new Exception("Entity id can not be empty");

                        using (DbParameterCollection par = new DbParameterCollection())
                        {
                            par.Add("@workflow_id", typeof(Int64)).Value = this.workflow_id;
                            par.Add("@entity_id", typeof(Int64)).Value = entityDelegation.Entity;

                            database.ExecuteNonQuery("INSERT INTO st_workflow_access_entity (workflow_id, entity_id) VALUES (@workflow_id, @entity_id)", CommandType.Text, par, trans);
                        }
                        break;

                    case WorkflowAccessType.Unlock:
                        //Nada
                        break;
                }

                //Activities
                List<String> activitiesIds = new List<String>();
                Int32 order = 0;
                foreach (WorkflowActivity activity in this.activities)
                {
                    if ((activity.AutoDeny == 0) && (activity.AutoDeny == 0) && (activity.ManualApproval == null || (activity.ManualApproval.EntityApprover == 0 && activity.ManualApproval.RoleApprover == 0)))
                        throw new Exception("All activity approvers is empty");

                    activity.ExeutionOrder = ++order;

                    if (activity.ActivityId == 0)//Novo
                    {
                        //SELECT SCOPE_IDENTITY()
                        using (DbParameterCollection par = new DbParameterCollection())
                        {
                            par.Add("@workflow_id", typeof(Int64)).Value = this.workflow_id;
                            par.Add("@name", typeof(String)).Value = activity.Name;
                            par.Add("@escalation_days", typeof(Int32)).Value = activity.EscalationDays;
                            par.Add("@expiration_days", typeof(Int32)).Value = activity.ExpirationDays;
                            par.Add("@auto_deny", typeof(Int64)).Value = activity.AutoDeny;
                            par.Add("@auto_approval", typeof(Int64)).Value = activity.AutoApproval;
                            par.Add("@execution_order", typeof(Int32)).Value = activity.ExeutionOrder;

                            activity.ActivityId = database.ExecuteScalar<Int64>("INSERT INTO st_workflow_activity ([workflow_id],[name],[escalation_days],[expiration_days],[auto_deny],[auto_approval],[execution_order]) VALUES (@workflow_id,@name,@escalation_days,@expiration_days," + (activity.AutoDeny > 0 ? "@auto_deny" : "null") + "," + (activity.AutoApproval > 0 ? "@auto_approval" : "null") + ",@execution_order); SELECT SCOPE_IDENTITY()", CommandType.Text, par, trans);

                        }
                    }
                    else//Atualiza
                    {
                        using (DbParameterCollection par = new DbParameterCollection())
                        {
                            par.Add("@activity_id", typeof(Int64)).Value = activity.ActivityId;
                            par.Add("@workflow_id", typeof(Int64)).Value = this.workflow_id;
                            par.Add("@name", typeof(String)).Value = activity.Name;
                            par.Add("@escalation_days", typeof(Int32)).Value = activity.EscalationDays;
                            par.Add("@expiration_days", typeof(Int32)).Value = activity.ExpirationDays;
                            par.Add("@auto_deny", typeof(Int64)).Value = activity.AutoDeny;
                            par.Add("@auto_approval", typeof(Int64)).Value = activity.AutoApproval;
                            par.Add("@execution_order", typeof(Int32)).Value = activity.ExeutionOrder;

                            database.ExecuteNonQuery("UPDATE [st_workflow_activity] SET [name] = @name ,[escalation_days] = @escalation_days ,[expiration_days] = @expiration_days ,[auto_deny] = " + (activity.AutoDeny > 0 ? "@auto_deny" : "null") + " ,[auto_approval] = " + (activity.AutoApproval > 0 ? "@auto_approval" : "null") + ", [execution_order] = @execution_order WHERE [workflow_id] = @workflow_id", CommandType.Text, par, trans);

                        }
                    }

                    //Adiciona as activities que estão sendo utilizadas, para que posteriormente possam ser excluidas as não utilizadas
                    activitiesIds.Add(activity.ActivityId.ToString());

                    //Exclui todas as aprovações manuais
                    database.ExecuteNonQuery("delete from st_workflow_activity_manual_approval where workflow_activity_id = " + activity.ActivityId, CommandType.Text, null, trans);
                    if (activity.ManualApproval != null && (activity.ManualApproval.EntityApprover != 0 || activity.ManualApproval.RoleApprover != 0))
                    {
                        using (DbParameterCollection par = new DbParameterCollection())
                        {
                            par.Add("@workflow_activity_id", typeof(Int64)).Value = activity.ActivityId;
                            par.Add("@entity_approver", typeof(Int64)).Value = activity.ManualApproval.EntityApprover;
                            par.Add("@role_approver", typeof(Int64)).Value = activity.ManualApproval.RoleApprover;

                            database.ExecuteNonQuery("INSERT INTO [st_workflow_activity_manual_approval] ([workflow_activity_id],[entity_approver],[role_approver])VALUES (@workflow_activity_id ," + (activity.ManualApproval.EntityApprover > 0 ? "@entity_approver" : "null") + " ," + (activity.ManualApproval.RoleApprover > 0 ? "@role_approver" : "null") + ")", CommandType.Text, par, trans);

                        }
                    }

                }

                //Exclui as activities que não fazem mais parte deste workflow
                database.ExecuteNonQuery("delete from st_workflow_activity where workflow_id = " + this.workflow_id + " and id not in (" + String.Join(",", activitiesIds) + ")", CommandType.Text, null, trans);

                database.Commit();

            }
            catch (Exception ex)
            {
                String tst = ex.ToString();
                database.Rollback();
                throw ex;
            }
        }

        public void ParseFromJsonBase64String(String jsonBase64Data)
        {
            this.ParseFromJsonString(Encoding.UTF8.GetString(Convert.FromBase64String(jsonBase64Data)));
        }

        public void ParseFromJsonString(String jsonData)
        {
            this.ParseFromJsonObject(SafeTrend.Json.JSON.Deserialize2<Dictionary<String, Object>>(jsonData));
        }

        public String ToJsonString()
        {
            return SafeTrend.Json.JSON.Serialize2(this.ToJsonObject());
        }


        public void ParseFromJsonObject(Dictionary<String, Object> data)
        {
            this.context_id = Int64.Parse(data["context_id"].ToString());
            this.workflow_id = Int64.Parse(data["workflow_id"].ToString());
            this.name = (String)data["name"];
            this.description = (String)data["description"];
            this.owner = Int64.Parse(data["owner_id"].ToString());
            this.enabled = Boolean.Parse(data["enabled"].ToString());
            this.deleted = Boolean.Parse(data["deleted"].ToString());
            this.deprecated = Boolean.Parse(data["deprecated"].ToString());
            this.original_id = Int64.Parse(data["original_id"].ToString());
            this.version = Int64.Parse(data["version"].ToString());
            this.create_date = new DateTime(1970, 1, 1).AddSeconds(Int64.Parse(data["create_date"].ToString()));

            switch (data["type"].ToString().ToLower())
            {
                case "rolegrant":
                    this.access_type = WorkflowAccessType.RoleGrant;
                    break;

                case "delegation":
                    this.access_type = WorkflowAccessType.Delegation;
                    break;

                case "unlock":
                    this.access_type = WorkflowAccessType.Unlock;
                    break;

                default:
                    throw new Exception(String.Format("Access type {0} not implemented yet", data["type"]));
                    break;
            }


            //if (!(lst[i] is Dictionary<String, Object>))
            //if (!(parameters["mapping"] is ArrayList))


            if (!(data["access"] is Dictionary<String, Object>))
                throw new Exception("Access is not valid");

            Dictionary<String, Object> access = (Dictionary<String, Object>)data["access"];
            switch (this.access_type)
            {
                case WorkflowAccessType.RoleGrant:
                    WorkflowAccessRoleGrant roleGrant = new WorkflowAccessRoleGrant();

                    if (!(access["role_id"] is ArrayList))
                        throw new Exception("Access is not valid");

                    List<Object> lst = new List<Object>();
                    lst.AddRange(((ArrayList)access["role_id"]).ToArray());

                    foreach (Object r in lst)
                        roleGrant.Add(Int64.Parse(r.ToString()));

                    if ((roleGrant.Roles == null) || (roleGrant.Roles.Count == 0))
                        throw new Exception("Role list is empty");

                    this.access = roleGrant;
                    break;

                case WorkflowAccessType.Delegation:
                    WorkflowAccessDelegation entityDelegation = new WorkflowAccessDelegation();

                    try
                    {
                        entityDelegation.Entity = Int64.Parse(access["entity_id"].ToString());
                    }
                    catch
                    {
                        throw new Exception("Access is not valid");
                    }

                    if (entityDelegation.Entity == 0)
                        throw new Exception("Entity id is empty");

                    this.access = entityDelegation;
                    break;

                case WorkflowAccessType.Unlock:
                    this.access = new WorkflowAccessUnlock();
                    break;
            }


            if (!(data["activities"] is ArrayList))
                throw new Exception("Activity list is not valid");

            List<Object> act = new List<Object>();
            act.AddRange(((ArrayList)data["activities"]).ToArray());

            for (Int32 i = 0; i < act.Count; i++)
            {

                if (!(act[i] is Dictionary<String, Object>))
                    throw new Exception("Activity " + i + " is not valid");

                Dictionary<String, Object> a = (Dictionary<String, Object>)act[i];

                WorkflowActivity activity = new WorkflowActivity(
                    a["name"].ToString(),
                    Int64.Parse(a["auto_approval"].ToString()),
                    Int64.Parse(a["auto_deny"].ToString()),
                    Int32.Parse(a["escalation_days"].ToString()),
                    Int32.Parse(a["expiration_days"].ToString())
                    );

                activity.ActivityId = Int64.Parse(a["activity_id"].ToString());

                if (a.ContainsKey("manual_approval") && (a["manual_approval"] is Dictionary<string, object>))
                {
                    activity.SetApprover(
                            Int64.Parse(((Dictionary<string, object>)a["manual_approval"])["entity_approver"].ToString()),
                            Int64.Parse(((Dictionary<string, object>)a["manual_approval"])["role_approver"].ToString())
                            );
                }

                if ((activity.AutoDeny == 0) && (activity.AutoDeny == 0) && (activity.ManualApproval == null || (activity.ManualApproval.EntityApprover == 0 && activity.ManualApproval.RoleApprover == 0)))
                    throw new Exception("All activity approvers is empty in activity " + activity.Name);

                this.activities.Add(activity);
            }

            if (this.activities.Count == 0)
                throw new Exception("Activity list is empty");

        }

        public Dictionary<String, Object> ToJsonObject()
        {
            Dictionary<String, Object> ret = new Dictionary<string, object>();

            ret.Add("workflow_id", this.workflow_id);
            ret.Add("context_id", this.context_id);
            ret.Add("name", this.name);
            ret.Add("description", this.description);
            ret.Add("owner_id", this.owner);
            ret.Add("type", this.access_type.ToString().ToLower());
            ret.Add("enabled", this.enabled);
            ret.Add("deleted", this.deleted);
            ret.Add("deprecated", this.deprecated);
            ret.Add("original_id", this.original_id);
            ret.Add("version", this.version);
            ret.Add("create_date", (Int64)(this.create_date - new DateTime(1970,1,1)).TotalSeconds);

            Dictionary<String, Object> access = new Dictionary<string, object>();
            switch (access_type)
            {
                case WorkflowAccessType.RoleGrant:
                    WorkflowAccessRoleGrant roleGrant = ((WorkflowAccessRoleGrant)this.access);

                    if ((roleGrant.Roles == null) || (roleGrant.Roles.Count == 0))
                        throw new Exception("Role list can not be empty");

                    access.Add("role_id", roleGrant.Roles.ToArray());
                    break;

                case WorkflowAccessType.Delegation:
                    WorkflowAccessDelegation entityDelegation = ((WorkflowAccessDelegation)this.access);

                    if (entityDelegation.Entity == 0)
                        throw new Exception("Entity id can not be empty");

                    access.Add("entity_id", entityDelegation.Entity);

                    break;

                case WorkflowAccessType.Unlock:
                    //Nada
                    break;
            }

            ret.Add("access", access);

            //Activities
            List<Dictionary<String, Object>> act = new List<Dictionary<string, object>>();
            foreach (WorkflowActivity activity in this.activities)
            {
                if ((activity.AutoDeny == 0) && (activity.AutoDeny == 0) && (activity.ManualApproval == null || (activity.ManualApproval.EntityApprover == 0 && activity.ManualApproval.RoleApprover == 0)))
                    throw new Exception("All activity approvers is empty");

                Dictionary<string, object> a = new Dictionary<string, object>();

                a.Add("activity_id", activity.ActivityId);
                a.Add("name", activity.Name);
                a.Add("escalation_days", activity.EscalationDays);
                a.Add("expiration_days", activity.ExpirationDays);
                a.Add("auto_deny", activity.AutoDeny);
                a.Add("auto_approval", activity.AutoApproval);

                if (activity.ManualApproval != null && (activity.ManualApproval.EntityApprover != 0 || activity.ManualApproval.RoleApprover != 0))
                {
                    Dictionary<string, object> manual_approval = new Dictionary<string, object>();

                    manual_approval.Add("entity_approver", activity.ManualApproval.EntityApprover);
                    manual_approval.Add("role_approver", activity.ManualApproval.RoleApprover);

                    a.Add("manual_approval", manual_approval);
                }

                act.Add(a);
            }

            ret.Add("activities", act);

            return ret;
        }

        public void Dispose()
        {

        }
    }

}
