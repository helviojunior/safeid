using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.Config
{
    public class UserDataFields : IDisposable
    {
        public PluginConfigMapping Mapping { get; set; }
        public Object Value { get; set; }
        public String StringValue
        {
            get
            {
                try
                {
                    switch (this.Mapping.data_type.ToLower())
                    {
                        case "date":
                        case "datetime":
                            return ((DateTime)this.Value).ToString("o");
                            break;

                        default:
                            return this.Value.ToString();
                            break;
                    }
                }
                catch
                {
                    return this.Value.ToString();
                }
            }
        }

        public UserDataFields(Int64 field_id, String field_name, String data_type, String Value)
            : this(new PluginConfigMapping(field_id, field_name, field_name, data_type, false, false, false, false, false, false), Value) { }

        public UserDataFields(PluginConfigMapping Mapping, String Value)
        {
            this.Mapping = Mapping;

            try
            {
                switch (this.Mapping.data_type.ToLower())
                {
                    case "date":
                    case "datetime":
                        this.Value = IAM.GlobalDefs.Tools.Tool.ParseDate(Value);
                        break;

                    case "int":
                    case "int32":
                        this.Value = Int32.Parse(Value);
                        break;

                    case "int64":
                    case "number":
                    case "long":
                        this.Value = Int64.Parse(Value);
                        break;

                    default:
                        this.Value = Value;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid cast of value '" + Value + "' to type '" + this.Mapping.data_type + "'", ex);
            }
        }

        public Boolean Equal(String value)
        {
            try
            {
                switch (this.Mapping.data_type.ToLower())
                {
                    case "date":
                    case "datetime":
                        return ((DateTime)this.Value).Equals(DateTime.Parse(value));
                        break;

                    default:
                        return (value.ToString() == this.Value.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                return (value == this.Value.ToString());
            }
        }

        public void Dispose()
        {
            if (this.Mapping != null) this.Mapping.Dispose();
            this.Mapping = null;
            this.Value = null;
        }

        public Object ToSerialObject()
        {
            Dictionary<String, String> items = new Dictionary<string, string>();
            items.Add("data_name", this.Mapping.data_name);
            items.Add("field_id", this.Mapping.field_id.ToString());
            items.Add("data_type", this.Mapping.data_type);
            items.Add("value", this.StringValue);

            return items;
        }
    }

}
