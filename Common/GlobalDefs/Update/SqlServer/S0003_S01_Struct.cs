using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0003_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'resource_plugin' AND  COLUMN_NAME = 'import_groups'))
                    BEGIN
                        ALTER TABLE dbo.resource_plugin ADD import_groups bit NOT NULL CONSTRAINT DF_resource_plugin_import_groups DEFAULT 0;
                    END

                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'resource_plugin' AND  COLUMN_NAME = 'import_containers'))
                    BEGIN
                        ALTER TABLE dbo.resource_plugin ADD import_containers bit NOT NULL CONSTRAINT DF_resource_plugin_import_containers DEFAULT 0;
                    END

                    INSERT INTO [db_ver] ([version]) VALUES (3);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 3.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
