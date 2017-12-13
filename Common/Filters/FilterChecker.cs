using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace IAM.Filters
{
    
    public class FilterChecker : IDisposable
    {
        private DataTable check_data;
        private FilterRuleCollection filters;

        public Int32 DataCount { get { return (check_data == null ? 0 : check_data.Rows.Count); } }

        public FilterChecker()
        {
            this.check_data = new DataTable();
            this.check_data.Columns.Add("field_id", typeof(Int64));
            this.check_data.Columns.Add("text", typeof(String));
            this.check_data.Columns.Add("date", typeof(Int64));
            this.check_data.Columns.Add("numeric", typeof(Int64));
            this.filters = new FilterRuleCollection();
        }

        public FilterChecker(FilterRuleCollection filters)
            : this()
        {
            this.filters.Dispose();
            this.filters = filters;
        }

        public void AddFieldData(Int64 fieldId, String dataType, Object data)
        {

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

            AddFieldData(fieldId, dt, data);
        }

        public void AddFieldData(Int64 fieldId, DataType dataType, Object data)
        {
            if (data == null)
                return;

            try
            {
                switch (dataType)
                {
                    case Filters.DataType.DateTime:
                        if (data.ToString().Trim() == "")
                            return;

                        switch (data.ToString().Trim().Trim(";".ToCharArray()).ToLower())
                        {
                            case "now":
                            case "now()":
                                data = DateTime.Now;
                                break;

                        }

                        try
                        {
                            DateTime tst = (DateTime)data;
                            TimeSpan ts = tst - new DateTime(1970, 1, 1, 0, 0, 0);
                            this.check_data.Rows.Add(new Object[] { fieldId, "", (Int64)ts.TotalSeconds, 0 });
                            return;
                        }
                        catch
                        {
                            DateTime tst = DateTime.Parse(data.ToString());
                            TimeSpan ts = tst - new DateTime(1970, 1, 1, 0, 0, 0);
                            this.check_data.Rows.Add(new Object[] { fieldId, "", (Int64)ts.TotalSeconds, 0 });
                        }
                        break;


                    case Filters.DataType.Numeric:

                        if (data.ToString().Trim() == "")
                            return;

                        this.check_data.Rows.Add(new Object[] { fieldId, "", 0, Int64.Parse(data.ToString()) });
                        break;

                    default:
                        this.check_data.Rows.Add(new Object[] { fieldId, data.ToString().ToLower(), 0, 0 });
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid cast of data '" + data + "' to type '" + dataType.ToString() + "'", ex);
            }
        }

        public void AddFilterRule(FilterRule f)
        {
            this.filters.AddFilterRule(f);
        }

        public FilterMatch Match()
        {
            foreach (FilterRule r1 in filters)
            {   
                if (this.check_data.Select(r1.ToSqlString()).Length > 0)
                    return new FilterMatch(true, r1);
            }

            return new FilterMatch(true, null);
        }

        public FilterMatchCollection Matches()
        {
            FilterMatchCollection col = new FilterMatchCollection();

            foreach (FilterRule r1 in filters)
            {
                /*
                if (this.check_data.Select(r1.ToSqlString()).Length > 0)
                    col.AddMatch(new FilterMatch(true, r1));*/

                //Novo método
                if (r1.FilterGroups != null)
                {
                    String exp = "";

                    FilterSelector groupLastSelector = FilterSelector.OR;

                    foreach (FilterGroup g in r1.FilterGroups)
                    {
                        if (g.FilterRules != null)
                        {
                            String exp1 = "";

                            FilterSelector lastSelector = FilterSelector.AND;

                            foreach (FilterCondition f in g.FilterRules)
                            {
                                if (exp1 != "") exp1 += " " + lastSelector.ToString() + " ";
                                if (this.check_data.Select(f.ToSqlString()).Length > 0)
                                    exp1 += " true ";
                                else
                                    exp1 += " false ";

                                lastSelector = f.Selector;
                            }

                            if (exp != "" && exp1 != "") exp += " " + groupLastSelector.ToString().ToLower() + " ";
                            exp += (exp1 != "" ? "(" : "") + exp1 + (exp1 != "" ? ")" : "");

                            groupLastSelector = g.Selector;
                        }

                    }

                    if (ExecExpression(exp))
                        col.AddMatch(new FilterMatch(true, r1));
                }

            }

            return col;
        }

        private Boolean ExecExpression(String expression)
        {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("", typeof(Boolean));
            table.Columns[0].Expression = expression;

            System.Data.DataRow r = table.NewRow();
            table.Rows.Add(r);

            return (Boolean)r[0];
        }

        public void Dispose()
        {
            if (check_data != null)
                check_data.Dispose();   
        }
    }
}
