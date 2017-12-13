using System;

namespace InstallWizard
{
    public class S0025_S01_import_profiler : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'import_profiler'))
                    BEGIN

		
	                    CREATE TABLE [dbo].[import_profiler](
		                    [date] [datetime] NOT NULL,
		                    [total_sec] [bigint] NOT NULL,
		                    [text_total] [varchar](200) NOT NULL,
		                    [text] [varchar](max) NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[import_profiler] ADD  CONSTRAINT [DF_import_profiler_date]  DEFAULT (getdate()) FOR [date]

                    END



                    INSERT INTO [db_install] ([version]) VALUES (25);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 25.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
