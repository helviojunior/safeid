using System;

namespace InstallWizard
{
    public class S0020_S01_server_config : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'server_config'))
                    BEGIN
	                    CREATE TABLE [dbo].[server_config](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [data_name] [varchar](50) NOT NULL,
		                    [data_value] [varchar](5000) NOT NULL,
	                     CONSTRAINT [PK_server_config] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                    END

                    INSERT INTO [db_install] ([version]) VALUES (20);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 20.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
