using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using IAM.SQLDB;
using IAM.Config;
using IAM.GlobalDefs;

namespace IAM.Engine
{
    class LocalTheadObjects : IDisposable
    {
        public IAMDatabase db { get; set; }
        public LockRules lockRules { get; set; }
        public IgnoreRules ignoreRules { get; set; }
        public RoleRules roleRules { get; set; }

        public void Dispose()
        {
            if (db != null)
                db.Dispose();

            if (lockRules != null)
                lockRules.Dispose();


            if (ignoreRules != null)
                ignoreRules.Dispose();


            if (roleRules != null)
                roleRules.Dispose();

            db = null;
            lockRules = null;
            roleRules = null;
        }
    }

    /*
    class RegistryQueueItem
    {
        public String plugin_uri { get; internal set; }
        public Int64 resource_plugin_id { get; internal set; }
        public Int64 resource_id { get; internal set; }
        public Int64 plugin_id { get; internal set; }
        public String import_id { get; internal set; }
        public String package { get; internal set; }
        public String package_id { get; internal set; }
        public Int64 enterprise_id { get; internal set; }
        public Int64 context_id { get; internal set; }

        public RegistryQueueItem(Int64 enterprise_id, Int64 context_id, String plugin_uri, Int64 resource_id, Int64 plugin_id, Int64 resource_plugin_id, String import_id, String package_id, String package)
        {
            this.enterprise_id = enterprise_id;
            this.context_id = context_id;
            this.plugin_uri = plugin_uri;
            this.resource_id = resource_id;
            this.plugin_id = plugin_id;
            this.resource_plugin_id = resource_plugin_id;
            this.import_id = import_id;
            this.package_id = package_id;
            this.package = package;
        }
    }*/

    /*
    class RegistryQueue
    {
        private List<RegistryQueueItem> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public RegistryQueue()
        {
            _logItems = new List<RegistryQueueItem>();
        }

        public void Clear()
        {
            _logItems.Clear();
        }

        public void Add(Int64 enterprise_id, Int64 context_id, Int64 plugin_id, String plugin_uri, Int64 resource_id, String import_id, String registry_id)
        {
            Add(new RegistryQueueItem(enterprise_id, context_id, plugin_id, plugin_uri, resource_id, import_id, registry_id));
        }

        public void Add(RegistryQueueItem log)
        {
            lock (_logItems)
            {
                _logItems.Add(log);
            }
        }

        public RegistryQueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    RegistryQueueItem item = null;
                    lock (_logItems)
                    {
                        item = _logItems[0];
                        _logItems.RemoveAt(0);
                    }
                    return item;
                }
                catch
                {
                    return null;
                }
            }
        }
    }*/
}
