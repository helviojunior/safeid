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
    public class PluginConnectorBaseFetchResult: IDisposable
    {
        public String pkgId;
        public Int64 resourcePluginId;
        public Int64 fetchId;
        public Uri pluginUri;
        public Boolean success;
        public Dictionary<String, List<String>> fields;

        public PluginConnectorBaseFetchResult()
        {
            this.fields = new Dictionary<string, List<string>>();
            this.success = false;
        }

        public void Dispose()
        {
            this.pkgId = null;
            if (fields != null)
                fields = null;
            fields = null;
            this.pluginUri = null;
        }
    }

}
