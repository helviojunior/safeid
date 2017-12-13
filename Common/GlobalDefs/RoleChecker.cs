using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace IAM.GlobalDefs
{
    public class RoleChecker
    {
        class CheckerClass
        {
            Dictionary<String, List<String>> properties;

            public CheckerClass(Dictionary<String, String> properties)
            {
                this.properties = new Dictionary<String, List<String>>();
                foreach (String k in properties.Keys)
                {
                    this.properties.Add(k.ToLower(), new List<String>());

                    this.properties[k.ToLower()].Add(properties[k]);
                }
            }

            public CheckerClass(Dictionary<String, List<String>> properties)
            {
                this.properties = new Dictionary<String, List<String>>();
                foreach (String k in properties.Keys)
                {
                    this.properties.Add(k.ToLower(), new List<String>());

                    foreach (String v in properties[k])
                        this.properties[k.ToLower()].Add(v.ToLower());
                }
            }

            public Boolean containsproperty(String propertyName)
            {
                return (properties.ContainsKey(propertyName.ToLower()));
            }

            public Boolean propertyisequal(String propertyName, String value)
            {
                if (!properties.ContainsKey(propertyName.ToLower()))
                    return false;

                return this.properties[propertyName].Contains(value);
            }

            public Boolean propertycontainstext(String propertyName, String value)
            {
                if (!properties.ContainsKey(propertyName.ToLower()))
                    return false;

                foreach (String v in this.properties[propertyName])
                    if (v.ToLower().IndexOf(value.ToLower()) != -1)
                        return true;

                return false;
            }

            public Boolean fromthisplugin()
            {
                return true;
                /*
                if (!properties.ContainsKey(propertyName.ToLower()))
                    return false;

                return this.properties[propertyName].Contains(value);*/
            }

        }

        private List<RoleRuleItem> expressions;

        public RoleChecker(List<RoleRuleItem> expressions)
        {
            this.expressions = expressions;
        }

        public List<Int64> MatchRoles(Dictionary<String, String> properties)
        {
            if ((properties == null) || (properties.Count == 0))
                return new List<Int64>();

            if ((this.expressions == null) || (this.expressions.Count == 0))
                return new List<Int64>();

            CheckerClass c = new CheckerClass(properties);


            return _matchRoles(c);
        }

        public List<Int64> MatchRoles(Dictionary<String, List<String>> properties)
        {
            if ((properties == null) || (properties.Count == 0))
                return new List<Int64>();

            if ((this.expressions == null) || (this.expressions.Count == 0))
                return new List<Int64>();

            CheckerClass c = new CheckerClass(properties);

            return _matchRoles(c);
        }

        private List<Int64> _matchRoles(CheckerClass c)
        {

            List<Int64> matchRoles = new List<Int64>();

            foreach (RoleRuleItem e in this.expressions)
            {
                //string parsestr = "(user.ContainsProperty('lockoutTime'))";
                //string parsestr = "(user.PropertyIsEqual('Status', 'Ativo'))";
                //string parsestr = "(user.fromThisPlugin())";
                //string parsestr = "(user.propertyContainsText('Status', 'Ativo'))";
                /*
                var p = new CompiledExpression(e.Expression.ToLower());
                p.RegisterType("user", c);
                p.Parse();
                p.Compile();
                Object ret = p.Eval();

                if ((ret is Boolean) && ((Boolean)ret))
                    if (!matchRoles.Contains(e.RoleId))
                        matchRoles.Add(e.RoleId);

                //Console.WriteLine("Result: {0} {1}", p.Eval(), p.Eval().GetType().Name);*/
            }

            return matchRoles;
        }
    }
}
