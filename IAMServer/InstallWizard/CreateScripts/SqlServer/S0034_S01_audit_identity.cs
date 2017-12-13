using System;

namespace InstallWizard
{
    public class S0034_S01_audit_identity : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'audit_identity'))
                    BEGIN
	
	                    CREATE TABLE [dbo].[audit_identity](
		                    [resource_plugin_id] [bigint] NOT NULL,
		                    [id] [varchar](100) NOT NULL,
		                    [full_name] [varchar](500) NOT NULL,
		                    [event] [varchar](100) NOT NULL,
		                    [create_date] [datetime] NOT NULL,
		                    [update_date] [datetime] NOT NULL,
		                    [fields] [varchar](max) NOT NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[audit_identity] ADD  CONSTRAINT [DF_audit_identity_create_date]  DEFAULT (getdate()) FOR [create_date]
	
	                    ALTER TABLE [dbo].[audit_identity] ADD  CONSTRAINT [DF_audit_identity_update_date]  DEFAULT (getdate()) FOR [update_date]

	                    ALTER TABLE [dbo].[audit_identity]  WITH CHECK ADD  CONSTRAINT [FK_audit_identity_resource_plugin] FOREIGN KEY([resource_plugin_id])
	                    REFERENCES [dbo].[resource_plugin] ([id])
	                    ON UPDATE CASCADE
	                    ON DELETE CASCADE
	
	                    ALTER TABLE [dbo].[audit_identity] CHECK CONSTRAINT [FK_audit_identity_resource_plugin]

                    END

                    INSERT INTO [db_install] ([version]) VALUES (34);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 34.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
