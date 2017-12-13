using System;

namespace InstallWizard
{
    public class S0019_S01_service_status : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'service_status'))
                    BEGIN
	                    CREATE TABLE [dbo].[service_status](
		                    [service_name] [varchar](200) NOT NULL,
		                    [last_status] [datetime] NOT NULL,
		                    [additional_data] [varchar](max) NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[service_status] ADD  CONSTRAINT [DF_service_status_last_status]  DEFAULT (getdate()) FOR [last_status]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (19);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 19.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
