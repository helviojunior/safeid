using System;

namespace InstallWizard
{
    public class S0026_S01_notify_imports : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'notify_imports'))
                    BEGIN

			
	                    CREATE TABLE [dbo].[notify_imports](
		                    [date] [datetime] NOT NULL,
		                    [source] [varchar](500) NOT NULL,
		                    [plugin_uri] [varchar](500) NOT NULL,
		                    [resource_id] [bigint] NOT NULL,
		                    [entity_id] [bigint] NOT NULL
	                    ) ON [PRIMARY]

                    END



                    INSERT INTO [db_install] ([version]) VALUES (26);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 26.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
