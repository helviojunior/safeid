using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;
using System.IO;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseFetchPackage: IDisposable
    {
        public String pkgId;
        public Int64 resourcePluginId;
        public Int64 fetchId;
        public Uri pluginUri;
        public Byte[] pluginRawData;
        public Dictionary<String, Object> config;

        public PluginConnectorBaseFetchPackage() {
            this.pkgId = Guid.NewGuid().ToString();
            this.config = new Dictionary<string, object>();
        }

        public PluginConnectorBaseFetchPackage(Int64 resourcePluginId, Uri pluginUri, Byte[] pluginRawData, Dictionary<String, Object> config)
            : this()
        {
            
            this.resourcePluginId = resourcePluginId;
            this.pluginRawData = pluginRawData;
            this.config = config;
            this.pluginUri = pluginUri;
        }

        public void Dispose()
        {
            this.pkgId = null;
            if (pluginRawData != null)
                Array.Clear(pluginRawData, 0, pluginRawData.Length);
            pluginRawData = null;
            this.pluginUri = null;
        }
    }

}
