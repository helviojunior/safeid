using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseDeployPackage: IDisposable
    {
        [OptionalField()]
        public String pkgId;
        public String registryId;
        public Int64 entityId;
        public Int64 identityId;
        public FullName fullName;
        public String login;
        [OptionalField()]
        public String enterprise;
        [OptionalField()]
        public String context;
        [OptionalField()]
        public String password;
        [OptionalField()]
        public HashAlg hash_alg;
        [OptionalField()]
        public String container;
        public Boolean locked;
        [OptionalField()]
        public Boolean temp_locked;
        [OptionalField()]
        public Boolean deleted;
        [OptionalField()]
        public Boolean mustChangePassword;
        [OptionalField()]
        public String lastChangePassword;
        public List<PluginConnectorBasePackageData> properties;
        public List<PluginConnectorBasePackageData> ids;
        public List<PluginConnectorBasePackageData> pluginData;
        public List<PluginConnectorBasePackageData> importsPluginData;
        public List<PluginConnectorBasePackageData> entiyData;
        [OptionalField()]
        public List<PluginConnectorBaseDeployPackageAction> pluginAction;

        public PluginConnectorBaseDeployPackage()
        {
            properties = new List<PluginConnectorBasePackageData>();
            ids = new List<PluginConnectorBasePackageData>();
            pluginData = new List<PluginConnectorBasePackageData>();
            importsPluginData = new List<PluginConnectorBasePackageData>();
            pluginAction = new List<PluginConnectorBaseDeployPackageAction>();
            entiyData = new List<PluginConnectorBasePackageData>();
            this.pkgId = Guid.NewGuid().ToString();
        }

        public PluginConnectorBaseDeployPackage(String registryId, Int64 entityId, Int64 identityId, String fullName, String password, String container, Boolean locked, DateTime? lastChangePassword, Boolean deleted) :
            this(registryId, entityId, identityId, fullName, password, container, locked, lastChangePassword, false, deleted) { }

        public PluginConnectorBaseDeployPackage(String registryId, Int64 entityId, Int64 identityId, String fullName, String password, String container, Boolean locked, DateTime? lastChangePassword, Boolean mustChangePassword, Boolean deleted) :
            this()
        {

            this.registryId = registryId;
            this.entityId = entityId;
            this.identityId = identityId;
            if (lastChangePassword.HasValue)
                this.lastChangePassword = lastChangePassword.Value.ToString("yyyy-MM-dd HH:mm:ss");
            this.fullName = new FullName(fullName);
            this.password = password;
            this.container = container;
            this.locked = locked;
            this.mustChangePassword = mustChangePassword;
            this.deleted = deleted;

            if (this.deleted)
                this.locked = true;
        }

        public void Dispose()
        {
            this.pkgId = null;
            this.registryId = null;
            this.fullName = null;
            this.login = null;
            this.password = null;
            this.container = null;
            this.lastChangePassword = null;

            if (properties != null)
            {
                foreach (PluginConnectorBasePackageData p in properties)
                    if (p != null) p.Dispose();

                properties.Clear();
            }
            properties = null;

            if (ids != null)
            {
                foreach (PluginConnectorBasePackageData t in ids)
                    if (t != null) t.Dispose();

                ids.Clear();
            }
            ids = null;


            if (pluginData != null)
            {
                foreach (PluginConnectorBasePackageData t in pluginData)
                    if (t != null) t.Dispose();
                pluginData.Clear();
            }
            pluginData = null;


            if (importsPluginData != null)
            {
                foreach (PluginConnectorBasePackageData t in importsPluginData)
                    if (t != null) t.Dispose();
                importsPluginData.Clear();
            }
            importsPluginData = null;


            if (pluginAction != null)
            {
                foreach (PluginConnectorBaseDeployPackageAction t in pluginAction)
                    if (t != null) t.Dispose();
                pluginAction.Clear();
            }
            pluginAction = null;

            if (entiyData != null)
            {
                foreach (PluginConnectorBasePackageData t in entiyData)
                    if (t != null) t.Dispose();
                entiyData.Clear();
            }
            entiyData = null;


        }
    }

}
