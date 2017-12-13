using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.AuthPlugins
{

    [Serializable()]
    public class AuthConfigFields
    {
        public String Name { get; private set; }
        public String Key { get; private set; }
        public String Description { get; private set; }
        public String DefaultValue { get; private set; }
        public AuthConfigTypes Type { get; private set; }
        public Boolean Required { get; private set; }

        public AuthConfigFields(String name, String key, String description, AuthConfigTypes type, Boolean required, String defaultValue)
        {
            this.Name = name;
            this.Key = key;
            this.Description = description;
            this.Type = type;
            this.Required = required;
            this.DefaultValue = defaultValue;
        }

        public void SetValue(String defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

    }
}
