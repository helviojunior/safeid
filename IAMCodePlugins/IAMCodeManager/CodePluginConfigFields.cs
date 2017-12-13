using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.CodeManager
{
    [Serializable()]
    public class CodePluginConfigFields
    {
        public String Name { get; private set; }
        public String Key { get; private set; }
        public String Description { get; private set; }
        public String DefaultValue { get; private set; }
        public Boolean Required { get; private set; }
        public CodePluginConfigTypes Type { get; private set; }
        
        public CodePluginConfigFields(String name, String key, String description, CodePluginConfigTypes type, Boolean required, String defaultValue)
        {
            this.Name = name;
            this.Key = key;
            this.Description = description;
            this.Type = type;
            this.DefaultValue = defaultValue;
            this.Required = required;
        }

        public void SetValue(String defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

    }
}
