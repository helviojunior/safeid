using System;

namespace InstallWizard
{
    public class S0027_S01_debug : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'debug'))
                    BEGIN

	                    CREATE TABLE [dbo].[debug](
		                    [date] [datetime] NOT NULL,
		                    [text] [varchar](max) NULL
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[debug] ADD  CONSTRAINT [DF_debug_date]  DEFAULT (getdate()) FOR [date]

                    END

                    INSERT INTO [db_install] ([version]) VALUES (27);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 27.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
