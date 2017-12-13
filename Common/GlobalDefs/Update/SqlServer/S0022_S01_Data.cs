using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0022_S01_Data : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    

                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly])
                    select 0, 'Linux SSH Plugin', 'connector', 'connector://iam/plugins/linux','Linux.dll'
                    where not exists (select 1 from [plugin] where uri = 'connector://iam/plugins/linux')

                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly])
                    select 0, 'Zabbix API Plugin', 'connector', 'connector://iam/plugins/zabbix','Zabbix.dll'
                    where not exists (select 1 from [plugin] where uri = 'connector://iam/plugins/zabbix')


                    INSERT INTO [db_ver] ([version]) VALUES (22);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 22; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
