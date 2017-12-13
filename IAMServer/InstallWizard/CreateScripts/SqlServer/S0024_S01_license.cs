using System;

namespace InstallWizard
{
    public class S0024_S01_license : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'license'))
                    BEGIN

	                    CREATE TABLE [dbo].[license](
		                    [enterprise_id] [bigint] NOT NULL,
		                    [license_data] [varchar](max) NOT NULL
	                    ) ON [PRIMARY]


	                    ALTER TABLE [dbo].[license] ADD  CONSTRAINT [DF_license_enterprise_id]  DEFAULT ((0)) FOR [enterprise_id]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (24);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 24.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
