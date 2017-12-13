using System;

namespace InstallWizard
{
    public class S0004_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'deploy_now'))
                    BEGIN

	                    CREATE TABLE [dbo].[deploy_now](
		                    [entity_id] [bigint] NOT NULL,
		                    [date] [datetime] NOT NULL
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[deploy_now] ADD  CONSTRAINT [DF_deploy_now_date]  DEFAULT (getdate()) FOR [date]

                        ALTER TABLE [dbo].[deploy_now]  WITH CHECK ADD  CONSTRAINT [FK_deploy_now_entity] FOREIGN KEY([entity_id])
                        REFERENCES [dbo].[entity] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[deploy_now] CHECK CONSTRAINT [FK_deploy_now_entity]


                    END

                    INSERT INTO [db_install] ([version]) VALUES (4);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 4.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
