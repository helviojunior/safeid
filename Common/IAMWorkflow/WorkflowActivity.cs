using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.Workflow
{

    [Serializable()]
    public class WorkflowActivity : IDisposable
    {
        private Int64 activity_id;
        private String name;
        private Int64 auto_approval;
        private Int64 auto_deny;
        private Int32 escalation_days;
        private Int32 expiration_days;
        private Int32 exeution_order;
        private WorkflowActivityManualApproval manual_approval;

        public Int64 ActivityId { get { return activity_id; } set { activity_id = value; } }
        public String Name { get { return name; } }
        public Int64 AutoApproval { get { return auto_approval; } }
        public Int64 AutoDeny { get { return auto_deny; } }
        public Int32 EscalationDays { get { return escalation_days; } }
        public Int32 ExpirationDays { get { return expiration_days; } }
        public Int32 ExeutionOrder { get { return exeution_order; } set { exeution_order = value; } }
        public WorkflowActivityManualApproval ManualApproval { get { return manual_approval; } }

        public WorkflowActivity()
        {

        }

        public WorkflowActivity(String name, Int64 autoApproval, Int64 autoDeny, Int32 escalationDays, Int32 expirationDays)
            : this(name, autoApproval, autoDeny, escalationDays, expirationDays, 0, 0) { }

        public WorkflowActivity(String name, Int64 autoApproval, Int64 autoDeny, Int32 escalationDays, Int32 expirationDays, Int64 entityApprover, Int64 roleApprover)
        {
            this.name = name;
            this.auto_approval = autoApproval;
            this.auto_deny = autoDeny;
            this.escalation_days = escalationDays;
            this.expiration_days = expirationDays;
            this.exeution_order = 0;

            this.SetApprover(entityApprover, roleApprover);
        }

        public void SetApprover(Int64 entityApprover, Int64 roleApprover)
        {
            if ((entityApprover > 0) || (roleApprover > 0))
                this.manual_approval = new WorkflowActivityManualApproval(entityApprover, roleApprover);
        }

        public void Dispose()
        {

        }
    }

    [Serializable()]
    public class WorkflowActivityManualApproval : IDisposable
    {
        private Int64 entity_approver;
        private Int64 role_approver;

        public Int64 EntityApprover { get { return entity_approver; } }
        public Int64 RoleApprover { get { return role_approver; } }

        public WorkflowActivityManualApproval(Int64 entityApprover, Int64 roleApprover)
        {
            this.entity_approver = entityApprover;
            this.role_approver = roleApprover;
        }

        public void Dispose()
        {

        }
    }

}
