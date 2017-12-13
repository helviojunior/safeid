using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0002_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'collector_imports_struct'))
                    BEGIN
	                    
                        CREATE TABLE [dbo].[collector_imports_struct](
	                        [date] [datetime] NOT NULL,
	                        [file_name] [varchar](500) NOT NULL,
	                        [resource_plugin_id] [bigint] NOT NULL,
	                        [import_id] [varchar](50) NOT NULL,
	                        [package_id] [varchar](50) NOT NULL,
	                        [package] [varchar](max) NOT NULL,
	                        [status] [varchar](2) NOT NULL
                        );

                        ALTER TABLE [dbo].[collector_imports_struct] ADD  CONSTRAINT [DF_collector_imports_struct_status]  DEFAULT ('I') FOR [status];

                    END

                    INSERT INTO [db_ver] ([version]) VALUES (2);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 2.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}