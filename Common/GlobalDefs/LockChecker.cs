using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Json;

namespace IAM.GlobalDefs
{
    /*
    public class LockChecker
    {
        public delegate void ExecutionLog(String method, String jData, String jCheck, Boolean status);
        public event ExecutionLog OnExecutionLog;

        class CheckerClass
        {
            public event ExecutionLog ExecutionLog;

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
                Boolean ret = (properties.ContainsKey(propertyName.ToLower()));

                ExecutionLog("containsproperty", JSON.Serialize2(properties), propertyName, ret);

                return ret;
            }

            public Boolean propertyisequal(String propertyName, String value)
            {
                var tmp = new
                {
                    propertyName = propertyName,
                    value = value
                };

                Boolean ret = true;
                if (!properties.ContainsKey(propertyName.ToLower()))
                    ret = false;

                ExecutionLog("containsproperty> 1", JSON.Serialize2(properties), JSON.Serialize2(tmp), ret);

                if (!ret)
                    return ret;

                ret = this.properties[propertyName].Contains(value);

                ExecutionLog("containsproperty> 2", JSON.Serialize2(properties), JSON.Serialize2(tmp), ret);
                return ret;
            }

        }


        private List<String> expressions;
        
        public LockChecker(List<String> expressions)
        {
            this.expressions = expressions;
        }

        public Boolean Lock(Dictionary<String, String> properties)
        {
            if ((properties == null) || (properties.Count == 0))
                return false;

            if ((this.expressions == null) || (this.expressions.Count == 0))
                return false;

            CheckerClass c = new CheckerClass(properties);
            c.ExecutionLog += new LockChecker.ExecutionLog(c_ExecutionLog);

            return _lock(c);
        }

        void c_ExecutionLog(string method, string jData, string jCheck, bool status)
        {
            if (OnExecutionLog != null)
                OnExecutionLog(method, jData, jCheck, status);
        }

        public Boolean Lock(Dictionary<String, List<String>> properties)
        {
            if ((properties == null) || (properties.Count == 0))
                return false;

            if ((this.expressions == null) || (this.expressions.Count == 0))
                return false;

            CheckerClass c = new CheckerClass(properties);
            c.ExecutionLog += new LockChecker.ExecutionLog(c_ExecutionLog);

            return _lock(c);
        }

        private Boolean _lock(CheckerClass c)
        {

            Boolean l = false;

            foreach (String e in this.expressions)
            {
                //string parsestr = "(user.ContainsProperty('lockoutTime'))";
                //string parsestr = "(user.PropertyIsEqual('Status', 'Ativo'))";

                var p = new CompiledExpression(e.ToLower());
                p.RegisterType("user", c);
                p.Parse();
                p.Compile();
                Object ret = p.Eval();

                if (ret is Boolean)
                    l = (Boolean)ret;

                p = null;

                if (l == true)
                    break;

                //Console.WriteLine("Result: {0} {1}", p.Eval(), p.Eval().GetType().Name);
            }

            return l;
        }
    }*/
}
