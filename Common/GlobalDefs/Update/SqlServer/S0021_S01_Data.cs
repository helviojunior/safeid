using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0021_S01_Data : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    

                    INSERT INTO [plugin] ([enterprise_id],[name],[scheme],[uri],[assembly])
                    select 0, 'Microsoft Excel spreadsheet connector', 'connector', 'connector://iam/plugins/excel','excel.dll'
                    where not exists (select 1 from [plugin] where uri = 'connector://iam/plugins/excel')


                    INSERT INTO [db_ver] ([version]) VALUES (21);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 21; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
