using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.PluginInterface
{
    [Serializable()]
    public class PluginConnectorConfigActionsFields
    {
        public String Name { get; private set; }
        public String Key { get; private set; }
        public String Description { get; private set; }

        public PluginConnectorConfigActionsFields(String Name, String Key, String Description)
        {
            this.Name = Name;
            this.Key = Key;
            this.Description = Description;
        }
    }

    [Serializable()]
    public class PluginConnectorConfigActions
    {
        public String Name { get; private set; }
        public String Key { get; private set; }
        public String Description { get; private set; }
        public PluginConnectorConfigActionsFields Field  { get; private set; }
        public List<String> Macro { get; private set; }
        
        public PluginConnectorConfigActions(String name, String key, String description, String fieldName, String fieldKey, String fieldDescription)
            : this(name, key, description, new PluginConnectorConfigActionsFields(fieldName, fieldKey, fieldDescription)) { }

        public PluginConnectorConfigActions(String name, String key, String description, String fieldName, String fieldKey, String fieldDescription, List<String> macros)
            : this(name, key, description, new PluginConnectorConfigActionsFields(fieldName, fieldKey, fieldDescription), macros) { }

        public PluginConnectorConfigActions(String name, String key, String description, PluginConnectorConfigActionsFields field)
            : this(name, key, description, field, null) { }

        public PluginConnectorConfigActions(String name, String key, String description, PluginConnectorConfigActionsFields field, List<String> macros)
        {
            this.Name = name;
            this.Key = key;
            this.Description = description;
            this.Field = field;
            this.Macro = new List<String>();
            if (macros != null)
                this.Macro.AddRange(macros);
        }

        /*
        public PluginConnectorConfigActions(String name, String key, String description) : this(name, key, description, null) { }
        public PluginConnectorConfigActions(String name, String key, String description, List<String> macros)
        {
            this.Name = name;
            this.Key = key;
            this.Description = description;
            this.Macro = new List<String>();
            if (macros != null)
                this.Macro.AddRange(macros);
        }

        public void AddMacro(String macro)
        {
            if (!this.Macro.Contains(macro))
                this.Macro.Add(macro);
        }*/

    }
}
