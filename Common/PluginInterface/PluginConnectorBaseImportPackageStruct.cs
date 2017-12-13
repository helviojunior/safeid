using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorBaseImportPackageStruct: IDisposable
    {

        public String pkgId;
        public String importId;
        public String build_data;

        public List<String> containers;
        public List<String> groups;

        public PluginConnectorBaseImportPackageStruct(String importId)
        {
            this.pkgId = Guid.NewGuid().ToString();
            this.build_data = DateTime.Now.ToString("o");
            this.containers = new List<string>();
            this.groups = new List<string>();
            this.importId = importId;
        }

        public void AddContainer(String path)
        {
            containers.Add(path);
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

            if (containers != null)
                containers.Clear();
            containers = null;

            if (groups != null)
                groups.Clear();
            groups = null;


        }
    }

}
