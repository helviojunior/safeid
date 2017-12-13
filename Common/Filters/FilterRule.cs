using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;


namespace IAM.Filters
{
    /*-> condition
       -> order
       -> group
       -> field
       -> condition (é igual, é diferente, contenha, não contenha, começa com, finaliza com)
       -> text
       -> selector (AND, or)*/

    [Serializable]
    public class FilterRule : ICloneable, IDisposable
    {
        private string filter_name;
        private List<FilterGroup> filter_groups;

        public String FilterName { get { return filter_name; } set { filter_name = value; } }
        public List<FilterGroup> FilterGroups { get { return filter_groups; } }

        public FilterRule(String filterName)
        {
            this.filter_name = filterName;
            this.filter_groups = new List<FilterGroup>();
        }

        public void AddGroup(FilterGroup group)
        {
            this.filter_groups.Add(group);
        }

        public void AddCondition(String groupId, String groupSelector, Int64 fieldId, String fieldName, String dataType, String text, String conditionType, String selector)
        {
            FilterSelector gs = FilterSelector.OR;
            switch (groupSelector.ToLower())
            {
                case "and":
                    gs = FilterSelector.AND;
                    break;

                case "or":
                    gs = FilterSelector.OR;
                    break;
            }

            FilterSelector s = FilterSelector.OR;
            switch (selector.ToLower())
            {
                case "and":
                    s = FilterSelector.AND;
                    break;

                case "or":
                    s = FilterSelector.OR;
                    break;
            }


            FilterConditionType c = FilterConditionType.Equal;
            switch (conditionType.ToLower())
            {
                case "contains":
                    c = FilterConditionType.Contains;
                    break;

                case "endwith":
                    c = FilterConditionType.EndWith;
                    break;

                case "equal":
                    c = FilterConditionType.Equal;
                    break;

                case "notcontains":
                    c = FilterConditionType.NotContains;
                    break;

                case "notequal":
                    c = FilterConditionType.NotEqual;
                    break;

                case "startwith":
                    c = FilterConditionType.StartWith;
                    break;
                    
                case "greater":
                    c = FilterConditionType.Greater;
                    break;

                case "less":
                    c = FilterConditionType.Less;
                    break;

                case "greaterorequal":
                    c = FilterConditionType.GreaterOrEqual;
                    break;

                case "lessorequal":
                    c = FilterConditionType.LessOrEqual;
                    break;

            }

            DataType dt = DataType.Text;
            switch (dataType.ToLower())
            {
                case "datetime":
                case "date":
                    dt = DataType.DateTime;
                    break;

                case "numeric":
                    dt = DataType.Numeric;
                    break;

                default:
                    dt = DataType.Text;
                    break;
            }


            AddCondition(groupId, gs, fieldId, fieldName, dt, text, c, s);
        }

        public void AddCondition(String groupId, FilterSelector groupSelector, Int64 fieldId, String fieldName, DataType dataType, String text, FilterConditionType conditionType, FilterSelector selector)
        {
            FilterGroup g = filter_groups.Find(g1 => (g1.GroupId == groupId));

            if (g == null)
            {
                g = new FilterGroup(groupId, groupSelector);
                filter_groups.Add(g);
            }

            g.AddFilter(fieldId, fieldName, dataType, text, conditionType, selector);
        }

        public Object Clone()
        {
            FilterRule c = new FilterRule(this.filter_name);
            if (filter_groups != null)
                foreach (FilterGroup g in filter_groups)
                    c.AddGroup((FilterGroup)g.Clone());
            return c;
        }

        public void Dispose()
        {

        }

        public override string ToString()
        {
            String ret = "";

            FilterSelector lastSelector = FilterSelector.OR;

            if (filter_groups != null)
                foreach (FilterGroup g in filter_groups)
                {
                    if (ret != "") ret += " " + MessageResource.GetMessage(lastSelector.ToString().ToLower(), lastSelector.ToString()).ToLower() + " ";
                    
                    ret += g.ToString();
                    lastSelector = g.Selector;
                }

            return ret;
        }
        
        public string ToSqlString()
        {
            String ret = "";

            FilterSelector lastSelector = FilterSelector.OR;

            if (filter_groups != null)
                foreach (FilterGroup g in filter_groups)
                {
                    if (ret != "") ret += " " + lastSelector.ToString().ToLower() + " ";
                    ret += g.ToSqlString().ToLower();
                    lastSelector = g.Selector;
                }

            return ret;
        }

        /*
        public Dictionary<String, Object> ToJsonObject()
        {
            Dictionary<String, Object> ret = new Dictionary<String, Object>();
            List<Dictionary<String, Object>> g1 = new List<Dictionary<string, object>>();

            ret.Add("filter_name", this.filter_name);
            
            if (filter_groups != null)
                foreach (FilterGroup g in filter_groups)
                    g1.Add(g.ToJsonObject());

            ret.Add("filter_groups", g1);

            return ret;
        }*/

        public Dictionary<String, Object> ToJsonObject()
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            List<Dictionary<String, Object>> conditions = new List<Dictionary<string, object>>();

            ret.Add("name", this.filter_name);

            if (filter_groups != null)
                foreach (FilterGroup g in filter_groups)
                {
                    foreach (FilterCondition f in g.FilterRules)
                    {
                        Dictionary<string, object> c1 = new Dictionary<string, object>();
                        c1.Add("group_id", g.GroupId);
                        c1.Add("group_selector", g.Selector.ToString());
                        c1.Add("field_id", f.FieldId);
                        c1.Add("field_name", f.FieldName);
                        c1.Add("data_type", f.DataType);
                        c1.Add("text", f.DataString);
                        c1.Add("condition", f.ConditionType.ToString());
                        c1.Add("selector", f.Selector.ToString());

                        conditions.Add(c1);
                    }
                    
                }


            ret.Add("conditions", conditions);

            return ret;
        }

    }

}
