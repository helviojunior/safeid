using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0034_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    
                    ALTER TABLE dbo.resource_plugin ADD deploy_individual_package bit NOT NULL CONSTRAINT DF_resource_plugin_deploy_individual_package DEFAULT 0

                    INSERT INTO [db_ver] ([version]) VALUES (34);
                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 34; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
