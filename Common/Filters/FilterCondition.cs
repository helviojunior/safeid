using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;

namespace IAM.Filters
{

    [Serializable]
    public class FilterCondition : ICloneable, IDisposable
    {
        private Int64 field_id;
        private String field_name;
        private DataType data_type;
        private Object data;
        private FilterConditionType condition_type;
        private FilterSelector selector_type;

        public Int64 FieldId { get { return field_id; } }
        public String FieldName { get { return field_name; } }
        public DataType DataType { get { return data_type; } }
        public Object Data { get { return data; } }
        public FilterConditionType ConditionType { get { return condition_type; } }
        public FilterSelector Selector { get { return selector_type; } }

        public String DataString
        {
            get
            {
                if (this.data_type == Filters.DataType.DateTime)
                    return ((DateTime)data).ToString("o");
                else
                    return data.ToString();
            }
        }
        


        public FilterCondition(Int64 fieldId, String fieldName, DataType dataType, Object data, FilterConditionType conditionType, FilterSelector selector)
        {
            this.field_id = fieldId;
            this.field_name = fieldName;
            this.condition_type = conditionType;
            this.selector_type = selector;
            this.data_type = dataType;

            try
            {
                switch (this.data_type)
                {
                    case Filters.DataType.DateTime:
                        try
                        {
                            this.data = (DateTime)data;
                        }
                        catch
                        {
                            this.data = DateTime.Parse(data.ToString());
                        }
                        break;

                    case Filters.DataType.Numeric:
                        try
                        {
                            this.data = (Int64)data;
                        }
                        catch
                        {
                            this.data = Int64.Parse(data.ToString());
                        }
                        break;

                    default:
                        this.data = (String)data;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid cast of data '" + data + "' to type '" + dataType.ToString() + "'", ex);
            }

            switch (this.data_type)
            {
                case Filters.DataType.DateTime:
                    switch (this.condition_type)
                    {
                        case FilterConditionType.NotEqual:
                        case FilterConditionType.Greater:
                        case FilterConditionType.Less:
                        case FilterConditionType.GreaterOrEqual:
                        case FilterConditionType.LessOrEqual:
                        case FilterConditionType.Equal:
                            break;
                        
                        default:
                            throw new Exception("Invalid condition (" + this.condition_type.ToString() + ") of data type (" + this.data_type.ToString() + ")");
                            break;

                    }
                    break;

                case Filters.DataType.Numeric:
                    switch (condition_type)
                    {

                        case FilterConditionType.Equal:
                        case FilterConditionType.NotEqual:
                        case FilterConditionType.Greater:
                        case FilterConditionType.Less:
                        case FilterConditionType.GreaterOrEqual:
                        case FilterConditionType.LessOrEqual:
                            break;

                        default:
                            throw new Exception("Invalid condition (" + this.condition_type.ToString() + ") of data type (" + this.data_type.ToString() + ")");
                            break;

                    }
                    break;

                default:
                    switch (condition_type)
                    {
                        case FilterConditionType.Contains:
                        case FilterConditionType.EndWith:
                        case FilterConditionType.NotContains:
                        case FilterConditionType.NotEqual:
                        case FilterConditionType.StartWith:
                        case FilterConditionType.Equal:
                            break;

                        default:
                            throw new Exception("Invalid condition (" + this.condition_type.ToString() + ") of data type (" + this.data_type.ToString() + ")");
                            break;

                    }
                    break;
            }

        }

        public static List<FilterConditionType> ConditionByDataType(String dataType)
        {
            DataType dt = Filters.DataType.Text;
            switch (dataType.ToLower())
            {
                case "numeric":
                    dt = Filters.DataType.Numeric;
                    break;

                case "datetime":
                    dt = Filters.DataType.DateTime;
                    break;
            }

            return ConditionByDataType(dt);
        }

        public static List<FilterConditionType> ConditionByDataType(DataType dataType)
        {

            List<FilterConditionType> ret = new List<FilterConditionType>();

            switch (dataType)
            {
                case Filters.DataType.DateTime:
                    ret.Add(FilterConditionType.Equal);
                    ret.Add(FilterConditionType.NotEqual);
                    ret.Add(FilterConditionType.Greater);
                    ret.Add(FilterConditionType.Less);
                    ret.Add(FilterConditionType.GreaterOrEqual);
                    ret.Add(FilterConditionType.LessOrEqual);
                    break;

                case Filters.DataType.Numeric:
                    ret.Add(FilterConditionType.Equal);
                    ret.Add(FilterConditionType.NotEqual);
                    ret.Add(FilterConditionType.Greater);
                    ret.Add(FilterConditionType.Less);
                    ret.Add(FilterConditionType.GreaterOrEqual);
                    ret.Add(FilterConditionType.LessOrEqual);
                    break;


                default:
                    ret.Add(FilterConditionType.Equal);
                    ret.Add(FilterConditionType.NotEqual);
                    ret.Add(FilterConditionType.StartWith);
                    ret.Add(FilterConditionType.EndWith);
                    ret.Add(FilterConditionType.Contains);
                    ret.Add(FilterConditionType.NotContains);
                    break;

            }

            return ret;
        }


        public Object Clone()
        {
            return new FilterCondition(this.field_id, this.field_name, this.data_type, this.data, this.condition_type, this.selector_type);
        }

        public void Dispose()
        {

        }

        public override string ToString()
        {
            if (this.data_type == Filters.DataType.DateTime)
                return field_name + " " + MessageResource.GetMessage(condition_type.ToString().ToLower(), condition_type.ToString()).ToLower() + " " + MessageResource.FormatDate(((DateTime)data), false);
            else
                return field_name + " " + MessageResource.GetMessage(condition_type.ToString().ToLower(), condition_type.ToString()).ToLower() + " " + data.ToString();
        }


        public string ToSqlString()
        {
            String ret = "(field_id = " + this.field_id;

            switch (this.data_type)
            {
                case Filters.DataType.DateTime:
                    TimeSpan ts = (DateTime)data - new DateTime(1970, 1, 1, 0, 0, 0);
                    switch (condition_type)
                    {
                        case FilterConditionType.NotEqual:
                            ret += " and date > 0 and date ";
                            ret += "<> " + (Int64)ts.TotalSeconds;
                            break;

                        case FilterConditionType.Greater:
                            ret += " and date > 0 and date ";
                            ret += "> " + (Int64)ts.TotalSeconds;
                            break;

                        case FilterConditionType.Less:
                            ret += " and date > 0 and date ";
                            ret += "< " + (Int64)ts.TotalSeconds;
                            break;

                        case FilterConditionType.GreaterOrEqual:
                            ret += " and date > 0 and date ";
                            ret += ">= " + (Int64)ts.TotalSeconds;
                            break;

                        case FilterConditionType.LessOrEqual:
                            ret += " and date > 0 and date ";
                            ret += "<= " + (Int64)ts.TotalSeconds;
                            break;

                        case FilterConditionType.Equal:
                            ret += " and date > 0 and date ";
                            ret += "= " + (Int64)ts.TotalSeconds;
                            break;

                    }
                    break;

                case Filters.DataType.Numeric:
                    switch (condition_type)
                    {

                        case FilterConditionType.Equal:
                            ret += " and numeric ";
                            ret += "= " + data.ToString();
                            break;

                        case FilterConditionType.NotEqual:
                            ret += " and numeric ";
                            ret += "<> " + data.ToString();
                            break;

                        case FilterConditionType.Greater:
                            ret += " and numeric ";
                            ret += "> " + data.ToString();
                            break;
                            
                        case FilterConditionType.Less:
                            ret += " and numeric ";
                            ret += "< " + data.ToString();
                            break;

                        case FilterConditionType.GreaterOrEqual:
                            ret += " and numeric ";
                            ret += ">= " + data.ToString();
                            break;

                        case FilterConditionType.LessOrEqual:
                            ret += " and numeric ";
                            ret += "<= " + data.ToString();
                            break;
                    }
                    break;

                default:
                    ret += " and text ";
                    switch (condition_type)
                    {
                        case FilterConditionType.Contains:
                            ret += "like '%" + data.ToString() + "%'";
                            break;

                        case FilterConditionType.EndWith:
                            ret += "like '%" + data.ToString() + "'";
                            break;

                        case FilterConditionType.NotContains:
                            ret += "not like '%" + data.ToString() + "%'";
                            break;

                        case FilterConditionType.NotEqual:
                            ret += "<> '" + data.ToString() + "'";
                            break;

                        case FilterConditionType.StartWith:
                            ret += "like '%" + data.ToString() + "'";
                            break;

                        case FilterConditionType.Equal:
                            ret += "= '" + data.ToString() + "'";
                            break;
                    }
                    break;
            }
            
            
            ret += ")";
            return ret;
        }
        
        public Dictionary<String, Object> ToJsonObject()
        {
            Dictionary<String, Object> ret = new Dictionary<string, object>();

            ret.Add("field_id", this.field_id);
            ret.Add("field_name", this.field_name);
            ret.Add("data_type", this.data_type.ToString());
            ret.Add("condition_type", this.condition_type.ToString());
            ret.Add("selector_type", this.selector_type.ToString());

            if (this.data_type == Filters.DataType.DateTime)
                ret.Add("data", ((DateTime)data).ToString("o"));
            else
                ret.Add("data", data.ToString());

            return ret;
        }
    }
    
}
