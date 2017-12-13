using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0001_S01_Data : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    INSERT INTO [db_ver] ([version]) VALUES (1);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when count(*) = 0 then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 1.2; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
