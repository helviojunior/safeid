using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0010_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    UPDATE [sys_sub_module] SET [api_module] = 'user' WHERE [key] = 'users';

                    INSERT INTO [db_ver] ([version]) VALUES (10);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 10.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
