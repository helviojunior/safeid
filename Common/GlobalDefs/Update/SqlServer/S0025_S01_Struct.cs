using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class S0025_S01_Struct : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"
                    

                    
                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'resource_plugin' AND  COLUMN_NAME = 'use_password_salt'))
                    BEGIN
	                    ALTER TABLE dbo.resource_plugin ADD use_password_salt bit NOT NULL CONSTRAINT DF_resource_plugin_use_password_salt DEFAULT 0;
                    END

                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'resource_plugin' AND  COLUMN_NAME = 'password_salt_end'))
                    BEGIN
	                    ALTER TABLE dbo.resource_plugin ADD password_salt_end bit NOT NULL CONSTRAINT DF_resource_plugin_password_salt_end DEFAULT 0;
                    END

                    IF (NOT EXISTS( SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'resource_plugin' AND  COLUMN_NAME = 'password_salt'))
                    BEGIN
	                    ALTER TABLE dbo.resource_plugin ADD password_salt varchar(200) NULL;
                    END


                    INSERT INTO [db_ver] ([version]) VALUES (25);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(max([version]),0) < " + ((Int64)Serial).ToString() + @" then 1 ELSE 0 END FROM [db_ver]"; }
        }

        public double Serial { get { return 25; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
