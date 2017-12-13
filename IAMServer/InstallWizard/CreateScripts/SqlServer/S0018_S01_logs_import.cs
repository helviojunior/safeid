using System;

namespace InstallWizard
{
    public class S0018_S01_logs_import : ICreateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'logs_imports'))
                    BEGIN
			
	                    CREATE TABLE [dbo].[logs_imports](
		                    [date] [datetime] NOT NULL,
		                    [source] [varchar](500) NOT NULL,
		                    [key] [int] NOT NULL,
		                    [enterprise_id] [bigint] NOT NULL,
		                    [proxy_name] [varchar](200) NULL,
		                    [proxy_id] [bigint] NOT NULL,
		                    [plugin_uri] [varchar](500) NOT NULL,
		                    [plugin_id] [bigint] NOT NULL,
		                    [resource_id] [bigint] NOT NULL,
		                    [entity_id] [bigint] NOT NULL,
		                    [identity_id] [bigint] NOT NULL,
		                    [type] [varchar](50) NOT NULL,
		                    [text] [varchar](max) NOT NULL,
		                    [additional_data] [varchar](max) NULL
	                    ) ON [PRIMARY]

	                    ALTER TABLE [dbo].[logs_imports] ADD  CONSTRAINT [DF_logs_imports_key]  DEFAULT ((0)) FOR [key]
	                    ALTER TABLE [dbo].[logs_imports] ADD  CONSTRAINT [DF_logs_imports_enterprise_id]  DEFAULT ((0)) FOR [enterprise_id]
	                    ALTER TABLE [dbo].[logs_imports] ADD  CONSTRAINT [DF_logs_imports_proxy_id]  DEFAULT ((0)) FOR [proxy_id]
	                    ALTER TABLE [dbo].[logs_imports] ADD  CONSTRAINT [DF_logs_imports_plugin_id]  DEFAULT ((0)) FOR [plugin_id]
	                    ALTER TABLE [dbo].[logs_imports] ADD  CONSTRAINT [DF_logs_imports_identity_id]  DEFAULT ((0)) FOR [identity_id]

                    END


                    INSERT INTO [db_install] ([version]) VALUES (18);

                    ";
            }
        }

        public string Precondition
        {
            get { return @"SELECT case when isnull(count([version]),0) < 1 then 1 ELSE 0 END FROM [db_install] where [version] = " + ((Int64)Serial).ToString(); }
        }

        public double Serial { get { return 18.1; } }
        public string Provider { get { return "System.Data.SqlClient"; } }

    }
}
