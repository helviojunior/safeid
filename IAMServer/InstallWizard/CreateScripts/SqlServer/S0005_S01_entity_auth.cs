using System;

namespace InstallWizard
{
    public class S0005_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

					IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'entity_auth'))
                    BEGIN

	                    CREATE TABLE [dbo].[entity_auth](
		                    [entity_id] [bigint] NOT NULL,
		                    [auth_key] [varchar](50) NOT NULL,
		                    [start_date] [datetime] NOT NULL,
		                    [end_date] [datetime] NOT NULL,
	                     CONSTRAINT [PK_entity_auth] PRIMARY KEY CLUSTERED 
	                    (
		                    [entity_id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

                        ALTER TABLE [dbo].[entity_auth] ADD  CONSTRAINT [DF_entity_auth_date]  DEFAULT (getdate()) FOR [start_date]
                        ALTER TABLE [dbo].[entity_auth] ADD  CONSTRAINT [DF_entity_auth_end_date]  DEFAULT (dateadd(minute,(5),getdate())) FOR [end_date]

                        ALTER TABLE [dbo].[entity_auth]  WITH CHECK ADD  CONSTRAINT [FK_entity_auth_entity] FOREIGN KEY([entity_id])
                        REFERENCES [dbo].[entity] ([id])
                        ON UPDATE CASCADE
                        ON DELETE CASCADE

                        ALTER TABLE [dbo].[entity_auth] CHECK CONSTRAINT [FK_entity_auth_entity]


                    END

                    INSERT INTO [db_install] ([version]) VALUES (5);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 5.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
