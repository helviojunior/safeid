using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseImportPackageUser: IDisposable
    {

        public String pkgId;
        public String importId;
        public String build_data;

        public String container;

        public List<PluginConnectorBasePackageData> properties;
        public List<String> groups;

        public PluginConnectorBaseImportPackageUser(String importId)
        {
            this.pkgId = Guid.NewGuid().ToString();
            this.build_data = DateTime.Now.ToString("o");
            this.properties = new List<PluginConnectorBasePackageData>();
            this.groups = new List<String>();
            this.importId = importId;
            this.container = "\\";
        }

        public void AddProperty(String dataName, String dataValue, String dataType)
        {
            AddProperty(new PluginConnectorBasePackageData(dataName, dataValue, dataType));
        }

        public void AddProperty(PluginConnectorBasePackageData propertyData)
        {
            properties.Add(propertyData);
        }

        public void AddGroup(String groupName)
        {
            groups.Add(groupName);
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

            if (groups != null)
                groups.Clear();
            groups = null;


        }
    }

}
