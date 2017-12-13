using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IAM.CA;
using System.IO;
using IAM.GlobalDefs;
using IAM.Filters;

namespace Test
{


    class Program
    {
        static void Main(string[] args)
        {

            String data = "Integrity check error: Multiplus entities (10583, 13065) found at this filtered data";

            List<String> duplicatedEntities = new List<String>();

            //Captura somente os IDs das entidades
            Regex rex = new Regex(@"\((.*?)\)");
            Match m = rex.Match(data);
            if (m.Success)
            {
                String[] entities = m.Groups[1].Value.Replace(" ","").Split(",".ToCharArray());
                duplicatedEntities.AddRange(entities);
            }


            FilterRule r = new FilterRule("teste001");
            r.AddCondition("g1", FilterSelector.AND, 1, "Nome", DataType.Text, "Helvio", FilterConditionType.Equal, FilterSelector.OR);
            r.AddCondition("g1", FilterSelector.OR, 1, "Nome", DataType.Text, "Thais", FilterConditionType.Equal, FilterSelector.OR);
            r.AddCondition("g2", FilterSelector.AND, 2, "Setor", DataType.Text, "Financeiro", FilterConditionType.Equal, FilterSelector.OR);
            r.AddCondition("g3", FilterSelector.OR, 3, "Cidade", DataType.Text, "Curitiba", FilterConditionType.Equal, FilterSelector.OR);

            FilterRuleCollection frCol = new FilterRuleCollection();
            frCol.AddFilterRule(r);

            foreach (FilterRule r1 in frCol)
            {
                Console.WriteLine(r1.ToString());
                Console.WriteLine(r1.ToSqlString());
            }

            FilterChecker chk = new FilterChecker(frCol);
            for (Int32 i = 10; i <= 100; i++)
                chk.AddFieldData(i, DataType.Text, "Test 0" + i);

            for (Int32 i = 10; i <= 100; i++)
                chk.AddFieldData(1, DataType.Text, "Teste 1 " + i);

            for (Int32 i = 10; i <= 100; i++)
                chk.AddFieldData(2, DataType.Text, "Teste 2 " + i);

            for (Int32 i = 10; i <= 100; i++)
                chk.AddFieldData(3, DataType.Text, "Teste 3 " + i);

            chk.AddFieldData(1, DataType.Text, "Helvio");
            chk.AddFieldData(1, DataType.Text, "SafeTrend.com.br");
            chk.AddFieldData(1, DataType.Text, "Helvio Carvalho");
            chk.AddFieldData(1, DataType.Text, "Thais Freitas Lima");
            chk.AddFieldData(2, DataType.Text, "Financeiro");
            chk.AddFieldData(3, DataType.Text, "Curitiba");

            FilterMatchCollection col = chk.Matches();

            return;

            String tstj = "{ \"type\":\"SpecificTime\", \"start_time\":\"07:30:00\", \"end_time\":\"19:00:00\", \"week_day\":[\"monday\",\"tuesday\",\"wednesday\",\"thursday\",\"friday\"] }";
            IAM.TimeACL.TimeAccess acl = new IAM.TimeACL.TimeAccess();
            acl.FromJsonString(tstj);
            Console.WriteLine(acl.BetweenTimes(DateTime.Now));


            return;

            String tst = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
            DateTime tst2 = DateTime.Parse(tst);

            EnterpriseKey ent = new EnterpriseKey(new Uri("//demo.safeid.com.br"), "SafeID - Demo");
            ent.BuildCerts(); //Cria os certificados

            StringBuilder text = new StringBuilder();
            text.AppendLine("Server cert");
            text.AppendLine(ent.ServerCert);
            text.AppendLine("");

            text.AppendLine("Server PKCS#12 cert");
            text.AppendLine(ent.ServerPKCS12Cert);
            text.AppendLine("");

            text.AppendLine("Client PKCS#12 cert");
            text.AppendLine(ent.ClientPKCS12Cert);

            File.WriteAllText("certdata.txt", text.ToString());


            /*
            Dictionary<String, String> properties = new Dictionary<string, string>();
            properties.Add("locked", "false");

            UserData user = new UserData(properties);

            //string parsestr = "(u.ContainsProperty('locked'))";
            string parsestr = "(u.PropertyIsEqual('locked', 'false'))";
            var p = new CompiledExpression(parsestr);
            p.RegisterType("u", user);
            p.Parse();
            p.Compile();
            Console.WriteLine("Result: {0} {1}", p.Eval(), p.Eval().GetType().Name);*/

            //"(vars.myExternalVar + 3) / 2 * 4.5 " // returns 20.25 
            //"vars.getRandomNumber()"  // returns a random number

        }
    }
}
