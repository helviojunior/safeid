using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;
using System.IO;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseDeployPackageAction: IDisposable
    {
        public String roleName;
        public String actionKey;
        public String actionValue;
        public PluginActionType actionType;
        public String additionalData;

        public PluginConnectorBaseDeployPackageAction(PluginActionType actionType, String roleName, String actionKey, String actionValue)
            : this(actionType, roleName, actionKey, actionValue, "") { }

        public PluginConnectorBaseDeployPackageAction(PluginActionType actionType, String roleName, String actionKey, String actionValue, String additionalData)
        {
            this.actionKey = actionKey;
            this.roleName = roleName;
            this.actionValue = actionValue;
            this.actionType = actionType;
            this.additionalData = additionalData;
        }

        public override string ToString()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(PluginConnectorBaseDeployPackageAction));

            String ret = "";

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, this);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }

        public void Dispose()
        {
            this.roleName = null;
            this.actionKey = null;
            this.actionValue = null;
            this.additionalData = null;
        }
    }
}
