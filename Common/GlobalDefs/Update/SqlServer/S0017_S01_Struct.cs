using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0017_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'PK_plugin_mapping') AND type in (N'PK'))
                    BEGIN
	                    ALTER TABLE dbo.resource_plugin_mapping DROP CONSTRAINT PK_plugin_mapping
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (17);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 17.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
