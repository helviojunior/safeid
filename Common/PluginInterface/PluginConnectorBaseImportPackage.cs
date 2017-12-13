using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseImportPackage: IDisposable
    {

        public String pkgId;
        public String importId;
        public String build_data;

        public List<PluginConnectorBasePackageData> properties;

        public PluginConnectorBaseImportPackage(String importId)
        {
            this.pkgId = Guid.NewGuid().ToString();
            this.build_data = DateTime.Now.ToString("o");
            this.properties = new List<PluginConnectorBasePackageData>();
            this.importId = importId;
        }

        public void AddProperty(String dataName, String dataValue, String dataType)
        {
            AddProperty(new PluginConnectorBasePackageData(dataName, dataValue, dataType));
        }

        public void AddProperty(PluginConnectorBasePackageData propertyData)
        {
            properties.Add(propertyData);
        }

        public void Dispose()
        {
            this.pkgId = null;
            this.importId = null;
            this.build_data = null;

            if (properties != null)
            {
                foreach (PluginConnectorBasePackageData p in properties)
                    if (p != null) p.Dispose();

                properties.Clear();
            }
            properties = null;


        }
    }

}
