using System;

namespace InstallWizard
{
    public class S0032_S01_confirmation_rules : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'confirmation_rules'))
                    BEGIN


	                    CREATE TABLE [dbo].[confirmation_rules](
		                    [id] [bigint] IDENTITY(1,1) NOT NULL,
		                    [field_id] [bigint] NOT NULL,
		                    [is_mail] [bit] NOT NULL,
		                    [is_sms] [bit] NOT NULL,
	                     CONSTRAINT [PK_confirmation_rules] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[confirmation_rules] ADD  CONSTRAINT [DF_confirmation_rules_is_mail]  DEFAULT ((0)) FOR [is_mail]

	                    ALTER TABLE [dbo].[confirmation_rules]  WITH CHECK ADD  CONSTRAINT [FK_confirmation_rules_field] FOREIGN KEY([field_id])
	                    REFERENCES [dbo].[field] ([id])
	
	                    ALTER TABLE [dbo].[confirmation_rules] CHECK CONSTRAINT [FK_confirmation_rules_field]

                    END



                    INSERT INTO [db_install] ([version]) VALUES (32);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 32.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
