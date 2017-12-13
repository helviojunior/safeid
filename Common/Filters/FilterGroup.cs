using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IAM.GlobalDefs;

namespace IAM.Filters
{

    [Serializable]
    public class FilterGroup : ICloneable, IDisposable
    {
        private string group_id;
        private List<FilterCondition> filter_rules;
        private FilterSelector selector_type;

        public String GroupId { get { return group_id; } }
        public List<FilterCondition> FilterRules { get { return filter_rules; } }
        public FilterSelector Selector { get { return selector_type; } set { selector_type = value; } }

        public FilterGroup(String groupId, FilterSelector groupSelector)
        {
            this.group_id = groupId;
            this.selector_type = groupSelector;
            this.filter_rules = new List<FilterCondition>();
        }

        public void AddFilter(Int64 fieldId, String fieldName, DataType dataType, String text, FilterConditionType conditionType, FilterSelector selector)
        {
            AddFilter(new FilterCondition(fieldId, fieldName, dataType, text, conditionType, selector));
        }

        public void AddFilter(FilterCondition filter)
        {
            this.filter_rules.Add(filter);
        }


        public Object Clone()
        {
            FilterGroup g = new FilterGroup(this.group_id, this.selector_type);
            if (filter_rules != null)
                foreach (FilterCondition f in filter_rules)
                    g.AddFilter(f);
            return g;
        }

        public void Dispose()
        {

        }


        public override string ToString()
        {
            String ret = "";

            FilterSelector lastSelector = FilterSelector.AND;

            if (filter_rules != null)
                foreach (FilterCondition f in filter_rules)
                {
                    if (ret != "") ret += " " + MessageResource.GetMessage(lastSelector.ToString().ToLower(), lastSelector.ToString()).ToLower() + " ";
                    ret += f.ToString();
                    lastSelector = f.Selector;
                }

            return (ret != "" ? "(" : "") + ret + (ret != "" ? ")" : "");
        }


        public string ToSqlString()
        {
            String ret = "";

            FilterSelector lastSelector = FilterSelector.AND;

            if (filter_rules != null)
                foreach (FilterCondition f in filter_rules)
                {
                    if (ret != "") ret += " " + lastSelector.ToString() + " ";
                    ret += f.ToSqlString();
                    lastSelector = f.Selector;
                }

            return (ret != "" ? "(" : "") + ret + (ret != "" ? ")" : "");
        }

        public Dictionary<String, Object> ToJsonObject()
        {
            Dictionary<String, Object> ret = new Dictionary<string, object>();

            ret.Add("group_id", this.group_id);
            ret.Add("selector_type", this.selector_type.ToString());

            List<Dictionary<String, Object>> r = new List<Dictionary<string, object>>();

            if (filter_rules != null)
                foreach (FilterCondition f in filter_rules)
                    r.Add(f.ToJsonObject());

            ret.Add("filter_rules", r);

            return ret;
        }
    }
}
