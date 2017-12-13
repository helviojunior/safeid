using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.Workflow
{

    public enum WorkflowAccessType
    {
        None = 0,
        RoleGrant = 1,
        Delegation = 2,
        Unlock = 3
    }


    [Serializable()]
    public abstract class WorkflowAccessBase : IDisposable
    {

        public void Dispose() { }
    }


    [Serializable()]
    public class WorkflowAccessRoleGrant : WorkflowAccessBase, IDisposable
    {
        private List<Int64> roles;

        public List<Int64> Roles { get { return roles; } }

        public WorkflowAccessRoleGrant()
        {
            this.roles = new List<Int64>();
        }

        public WorkflowAccessRoleGrant(IEnumerable<Int64> roles)
            :this()
        {
            AddRange(roles);
        }

        public void Add(Int64 role)
        {
            this.roles.Add(role);
        }

        public void AddRange(IEnumerable<Int64> roles)
        {
            this.roles.AddRange(roles);
        }

        public void Dispose()
        {
            if (roles != null) roles.Clear();
            roles = null;
        }
    }


    [Serializable()]
    public class WorkflowAccessUnlock : WorkflowAccessBase, IDisposable
    {
        public void Dispose() { }
    }


    [Serializable()]
    public class WorkflowAccessDelegation : WorkflowAccessBase, IDisposable
    {
        private Int64 entity;

        public Int64 Entity { get { return entity; } set { entity = value; } }

        public WorkflowAccessDelegation() { }

        public WorkflowAccessDelegation(Int64 entity) { this.entity = entity; }


        public void Dispose() { }
    }
}
