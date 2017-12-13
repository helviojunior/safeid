using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0012_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'enterprise' AND  COLUMN_NAME = 'last_uri'))
                    BEGIN
                        ALTER TABLE dbo.enterprise ADD last_uri varchar(2000) NULL
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (12);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 12.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
