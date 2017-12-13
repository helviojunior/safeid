using System;

namespace InstallWizard
{
    public class S0015_S01_log_seed : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'log_seed'))
                    BEGIN


	                    CREATE TABLE [dbo].[log_seed](
		                    [seed] [int] NOT NULL
	                    ) ON [PRIMARY]

	                    INSERT INTO [log_seed] (seed) VALUES(1);

                    END


                    INSERT INTO [db_install] ([version]) VALUES (15);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 15.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
