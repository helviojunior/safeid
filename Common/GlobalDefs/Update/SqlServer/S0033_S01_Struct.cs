using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0033_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    ALTER TABLE dbo.proxy ADD restart bit NOT NULL CONSTRAINT DF_proxy_restart DEFAULT 0;
                    ALTER TABLE dbo.proxy ADD pid int NOT NULL CONSTRAINT DF_proxy_pid DEFAULT 0

                    INSERT INTO [db_ver] ([version]) VALUES (33);
                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 33; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
