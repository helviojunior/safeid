using System;
using System.Collections.Generic;
using System.Text;

namespace CAS.PluginInterface
{
    [Serializable()]
    public class PluginConfigFields
    {
        public String Name { get; private set; }
        public String Key { get; private set; }
        public String Description { get; private set; }
        public String DefaultValue { get; private set; }
        public String[] ListValue { get; private set; }
        public PluginConfigTypes Type { get; private set; }
        public Boolean ImportRequired { get; private set; }
        public Boolean DeployRequired { get; private set; }

        public PluginConfigFields(String name, String key, String description, PluginConfigTypes type, Boolean required, String defaultValue)
            : this(name, key, description, type, required, required, defaultValue) { }

        public PluginConfigFields(String name, String key, String description, PluginConfigTypes type, Boolean importRequired, Boolean deployRequired, String defaultValue)
        {
            if (this.Type == PluginConfigTypes.StringFixedList)
                throw new Exception("Default value can not be set with type 'StringFixedList'");

            this.Name = name;
            this.Key = key;
            this.Description = description;
            this.Type = type;
            this.ImportRequired = importRequired;
            this.DeployRequired = deployRequired;
            this.DefaultValue = defaultValue;
            this.ListValue = null;
        }

        public PluginConfigFields(String name, String key, String description, Boolean required, String[] listValues)
            : this(name, key, description, required, required, listValues) { }

        public PluginConfigFields(String name, String key, String description, Boolean importRequired, Boolean deployRequired, String[] listValues)
        {
            this.Name = name;
            this.Key = key;
            this.Description = description;
            this.Type = PluginConfigTypes.StringFixedList;
            this.ImportRequired = importRequired;
            this.DeployRequired = deployRequired;
            this.ListValue = listValues;
            this.DefaultValue = null;
        }

        public void SetValue(String defaultValue)
        {
            if (this.Type == PluginConfigTypes.StringFixedList)
                throw new Exception("Default value can not be set with type 'StringFixedList'");

            this.DefaultValue = defaultValue;
            this.ListValue = null;
        }

        public void SetValue(String[] listValues)
        {
            if (this.Type != PluginConfigTypes.StringFixedList)
                throw new Exception("List values can not be set with type different of 'StringFixedList'");

            this.DefaultValue = null;
            this.ListValue = listValues;
        }
    }
}
