using System;

namespace InstallWizard
{
    public class S0013_S01_Struct : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'entity_timeline'))
                    BEGIN

	                    CREATE TABLE [dbo].[entity_timeline](
		                    [entity_id] [bigint] NOT NULL,
		                    [identity_id] [bigint] NOT NULL,
		                    [date] [datetime] NOT NULL,
		                    [key] [int] NOT NULL,
		                    [log_id] [varchar](50) NOT NULL,
		                    [title] [varchar](500) NOT NULL,
		                    [text] [varchar](max) NOT NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[entity_timeline] ADD  CONSTRAINT [DF_entity_timeline_identity_id]  DEFAULT ((0)) FOR [identity_id]
	
	                    ALTER TABLE [dbo].[entity_timeline]  WITH CHECK ADD  CONSTRAINT [FK_entity_timeline_entity] FOREIGN KEY([entity_id])
	                    REFERENCES [dbo].[entity] ([id]) ON UPDATE  CASCADE ON DELETE  CASCADE

	                    ALTER TABLE [dbo].[entity_timeline] CHECK CONSTRAINT [FK_entity_timeline_entity]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (13);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 13.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
