using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{

    public delegate void LogEvent(Object sender, PluginLogType type, String text);
    public delegate void LogEvent2(Object sender, PluginLogType type, Int64 entityId, Int64 identityId, String text, String additionalData);
    public delegate void NotityChangeUserEvent(Object sender, Int64 entityId, Int64 identityId = 0);
    //public delegate void RegistryEvent(String importId, String registryId, String dataName, String dataValue, String dataType);
    public delegate void ImportPackageUserEvent(PluginConnectorBaseImportPackageUser package);
    public delegate void ImportPackageStructEvent(PluginConnectorBaseImportPackageStruct package);

}
